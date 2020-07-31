//compile like so:
//csc btnet_service.cs POP3Main.cs POP3Client.cs

//then run "installutil.exe"

namespace btnet
{
    using System;
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.Diagnostics;
    using System.IO;
    using System.ServiceProcess;

    ///////////////////////////////////////////////////////////////////////
    public class service : ServiceBase
    {
        protected static POP3Main pop3;

        public service()
        {
            ServiceName = "btnet_service";
            CanStop = true;
            CanPauseAndContinue = true;
            AutoLog = true;
        }

        public static void Main(string[] args)
        {
            Run(new service());
        }

        protected override void OnStart(string[] args)
        {
            var verbose = false;
            // look in this exe's folder for the config, not the c:\ root folder.
            var this_exe = Process.GetCurrentProcess().MainModule.FileName;
            pop3 = new POP3Main(Path.GetDirectoryName(this_exe) + "\\btnet_service.exe.config", verbose);
            OnContinue();
        }

        protected override void OnStop()
        {
            pop3.stop();
        }

        protected override void OnPause()
        {
            pop3.pause();
        }

        protected override void OnContinue()
        {
            pop3.start();
        }
    }

    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private const string SERVICE_NAME = "btnet_service";
        private ServiceController serviceController1;
        private readonly ServiceInstaller serviceInstaller1;

        private readonly ServiceProcessInstaller serviceProcessInstaller1;

        public ProjectInstaller()
        {
            this.serviceProcessInstaller1 = new ServiceProcessInstaller();
            this.serviceProcessInstaller1.Account = ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;

            this.serviceInstaller1 = new ServiceInstaller();
            this.serviceInstaller1.AfterInstall += AfterInstallEventHandler;
            this.serviceInstaller1.StartType = ServiceStartMode.Automatic;
            this.serviceInstaller1.ServiceName = SERVICE_NAME;
            this.serviceInstaller1.ServicesDependedOn = new[] { "Tcpip" };

            Installers.AddRange(
                new Installer[]
                {
                    this.serviceProcessInstaller1,
                    this.serviceInstaller1
                }
            );
        }

        private void AfterInstallEventHandler(object sender, InstallEventArgs e)
        {
            this.serviceController1 = new ServiceController(SERVICE_NAME);
            this.serviceController1.Start();
            this.serviceController1.WaitForStatus(
                ServiceControllerStatus.Running,
                TimeSpan.FromMinutes(1));
            this.serviceController1.Close();
        }
    }
}