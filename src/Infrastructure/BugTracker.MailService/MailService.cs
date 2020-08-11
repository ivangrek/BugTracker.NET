namespace BugTracker.MailService
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.ServiceProcess;

    internal sealed class MailService : ServiceBase
    {
        private static Pop3Main _pop3;

        public MailService()
        {
            ServiceName = "BugTracker.MailService";
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }

        public void RunAsConsole(string[] args)
        {
            // check the command line
            if (args.Length != 1)
            {
                Console.WriteLine("usage BugTracker.MailService.exe [path to MailService.config file]");
                Console.WriteLine("example BugTracker.MailService.exe MailService.config");

                return;
            }

            Console.WriteLine("Press any key to exit...");

            var configFile = args[0];
            var pop3 = new Pop3Main(configFile, true);

            pop3.Start();

            Console.ReadLine();

            pop3.Stop();
        }

        protected override void OnStart(string[] args)
        {
            // look in this exe's folder for the config, not the c:\ root folder.
            var thisExe = Assembly.GetExecutingAssembly().Location;
            var configFile = Path.Combine(Path.GetDirectoryName(thisExe), "MailService.config");

            _pop3 = new Pop3Main(configFile, false);

            OnContinue();
        }

        protected override void OnStop()
        {
            _pop3.Stop();
        }

        protected override void OnPause()
        {
            _pop3.Pause();
        }

        protected override void OnContinue()
        {
            _pop3.Start();
        }
    }
}