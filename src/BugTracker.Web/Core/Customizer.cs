namespace BugTracker.Web.Core
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;

    public interface ICustomizer
    {
        string LoginLogoHtml { get; }

        string HeaderHtml { get; }

        string FooterHtml { get; }
    }

    internal sealed class Customizer : ICustomizer
    {
        private const string CustomFolder = "custom";
        private const string CustomLoginLogoFile = "login_logo.html";
        private const string CustomHeaderFile = "header.html";
        private const string CustomFooterFile = "footer.html";

        private readonly IWebHostEnvironment environment;

        public Customizer(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public string LoginLogoHtml => File.ReadAllText(Path.Combine(this.environment.WebRootPath, CustomFolder, CustomLoginLogoFile));

        public string HeaderHtml => File.ReadAllText(Path.Combine(this.environment.WebRootPath, CustomFolder, CustomHeaderFile));

        public string FooterHtml => File.ReadAllText(Path.Combine(this.environment.WebRootPath, CustomFolder, CustomFooterFile));
    }
}
