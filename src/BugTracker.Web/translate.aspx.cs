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

    public partial class translate : Page
    {
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "translate";

            var string_bp_id = Request["postid"];
            var string_bg_id = Request["bugid"];

            if (!IsPostBack)
            {
                if (string_bp_id != null && string_bp_id != "")
                {
                    string_bp_id = Util.sanitize_integer(string_bp_id);

                    this.sql = @"select bp_bug, bp_comment
						from bug_posts
						where bp_id = $id";

                    this.sql = this.sql.Replace("$id", string_bp_id);

                    var dr = DbUtil.get_datarow(this.sql);

                    string_bg_id = Convert.ToString((int) dr["bp_bug"]);
                    var obj = dr["bp_comment"];
                    if (dr["bp_comment"] != DBNull.Value) this.source.InnerText = obj.ToString();
                }
                else if (string_bg_id != null && string_bg_id != "")
                {
                    string_bg_id = Util.sanitize_integer(string_bg_id);

                    this.sql = @"select bg_short_desc
						from bugs
						where bg_id = $id";

                    this.sql = this.sql.Replace("$id", string_bg_id);

                    var obj = DbUtil.execute_scalar(this.sql);

                    if (obj != DBNull.Value) this.source.InnerText = obj.ToString();
                }

                // added check for permission level - corey
                var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(string_bg_id), this.security);
                if (permission_level == Security.PERMISSION_NONE)
                {
                    Response.Write("You are not allowed to view this item");
                    Response.End();
                }

                this.back_href.HRef = "edit_bug.aspx?id=" + string_bg_id;

                this.bugid.Value = string_bg_id;

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