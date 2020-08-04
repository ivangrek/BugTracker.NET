[assembly: Microsoft.Owin.OwinStartup(typeof(BugTracker.Web.Startup))]

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Net.Mime;
    using System.Security.Claims;
    using System.Web.Helpers;
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Owin.StaticFiles;
    using Microsoft.Owin.StaticFiles.ContentTypes;
    using Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuthentication(app);
            ConfigureStaticFiles(app);
        }

        public void ConfigureAuthentication(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Account/Login")
            });

            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
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
