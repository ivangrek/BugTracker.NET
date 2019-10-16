/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Net.Mime;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;

    public static class Util
    {
        private static IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        public static HttpContext Context => HttpContext.Current;
        private static HttpRequest _request => HttpContext.Current.Request;

        private static readonly object Dummy = new object();

        public static Regex ReCommas = new Regex(",");
        public static Regex RePipes = new Regex("\\|");

        private static readonly Regex ReEmail =
            new Regex("^([a-zA-Z0-9_\\-\\'\\.]+)@([a-zA-Z0-9_\\-\\.]+)\\.([a-zA-Z]{2,5})$");

        public static bool ValidateEmail(string s)
        {
            return ReEmail.IsMatch(s);
        }

        public static string GetLogFilePath()
        {
            // determine log file name
            var logFileFolder = GetLogFolder();

            var now = DateTime.Now;
            var nowString =
                now.Year
                + "_" +
                now.Month.ToString("0#")
                + "_" +
                now.Day.ToString("0#");

            var path = logFileFolder
                       + "\\"
                       + "btnet_log_"
                       + nowString
                       + ".txt";

            return path;
        }

        public static void WriteToLog(string s)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (!applicationSettings.LogEnabled)
            {
                return;
            }

            var path = GetLogFilePath();

            lock (Dummy)
            {
                var w = File.AppendText(path);

                // write to it

                var url = "";

                try // To workaround problem with IIS integrated mode
                {
                    if (HttpContext.Current != null)
                        if (HttpContext.Current.Request != null)
                            url = HttpContext.Current.Request.Url.ToString();
                }
                catch
                {
                    // do nothing
                }

                w.WriteLine(DateTime.Now.ToString("yyy-MM-dd HH:mm:ss")
                            + " "
                            + url
                            + " "
                            + s);

                w.Close();
            }
        }

        // TODO research
        //public static void WriteToMemoryLog(string s)
        //{
        //    if (HttpContext.Current == null) return;

        //    if (GetSetting("MemoryLogEnabled", "0") == "0") return;

        //    var url = "";
        //    if (HttpContext.Current.Request != null) url = HttpContext.Current.Request.Url.ToString();

        //    var line = DateTime.Now.ToString("yyy-MM-dd HH:mm:ss:fff")
        //               + " "
        //               + url
        //               + " "
        //               + s;

        //    var list = (List<string>)HttpContext.Current.Application["log"];

        //    if (list == null)
        //    {
        //        list = new List<string>();
        //        HttpContext.Current.Application["log"] = list;
        //    }

        //    list.Add(line);
        //}

        [Obsolete("Use [OutputCache(Location = OutputCacheLocation.None)]")]
        public static void DoNotCache(HttpResponse response)
        {
            response.CacheControl = "no-cache";
            response.AddHeader("Pragma", "no-cache");
            response.Expires = -1;
        }

        public static bool IsInt(string maybeInt)
        {
            try
            {
                var i = int.Parse(maybeInt);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string IsValidDecimal(string name, string val, int leftOfDecimal, int rightOfDecimal)
        {
            var ci = GetCultureInfo();
            if (!decimal.TryParse(val, NumberStyles.Float, ci, out _))
                return name + " is not in a valid decimal format";

            var vals = val.Split(new[] { ci.NumberFormat.NumberDecimalSeparator }, StringSplitOptions.None);

            if (vals.Length > 0)
            {
                if (vals[0].Length > leftOfDecimal)
                    return name + " has too many digits to the left of the decimal point";
            }
            else if (vals.Length > 1)
            {
                if (vals[1].Length > rightOfDecimal)
                    return name + " has too many digits to the right of the decimal point";
            }

            return "";
        }

        public static bool IsDateTime(string maybeDatetime)
        {
            DateTime d;

            try
            {
                d = DateTime.Parse(maybeDatetime, GetCultureInfo());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string BoolToString(bool b)
        {
            return b ? "1" : "0";
        }

        public static string StripHtml(string textWithTags)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (applicationSettings.StripHtmlTagsFromSearchableText)
                return HttpUtility.HtmlDecode(Regex.Replace(textWithTags, @"<(.|\n)*?>", string.Empty));
            return textWithTags;
        }

        public static string StripDangerousTags(string textWithTags)
        {
            var s = Regex.Replace(textWithTags,
                @"<script", "<scrSAFEipt", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</script", "</scrSAFEipt", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<object", "<obSAFEject", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</object", "</obSAFEject", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<embed", "<emSAFEbed", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</embed", "</emSAFEbed", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onabort", "onSAFEabort", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onblur", "onSAFEblur", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onchange", "onSAFEchange", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onclick", "onSAFEclick", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"ondblclick", "onSAFEdblclick", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onerror", "onSAFEerror", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onfocus", "onSAFEfocus", RegexOptions.IgnoreCase);

            s = Regex.Replace(s, @"onkeydown", "onSAFEkeydown", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onkeypress", "onSAFEkeypress", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onkeyup", "onSAFEkeyup", RegexOptions.IgnoreCase);

            s = Regex.Replace(s, @"onload", "onSAFEload", RegexOptions.IgnoreCase);

            s = Regex.Replace(s, @"onmousedown", "onSAFEmousedown", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onmousemove", "onSAFEmousemove", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onmouseout", "onSAFEmouseout", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onmouseup", "onSAFEmouseup", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onmouseup", "onSAFEmouseup", RegexOptions.IgnoreCase);

            s = Regex.Replace(s, @"onreset", "onSAFEresetK", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onresize", "onSAFEresize", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onselect", "onSAFEselect", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onsubmit", "onSAFEsubmit", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"onunload", "onSAFEunload", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<body>", "<span>", RegexOptions.IgnoreCase);

            return s;
        }

        public static CultureInfo GetCultureInfo()
        {
            // Create a basic culture object to provide also all input parsing
            return new CultureInfo(Thread.CurrentThread.CurrentCulture.Name);
        }

        public static string FormatDbDateTime(object date)
        {
            if (date is DBNull) return "";

            if (date is string s)
            {
                if (s == "") return "";
                date = Convert.ToDateTime(date);
            }

            // We don't know whether time is significant or not,
            // but we can guess.  Probably, not for sure, but probably
            // if the time is 12:00 AM, the time is just debris.

            var dt = (DateTime)date;

            if (dt.Year == 1900) return "";

            IApplicationSettings applicationSettings = new ApplicationSettings();

            var dateTimeFormat = "";
            if ((dt.Hour == 0 || dt.Hour == 12) && dt.Minute == 0 && dt.Second == 0)
                dateTimeFormat = applicationSettings.JustDateFormat;
            else
                dateTimeFormat = applicationSettings.DateTimeFormat;

            var hoursOffset = applicationSettings.DisplayTimeOffsetInHours;

            if (hoursOffset != 0) dt = dt.AddHours(hoursOffset);
            return dt.ToString(dateTimeFormat, GetCultureInfo());
        }

        //modified by CJU on jan 9 2008

        public static string FormatDbValue(decimal val)
        {
            return val.ToString(GetCultureInfo());
        }

        public static string FormatDbValue(DateTime val)
        {
            return FormatDbDateTime(val);
        }

        public static string FormatDbValue(object val)
        {
            if (val is decimal)
                return FormatDbValue((decimal)val);
            if (val is DateTime)
                return FormatDbValue((DateTime)val);
            return Convert.ToString(val);
        }
        //end modified by CJU on jan 9 2008

        public static string FormatLocalDateIntoDbFormat(string date)
        {
            // seems to already be in the right format
            DateTime d;
            try
            {
                d = DateTime.Parse(date, GetCultureInfo());
            }
            catch (FormatException)
            {
                // Can not translate this
                return "";
            }

            IApplicationSettings applicationSettings = new ApplicationSettings();

            // Note that yyyyMMdd HH:mm:ss is a universal SQL dateformat for strings.
            return d.ToString(applicationSettings.SQLServerDateFormat);
        }

        public static string FormatLocalDecimalIntoDbFormat(string val)
        {
            var x = decimal.Parse(val, GetCultureInfo());

            return x.ToString(CultureInfo.InvariantCulture);
        }

        public static string AlterSqlPerProjectPermissions(string sql, ISecurity security)
        {
            string projectPermissionsSql;

            IApplicationSettings applicationSettings = new ApplicationSettings();
            var dpl = applicationSettings.DefaultPermissionLevel;

            if (dpl == 0)
                projectPermissionsSql = @" (bugs.bg_project in (
                    select pu_project
                    from project_user_xref
                    where pu_user = $user
                    and pu_permission_level > 0)) ";
            else
                projectPermissionsSql = @" (bugs.bg_project not in (
                    select pu_project
                    from project_user_xref
                    where pu_user = $user
                    and pu_permission_level = 0)) ";

            if (security.User.CanOnlySeeOwnReported)
            {
                projectPermissionsSql += @"
                        and bugs.bg_reported_user = $user ";
            }
            else
            {
                if (security.User.OtherOrgsPermissionLevel == 0)
                    projectPermissionsSql += @"
                        and bugs.bg_org = $user.org ";
            }

            projectPermissionsSql
                = projectPermissionsSql.Replace("$user.org", Convert.ToString(security.User.Org));

            projectPermissionsSql
                = projectPermissionsSql.Replace("$user", Convert.ToString(security.User.Usid));

            // Figure out where to alter sql for project permissions
            // I've tried lots of different schemes over the years....

            var alterHerePos = sql.IndexOf("$ALTER_HERE"); // places - can be multiple - are explicitly marked
            if (alterHerePos != -1) return sql.Replace("$ALTER_HERE", "/* ALTER_HERE */ " + projectPermissionsSql);

            string bugSql;

            var wherePos =
                sql.IndexOf(
                    "WhErE"); // first look for a "special" where, case sensitive, in case there are multiple where's to choose from
            if (wherePos == -1)
                wherePos = sql.ToUpper().IndexOf("WHERE");

            var orderPos = sql.IndexOf("/*ENDWHR*/"); // marker for end of the where statement

            if (orderPos == -1)
                orderPos = sql.ToUpper().LastIndexOf("ORDER BY");

            if (orderPos < wherePos)
                orderPos = -1; // ignore an order by that occurs in a subquery, for example

            if (wherePos != -1 && orderPos != -1)
                // both WHERE and ORDER BY clauses
                bugSql = sql.Substring(0, wherePos + 5)
                          + " /* altered - both  */ ( "
                          + sql.Substring(wherePos + 5, orderPos - (wherePos + 5))
                          + " ) AND ( "
                          + projectPermissionsSql
                          + " ) "
                          + sql.Substring(orderPos);
            else if (orderPos == -1 && wherePos == -1)
                // Neither
                bugSql = sql + " /* altered - neither */ WHERE " + projectPermissionsSql;
            else if (orderPos == -1)
                // WHERE, without order
                bugSql = sql.Substring(0, wherePos + 5)
                          + " /* altered - just where */ ( "
                          + sql.Substring(wherePos + 5)
                          + " ) AND ( "
                          + projectPermissionsSql + " )";
            else
                // ORDER BY, without WHERE
                bugSql = sql.Substring(0, orderPos)
                          + " /* altered - just order by  */ WHERE "
                          + projectPermissionsSql
                          + sql.Substring(orderPos);

            return bugSql;
        }

        public static string EncryptStringUsingMd5(string s)
        {
            var byteArray = Encoding.Default.GetBytes(s);

            using (var alg = HashAlgorithm.Create("MD5"))
            {
                var byteArray2 = alg.ComputeHash(byteArray);
                var sb = new StringBuilder(byteArray2.Length);

                foreach (var b in byteArray2)
                {
                    sb.AppendFormat("{0:X2}", b);
                }

                return sb.ToString();
            }
        }

        public static void UpdateUserPassword(int usId, string unencypted)
        {
            var random = new Random();
            var salt = random.Next(10000, 99999);

            var encrypted = EncryptStringUsingMd5(unencypted + Convert.ToString(salt));

            var sql = "update users set us_password = N'$en', us_salt = $salt where us_id = $id";

            sql = sql.Replace("$en", encrypted);
            sql = sql.Replace("$salt", Convert.ToString(salt));
            sql = sql.Replace("$id", Convert.ToString(usId));

            DbUtil.ExecuteNonQuery(sql);
        }

        public static string CapitalizeFirstLetter(string s)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (s.Length > 0 && !applicationSettings.NoCapitalization)
                return s.Substring(0, 1).ToUpper() + s.Substring(1, s.Length - 1);
            return s;
        }

        public static string SanitizeInteger(string s)
        {
            int n;
            string s2;
            try
            {
                n = Convert.ToInt32(s);
                s2 = Convert.ToString(n);
            }
            catch
            {
                throw new Exception("Expected integer.  Possible SQL injection attempt?");
            }

            return s;
        }

        public static bool IsNumericDataType(Type datatype)
        {
            if (datatype == typeof(int)
                || datatype == typeof(decimal)
                || datatype == typeof(double)
                || datatype == typeof(float)
                || datatype == typeof(uint)
                || datatype == typeof(long)
                || datatype == typeof(ulong)
                || datatype == typeof(short)
                || datatype == typeof(ushort))
                return true;
            return false;
        }

        private static string GetAbsoluteOrRelativeFolder(string folder)
        {
            if (folder.IndexOf(":") == 1
                || folder.StartsWith("\\\\"))
                // leave as is
                return folder;

            var mapPath = (string)HttpRuntime.Cache["MapPath"];
            return mapPath + "\\" + folder;
        }

        public static string GetFolder(string name, string dflt)
        {
            var folder = ApplicationSettings[name];
            if (folder == "")
                return dflt;

            folder = GetAbsoluteOrRelativeFolder(folder);
            if (!Directory.Exists(folder))
                throw new Exception(name + " specified in Web.config, \""
                                         + folder
                                         + "\", not found.  Edit Web.config.");

            return folder;
        }

        public static string GetLuceneIndexFolder()
        {
            var mapPath = (string)HttpRuntime.Cache["MapPath"];
            return GetFolder("LuceneIndexFolder", mapPath + "\\App_Data\\lucene_index");
        }

        public static string GetUploadFolder()
        {
            var mapPath = (string)HttpRuntime.Cache["MapPath"];
            return GetFolder("UploadFolder", mapPath + "\\App_Data\\uploads");
        }

        public static string GetLogFolder()
        {
            var mapPath = (string)HttpRuntime.Cache["MapPath"];
            return GetFolder("LogFileFolder", mapPath + "\\App_Data\\logs");
        }

        public static string[] SplitStringUsingCommas(string s)
        {
            return ReCommas.Split(s);
        }

        public static string[] SplitDropdownVals(string s)
        {
            var array = RePipes.Split(s);
            for (var i = 0; i < array.Length; i++) array[i] = array[i].Trim().Replace("\r", "").Replace("\n", "");
            return array;
        }

        // common to add/edit custom files, project
        public static string ValidateDropdownValues(string vals)
        {
            if (string.IsNullOrEmpty(vals))
            {
                return string.Empty;
            }

            if (vals.Contains("'")
                || vals.Contains("\"")
                || vals.Contains("<")
                || vals.Contains(">")
                || vals.Contains("\t"))
            {
                return "Special characters like <, >, or quotes not allowed.";
            }

            return string.Empty;
        }

        public static string HowLongAgo(int seconds)
        {
            return HowLongAgo(new TimeSpan(0, 0, seconds));
        }

        public static string HowLongAgo(TimeSpan ts)
        {
            if (ts.Days > 0)
            {
                if (ts.Days == 1)
                {
                    if (ts.Hours > 2)
                        return "1 day and " + ts.Hours + " hours ago";
                    return "1 day ago";
                }

                return ts.Days + " days ago";
            }

            if (ts.Hours > 0)
            {
                if (ts.Hours == 1)
                {
                    if (ts.Minutes > 5)
                        return "1 hour and " + ts.Minutes + " minutes ago";
                    return "1 hour ago";
                }

                return ts.Hours + " hours ago";
            }

            if (ts.Minutes > 0)
            {
                if (ts.Minutes == 1)
                    return "1 minute ago";
                return ts.Minutes + " minutes ago";
            }

            return ts.Seconds + " seconds ago";
        }

        public static DataTable GetRelatedUsers(ISecurity security, bool forceFullNames)
        {
            string sql;

            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (applicationSettings.DefaultPermissionLevel == 0)
                // only show users who have explicit permission
                // for projects that this user has permissions for

                sql = @"
/* get related users 1 */

select us_id,
case when $fullnames then
    case when len(isnull(us_firstname,'') + ' ' + isnull(us_lastname,'')) > 1
    then isnull(us_firstname,'') + ' ' + isnull(us_lastname,'')
    else us_username end
else us_username end us_username,
isnull(us_email,'') us_email,
us_org,
og_external_user
into #temp
from users
inner join orgs on us_org = og_id
where us_id in
    (select pu1.pu_user from project_user_xref pu1
    where pu1.pu_project in
        (select pu2.pu_project from project_user_xref pu2
        where pu2.pu_user = $user.usid
        and pu2.pu_permission_level <> 0
        )
    and pu1.pu_permission_level <> 0
    )

if $og_external_user = 1 -- external
and $og_other_orgs_permission_level = 0 -- other orgs
begin
    delete from #temp where og_external_user = 1 and us_org <> $user.org 
end

$limit_users

select us_id, us_username, us_email from #temp order by us_username

drop table #temp";
            else
                // show users UNLESS they have been explicitly excluded
                // from all the projects the viewer is able to view

                // the cartesian join in the first select is intentional

                sql = @"
/* get related users 2 */
select  pj_id, us_id,
case when $fullnames then
    case when len(isnull(us_firstname,'') + ' ' + isnull(us_lastname,'')) > 1
    then isnull(us_firstname,'') + ' ' + isnull(us_lastname,'')
    else us_username end
else us_username end us_username,
isnull(us_email,'') us_email
into #temp
from projects, users
where pj_id not in
(
    select pu_project from project_user_xref
    where pu_permission_level = 0 and pu_user = $user.usid
)


$limit_users


if $og_external_user = 1 -- external
and $og_other_orgs_permission_level = 0 -- other orgs
begin
    select distinct a.us_id, a.us_username, a.us_email
    from #temp a
    inner join users b on a.us_id = b.us_id
    inner join orgs on b.us_org = og_id
    where og_external_user = 0 or b.us_org = $user.org
    order by a.us_username
end
else
begin

    select distinct us_id, us_username, us_email
        from #temp
        left outer join project_user_xref on pj_id = pu_project
        and us_id = pu_user
        where isnull(pu_permission_level,2) <> 0
        order by us_username
end

drop table #temp";

            if (applicationSettings.LimitUsernameDropdownsInSearch)
            {
                var sqlLimitUserNames = @"

select isnull(bg_assigned_to_user,0) keep_me
into #temp2
from bugs
union
select isnull(bg_reported_user,0) from bugs

delete from #temp
where us_id not in (select keep_me from #temp2)
drop table #temp2";

                sql = sql.Replace("$limit_users", sqlLimitUserNames);
            }
            else
            {
                sql = sql.Replace("$limit_users", "");
            }

            if (forceFullNames || applicationSettings.UseFullNames)
                // true condition
                sql = sql.Replace("$fullnames", "1 = 1");
            else
                // false condition
                sql = sql.Replace("$fullnames", "0 = 1");

            sql = sql.Replace("$user.usid", Convert.ToString(security.User.Usid));
            sql = sql.Replace("$user.org", Convert.ToString(security.User.Org));
            sql = sql.Replace("$og_external_user", Convert.ToString(security.User.ExternalUser ? 1 : 0));
            sql = sql.Replace("$og_other_orgs_permission_level",
                Convert.ToString((int)security.User.OtherOrgsPermissionLevel));

            return DbUtil.GetDataSet(sql).Tables[0];
        }

        public static int GetDefaultUser(int projectid)
        {
            if (projectid == 0) return 0;

            var sql = @"select isnull(pj_default_user,0)
                    from projects
                    where pj_id = $pj";

            sql = sql.Replace("$pj", Convert.ToString(projectid));
            var obj = DbUtil.ExecuteScalar(sql);

            if (obj != null)
                return (int)obj;
            return 0;
        }

        public static DataSet GetCustomColumns()
        {
            var ds = (DataSet)Context.Application["custom_columns_dataset"];

            if (ds != null) return ds;

            ds = DbUtil.GetDataSet(@"
/* custom columns */ select sc.name, st.[name] [datatype], 
case when st.[name] = 'nvarchar' or st.[name] = 'nchar' then sc.length/2 else sc.length end as [length], 
sc.xprec, sc.xscale, sc.isnullable,
mm.text [default value], 
dflts.name [default name], 
isnull(ccm_dropdown_type,'') [dropdown type],
isnull(ccm_dropdown_vals,'') [vals],
isnull(ccm_sort_seq, sc.colorder) [column order],
sc.colorder
from syscolumns sc
inner join systypes st on st.xusertype = sc.xusertype
inner join sysobjects so on sc.id = so.id
left outer join syscomments mm on sc.cdefault = mm.id
left outer join custom_col_metadata on ccm_colorder = sc.colorder
left outer join sysobjects dflts on dflts.id = mm.id
where so.name = 'bugs'
and st.[name] <> 'sysname'
and sc.name not in ('rowguid',
'bg_id',
'bg_short_desc',
'bg_reported_user',
'bg_reported_date',
'bg_project',
'bg_org',
'bg_category',
'bg_priority',
'bg_status',
'bg_assigned_to_user',
'bg_last_updated_user',
'bg_last_updated_date',
'bg_user_defined_attribute',
'bg_project_custom_dropdown_value1',
'bg_project_custom_dropdown_value2',
'bg_project_custom_dropdown_value3',
'bg_tags')
order by sc.id, isnull(ccm_sort_seq,sc.colorder)");

            Context.Application["custom_columns_dataset"] = ds;
            return ds;
        }

        public static bool CheckPasswordStrength(string pw)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (!applicationSettings.RequireStrongPasswords)
            {
                return true;
            }

            if (pw.Length < 8) return false;
            if (pw.IndexOf("password") > -1) return false;
            if (pw.IndexOf("123") > -1) return false;
            if (pw.IndexOf("asdf") > -1) return false;
            if (pw.IndexOf("qwer") > -1) return false;
            if (pw.IndexOf("test") > -1) return false;

            var lowercase = 0;
            var uppercase = 0;
            var digits = 0;
            var specialChars = 0;

            for (var i = 0; i < pw.Length; i++)
            {
                var c = pw[i];
                if (c >= 'a' && c <= 'z') lowercase = 1;
                else if (c >= 'A' && c <= 'Z') uppercase = 1;
                else if (c >= '0' && c <= '9') digits = 1;
                else specialChars = 1;
            }

            if (lowercase + uppercase + digits + specialChars < 2) return false;

            return true;
        }

        public static string FilenameToContentType(string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();

            if (ext == ".jpg" || ext == ".jpeg")
            {
                return MediaTypeNames.Image.Jpeg;
            }

            if (ext == ".gif")
            {
                return MediaTypeNames.Image.Gif;
            }

            if (ext == ".bmp")
            {
                return "image/bmp";
            }

            if (ext == ".tiff")
            {
                return MediaTypeNames.Image.Tiff;
            }

            if (ext == ".txt" || ext == ".ini" || ext == ".bat" || ext == ".js")
            {
                return MediaTypeNames.Text.Plain;
            }

            if (ext == ".doc" || ext == ".docx")
            {
                return "application/msword";
            }

            if (ext == ".xls")
            {
                return "application/excel";
            }

            if (ext == ".zip")
            {
                return MediaTypeNames.Application.Zip;
            }

            if (ext == ".htm"
                || ext == ".html"
                || ext == ".asp"
                || ext == ".aspx"
                || ext == ".php")
            {
                return "text/html";
            }

            if (ext == ".xml")
            {
                return "text/xml";
            }

            if (ext == ".pdf")
            {
                return MediaTypeNames.Application.Pdf;
            }

            return MediaTypeNames.Application.Octet;
        }

        public static string RequestToStringForSql(string val, string datatype)
        {
            if (val == null || val.Length == 0)
            {
                if (datatype == "varchar"
                    || datatype == "nvarchar"
                    || datatype == "char"
                    || datatype == "nchar")
                    return "N''";
                return "null";
            }

            val = val.Replace("'", "''");

            if (datatype == "datetime")
                return "'" + FormatLocalDateIntoDbFormat(val) + "'";
            if (datatype == "decimal")
                return FormatLocalDecimalIntoDbFormat(val);
            if (datatype == "int")
                return val;
            return "N'" + val + "'";
        }

        public static void Redirect(HttpRequest request, HttpResponse response)
        {
            // redirect to the page the user was going to or start off with Bugs/List.aspx
            var url = request.QueryString["url"];
            var qs = request.QueryString["qs"];

            if (string.IsNullOrEmpty(url))
            {
                var mobile = request["mobile"];
                if (string.IsNullOrEmpty(mobile))
                    response.Redirect("~/Bugs/List.aspx");
                else
                    response.Redirect("~/Bugs/MobileList.aspx");
            }
            else if (url == request.ServerVariables["URL"]) // I can't remember what this code means...
            {
                response.Redirect("~/Bugs/List.aspx");
            }
            else
            {
                response.Redirect(RemoveLineBreaks(url) + "?" + RemoveLineBreaks(HttpUtility.UrlDecode(qs)));
            }
        }

        public static string RedirectUrl(HttpRequest request)
        {
            // redirect to the page the user was going to or start off with Bugs/List.aspx
            var url = request.QueryString["url"];
            var qs = request.QueryString["qs"];

            if (string.IsNullOrEmpty(url))
            {
                var mobile = request["mobile"];

                if (string.IsNullOrEmpty(mobile))
                {
                    return "~/Bugs/List.aspx";
                }
                else
                {
                    return "~/Bugs/MobileList.aspx";
                }
            }
            else if (url == request.ServerVariables["URL"]) // I can't remember what this code means...
            {
                return "~/Bugs/List.aspx";
            }

            return RemoveLineBreaks(url) + "?" + RemoveLineBreaks(HttpUtility.UrlDecode(qs));
        }

        public static void Redirect(string url, HttpRequest request, HttpResponse response)
        {
            //redirect to the url supplied with the original querystring
            if (url.IndexOf("?") > 0)
                response.Redirect(url + "&url="
                                      + RemoveLineBreaks(request.QueryString["url"])
                                      + "&qs="
                                      + RemoveLineBreaks(request.QueryString["qs"]));
            else
                response.Redirect(url + "?url="
                                      + RemoveLineBreaks(request.QueryString["url"])
                                      + "&qs="
                                      + RemoveLineBreaks(request.QueryString["qs"]));
        }

        public static string RedirectUrl(string url, HttpRequest request)
        {
            //redirect to the url supplied with the original querystring
            if (url.IndexOf("?") > 0)
            {
                return $"{url}&url={RemoveLineBreaks(request.QueryString["url"])}&qs={RemoveLineBreaks(request.QueryString["qs"])}";
            }

            return $"{url}?url={RemoveLineBreaks(request.QueryString["url"])}&qs={RemoveLineBreaks(request.QueryString["qs"])}";
        }

        public static string RemoveLineBreaks(string s)
        {
            if (s == null)
                return "";
            return s.Replace("\n", "").Replace("\r", "");
        }

        public static void UpdateMostRecentLoginDateTime(int usId)
        {
            var sql = @"update users set us_most_recent_login_datetime = getdate() where us_id = $us";
            sql = sql.Replace("$us", Convert.ToString(usId));
            DbUtil.ExecuteNonQuery(sql);
        }

        //
        //public static void PrintAsExcel(HttpResponse Response, DataView dv)
        //{ 
        //    Response.AddHeader("content-disposition", "attachment;filename=bugs.xls");
        //    Response.Write("<html><head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'/></head><body>");
        //    Response.Write("<table border=1>");
        //    int startCol = 0;
        //    int col;

        //    // column names first_column = true;
        //    for (col = startCol; col < dv.Table.Columns.Count;col++)
        //    { 
        //        if (dv.Table.Columns[col].ColumnName == "$FLAG")
        //            continue;
        //        if (dv.Table.Columns[col].ColumnName == "$SEEN")
        //            continue;
        //        Response.Write("<td>");
        //        Response.Write(dv.Table.Columns[col].ColumnName.Replace("<br>"," "));
        //        Response.Write("</td>");

        //    } // bug rows 

        //    foreach (DataRowView drv in dv)
        //    {
        //        Response.Write("<tr>");

        //        for (col = startCol; col < dv.Table.Columns.Count; col++)
        //        {
        //            if (dv.Table.Columns[col].ColumnName == "$FLAG")
        //                continue;
        //            if (dv.Table.Columns[col].ColumnName == "$SEEN")
        //                continue;

        //            Response.Write("<td>");

        //            if (drv[col].ToString().IndexOf("\r\n") >= 0)
        //            {
        //                Response.Write("\"" + drv[col].ToString().Replace("\"", "\"\"").Replace("\r\n", "\n") + "\"");
        //            }
        //            else
        //            {
        //                Response.Write(drv[col].ToString().Replace("\n", ""));
        //            }

        //            Response.Write("</td>");
        //        }
        //        Response.Write("</tr>");

        //    } 

        //    Response.Write("</table>");

        //} 

        public static void PrintAsExcel(HttpResponse response, DataView dv)
        {
            response.Clear();
            response.AddHeader("content-disposition",
                "attachment; filename=btnet_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            response.ContentType = "application/ms-excel";
            response.ContentEncoding = Encoding.UTF8;

            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (applicationSettings.WriteUtf8Preamble)
            {
                response.BinaryWrite(Encoding.UTF8.GetPreamble());
            }

            int col;
            bool firstColumn;
            var quote = "\"";
            var twoQuotes = quote + quote;
            var lineBreak = "\r\n";

            // column names
            firstColumn = true;
            for (col = 1; col < dv.Table.Columns.Count; col++)
            {
                if (dv.Table.Columns[col].ColumnName == "$FLAG")
                    continue;
                if (dv.Table.Columns[col].ColumnName == "$SEEN")
                    continue;

                if (!firstColumn) response.Write(",");
                response.Write(quote);
                response.Write(dv.Table.Columns[col].ColumnName.Replace("<br>", " ").Replace(quote, twoQuotes));
                response.Write(quote);
                firstColumn = false;
            }

            response.Write(lineBreak);

            // bug rows
            foreach (DataRowView drv in dv)
            {
                firstColumn = true;
                for (col = 1; col < dv.Table.Columns.Count; col++)
                {
                    var column = dv.Table.Columns[col];

                    if (column.ColumnName == "$FLAG")
                        continue;
                    if (column.ColumnName == "$SEEN")
                        continue;

                    if (!firstColumn) response.Write(",");

                    response.Write(quote);
                    if (column.DataType == typeof(DateTime))
                        response.Write(FormatDbDateTime(drv[col]));
                    else
                        response.Write(drv[col].ToString().Replace(lineBreak, "|").Replace("\n", "|")
                            .Replace(quote, twoQuotes));

                    response.Write(quote);

                    firstColumn = false;
                }

                response.Write(lineBreak);
            }

            response.End();
        }

        public static DataSet GetAllTasks(ISecurity security, int bugid)
        {
            var sql = "select ";

            if (bugid == 0)
                sql += @"
bg_id as [id], 
bg_short_desc as [description], 
pj_name as [project], 
ct_name as [category], 
bug_statuses.st_name as [status],  
bug_users.us_username as [assigned to],";

            sql += "tsk_id [task<br>id], tsk_description [task<br>description] ";

            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (applicationSettings.ShowTaskAssignedTo)
            {
                sql += ", task_users.us_username [task<br>assigned to]";
            }

            if (applicationSettings.ShowTaskPlannedStartDate)
            {
                sql += ", tsk_planned_start_date [planned start]";
            }

            if (applicationSettings.ShowTaskActualStartDate)
            {
                sql += ", tsk_actual_start_date [actual start]";
            }

            if (applicationSettings.ShowTaskPlannedEndDate)
            {
                sql += ", tsk_planned_end_date [planned end]";
            }

            if (applicationSettings.ShowTaskActualEndDate)
            {
                sql += ", tsk_actual_end_date [actual end]";
            }

            if (applicationSettings.ShowTaskPlannedDuration)
            {
                sql += ", tsk_planned_duration [planned<br>duration]";
            }

            if (applicationSettings.ShowTaskActualDuration)
            {
                sql += ", tsk_actual_duration  [actual<br>duration]";
            }

            if (applicationSettings.ShowTaskDurationUnits)
            {
                sql += ", tsk_duration_units [duration<br>units]";
            }

            if (applicationSettings.ShowTaskPercentComplete)
            {
                sql += ", tsk_percent_complete [percent<br>complete]";
            }

            if (applicationSettings.ShowTaskStatus)
            {
                sql += ", task_statuses.st_name  [task<br>status]";
            }

            if (applicationSettings.ShowTaskSortSequence)
            {
                sql += ", tsk_sort_sequence  [seq]";
            }

            sql += @"
from bug_tasks 
inner join bugs on tsk_bug = bg_id
left outer join projects on bg_project = pj_id
left outer join categories on bg_category = ct_id
left outer join statuses bug_statuses on bg_status = bug_statuses.st_id
left outer join statuses task_statuses on tsk_status = task_statuses.st_id
left outer join users bug_users on bg_assigned_to_user = bug_users.us_id
left outer join users task_users on tsk_assigned_to_user = task_users.us_id
where tsk_bug in 
(";

            if (bugid == 0)
                sql += AlterSqlPerProjectPermissions("select bg_id from bugs", security);
            else
                sql += Convert.ToString(bugid);
            sql += @"
)
order by tsk_sort_sequence, tsk_id";

            var ds = DbUtil.GetDataSet(sql);

            return ds;
        }
    }
}