/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class Translate : Page
    {
        public Security Security;
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "translate";

            var stringBpId = Request["postid"];
            var stringBgId = Request["bugid"];

            if (!IsPostBack)
            {
                if (stringBpId != null && stringBpId != "")
                {
                    stringBpId = Util.SanitizeInteger(stringBpId);

                    this.Sql = @"select bp_bug, bp_comment
						from bug_posts
						where bp_id = $id";

                    this.Sql = this.Sql.Replace("$id", stringBpId);

                    var dr = DbUtil.GetDataRow(this.Sql);

                    stringBgId = Convert.ToString((int) dr["bp_bug"]);
                    var obj = dr["bp_comment"];
                    if (dr["bp_comment"] != DBNull.Value) this.source.InnerText = obj.ToString();
                }
                else if (stringBgId != null && stringBgId != "")
                {
                    stringBgId = Util.SanitizeInteger(stringBgId);

                    this.Sql = @"select bg_short_desc
						from bugs
						where bg_id = $id";

                    this.Sql = this.Sql.Replace("$id", stringBgId);

                    var obj = DbUtil.ExecuteScalar(this.Sql);

                    if (obj != DBNull.Value) this.source.InnerText = obj.ToString();
                }

                // added check for permission level - corey
                var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(stringBgId), this.Security);
                if (permissionLevel == Security.PermissionNone)
                {
                    Response.Write("You are not allowed to view this item");
                    Response.End();
                }

                this.back_href.HRef = "EditBug.aspx?id=" + stringBgId;

                this.bugid.Value = stringBgId;

                fill_translationmodes();
            }
            else
            {
                on_translate();
            }
        }

        public void fill_translationmodes()
        {
            var ts = new TranslationService();

            foreach (var tm in ts.GetAllTranslationModes())
                this.mode.Items.Add(new ListItem(tm.VisualNameEN, tm.ObjectID));

            this.mode.SelectedValue = "fr_nl";

            ts = null;
        }

        public void on_translate()
        {
            var ts = new TranslationService();
            var tm = ts.GetTranslationModeByObjectID(this.mode.SelectedValue);

            var result = ts.Translate(tm, this.source.InnerText);

            result = result.Replace("\n", "<br>");

            this.dest.Text = result;

            this.pnl.Visible = true;

            tm = null;
            ts = null;
        }
    }
}