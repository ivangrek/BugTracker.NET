namespace BugTracker.Web.Core
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    // From Util.WriteToLog
    public interface IApplicationLogger
    {
        void WriteToLog(string value);
    }

    internal sealed class ApplicationLogger : IApplicationLogger
    {
        private static readonly object Dummy = new object();

        private readonly IApplicationSettings applicationSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ApplicationLogger(
            IApplicationSettings applicationSettings,
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment)
        {
            this.applicationSettings = applicationSettings;
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
        }

        public void WriteToLog(string value)
        {
            if (!applicationSettings.LogEnabled)
            {
                return;
            }

            var path = GetLogFilePath();

            lock (Dummy)
            {
                using (var streamWriter = File.AppendText(path))
                {
                    // write to it
                    var url = string.Empty;

                    if (this.httpContextAccessor.HttpContext?.Request != null)
                    {
                        url = this.httpContextAccessor.HttpContext.Request.Path;
                    }

                    streamWriter.WriteLine($"{DateTime.Now:yyy-MM-dd HH:mm:ss} {url} {value}");
                }
            }
        }

        private string GetLogFilePath()
        {
            // determine log file name
            var logFileFolder = applicationSettings.LogFileFolder;
            var now = DateTime.Now;
            var nowString = $"{now.Year}_{now.Month:0#}_{now.Day:0#}";
            var path = Path.Combine(this.webHostEnvironment.ContentRootPath, logFileFolder, $"btnet_log_{nowString}.txt");

            return path;
        }
    }
}
