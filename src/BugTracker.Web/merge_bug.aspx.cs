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
    using System.Web.UI;
    using Core;

    public partial class merge_bug : Page
    {
        public DataRow dr;

        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_merge_bugs)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "merge " +
                                                                        Util.get_setting("SingularBugLabel",
                                                                            "bug");

            if (!IsPostBack)
            {
                var orig_id_string = Util.sanitize_integer(Request["id"]);
                this.orig_id.Value = orig_id_string;
                this.back_href.HRef = "edit_bug.aspx?id=" + orig_id_string;
                this.from_bug.Value = orig_id_string;
            }
            else
            {
                this.from_err.InnerText = "";
                this.into_err.InnerText = "";
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            // validate FROM

            if (this.from_bug.Value == "")
            {
                this.from_err.InnerText = "\"From\" bug is required.";
                good = false;
            }
            else
            {
                if (!Util.is_int(this.from_bug.Value))
                {
                    this.from_err.InnerText = "\"From\" bug must be an integer.";
                    good = false;
                }
            }

            // validate INTO

            if (this.into_bug.Value == "")
            {
                this.into_err.InnerText = "\"Into\" bug is required.";
                good = false;
            }
            else
            {
                if (!Util.is_int(this.into_bug.Value))
                {
                    this.into_err.InnerText = "\"Into\" bug must be an integer.";
                    good = false;
                }
            }

            if (!good) return false;

            if (this.from_bug.Value == this.into_bug.Value)
            {
                this.from_err.InnerText = "\"From\" bug cannot be the same as \"Into\" bug.";
                return false;
            }

            // Continue and see if from and to exist in db

            this.sql = @"
	declare @from_desc nvarchar(200)
	declare @into_desc nvarchar(200)
	declare @from_id int
	declare @into_id int
	set @from_id = -1
	set @into_id = -1
	select @from_desc = bg_short_desc, @from_id = bg_id from bugs where bg_id = $from
	select @into_desc = bg_short_desc, @into_id = bg_id from bugs where bg_id = $into
	select @from_desc, @into_desc, @from_id, @into_id	";

            this.sql = this.sql.Replace("$from", this.from_bug.Value);
            this.sql = this.sql.Replace("$into", this.into_bug.Value);

            this.dr = DbUtil.get_datarow(this.sql);

            if ((int) this.dr[2] == -1)
            {
                this.from_err.InnerText = "\"From\" bug not found.";
                good = false;
            }

            if ((int) this.dr[3] == -1)
            {
                this.into_err.InnerText = "\"Into\" bug not found.";
                good = false;
            }

            if (!good)
                return false;
            return true;
        }

        public void on_update()
        {
            // does it say "Merge" or "Confirm Merge"?

            if (this.submit.Value == "Merge")
                if (!validate())
                {
                    this.prev_from_bug.Value = "";
                    this.prev_into_bug.Value = "";
                    return;
                }

            if (this.prev_from_bug.Value == this.from_bug.Value
                && this.prev_into_bug.Value == this.into_bug.Value)
            {
                this.prev_from_bug.Value = Util.sanitize_integer(this.prev_from_bug.Value);
                this.prev_into_bug.Value = Util.sanitize_integer(this.prev_into_bug.Value);

                // rename the attachments

                var upload_folder = Util.get_upload_folder();
                if (upload_folder != null)
                {
                    this.sql = @"select bp_id, bp_file from bug_posts
			where bp_type = 'file' and bp_bug = $from";

                    this.sql = this.sql.Replace("$from", this.prev_from_bug.Value);
                    var ds = DbUtil.get_dataset(this.sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // create path
                        var path = new StringBuilder(upload_folder);
                        path.Append("\\");
                        path.Append(this.prev_from_bug.Value);
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_id"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_file"]));
                        if (File.Exists(path.ToString()))
                        {
                            var path2 = new StringBuilder(upload_folder);
                            path2.Append("\\");
                            path2.Append(this.prev_into_bug.Value);
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_id"]));
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_file"]));

                            File.Move(path.ToString(), path2.ToString());
                        }
                    }
                }

                // copy the from db entries to the to
                this.sql = @"
insert into bug_subscriptions
(bs_bug, bs_user)
select $into, bs_user
from bug_subscriptions
where bs_bug = $from
and bs_user not in (select bs_user from bug_subscriptions where bs_bug = $into)

insert into bug_user
(bu_bug, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime)
select $into, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime
from bug_user
where bu_bug = $from
and bu_user not in (select bu_user from bug_user where bu_bug = $into)

update bug_posts     set bp_bug     = $into	where bp_bug = $from
update bug_tasks     set tsk_bug    = $into where tsk_bug = $from
update svn_revisions set svnrev_bug = $into where svnrev_bug = $from
update hg_revisions  set hgrev_bug  = $into where hgrev_bug = $from
update git_commits   set gitcom_bug = $into where gitcom_bug = $from
";

                this.sql = this.sql.Replace("$from", this.prev_from_bug.Value);
                this.sql = this.sql.Replace("$into", this.prev_into_bug.Value);

                DbUtil.execute_nonquery(this.sql);

                // record the merge itself

                this.sql = @"insert into bug_posts
			(bp_bug, bp_user, bp_date, bp_type, bp_comment, bp_comment_search)
			values($into,$us,getdate(), 'comment', 'merged bug $from into this bug:', 'merged bug $from into this bug:')
			select scope_identity()";

                this.sql = this.sql.Replace("$from", this.prev_from_bug.Value);
                this.sql = this.sql.Replace("$into", this.prev_into_bug.Value);
                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));

                var comment_id = Convert.ToInt32(DbUtil.execute_scalar(this.sql));

                // update bug comments with info from old bug
                this.sql = @"update bug_posts
			set bp_comment = convert(nvarchar,bp_comment) + char(10) + bg_short_desc
			from bugs where bg_id = $from
			and bp_id = $bc";

                this.sql = this.sql.Replace("$from", this.prev_from_bug.Value);
                this.sql = this.sql.Replace("$bc", Convert.ToString(comment_id));
                DbUtil.execute_nonquery(this.sql);

                // delete the from bug
                var from_bugid = Convert.ToInt32(this.prev_from_bug.Value);
                Bug.delete_bug(from_bugid);

                // delete the from bug from the list, if there is a list
                var dv_bugs = (DataView) Session["bugs"];
                if (dv_bugs != null)
                {
                    // read through the list of bugs looking for the one that matches the from
                    var index = 0;
                    foreach (DataRowView drv in dv_bugs)
                    {
                        if (from_bugid == (int) drv[1])
                        {
                            dv_bugs.Delete(index);
                            break;
                        }

                        index++;
                    }
                }

                Bug.send_notifications(Bug.UPDATE, Convert.ToInt32(this.prev_into_bug.Value), this.security);

                Response.Redirect("edit_bug.aspx?id=" + this.prev_into_bug.Value);
            }
            else
            {
                this.prev_from_bug.Value = this.from_bug.Value;
                this.prev_into_bug.Value = this.into_bug.Value;
                this.static_from_bug.InnerText = this.from_bug.Value;
                this.static_into_bug.InnerText = this.into_bug.Value;
                this.static_from_desc.InnerText = (string) this.dr[0];
                this.static_into_desc.InnerText = (string) this.dr[1];
                this.from_bug.Style["display"] = "none";
                this.into_bug.Style["display"] = "none";
                this.static_from_bug.Style["display"] = "";
                this.static_into_bug.Style["display"] = "";
                this.static_from_desc.Style["display"] = "";
                this.static_into_desc.Style["display"] = "";
                this.submit.Value = "Confirm Merge";
            }
        }
    }
}