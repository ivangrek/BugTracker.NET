namespace btnet
{
    using System;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Windows.Forms;

    public partial class RubberBandForm : Form
    {
        private readonly Rectangle bounds;
        public Point lastLoc;
        public Size lastSize;
        private MainForm mainform;

        private bool mouseDown;
        private Point mouseDownPoint = Point.Empty;
        private Point mousePoint = Point.Empty;
        private readonly Pen pen;

        public RubberBandForm(MainForm mainform)
        {
            this.mainform = mainform;
            InitializeComponent();
            TopMost = true;
            Opacity = .30;
            TransparencyKey = Color.White;
            Location = new Point(0, 0);
            DoubleBuffered = true;
            this.pen = new Pen(Color.DarkRed, 3);
            this.pen.DashStyle = DashStyle.Dot;

            var maxX = 0;
            var maxY = 0;

            foreach (var screen in Screen.AllScreens)
            {
                var x = screen.Bounds.X + screen.Bounds.Width;
                if (x > maxX)
                    maxX = x;
                var y = screen.Bounds.Y + screen.Bounds.Height;
                if (y > maxY)
                    maxY = y;
            }

            this.bounds.X = 0;
            this.bounds.Y = 0;
            this.bounds.Width = maxX;
            this.bounds.Height = maxY;

            Size = new Size(this.bounds.Width, this.bounds.Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.mouseDown = true;
            this.mousePoint = this.mouseDownPoint = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            this.mouseDown = false;

            // corey
            this.lastLoc = new Point(Math.Min(this.mouseDownPoint.X, this.mousePoint.X),
                Math.Min(this.mouseDownPoint.Y, this.mousePoint.Y));
            this.lastSize = new Size(Math.Abs(this.mouseDownPoint.X - this.mousePoint.X),
                Math.Abs(this.mouseDownPoint.Y - this.mousePoint.Y));
            Close();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.mousePoint = e.Location;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);

            var region = new Region(this.bounds);

            if (this.mouseDown)
            {
                var selectionWindow = new Rectangle(
                    Math.Min(this.mouseDownPoint.X, this.mousePoint.X),
                    Math.Min(this.mouseDownPoint.Y, this.mousePoint.Y),
                    Math.Abs(this.mouseDownPoint.X - this.mousePoint.X),
                    Math.Abs(this.mouseDownPoint.Y - this.mousePoint.Y));

                // make a hole, where we can see thru this form
                region.Xor(selectionWindow);

                e.Graphics.FillRegion(Brushes.Black, region);
            }
            else
            {
                e.Graphics.FillRegion(Brushes.LightGray, region);
                e.Graphics.DrawLine(this.pen, this.mousePoint.X, 0, this.mousePoint.X, Size.Height);
                e.Graphics.DrawLine(this.pen,
                    0, this.mousePoint.Y,
                    Size.Width, this.mousePoint.Y);
            }
        }
    }
}