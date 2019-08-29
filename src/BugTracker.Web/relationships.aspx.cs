/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class relationships : Page
    {
        public int bugid;
        public DataSet ds;
        public int permission_level;
        public int previd;

        public Security security;
        public string ses;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "relationships";

            string sql;
            this.add_err.InnerText = "";

            this.bugid = Convert.ToInt32(Util.sanitize_integer(Request["bgid"]));

            if (string.IsNullOrEmpty(Request["bugid"]))
                this.previd = 0;
            else
                this.previd = Convert.ToInt32(Util.sanitize_integer(Request["prev"]));

            var bugid2 = 0;

            this.permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (this.permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.ses = (string) Session["session_cookie"];
            var action = Request["actn"];

            if (!string.IsNullOrEmpty(action))
            {
                if (Request["ses"] != this.ses)
                {
                    Response.Write("session in Request doesn't match session cookie");
                    Response.End();
                }

                if (this.permission_level == Security.PERMISSION_READONLY)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                if (action == "remove") // remove
                {
                    if (this.security.user.is_guest)
                    {
                        Response.Write("You are not allowed to delete a relationship");
                        Response.End();
                    }

                    bugid2 = Convert.ToInt32(Util.sanitize_integer(Request["bugid2"]));

                    sql = @"
				delete from bug_relationships where re_bug2 = $bg2 and re_bug1 = $bg;
				delete from bug_relationships where re_bug1 = $bg2 and re_bug2 = $bg;
				insert into bug_posts
						(bp_bug, bp_user, bp_date, bp_comment, bp_type)
						values($bg, $us, getdate(), N'deleted relationship to $bg2', 'update')";
                    sql = sql.Replace("$bg2", Convert.ToString(bugid2));
                    sql = sql.Replace("$bg", Convert.ToString(this.bugid));
                    sql = sql.Replace("$us", Convert.ToString(this.security.user.usid));
                    DbUtil.execute_nonquery(sql);
                }
                else
                {
                    // adding

                    if (Request["bugid2"] != null)
                    {
                        if (!Util.is_int(Request["bugid2"]))
                        {
                            this.add_err.InnerText = "Related ID must be an integer.";
                        }
                        else
                        {
                            bugid2 = Convert.ToInt32(Request["bugid2"]);

                            if (this.bugid == bugid2)
                            {
                                this.add_err.InnerText = "Cannot create a relationship to self.";
                            }
                            else
                            {
                                var rows = 0;

                                // check if bug exists
                                sql = @"select count(1) from bugs where bg_id = $bg2";
                                sql = sql.Replace("$bg2", Convert.ToString(bugid2));
                                rows = (int) DbUtil.execute_scalar(sql);

                                if (rows == 0)
                                {
                                    this.add_err.InnerText = "Not found.";
                                }
                                else
                                {
                                    // check if relationship exists
                                    sql =
                                        @"select count(1) from bug_relationships where re_bug1 = $bg and re_bug2 = $bg2";
                                    sql = sql.Replace("$bg2", Convert.ToString(bugid2));
                                    sql = sql.Replace("$bg", Convert.ToString(this.bugid));
                                    rows = (int) DbUtil.execute_scalar(sql);

                                    if (rows > 0)
                                    {
                                        this.add_err.InnerText = "Relationship already exists.";
                                    }
                                    else
                                    {
                                        // check permission of related bug
                                        var permission_level2 = Bug.get_bug_permission_level(bugid2, this.security);
                                        if (permission_level2 == Security.PERMISSION_NONE)
                                        {
                                            this.add_err.InnerText = "You are not allowed to view the related item.";
                                        }
                                        else
                                        {
                                            // insert the relationship both ways
                                            sql = @"
insert into bug_relationships (re_bug1, re_bug2, re_type, re_direction) values($bg, $bg2, N'$ty', $dir1);
insert into bug_relationships (re_bug2, re_bug1, re_type, re_direction) values($bg, $bg2, N'$ty', $dir2);
insert into bug_posts
	(bp_bug, bp_user, bp_date, bp_comment, bp_type)
	values($bg, $us, getdate(), N'added relationship to $bg2', 'update');";

                                            sql = sql.Replace("$bg2", Convert.ToString(bugid2));
                                            sql = sql.Replace("$bg", Convert.ToString(this.bugid));
                                            sql = sql.Replace("$us", Convert.ToString(this.security.user.usid));
                                            sql = sql.Replace("$ty", Request["type"].Replace("'", "''"));

                                            if (this.siblings.Checked)
                                            {
                                                sql = sql.Replace("$dir2", "0");
                                                sql = sql.Replace("$dir1", "0");
                                            }
                                            else if (this.child_to_parent.Checked)
                                            {
                                                sql = sql.Replace("$dir2", "1");
                                                sql = sql.Replace("$dir1", "2");
                                            }
                                            else
                                            {
                                                sql = sql.Replace("$dir2", "2");
                                                sql = sql.Replace("$dir1", "1");
                                            }

                                            DbUtil.execute_nonquery(sql);
                                            this.add_err.InnerText = "Relationship was added.";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            sql = @"
select bg_id [id],
	bg_short_desc [desc],
	re_type [comment],
	st_name [status],
	case
		when re_direction = 0 then ''
		when re_direction = 2 then 'child of $bg'
		else                       'parent of $bg' 
	end as [parent or child],
	'<a target=_blank href=edit_bug.aspx?id=' + convert(varchar,bg_id) + '>view</a>' [view]";

            if (!this.security.user.is_guest && this.permission_level == Security.PERMISSION_ALL)
                sql += @"
,'<a href=''javascript:remove(' + convert(varchar,re_bug2) + ')''>detach</a>' [detach]";

            sql += @"
from bugs
inner join bug_relationships on bg_id = re_bug2
left outer join statuses on st_id = bg_status
where re_bug1 = $bg
order by bg_id desc";

            sql = sql.Replace("$bg", Convert.ToString(this.bugid));
            sql = Util.alter_sql_per_project_permissions(sql, this.security);

            this.ds = DbUtil.get_dataset(sql);

            this.bgid.Value = Convert.ToString(this.bugid);
        }

        public string get_bug_html(DataRow dr)
        {
            var s = @"
	
	
<td valign=top>
<div
style='background: #dddddd; border: 1px solid blue; padding 15px;  width: 140px; height: 50px; overflow: hidden;'
><a 
href='relationships.aspx?bgid=$id&prev=$prev'>$id&nbsp;&nbsp;&nbsp;&nbsp;$title</a></div>";

            if (this.previd == (int) dr["id"]) s = s.Replace("1px solid blue", "2px solid red");

            s = s.Replace("$id", Convert.ToString(dr["id"]));
            s = s.Replace("$prev", Convert.ToString(this.bugid));
            s = s.Replace(
                "$title",
                Server.HtmlEncode(
                    Convert.ToString(dr["desc"])
                )
            );

            return s;
        }

        public void display_hierarchy()
        {
            var parents = "";
            var siblings = "";
            var children = "";

            foreach (DataRow dr in this.ds.Tables[0].Rows)
            {
                var level = (string) dr["parent or child"];

                if (level.StartsWith("parent"))
                    parents += get_bug_html(dr);
                else if (level.StartsWith("child"))
                    children += get_bug_html(dr);
                else
                    siblings += get_bug_html(dr);
            }

            Response.Write("Parents:&nbsp;<table border=0 cellspacing=15 cellpadding=0><tr>");
            Response.Write(parents);
            Response.Write("</table><p>");
            Response.Write("Siblings:&nbsp;<table border=0 cellspacing=15 cellpadding=0><tr>");
            Response.Write(siblings);
            Response.Write("</table><p>");
            Response.Write("Children:&nbsp;<table border=0 cellspacing=15 cellpadding=0><tr>");
            Response.Write(children);
            Response.Write("</table>");
        }
    }
}