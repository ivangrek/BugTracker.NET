namespace BugTracker.Web
{
    using System.IO;
    using System.Web.Mvc;
    using Core;
    using NLog;
    using NLog.Config;
    using NLog.Targets;

    internal static class LoggingConfig
    {
        public static void Configure()
        {
            var applicationSettings = DependencyResolver.Current
                .GetService<IApplicationSettings>();

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();

            fileTarget.FileName = Path.Combine(Util.GetLogFolder(), "btnet_log.txt");
            fileTarget.ArchiveNumbering = ArchiveNumberingMode.Date;
            fileTarget.ArchiveEvery = FileArchivePeriod.Day;
            config.AddTarget("File", fileTarget);

            var mailTarget = new MailTarget
            {
                UseSystemNetMailSettings = true,
                To = applicationSettings.ErrorEmailTo,
                From = applicationSettings.ErrorEmailFrom,
                Subject = "BTNET Error Notification",
                Layout = "${machinename}${newline} ${date} ${newline} ${newline} ${message} ${newline}  ${exception} ${newline}"
            };

            config.AddTarget("Mail", mailTarget);

            //Turn logging on/off based on the LogEnabled setting
            var logLevel = applicationSettings.LogEnabled ? LogLevel.Trace : LogLevel.Off;
            config.LoggingRules.Add(new LoggingRule("*", logLevel, fileTarget));

            var emailLogLevel = applicationSettings.ErrorEmailEnabled ? LogLevel.Fatal : LogLevel.Off;
            config.LoggingRules.Add(new LoggingRule("*", emailLogLevel, mailTarget));

            LogManager.Configuration = config;
        }
    }
}