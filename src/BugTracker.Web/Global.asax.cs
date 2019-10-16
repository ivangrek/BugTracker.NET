/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.Caching;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Autofac;
    using Autofac.Integration.Web;
    using Core;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "This is an infrastructure")]
    public class Global : HttpApplication, IContainerProviderAccessor
    {
        // Provider that holds the application container.
        private static IContainerProvider _containerProvider;

        // Instance property that will be used by Autofac HttpModules
        // to resolve and inject dependencies.
        public IContainerProvider ContainerProvider => _containerProvider;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_Start(object sender, EventArgs e)
        {
            var container = IoCConfig.Configure();

            AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            //FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            _containerProvider = new ContainerProvider(container);

            var path = HttpContext.Current.Server.MapPath("~/");

            HttpRuntime.Cache.Add("MapPath", path, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);
            HttpRuntime.Cache.Add("Application", Application, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);

            var dir = path + "\\App_Data";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\logs";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\uploads";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\lucene_index";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_header.html"));

            Application["custom_header"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_footer.html"));
            Application["custom_footer"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_logo.html"));
            Application["custom_logo"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_welcome.html"));
            Application["custom_welcome"] = sr.ReadToEnd();
            sr.Close();

            var applicationSettings = container
                .Resolve<IApplicationSettings>();

            if (applicationSettings.EnableVotes)
            {
                Core.Tags.CountVotes(Application); // in tags file for convenience for me....
            }

            if (applicationSettings.EnableTags)
            {
                Core.Tags.BuildTagIndex(Application);
            }

            if (applicationSettings.EnableLucene)
            {
                MyLucene.BuildLuceneIndex(Application);
            }

            if (applicationSettings.EnablePop3)
            {
                MyPop3.StartPop3(Application);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            var applicationSettings = (IApplicationSettings)DependencyResolver.Current
                .GetService(typeof(IApplicationSettings));

            var aspNetContext = HttpContext.Current;

            if (aspNetContext.Request.Path.Contains("Account"))
            {
                return; // allow
            }

            var request = aspNetContext.Request;
            var response = aspNetContext.Response;
            var cookie = request.Cookies["se_id2"];

            // This logic allows somebody to put a link in an email, like
            // Bugs/Edit.aspx?id=66
            // The user would click on the link, go to the logon page (Account/Login),
            // and then after logging in continue on to Bugs/Edit.aspx?id=66
            var originalUrl = request.ServerVariables["URL"].ToLower();
            var originalQuerystring = request.ServerVariables["QUERY_STRING"].ToLower();

            var target = "~/Account/Login";

            if (originalUrl.EndsWith("MobileEdit.aspx"))
            {
                target = "~/Account/MobileLogin";
            }

            target += "?url=" + originalUrl + "&qs=" + HttpUtility.UrlEncode(originalQuerystring);

            DataRow dr = null;

            if (cookie == null)
            {
                if (!applicationSettings.AllowGuestWithoutLogin)
                {
                    Util.WriteToLog("se_id cookie is null, so redirecting");
                    response.Redirect(target);
                    response.End();
                }
            }
            else
            {
                // guard against "Sql Injection" exploit
                var seId = cookie.Value.Replace("'", "''");

                // not used
                //var userId = 0;
                //var obj = aspNetContext.Session[seId];
                //if (obj != null) userId = Convert.ToInt32(obj);

                // check for existing session for active user
                var sql = @"
                        /* check session */
                        declare @project_admin int
                        select @project_admin = count(1)
                            from sessions
                            inner join project_user_xref on pu_user = se_user
                            and pu_admin = 1
                            where se_id = '$se';

                        select us_id, us_admin,
                        us_username, us_firstname, us_lastname,
                        isnull(us_email,'') us_email,
                        isnull(us_bugs_per_page,10) us_bugs_per_page,
                        isnull(us_forced_project,0) us_forced_project,
                        us_use_fckeditor,
                        us_enable_bug_list_popups,
                        og.*,
                        isnull(us_forced_project, 0 ) us_forced_project,
                        isnull(pu_permission_level, $dpl) pu_permission_level,
                        @project_admin [project_admin]
                        from sessions
                        inner join users on se_user = us_id
                        inner join orgs og on us_org = og_id
                        left outer join project_user_xref
                            on pu_project = us_forced_project
                            and pu_user = us_id
                        where se_id = '$se'
                        and us_active = 1";

                sql = sql.Replace("$se", seId);
                sql = sql.Replace("$dpl", applicationSettings.DefaultPermissionLevel.ToString());
                dr = DbUtil.GetDataRow(sql);
            }

            if (dr == null)
                if (applicationSettings.AllowGuestWithoutLogin)
                {
                    // allow users in, even without logging on.
                    // The user will have the permissions of the "guest" user.
                    var sql = @"
                            /* get guest  */
                            select us_id, us_admin,
                            us_username, us_firstname, us_lastname,
                            isnull(us_email,'') us_email,
                            isnull(us_bugs_per_page,10) us_bugs_per_page,
                            isnull(us_forced_project,0) us_forced_project,
                            us_use_fckeditor,
                            us_enable_bug_list_popups,
                            og.*,
                            isnull(us_forced_project, 0 ) us_forced_project,
                            isnull(pu_permission_level, $dpl) pu_permission_level,
                            0 [project_admin]
                            from users
                            inner join orgs og on us_org = og_id
                            left outer join project_user_xref
                                on pu_project = us_forced_project
                                and pu_user = us_id
                            where us_username = 'guest'
                            and us_active = 1";

                    sql = sql.Replace("$dpl", applicationSettings.DefaultPermissionLevel.ToString());

                    dr = DbUtil.GetDataRow(sql);
                }

            // no previous session, no guest login allowed
            if (dr == null)
            {
                Util.WriteToLog("no previous session, no guest login allowed");
                response.Redirect(target);
                response.End();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_Error(object sender, EventArgs e)
        {
            // Put the server vars into a string
            var serverVarsString = new StringBuilder();

            // Load ServerVariable collection into NameValueCollection object.
            var coll = Request.ServerVariables;

            // Get names of all keys into a string array.
            var arr1 = coll.AllKeys;

            for (var loop1 = 0; loop1 < arr1.Length; loop1++)
            {
                var key = arr1[loop1];

                if (key.StartsWith("AUTH_PASSWORD", StringComparison.InvariantCulture))
                {
                    continue;
                }

                var arr2 = coll.GetValues(key);

                for (var loop2 = 0; loop2 < 1; loop2++)
                {
                    var val = arr2[loop2];

                    if (string.IsNullOrEmpty(val))
                    {
                        break;
                    }

                    serverVarsString.Append("\n");
                    serverVarsString.Append(key);
                    serverVarsString.Append("=");
                    serverVarsString.Append(val);
                }
            }

            var exc = Server.GetLastError()
                .GetBaseException();

            var applicationSettings = ContainerProvider.ApplicationContainer
                .Resolve<IApplicationSettings>();

            var logEnabled = applicationSettings.LogEnabled;

            if (logEnabled)
            {
                var path = Util.GetLogFilePath();

                // open file
                var w = File.AppendText(path);

                w.WriteLine($"\nTIME: {DateTime.Now.ToLongTimeString()}");
                w.WriteLine($"MSG: {exc.Message}");
                w.WriteLine($"URL: {Request.Url}");
                w.WriteLine($"EXCEPTION: {exc}");
                w.WriteLine(serverVarsString.ToString());
                w.Close();
            }

            var errorEmailEnabled = applicationSettings.ErrorEmailEnabled;

            if (errorEmailEnabled)
            {
                if (exc.Message == "Expected integer. Possible SQL injection attempt?")
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else if (exc.Message.Contains("Invalid postback or callback argument"))
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else
                {
                    var to = applicationSettings.ErrorEmailTo;
                    var from = applicationSettings.ErrorEmailFrom;
                    var subject = $"Error: {exc.Message}";
                    var body = new StringBuilder();

                    body.Append("\nTIME: ");
                    body.Append(DateTime.Now.ToLongTimeString());
                    body.Append("\nURL: ");
                    body.Append(Request.Url);
                    body.Append("\nException: ");
                    body.Append(exc);
                    body.Append(serverVarsString);

                    Email.SendEmail(to, from, string.Empty, subject, body.ToString()); // 5 args
                }
            }
        }
    }
}