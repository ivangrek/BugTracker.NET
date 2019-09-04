/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web.UI;
    using Core;

    public partial class Merge : Page
    {
        public DataRow Dr;
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            if (security.User.IsAdmin || security.User.CanMergeBugs)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "merge " +
                                                                        Util.GetSetting("SingularBugLabel",
                                                                            "bug");

            if (!IsPostBack)
            {
                var origIdString = Util.SanitizeInteger(Request["id"]);
                this.orig_id.Value = origIdString;
                this.back_href.HRef = ResolveUrl($"~/Bugs/Edit.aspx?id={origIdString}");
                this.from_bug.Value = origIdString;
            }
            else
            {
                this.from_err.InnerText = "";
                this.into_err.InnerText = "";
                on_update(security);
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
                if (!Util.IsInt(this.from_bug.Value))
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
                if (!Util.IsInt(this.into_bug.Value))
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

            this.Sql = @"
    declare @from_desc nvarchar(200)
    declare @into_desc nvarchar(200)
    declare @from_id int
    declare @into_id int
    set @from_id = -1
    set @into_id = -1
    select @from_desc = bg_short_desc, @from_id = bg_id from bugs where bg_id = $from
    select @into_desc = bg_short_desc, @into_id = bg_id from bugs where bg_id = $into
    select @from_desc, @into_desc, @from_id, @into_id	";

            this.Sql = this.Sql.Replace("$from", this.from_bug.Value);
            this.Sql = this.Sql.Replace("$into", this.into_bug.Value);

            this.Dr = DbUtil.GetDataRow(this.Sql);

            if ((int) this.Dr[2] == -1)
            {
                this.from_err.InnerText = "\"From\" bug not found.";
                good = false;
            }

            if ((int) this.Dr[3] == -1)
            {
                this.into_err.InnerText = "\"Into\" bug not found.";
                good = false;
            }

            if (!good)
                return false;
            return true;
        }

        public void on_update(Security security)
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
                this.prev_from_bug.Value = Util.SanitizeInteger(this.prev_from_bug.Value);
                this.prev_into_bug.Value = Util.SanitizeInteger(this.prev_into_bug.Value);

                // rename the attachments

                var uploadFolder = Util.GetUploadFolder();
                if (uploadFolder != null)
                {
                    this.Sql = @"select bp_id, bp_file from bug_posts
            where bp_type = 'file' and bp_bug = $from";

                    this.Sql = this.Sql.Replace("$from", this.prev_from_bug.Value);
                    var ds = DbUtil.GetDataSet(this.Sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // create path
                        var path = new StringBuilder(uploadFolder);
                        path.Append("\\");
                        path.Append(this.prev_from_bug.Value);
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_id"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_file"]));
                        if (File.Exists(path.ToString()))
                        {
                            var path2 = new StringBuilder(uploadFolder);
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
                this.Sql = @"
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

                this.Sql = this.Sql.Replace("$from", this.prev_from_bug.Value);
                this.Sql = this.Sql.Replace("$into", this.prev_into_bug.Value);

                DbUtil.ExecuteNonQuery(this.Sql);

                // record the merge itself

                this.Sql = @"insert into bug_posts
            (bp_bug, bp_user, bp_date, bp_type, bp_comment, bp_comment_search)
            values($into,$us,getdate(), 'comment', 'merged bug $from into this bug:', 'merged bug $from into this bug:')
            select scope_identity()";

                this.Sql = this.Sql.Replace("$from", this.prev_from_bug.Value);
                this.Sql = this.Sql.Replace("$into", this.prev_into_bug.Value);
                this.Sql = this.Sql.Replace("$us", Convert.ToString(security.User.Usid));

                var commentId = Convert.ToInt32(DbUtil.ExecuteScalar(this.Sql));

                // update bug comments with info from old bug
                this.Sql = @"update bug_posts
            set bp_comment = convert(nvarchar,bp_comment) + char(10) + bg_short_desc
            from bugs where bg_id = $from
            and bp_id = $bc";

                this.Sql = this.Sql.Replace("$from", this.prev_from_bug.Value);
                this.Sql = this.Sql.Replace("$bc", Convert.ToString(commentId));
                DbUtil.ExecuteNonQuery(this.Sql);

                // delete the from bug
                var fromBugid = Convert.ToInt32(this.prev_from_bug.Value);
                Bug.DeleteBug(fromBugid);

                // delete the from bug from the list, if there is a list
                var dvBugs = (DataView) Session["bugs"];
                if (dvBugs != null)
                {
                    // read through the list of bugs looking for the one that matches the from
                    var index = 0;
                    foreach (DataRowView drv in dvBugs)
                    {
                        if (fromBugid == (int) drv[1])
                        {
                            dvBugs.Delete(index);
                            break;
                        }

                        index++;
                    }
                }

                Bug.SendNotifications(Bug.Update, Convert.ToInt32(this.prev_into_bug.Value), security);

                Response.Redirect($"~/Bugs/Edit.aspx?id={this.prev_into_bug.Value}");
            }
            else
            {
                this.prev_from_bug.Value = this.from_bug.Value;
                this.prev_into_bug.Value = this.into_bug.Value;
                this.static_from_bug.InnerText = this.from_bug.Value;
                this.static_into_bug.InnerText = this.into_bug.Value;
                this.static_from_desc.InnerText = (string) this.Dr[0];
                this.static_into_desc.InnerText = (string) this.Dr[1];
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