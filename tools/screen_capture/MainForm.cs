/*
 *  feedback while sending
 *  "x" should be minimize
 *  capture should work from icon's context menu
 *  
 * 
 * */

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace btnet
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Web;
    using System.Windows.Forms;

    public partial class MainForm : Form
    {
        // The original bitmap
        private Bitmap bitmap;

        // The bitmap after we've drawn on it
        private Bitmap bitmapWithStrokes;
        private Stroke currentStroke;

        // Declare an array to hold the bytes of the bitmap.
        private int numberOfBytes;
        private readonly Pen penRedArrow = new Pen(Color.Red, 3);
        private readonly Pen penRedMarker = new Pen(Color.Red, 3);
        private readonly Pen penYellowHighlighter;

        private bool rbfIsBeingShown;

        private bool reallyClose;

        // For drawing
        private readonly List<Stroke> strokes = new List<Stroke>();
        private readonly byte yellowsRedComponent = 0xFF; // Color.Yellow.R;

        public MainForm()
        {
            //Ash <2010-08-03>
            // Removing warning for Obsolete class (After Framework 2.0)
            // Basically All certificates will be accepted.
            //ServicePointManager.CertificatePolicy = new AcceptAllCertificatePolicy();
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };
            //End Ash <2010-08-03>

            this.penRedArrow.CustomEndCap = new AdjustableArrowCap(6, 6);
            //Color transparentYellowColor = Color(Color.Yellow);
            this.penYellowHighlighter = new Pen(Color.Yellow, 14);

            InitializeComponent();
            this.toolStripComboBoxPenType.SelectedIndex = 0;

            EnableDisable(false);

            Width = Program.main_window_width;
            Height = Program.main_window_height;

            KeyPreview = true; // for capturing CTRL-C

            this.pictureBox1.Cursor = Cursors.Hand; // really, I should have a Sharpie cursor

            // For the notify icon
            Resize += MainForm_Resize;
            this.notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;

            this.saveFileDialog1.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
            this.saveFileDialog1.FilterIndex = 1;
            this.saveFileDialog1.RestoreDirectory = true;

            // Add menu items to context menu.
            var cm = new ContextMenu();
            var notifyIconOpen = cm.MenuItems.Add("Open");
            notifyIconOpen.Click += notifyIcon1_DoubleClick;

            var notifyIconCapture = cm.MenuItems.Add("Capture");
            notifyIconCapture.Click += buttonCapture_Click;

            var notifyIconExit = cm.MenuItems.Add("Exit");
            notifyIconExit.Click += buttonExit_Click;

            this.notifyIcon1.ContextMenu = cm;
        }

        // Native stuff
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        private void EnableDisable(bool EnabledOrDisabled)
        {
            // disable toolbar
            this.toolStripButtonCopy.Enabled = EnabledOrDisabled;
            this.toolStripButtonSaveAs.Enabled = EnabledOrDisabled;
            this.toolStripButtonUndo.Enabled = EnabledOrDisabled;
            this.toolStripComboBoxPenType.Enabled = EnabledOrDisabled;

            // disable send fields
            this.buttonSend.Enabled = EnabledOrDisabled;
            this.labelDescription.Enabled = EnabledOrDisabled;
            this.textBoxShortDescription.Enabled = EnabledOrDisabled;
            this.radioButtonCreateNew.Enabled = EnabledOrDisabled;
            this.radioButtonUpdateExisting.Enabled = EnabledOrDisabled;
            this.labelBugId.Enabled = EnabledOrDisabled;
            this.textBoxBugId.Enabled = EnabledOrDisabled;

            enableDisableDelay();
        }

        private Bitmap getBitmap()
        {
            if (this.bitmapWithStrokes != null)
                return this.bitmapWithStrokes;
            return this.bitmap;
        }

        private void ShowRubberBandForm()
        {
            if (this.rbfIsBeingShown)
                return;

            this.rbfIsBeingShown = true;

            if (this.bitmap != null)
            {
                this.pictureBox1.Image = null;
                this.bitmap.Dispose();
                this.strokes.Clear();
            }

            using (var rbf = new RubberBandForm(this))
            {
                rbf.ShowDialog();

                //Ash <2010-08-03>
                // To remove the "marshal-by-reference" warning we declare the last size as
                // a local variable.
                var sLastSize = rbf.lastSize;

                //if (rbf.lastSize.Width > 0 && rbf.lastSize.Height > 0)
                if (sLastSize.Width > 0 && sLastSize.Height > 0)
                {
                    var r = new Rectangle();
                    r.Location = rbf.lastLoc;
                    //r.Size = rbf.lastSize;
                    r.Size = sLastSize;
                    CaptureBitmap(r);
                }

                //End Ash <2010-08-03>
            }

            Show();
            this.rbfIsBeingShown = false;
        }

        private void CaptureBitmap(Rectangle r)
        {
            this.bitmap = new Bitmap(r.Width, r.Height);

            using (var g = Graphics.FromImage(this.bitmap))
            {
                g.CopyFromScreen(r.Location, new Point(0, 0), r.Size);
            }

            this.pictureBox1.Image = this.bitmap;

            if (this.bitmapWithStrokes != null)
            {
                this.bitmapWithStrokes.Dispose();
                this.bitmapWithStrokes = null;
            }

            var bitmapData = this.bitmap.LockBits(
                new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height),
                ImageLockMode.ReadOnly, this.bitmap.PixelFormat);

            this.numberOfBytes = Math.Abs(bitmapData.Stride) * this.bitmap.Height;

            this.bitmap.UnlockBits(bitmapData);

            EnableDisable(true);
        }

        private void Delay()
        {
            // delay...
            Thread.Sleep(500 + 1000 * (int) this.numericUpDownDelay.Value);
        }

        private void CaptureForeground()
        {
            Delay();

            // Get foreground window rect using native calls
            var hWnd = GetForegroundWindow();
            var rct = new RECT();
            GetWindowRect(hWnd, ref rct);

            var r = new Rectangle();
            r.Location = new Point(rct.Left, rct.Top);
            r.Size = new Size(rct.Right - rct.Left, rct.Bottom - rct.Top);
            CaptureBitmap(r);

            Show();
        }

        private void CaptureFull()
        {
            Delay();

            // Current screen
            var screen = Screen.FromControl(this);
            CaptureBitmap(screen.Bounds);

            Show();
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            if (this.radioButtonArea.Checked)
            {
                Hide();
                ShowRubberBandForm();
            }
            else if (this.radioButtonForeground.Checked)
            {
                Hide();
                BeginInvoke(new SimpleDelegeate(CaptureForeground));
            }
            else
            {
                Hide();
                BeginInvoke(new SimpleDelegeate(CaptureFull));
            }
        }

        private void notifyIcon1_DoubleClick(object sender,
            EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState) Hide();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.bitmap == null)
                return;

            this.currentStroke = new Stroke();
            this.strokes.Add(this.currentStroke);
            this.currentStroke.points.Add(e.Location);

            if (e.Button == MouseButtons.Right)
            {
                // arrow
                this.currentStroke.drawingMode = Stroke.DrawingMode.RedArrow;
                this.currentStroke.points.Add(e.Location); // and the line's endpoint
            }
            else
            {
                if (this.toolStripComboBoxPenType.Text == "red arrow")
                {
                    // arrow
                    this.currentStroke.drawingMode = Stroke.DrawingMode.RedArrow;
                    this.currentStroke.points.Add(e.Location); // and the line's endpoint
                }
                else if (this.toolStripComboBoxPenType.Text == "red marker")
                {
                    this.currentStroke.drawingMode = Stroke.DrawingMode.RedMarker;
                }
                else
                {
                    this.currentStroke.drawingMode = Stroke.DrawingMode.YellowHighlighter;
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.bitmap == null)
                return;

            if (this.currentStroke != null)
            {
                if (this.currentStroke.drawingMode == Stroke.DrawingMode.RedArrow)
                    this.currentStroke.points[this.currentStroke.points.Count - 1] = e.Location; // replace endpoint
                else
                    this.currentStroke.points.Add(e.Location);
            }

            drawStrokes();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.bitmap == null)
                return;

            if (this.currentStroke != null)
            {
                if (this.currentStroke.drawingMode == Stroke.DrawingMode.RedArrow)
                    this.currentStroke.points[this.currentStroke.points.Count - 1] = e.Location; // replace endpoint
                else
                    this.currentStroke.points.Add(e.Location);
            }

            this.currentStroke = null;
            drawStrokes();
        }

        private void drawStrokes()
        {
            if (this.bitmapWithStrokes != null) this.bitmapWithStrokes.Dispose();

            this.bitmapWithStrokes = new Bitmap(this.bitmap);

            using (var g = Graphics.FromImage(this.bitmapWithStrokes))
            {
                for (var i = 0; i < this.strokes.Count; i++)
                {
                    var stroke = this.strokes[i];
                    if (stroke.drawingMode == Stroke.DrawingMode.RedArrow)
                        g.DrawLine(this.penRedArrow, stroke.points[0], stroke.points[1]);
                    else if (stroke.drawingMode == Stroke.DrawingMode.RedMarker)
                        g.DrawLines(this.penRedMarker, stroke.points.ToArray());
                    else if (stroke.drawingMode == Stroke.DrawingMode.YellowHighlighter)
                        using (var tempBitmap = new Bitmap(this.bitmap.Width, this.bitmap.Height))
                        {
                            using (var tempG = Graphics.FromImage(tempBitmap))
                            {
                                tempG.DrawLines(this.penYellowHighlighter, stroke.points.ToArray());

                                // get the raw bits of the source and target and remove the blue from every
                                // bit of the target where there is a yellow bit of the source
                                var rect = new Rectangle(0, 0, this.bitmapWithStrokes.Width,
                                    this.bitmapWithStrokes.Height);

                                // lock
                                var sourceData =
                                    tempBitmap.LockBits(
                                        rect,
                                        ImageLockMode.ReadOnly,
                                        tempBitmap.PixelFormat);

                                var targetData = this.bitmapWithStrokes.LockBits(
                                    rect,
                                    ImageLockMode.ReadWrite, this.bitmapWithStrokes.PixelFormat);

                                // Get the address of the first line.
                                var sourcePtr = sourceData.Scan0;
                                var targetPtr = targetData.Scan0;

                                // loop thru the source bytes
                                unsafe
                                {
                                    var s = (byte*) sourcePtr.ToPointer();
                                    var t = (byte*) targetPtr.ToPointer();

                                    for (var p = 2; p < this.numberOfBytes; p += 4)
                                        // if the source's red is yellows's red
                                        if (s[p] == this.yellowsRedComponent)
                                            // wipe out the target's blue
                                            t[p - 2] = 0;
                                }

                                // Unlock the bits.
                                tempBitmap.UnlockBits(sourceData);
                                this.bitmapWithStrokes.UnlockBits(targetData);
                            }
                        }
                }
            }

            this.pictureBox1.Image = this.bitmapWithStrokes;
            //pictureBox1.Invalidate();

            this.toolStripButtonUndo.Enabled = this.strokes.Count > 0;
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (this.radioButtonUpdateExisting.Checked)
                if (this.textBoxBugId.Text == "")
                {
                    MessageBox.Show("Please enter a Bug ID#");
                    return;
                }

            if (string.IsNullOrEmpty(Program.url)
                || string.IsNullOrEmpty(Program.username)
                || string.IsNullOrEmpty(Program.password))
                buttonConfigure_Click(null, null);

            if (string.IsNullOrEmpty(Program.url)
                || string.IsNullOrEmpty(Program.username)
                || string.IsNullOrEmpty(Program.password))
            {
                // skip send button
            }
            else
            {
                // labelWaiting.Text = "Waiting for response...";
                Cursor = Cursors.WaitCursor;

                // The domain, windows authentication stuff here is from Lars Wuckel.  It's pretty
                // difficult code for me, so I only want to go down that logical path if I have to.
                if (Program.domain != "")
                {
                    var thread = new Thread(threadproc_with_domain);
                    thread.Start(this);
                }
                else
                {
                    var thread = new Thread(threadproc);
                    thread.Start(this);
                }
            }
        }

        private void buttonConfigure_Click(object sender, EventArgs e)
        {
            using (var dlg = new ConfigForm())
            {
                dlg.ShowDialog();
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.reallyClose = true;
            Close();
        }

        private void radioButtonArea_CheckedChanged(object sender, EventArgs e)
        {
            enableDisableDelay();
        }

        private void radioButtonForeground_CheckedChanged(object sender, EventArgs e)
        {
            enableDisableDelay();
        }

        private void radioButtonDesktop_CheckedChanged(object sender, EventArgs e)
        {
            enableDisableDelay();
        }

        private void enableDisableDelay()
        {
            if (this.radioButtonArea.Checked)
            {
                this.labelDelay.Enabled = false;
                this.numericUpDownDelay.Enabled = false;
            }
            else
            {
                this.labelDelay.Enabled = true;
                this.numericUpDownDelay.Enabled = true;
            }
        }

        private void radioButtonCreateNew_CheckedChanged(object sender, EventArgs e)
        {
            enableDisableBugId();
        }

        private void radioButtonUpdateExisting_CheckedChanged(object sender, EventArgs e)
        {
            enableDisableBugId();
        }

        private void enableDisableBugId()
        {
            if (this.radioButtonCreateNew.Checked)
            {
                this.labelBugId.Enabled = false;
                this.textBoxBugId.Enabled = false;
            }
            else if (this.radioButtonUpdateExisting.Checked)
            {
                this.labelBugId.Enabled = true;
                this.textBoxBugId.Enabled = true;
            }
        }

        private void handleResponse(object obj)
        {
            HttpWebResponse res = null;
            Exception e = null;

            if (obj is Exception)
                e = (Exception) obj;
            else
                res = (HttpWebResponse) obj;

            if (e != null)
            {
                MessageBox.Show("Sending of screenshot failed.\n\n" + e.Message);
            }
            else if (res != null)
            {
                var http_status = (int) res.StatusCode;

                var http_response_header = res.Headers["BTNET"];
                res.Close();

                if (http_response_header != null)
                {
                    if (http_response_header.IndexOf("OK") == 0)
                    {
                        var bugid = http_response_header.Substring(3);
                        var result = MessageBox.Show("Posted screenshot to Bug ID# "
                                                     + bugid
                                                     + Environment.NewLine
                                                     + Environment.NewLine
                                                     + "Go to the BugTracker.NET website?", "BugTracker.NET",
                            MessageBoxButtons.YesNo);

                        if (result == DialogResult.Yes) Process.Start(Program.url + "/edit_bug.aspx?id=" + bugid);
                    }
                    else
                    {
                        MessageBox.Show("Sending of screenshot failed.\n\n" + http_response_header);
                    }
                }
            }

            //labelWaiting.Text = "";
            Cursor = Cursors.Default;
        }

        public static void threadproc(object obj)
        {
            var frm = (MainForm) obj;
            var sb = new StringBuilder();

            sb.Append("username=" + HttpUtility.UrlEncode(Program.username));
            sb.Append("&password=" + HttpUtility.UrlEncode(Program.password));
            sb.Append("&short_desc=" + HttpUtility.UrlEncode(frm.textBoxShortDescription.Text));
            sb.Append("&projectid=" + Convert.ToString(Program.project_id));
            if (frm.radioButtonUpdateExisting.Checked) sb.Append("&bugid=" + frm.textBoxBugId.Text);
            sb.Append("&attachment_content_type=image/jpg");
            sb.AppendFormat("&attachment_filename=screenshot_{0}.jpg", DateTime.Now.ToString("yyyyMMdd'_'HHmmss"));

            //Ash <2010-08-03>
            //sb.Append("&attachment_desc=screenshot");
            sb.Append("&attachment_desc=" + HttpUtility.UrlEncode(frm.textBoxShortDescription.Text));
            //End Ash <2010-08-03>
            sb.Append("&attachment=");

            var ms = new MemoryStream();
            frm.getBitmap().Save(ms, ImageFormat.Jpeg);
            var base64 = Convert.ToBase64String(ms.ToArray());
            ms.Close();
            ms.Dispose();
            sb.Append(HttpUtility.UrlEncode(base64));

            //  System.Byte[] byte_array2 = System.Convert.FromBase64String(base64);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            // send request to web server
            HttpWebResponse res = null;
            try
            {
                var req = (HttpWebRequest) WebRequest.Create(Program.url + "/insert_bug.aspx");

                req.Credentials = CredentialCache.DefaultCredentials;
                req.PreAuthenticate = true;

                //req.Timeout = 200; // maybe?
                //req.KeepAlive = false; // maybe?

                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = bytes.Length;
                var request_stream = req.GetRequestStream();
                request_stream.Write(bytes, 0, bytes.Length);
                request_stream.Close();

                res = (HttpWebResponse) req.GetResponse();
                frm.BeginInvoke(new ResponseDelegate(frm.handleResponse), res);
            }
            catch (Exception e2)
            {
                frm.BeginInvoke(new ResponseDelegate(frm.handleResponse), e2);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.reallyClose) return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (FormWindowState.Normal == WindowState)
                {
                    Program.main_window_width = Size.Width;
                    Program.main_window_height = Size.Height;
                    ConfigForm.WriteConfig();
                }

                Hide();
                e.Cancel = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.C)
                {
                    if (this.bitmap != null) Clipboard.SetImage(getBitmap());
                }
                else if (e.KeyCode == Keys.Z)
                {
                    toolStripButtonUndo_Click(null, null);
                }
            }
        }

        private void toolStripButtonSaveAs_Click(object sender, EventArgs e)
        {
            this.saveFileDialog1.FileName =
                string.Format("btnet_screenshot_{0}.jpg", DateTime.Now.ToString("yyyyMMdd'_'HHmmss"));

            if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Stream myStream;
                if ((myStream = this.saveFileDialog1.OpenFile()) != null)
                {
                    var b = getBitmap();
                    b.Save(myStream, ImageFormat.Jpeg);
                    myStream.Close();
                }
            }
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(getBitmap());
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            if (this.strokes.Count == 0)
                return;

            this.strokes.RemoveAt(this.strokes.Count - 1);
            drawStrokes();
        }

        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutForm())
            {
                dlg.ShowDialog();
            }
        }

        private static void send(MainForm frm)
        {
            var URL = new Uri(Program.url + "/insert_bug.aspx");

            var extendedWebClient = new ExtendedWebClient();
            var myCredCache = new CredentialCache();
            myCredCache.Add(URL, "Basic", new NetworkCredential(Program.username, Program.password));
            myCredCache.Add(URL, "NTLM", new NetworkCredential(Program.username, Program.password, Program.domain));
            extendedWebClient.Credentials = myCredCache;
            var ms = new MemoryStream();
            frm.getBitmap().Save(ms, ImageFormat.Jpeg);
            var base64 = Convert.ToBase64String(ms.ToArray());
            ms.Close();
            ms.Dispose();

            try
            {
                // Anmelden per POST; anonymer Typ als Parameterobjekt
                var req = extendedWebClient.Post(URL, new
                {
                    Program.username,
                    Program.password,
                    bugid = frm.textBoxBugId.Text,
                    short_desc = frm.textBoxShortDescription.Text,
                    projectid = Convert.ToString(Program.project_id),
                    attachment_content_type = "image/jpg",
                    attachment_filename =
                        string.Format("screenshot_{0}.jpg", DateTime.Now.ToString("yyyyMMdd'_'HHmmss")),
                    attachment = base64
                });

                var res = req.GetResponse();
                frm.BeginInvoke(new ResponseDelegate(frm.handleResponse), res);
            }
            catch (Exception e2)
            {
                frm.BeginInvoke(new ResponseDelegate(frm.handleResponse), e2);
            }
        }

        public static void threadproc_with_domain(object obj)
        {
            var frm = (MainForm) obj;
            send(frm);
        }

        private static bool EnablePreAuthentication(Uri uri, string authenticationType)
        {
            var e = AuthenticationManager.RegisteredModules;

            while (e.MoveNext())
            {
                var module = e.Current as IAuthenticationModule;

                if (string.Compare(module.AuthenticationType, authenticationType, true) == 0)
                {
                    var mi = typeof(AuthenticationManager).GetMethod("BindModule",
                        BindingFlags.NonPublic | BindingFlags.Static);

                    mi.Invoke(null, new object[] {uri, new Authorization(null), module});

                    return true;
                }
            }

            return false;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://ifdefined.com/Donate_to_BugTracker.NET.html");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;
        }

        private delegate void SimpleDelegeate();

        private delegate void ResponseDelegate(object obj);
    }

    internal class Stroke
    {
        public enum DrawingMode
        {
            RedArrow,
            RedMarker,
            YellowHighlighter
        }

        public DrawingMode drawingMode;
        public List<Point> points = new List<Point>();
    }
}

// For siliently accepting suspicious SSL certificates
internal class AcceptAllCertificatePolicy : ICertificatePolicy
{
    public bool CheckValidationResult(
        ServicePoint service_point,
        X509Certificate cert,
        WebRequest web_request,
        int certificate_problem)
    {
        // Always accept
        return true;
    }
}