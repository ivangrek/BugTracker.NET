﻿namespace BugTracker.MailService
{
    using System;
    using System.ServiceProcess;

    internal static class Program
    {
        public static void Main(string[] args)
        {
            using (var service = new MailService())
            {
                if (Environment.UserInteractive)
                {
                    service.RunAsConsole(args);
                }
                else
                {
                    ServiceBase.Run(service);
                }
            }
        }
    }
}
