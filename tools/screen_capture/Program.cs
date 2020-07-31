namespace btnet
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    internal static class Program
    {
        public static string url;
        public static string username;
        public static string password;
        public static string domain;
        public static string save_password;
        public static int main_window_width;
        public static int main_window_height;
        public static int project_id;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        private static void Main()
        {
            var createdNew = true;
            using (var mutex = new Mutex(true, "MyApplicationName", out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // fetch settings
                    url = ConfigurationManager.AppSettings["url"];
                    username = ConfigurationManager.AppSettings["username"];
                    password = ConfigurationManager.AppSettings["password"];
                    domain = ConfigurationManager.AppSettings["domain"];
                    save_password = ConfigurationManager.AppSettings["save_password"];
                    var tmp = ConfigurationManager.AppSettings["main_window_width"];
                    if (!string.IsNullOrEmpty(tmp))
                        main_window_width = Convert.ToInt32(tmp);
                    tmp = ConfigurationManager.AppSettings["main_window_height"];
                    if (!string.IsNullOrEmpty(tmp))
                        main_window_height = Convert.ToInt32(tmp);
                    tmp = ConfigurationManager.AppSettings["project_id"];
                    if (!string.IsNullOrEmpty(tmp))
                        project_id = Convert.ToInt32(tmp);
                    else
                        project_id = 0;

                    Application.Run(new MainForm());
                }
                else
                {
                    var current = Process.GetCurrentProcess();
                    foreach (var process in Process.GetProcessesByName(current.ProcessName))
                        if (process.Id != current.Id)
                        {
                            SetForegroundWindow(process.MainWindowHandle);
                            break;
                        }
                }
            }
        }
    }
}