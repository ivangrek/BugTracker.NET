namespace BugTracker.MailService
{
    using System;
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

        public void RunAsConsole()
        {
            Console.WriteLine("Press any key to exit...");

            var pop3 = new Pop3Main(true);

            pop3.Start();

            Console.ReadLine();

            pop3.Stop();
        }

        protected override void OnStart(string[] args)
        {
            _pop3 = new Pop3Main(false);

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