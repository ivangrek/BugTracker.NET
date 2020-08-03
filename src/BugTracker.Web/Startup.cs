[assembly: Microsoft.Owin.OwinStartup(typeof(BugTracker.Web.Startup))]

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Net.Mime;
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Microsoft.Owin.StaticFiles.ContentTypes;
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureStaticFiles(app);
        }

        public void ConfigureStaticFiles(IAppBuilder app)
        {
            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = PathString.Empty,
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Content"),
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Content"),
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content")),
                ContentTypeProvider = new FileExtensionContentTypeProvider
                {
                    Mappings =
                    {
                        [".exe"] = MediaTypeNames.Application.Octet
                    }
                }
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Content"),
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "html"))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Content"),
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "css"))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                RequestPath = new PathString("/Scripts"),
                FileSystem = new PhysicalFileSystem(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "js"))
            });
        }
    }
}
