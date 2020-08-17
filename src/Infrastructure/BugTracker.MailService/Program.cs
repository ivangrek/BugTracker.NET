namespace BugTracker.MailService
{
    using System;
    using System.ServiceProcess;

    internal static class Program
    {
        public static void Main()
        {
            using (var service = new MailService())
            {
                if (Environment.UserInteractive)
                {
                    service.RunAsConsole();
                }
                else
                {
                    ServiceBase.Run(service);
                }
            }
        }
    }
}
