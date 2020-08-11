namespace BugTracker.MailService
{
    using System;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;

    [RunInstaller(true)]
    public sealed class ProjectInstaller : Installer
    {
        private const string ServiceName = "BugTracker.MailService";

        public ProjectInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem,
                Password = null,
                Username = null
            };

            var serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,
                ServiceName = ServiceName,
                ServicesDependedOn = new[] { "Tcpip" }
            };

            serviceInstaller.AfterInstall += AfterInstallEventHandler;

            Installers.AddRange(new Installer[]
            {
                serviceProcessInstaller,
                serviceInstaller
            });
        }

        private static void AfterInstallEventHandler(object sender, InstallEventArgs e)
        {
            var serviceController = new ServiceController(ServiceName);

            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
            serviceController.Close();
        }
    }
}