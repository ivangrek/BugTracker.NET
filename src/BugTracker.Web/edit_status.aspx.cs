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
    using Core;

    public partial class edit_status : Page
    {
        public int id;

        public Security security;
        public string sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit status";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.id = 0;
            else
                this.id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                // add or edit?
                if (this.id == 0)
                {
                    this.sub.Value = "Create";
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form

                    this.sql =
                        @"select st_name, st_sort_seq, isnull(st_style,'') [st_style], st_default from statuses where st_id = $1";
                    this.sql = this.sql.Replace("$1", Convert.ToString(this.id));
                    var dr = DbUtil.get_datarow(this.sql);

                    // Fill in this form
                    this.name.Value = (string) dr["st_name"];
                    this.sort_seq.Value = Convert.ToString((int) dr["st_sort_seq"]);
                    this.style.Value = (string) dr["st_style"];
                    this.default_selection.Checked = Convert.ToBoolean((int) dr["st_default"]);
                }
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;
            if (this.name.Value == "")
            {
                good = false;
                this.name_err.InnerText = "Description is required.";
            }
            else
            {
                this.name_err.InnerText = "";
            }

            if (this.sort_seq.Value == "")
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence is required.";
            }
            else
            {
                this.sort_seq_err.InnerText = "";
            }

            if (!Util.is_int(this.sort_seq.Value))
            {
                good = false;
                this.sort_seq_err.InnerText = "Sort Sequence must be an integer.";
            }
            else
            {
                this.sort_seq_err.InnerText = "";
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.id == 0) // insert new
                {
                    this.sql =
                        "insert into statuses (st_name, st_sort_seq, st_style, st_default) values (N'$na', $ss, N'$st', $df)";
                }
                else // edit existing
                {
                    this.sql = @"update statuses set
				st_name = N'$na',
				st_sort_seq = $ss,
				st_style = N'$st',
				st_default = $df
				where st_id = $id";

                    this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                }

                this.sql = this.sql.Replace("$na", this.name.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$ss", this.sort_seq.Value);
                this.sql = this.sql.Replace("$st", this.style.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$df", Util.bool_to_string(this.default_selection.Checked));
                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("statuses.aspx");
            }
            else
            {
                if (this.id == 0) // insert new
                    this.msg.InnerText = "Status was not created.";
                else // edit existing
                    this.msg.InnerText = "Status was not updated.";
            }
        }
    }
}