/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Web;

    public class Bug
    {
        public static IApplicationSettings ApplicationSettings = new ApplicationSettings();

        public const int Insert = 1;
        public const int Update = 2;
        private static readonly object Dummy = new object(); // for a lock

        public static void AutoSubscribe(int bugid)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            // clean up bug subscriptions that no longer fit security rules
            // subscribe per auto_subscribe
            // subscribe project's default user
            // subscribe per-project auto_subscribers
            // subscribe per auto_subscribe_own_bugs
            var sql = @"
declare @pj int
select @pj = bg_project from bugs where bg_id = $id

delete from bug_subscriptions
where bs_bug = $id
and bs_user in
(select x.pu_user
from projects
left outer join project_user_xref x on pu_project = pj_id
where pu_project = @pj
and isnull(pu_permission_level,$dpl) = 0)

delete from bug_subscriptions
where bs_bug = $id
and bs_user in
(select us_id from users
 inner join orgs on us_org = og_id
 inner join bugs on bg_id = $id
 where og_other_orgs_permission_level = 0
 and bg_org <> og_id)

insert into bug_subscriptions (bs_bug, bs_user)
select $id, us_id
from users
inner join orgs on us_org = og_id
inner join bugs on bg_id = $id
left outer join project_user_xref on pu_project = @pj and pu_user = us_id
where us_auto_subscribe = 1
and
case
    when
        us_org <> bg_org
        and og_other_orgs_permission_level < 2
        and og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
            then og_other_orgs_permission_level
    else
        isnull(pu_permission_level,$dpl)
end <> 0
and us_active = 1
and us_id not in
(select bs_user from bug_subscriptions
where bs_bug = $id)

insert into bug_subscriptions (bs_bug, bs_user)
select $id, pj_default_user
from projects
inner join users on pj_default_user = us_id
where pj_id = @pj
and pj_default_user <> 0
and pj_auto_subscribe_default_user = 1
and us_active = 1
and pj_default_user not in
(select bs_user from bug_subscriptions
where bs_bug = $id)

insert into bug_subscriptions (bs_bug, bs_user)
select $id, pu_user from project_user_xref
inner join users on pu_user = us_id
inner join orgs on us_org = og_id
inner join bugs on bg_id = $id
where pu_auto_subscribe = 1
and
case
    when
        us_org <> bg_org
        and og_other_orgs_permission_level < 2
        and og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
            then og_other_orgs_permission_level
    else
        isnull(pu_permission_level,$dpl)
end <> 0
and us_active = 1
and pu_project = @pj
and pu_user not in
(select bs_user from bug_subscriptions
where bs_bug = $id)

insert into bug_subscriptions (bs_bug, bs_user)
select $id, us_id
from users
inner join bugs on bg_id = $id
inner join orgs on us_org = og_id
left outer join project_user_xref on pu_project = @pj and pu_user = us_id
where ((us_auto_subscribe_own_bugs = 1 and bg_assigned_to_user = us_id)
or
(us_auto_subscribe_reported_bugs = 1 and bg_reported_user = us_id))
and
case
    when
        us_org <> bg_org
        and og_other_orgs_permission_level < 2
        and og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
            then og_other_orgs_permission_level
    else
        isnull(pu_permission_level,$dpl)
end <> 0
and us_active = 1
and us_id not in
(select bs_user from bug_subscriptions
where bs_bug = $id)";

            sql = sql.Replace("$id", Convert.ToString(bugid));
            sql = sql.Replace("$dpl", applicationSettings.DefaultPermissionLevel.ToString());

            DbUtil.ExecuteNonQuery(sql);
        }

        public static void DeleteBug(int bugid)
        {
            // delete attachements

            var id = Convert.ToString(bugid);

            var uploadFolder = Util.GetUploadFolder();
            var sql = @"select bp_id, bp_file from bug_posts where bp_type = 'file' and bp_bug = $bg";
            sql = sql.Replace("$bg", id);

            var ds = DbUtil.GetDataSet(sql);
            if (uploadFolder != null && !string.IsNullOrEmpty(uploadFolder))
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    // create path
                    var path = new StringBuilder(uploadFolder);
                    path.Append("\\");
                    path.Append(id);
                    path.Append("_");
                    path.Append(Convert.ToString(dr["bp_id"]));
                    path.Append("_");
                    path.Append(Convert.ToString(dr["bp_file"]));
                    if (File.Exists(path.ToString())) File.Delete(path.ToString());
                }

            // delete the database entries

            sql = @"
delete bug_post_attachments from bug_post_attachments inner join bug_posts on bug_post_attachments.bpa_post = bug_posts.bp_id where bug_posts.bp_bug = $bg
delete from bug_posts where bp_bug = $bg
delete from bug_subscriptions where bs_bug = $bg
delete from bug_relationships where re_bug1 = $bg
delete from bug_relationships where re_bug2 = $bg
delete from bug_user where bu_bug = $bg
delete from bug_tasks where tsk_bug = $bg
delete from bugs where bg_id = $bg";

            sql = sql.Replace("$bg", id);
            DbUtil.ExecuteNonQuery(sql);
        }

        public static int InsertPostAttachmentCopy(
            ISecurity security,
            int bugid,
            int copyBpid,
            string comment,
            int parent,
            bool hiddenFromExternalUsers,
            bool sendNotifications)
        {
            return InsertPostAttachmentImpl(
                security,
                bugid,
                null,
                -1,
                copyBpid,
                null,
                comment,
                null,
                parent,
                hiddenFromExternalUsers,
                sendNotifications);
        }

        public static int InsertPostAttachment(
            ISecurity security,
            int bugid,
            Stream content,
            int contentLength,
            string file,
            string comment,
            string contentType,
            int parent,
            bool hiddenFromExternalUsers,
            bool sendNotifications)
        {
            return InsertPostAttachmentImpl(
                security,
                bugid,
                content,
                contentLength,
                -1, // copy_bpid
                file,
                comment,
                contentType,
                parent,
                hiddenFromExternalUsers,
                sendNotifications);
        }

        private static int InsertPostAttachmentImpl(
            ISecurity security,
            int bugid,
            Stream content,
            int contentLength,
            int copyBpid,
            string file,
            string comment,
            string contentType,
            int parent,
            bool hiddenFromExternalUsers,
            bool sendNotifications)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            // Note that this method does not perform any security check nor does
            // it check that content_length is less than MaxUploadSize.
            // These are left up to the caller.

            var uploadFolder = Util.GetUploadFolder();
            string sql;
            var storeAttachmentsInDatabase = applicationSettings.StoreAttachmentsInDatabase;
            var effectiveFile = file;
            var effectiveContentLength = contentLength;
            var effectiveContentType = contentType;
            Stream effectiveContent = null;

            try
            {
                // Determine the content. We may be instructed to copy an existing
                // attachment via copy_bpid, or a Stream may be provided as the content parameter.

                if (copyBpid != -1)
                {
                    var bpa = get_bug_post_attachment(copyBpid);

                    effectiveContent = bpa.Content;
                    effectiveFile = bpa.File;
                    effectiveContentLength = bpa.ContentLength;
                    effectiveContentType = bpa.ContentType;
                }
                else
                {
                    effectiveContent = content;
                    effectiveFile = file;
                    effectiveContentLength = contentLength;
                    effectiveContentType = contentType;
                }

                // Insert a new post into bug_posts.

                sql = @"
declare @now datetime

set @now = getdate()

update bugs
    set bg_last_updated_date = @now,
    bg_last_updated_user = $us
    where bg_id = $bg

insert into bug_posts
    (bp_type, bp_bug, bp_file, bp_comment, bp_size, bp_date, bp_user, bp_content_type, bp_parent, bp_hidden_from_external_users)
    values ('file', $bg, N'$fi', N'$de', $si, @now, $us, N'$ct', $pa, $internal)
    select scope_identity()";

                sql = sql.Replace("$bg", Convert.ToString(bugid));
                sql = sql.Replace("$fi", effectiveFile.Replace("'", "''"));
                sql = sql.Replace("$de", comment.Replace("'", "''"));
                sql = sql.Replace("$si", Convert.ToString(effectiveContentLength));
                sql = sql.Replace("$us", Convert.ToString(security.User.Usid));

                // Sometimes, somehow, content type is null.  Not sure how.
                sql = sql.Replace("$ct",
                    effectiveContentType != null
                        ? effectiveContentType.Replace("'", "''")
                        : string.Empty);

                if (parent == -1)
                    sql = sql.Replace("$pa", "null");
                else
                    sql = sql.Replace("$pa", Convert.ToString(parent));
                sql = sql.Replace("$internal", Util.BoolToString(hiddenFromExternalUsers));

                var bpId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

                try
                {
                    // Store attachment in bug_post_attachments table.

                    if (storeAttachmentsInDatabase)
                    {
                        var data = new byte[effectiveContentLength];
                        var bytesRead = 0;

                        while (bytesRead < effectiveContentLength)
                        {
                            var bytesReadThisIteration = effectiveContent.Read(data, bytesRead,
                                effectiveContentLength - bytesRead);
                            if (bytesReadThisIteration == 0)
                                throw new Exception(
                                    "Unexpectedly reached the end of the stream before all data was read.");
                            bytesRead += bytesReadThisIteration;
                        }

                        sql = @"insert into bug_post_attachments
                                (bpa_post, bpa_content)
                                values (@bp, @bc)";
                        using (var cmd = new SqlCommand(sql))
                        {
                            cmd.Parameters.AddWithValue("@bp", bpId);
                            cmd.Parameters.Add("@bc", SqlDbType.Image).Value = data;
                            cmd.CommandTimeout = ApplicationSettings.SqlCommandCommandTimeout;
                            DbUtil.ExecuteNonQuery(cmd);
                        }
                    }
                    else
                    {
                        // Store attachment in UploadFolder.

                        if (uploadFolder == null)
                            throw new Exception(
                                "StoreAttachmentsInDatabase is false and UploadFolder is not set in web.config.");

                        // Copy the content Stream to a file in the upload_folder.
                        var buffer = new byte[16384];
                        var bytesRead = 0;
                        using (var fs =
                            new FileStream(uploadFolder + "\\" + bugid + "_" + bpId + "_" + effectiveFile,
                                FileMode.CreateNew, FileAccess.Write))
                        {
                            while (bytesRead < effectiveContentLength)
                            {
                                var bytesReadThisIteration = effectiveContent.Read(buffer, 0, buffer.Length);
                                if (bytesReadThisIteration == 0)
                                    throw new Exception(
                                        "Unexpectedly reached the end of the stream before all data was read.");
                                fs.Write(buffer, 0, bytesReadThisIteration);
                                bytesRead += bytesReadThisIteration;
                            }
                        }
                    }
                }
                catch
                {
                    // clean up
                    sql = @"delete from bug_posts where bp_id = $bp";

                    sql = sql.Replace("$bp", Convert.ToString(bpId));

                    DbUtil.ExecuteNonQuery(sql);

                    throw;
                }

                if (sendNotifications) SendNotifications(Update, bugid, security);
                return bpId;
            }
            finally
            {
                // If this procedure "owns" the content (instead of our caller owning it), dispose it.
                if (effectiveContent != null && effectiveContent != content) effectiveContent.Dispose();
            }
        }

        public static BugPostAttachment get_bug_post_attachment(int bpId)
        {
            // Note that this method does not perform any security check.
            // This is left up to the caller.

            var uploadFolder = Util.GetUploadFolder();
            string sql;
            var storeAttachmentsInDatabase = ApplicationSettings.StoreAttachmentsInDatabase;
            int bugid;
            string file;
            int contentLength;
            string contentType;
            Stream content = null;

            try
            {
                sql = @"select bp_bug, bp_file, bp_size, bp_content_type
                        from bug_posts
                        where bp_id = $bp";

                sql = sql.Replace("$bp", Convert.ToString(bpId));
                using (var reader = DbUtil.ExecuteReader(sql, CommandBehavior.CloseConnection))
                {
                    if (reader.Read())
                    {
                        bugid = reader.GetInt32(reader.GetOrdinal("bp_bug"));
                        file = reader.GetString(reader.GetOrdinal("bp_file"));
                        contentLength = reader.GetInt32(reader.GetOrdinal("bp_size"));
                        contentType = reader.GetString(reader.GetOrdinal("bp_content_type"));
                    }
                    else
                    {
                        throw new Exception("Existing bug post not found.");
                    }
                }

                sql = @"select bpa_content
                            from bug_post_attachments
                            where bpa_post = $bp";

                sql = sql.Replace("$bp", Convert.ToString(bpId));

                object contentObject;
                contentObject = DbUtil.ExecuteScalar(sql);

                if (contentObject != null && !Convert.IsDBNull(contentObject))
                {
                    content = new MemoryStream((byte[])contentObject);
                }
                else
                {
                    // Could not find in bug_post_attachments. Try the upload_folder.
                    if (uploadFolder == null)
                        throw new Exception(
                            "The attachment could not be found in the database and UploadFolder is not set in web.config.");

                    var uploadFolderFilename = uploadFolder + "\\" + bugid + "_" + bpId + "_" + file;
                    if (File.Exists(uploadFolderFilename))
                        content = new FileStream(uploadFolderFilename, FileMode.Open, FileAccess.Read,
                            FileShare.Read);
                    else
                        throw new Exception("Attachment not found in database or UploadFolder.");
                }

                return new BugPostAttachment(file, content, contentLength, contentType);
            }
            catch
            {
                if (content != null)
                    content.Dispose();

                throw;
            }
        }

        public static DataRow GetBugDataRow(
            int bugid,
            ISecurity security)
        {
            var dsCustomCols = Util.GetCustomColumns();
            return GetBugDataRow(bugid, security, dsCustomCols);
        }

        public static DataRow GetBugDataRow(
            int bugid,
            ISecurity security,
            DataSet dsCustomCols)
        {
            var sql = @" /* get_bug_datarow */";

            if (ApplicationSettings.EnableSeen)
                sql += @"
if not exists (select bu_bug from bug_user where bu_bug = $id and bu_user = $this_usid)
    insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($id, $this_usid, 0, 1, 0) 
update bug_user set bu_seen = 1, bu_seen_datetime = getdate() where bu_bug = $id and bu_user = $this_usid and bu_seen <> 1";

            sql += @"
declare @svn_revisions int
declare @git_commits int
declare @hg_revisions int
declare @tasks int
declare @related int;
set @svn_revisions = 0
set @git_commits = 0
set @hg_revisions = 0
set @tasks = 0
set @related = 0";

            if (ApplicationSettings.EnableSubversionIntegration)
                sql += @"
select @svn_revisions = count(1)
from svn_affected_paths
inner join svn_revisions on svnap_svnrev_id = svnrev_id
where svnrev_bug = $id;";

            if (ApplicationSettings.EnableGitIntegration)
                sql += @"
select @git_commits = count(1)
from git_affected_paths
inner join git_commits on gitap_gitcom_id = gitcom_id
where gitcom_bug = $id;";

            if (ApplicationSettings.EnableMercurialIntegration)
                sql += @"
select @hg_revisions = count(1)
from hg_affected_paths
inner join hg_revisions on hgap_hgrev_id = hgrev_id
where hgrev_bug = $id;";

            if (ApplicationSettings.EnableTasks)
                sql += @"
select @tasks = count(1)
from bug_tasks
where tsk_bug = $id;";

            if (ApplicationSettings.EnableRelationships)
                sql += @"
select @related = count(1)
from bug_relationships
where re_bug1 = $id;";

            sql += @"

select bg_id [id],
bg_short_desc [short_desc],
isnull(bg_tags,'') [bg_tags],
isnull(ru.us_username,'[deleted user]') [reporter],
isnull(ru.us_email,'') [reporter_email],
case rtrim(ru.us_firstname)
    when null then isnull(ru.us_lastname, '')
    when '' then isnull(ru.us_lastname, '')
    else isnull(ru.us_lastname + ', ' + ru.us_firstname,'')
    end [reporter_fullname],
bg_reported_date [reported_date],
datediff(s,bg_reported_date,getdate()) [seconds_ago],
isnull(lu.us_username,'') [last_updated_user],
case rtrim(lu.us_firstname)
    when null then isnull(lu.us_lastname, '')
    when '' then isnull(lu.us_lastname, '')
    else isnull(lu.us_lastname + ', ' + lu.us_firstname,'')
    end [last_updated_fullname],


bg_last_updated_date [last_updated_date],
isnull(bg_project,0) [project],
isnull(pj_name,'[no project]') [current_project],

isnull(bg_org,0) [organization],
isnull(bugorg.og_name,'') [og_name],

isnull(bg_category,0) [category],
isnull(ct_name,'') [category_name],

isnull(bg_priority,0) [priority],
isnull(pr_name,'') [priority_name],

isnull(bg_status,0) [status],
isnull(st_name,'') [status_name],

isnull(bg_user_defined_attribute,0) [udf],
isnull(udf_name,'') [udf_name],

isnull(bg_assigned_to_user,0) [assigned_to_user],
isnull(asg.us_username,'[not assigned]') [assigned_to_username],
case rtrim(asg.us_firstname)
when null then isnull(asg.us_lastname, '[not assigned]')
when '' then isnull(asg.us_lastname, '[not assigned]')
else isnull(asg.us_lastname + ', ' + asg.us_firstname,'[not assigned]')
end [assigned_to_fullname],

isnull(bs_user,0) [subscribed],

case
when
    $this_org <> bg_org
    and userorg.og_other_orgs_permission_level < 2
    and userorg.og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
        then userorg.og_other_orgs_permission_level
else
    isnull(pu_permission_level,$dpl)
end [pu_permission_level],

isnull(bg_project_custom_dropdown_value1,'') [bg_project_custom_dropdown_value1],
isnull(bg_project_custom_dropdown_value2,'') [bg_project_custom_dropdown_value2],
isnull(bg_project_custom_dropdown_value3,'') [bg_project_custom_dropdown_value3],
@related [relationship_cnt],
@svn_revisions [svn_revision_cnt],
@git_commits [git_commit_cnt],
@hg_revisions [hg_commit_cnt],
@tasks [task_cnt],
getdate() [snapshot_timestamp]
$custom_cols_placeholder
from bugs
inner join users this_user on us_id = $this_usid
inner join orgs userorg on this_user.us_org = userorg.og_id
left outer join user_defined_attribute on bg_user_defined_attribute = udf_id
left outer join projects on bg_project = pj_id
left outer join orgs bugorg on bg_org = bugorg.og_id
left outer join categories on bg_category = ct_id
left outer join priorities on bg_priority = pr_id
left outer join statuses on bg_status = st_id
left outer join users asg on bg_assigned_to_user = asg.us_id
left outer join users ru on bg_reported_user = ru.us_id
left outer join users lu on bg_last_updated_user = lu.us_id
left outer join bug_subscriptions on bs_bug = bg_id and bs_user = $this_usid
left outer join project_user_xref on pj_id = pu_project
and pu_user = $this_usid
where bg_id = $id";

            if (dsCustomCols.Tables[0].Rows.Count == 0)
            {
                sql = sql.Replace("$custom_cols_placeholder", "");
            }
            else
            {
                var customColsSql = string.Empty;

                foreach (DataRow drcc in dsCustomCols.Tables[0].Rows) customColsSql += ",[" + drcc["name"] + "]";
                sql = sql.Replace("$custom_cols_placeholder", customColsSql);
            }

            sql = sql.Replace("$id", Convert.ToString(bugid));
            sql = sql.Replace("$this_usid", Convert.ToString(security.User.Usid));
            sql = sql.Replace("$this_org", Convert.ToString(security.User.Org));
            sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

            return DbUtil.GetDataRow(sql);
        }

        public static void ApplyPostInsertRules(int bugid)
        {
            var sql = ApplicationSettings.UpdateBugAfterInsertBugAspxSql;

            if (!string.IsNullOrEmpty(sql))
            {
                sql = sql.Replace("$BUGID$", Convert.ToString(bugid));
                DbUtil.ExecuteNonQuery(sql);
            }
        }

        public static DataRow GetBugDefaults()
        {
            var sql = @"/*fetch defaults*/
declare @pj int
declare @ct int
declare @pr int
declare @st int
declare @udf int
set @pj = 0
set @ct = 0
set @pr = 0
set @st = 0
set @udf = 0
select @pj = pj_id from projects where pj_default = 1 order by pj_name
select @ct = ct_id from categories where ct_default = 1 order by ct_name
select @pr = pr_id from priorities where pr_default = 1 order by pr_name
select @st = st_id from statuses where st_default = 1 order by st_name
select @udf = udf_id from user_defined_attribute where udf_default = 1 order by udf_name
select @pj pj, @ct ct, @pr pr, @st st, @udf udf";

            return DbUtil.GetDataRow(sql);
        }

        public static SecurityPermissionLevel GetBugPermissionLevel(int bugid, ISecurity security)
        {
            /*
                    public const int PERMISSION_NONE = 0;
                    public const int PERMISSION_READONLY = 1;
                    public const int PERMISSION_REPORTER = 3;
                    public const int PERMISSION_ALL = 2;
            */

            // fetch the revised permission level
            var sql = @"
declare @bg_org int

select isnull(pu_permission_level,$dpl),
bg_org
from bugs
left outer join project_user_xref
on pu_project = bg_project
and pu_user = $us
where bg_id = $bg";
            ;

            sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
            sql = sql.Replace("$bg", Convert.ToString(bugid));
            sql = sql.Replace("$us", Convert.ToString(security.User.Usid));

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null) return SecurityPermissionLevel.PermissionNone;

            var pl = (SecurityPermissionLevel)(int)dr[0];
            var bgOrg = (int)dr[1];

            // maybe reduce permissions
            if (bgOrg != security.User.Org)
                if (security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionNone
                    || security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionReadonly)
                    if (security.User.OtherOrgsPermissionLevel < pl)
                        pl = security.User.OtherOrgsPermissionLevel;

            return (SecurityPermissionLevel)pl;
        }

        public static NewIds InsertBug(
            string shortDesc,
            ISecurity security,
            string tags,
            int projectid,
            int orgid,
            int categoryid,
            int priorityid,
            int statusid,
            int assignedToUserid,
            int udfid,
            string projectCustomDropdownValue1,
            string projectCustomDropdownValue2,
            string projectCustomDropdownValue3,
            string commentFormated,
            string commentSearch,
            string from,
            string cc,
            string contentType,
            bool internalOnly,
            SortedDictionary<string, string> hashCustomCols,
            bool sendNotifications)
        {
            if (string.IsNullOrEmpty(shortDesc.Trim())) shortDesc = "[No Description]";

            if (assignedToUserid == 0) assignedToUserid = Util.GetDefaultUser(projectid);

            var sql = @"insert into bugs
                    (bg_short_desc,
                    bg_tags,
                    bg_reported_user,
                    bg_last_updated_user,
                    bg_reported_date,
                    bg_last_updated_date,
                    bg_project,
                    bg_org,
                    bg_category,
                    bg_priority,
                    bg_status,
                    bg_assigned_to_user,
                    bg_user_defined_attribute,
                    bg_project_custom_dropdown_value1,
                    bg_project_custom_dropdown_value2,
                    bg_project_custom_dropdown_value3
                    $custom_cols_placeholder1)
                    values (N'$short_desc', N'$tags', $reported_user,  $reported_user, getdate(), getdate(),
                    $project, $org,
                    $category, $priority, $status, $assigned_user, $udf,
                    N'$pcd1',N'$pcd2',N'$pcd3' $custom_cols_placeholder2)";

            sql = sql.Replace("$short_desc", shortDesc.Replace("'", "''"));
            sql = sql.Replace("$tags", tags.Replace("'", "''"));
            sql = sql.Replace("$reported_user", Convert.ToString(security.User.Usid));
            sql = sql.Replace("$project", Convert.ToString(projectid));
            sql = sql.Replace("$org", Convert.ToString(orgid));
            sql = sql.Replace("$category", Convert.ToString(categoryid));
            sql = sql.Replace("$priority", Convert.ToString(priorityid));
            sql = sql.Replace("$status", Convert.ToString(statusid));
            sql = sql.Replace("$assigned_user", Convert.ToString(assignedToUserid));
            sql = sql.Replace("$udf", Convert.ToString(udfid));
            sql = sql.Replace("$pcd1", projectCustomDropdownValue1);
            sql = sql.Replace("$pcd2", projectCustomDropdownValue2);
            sql = sql.Replace("$pcd3", projectCustomDropdownValue3);

            if (hashCustomCols == null)
            {
                sql = sql.Replace("$custom_cols_placeholder1", "");
                sql = sql.Replace("$custom_cols_placeholder2", "");
            }
            else
            {
                var customColsSql1 = string.Empty;
                var customColsSql2 = string.Empty;

                var dsCustomCols = Util.GetCustomColumns();

                foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string)drcc["name"];

                    // skip if no permission to update
                    if (security.User.DictCustomFieldPermissionLevel[columnName] !=
                        SecurityPermissionLevel.PermissionAll) continue;

                    customColsSql1 += ",[" + columnName + "]";

                    var datatype = (string)drcc["datatype"];

                    var customColVal = Util.RequestToStringForSql(
                        hashCustomCols[columnName],
                        datatype);

                    customColsSql2 += "," + customColVal;
                }

                sql = sql.Replace("$custom_cols_placeholder1", customColsSql1);
                sql = sql.Replace("$custom_cols_placeholder2", customColsSql2);
            }

            sql += "\nselect scope_identity()";

            var bugid = Convert.ToInt32(DbUtil.ExecuteScalar(sql));
            var postid = InsertComment(
                bugid,
                security.User.Usid,
                commentFormated,
                commentSearch,
                from,
                cc,
                contentType,
                internalOnly);

            AutoSubscribe(bugid);

            if (sendNotifications) SendNotifications(Insert, bugid, security);

            return new NewIds(bugid, postid);
        }

        public static int InsertComment(
            int bugid,
            int thisUsid,
            string commentFormated,
            string commentSearch,
            string from,
            string cc,
            string contentType,
            bool internalOnly)
        {
            if (!string.IsNullOrEmpty(commentFormated))
            {
                var sql = @"
declare @now datetime
set @now = getdate()

insert into bug_posts
(bp_bug, bp_user, bp_date, bp_comment, bp_comment_search, bp_email_from, bp_email_cc, bp_type, bp_content_type,
bp_hidden_from_external_users)
values(
$id,
$us,
@now,
N'$comment_formatted',
N'$comment_search',
N'$from',
N'$cc',
N'$type',
N'$content_type',
$internal)
select scope_identity();";

                if (from != null)
                {
                    // Update the bugs timestamp here.
                    // We don't do it unconditionally because it would mess up the locking.
                    // The Bugs/Edit.aspx page gets its snapshot timestamp from the update of the bug
                    // row, not the comment row, so updating the bug again would confuse it.
                    sql += @"update bugs
                        set bg_last_updated_date = @now,
                        bg_last_updated_user = $us
                        where bg_id = $id";

                    sql = sql.Replace("$from", from.Replace("'", "''"));
                    sql = sql.Replace("$type", "received"); // received email
                }
                else
                {
                    sql = sql.Replace("N'$from'", "null");
                    sql = sql.Replace("$type", "comment"); // bug comment
                }

                sql = sql.Replace("$id", Convert.ToString(bugid));
                sql = sql.Replace("$us", Convert.ToString(thisUsid));
                sql = sql.Replace("$comment_formatted", commentFormated.Replace("'", "''"));
                sql = sql.Replace("$comment_search", commentSearch.Replace("'", "''"));
                sql = sql.Replace("$content_type", contentType);
                if (cc == null) cc = string.Empty;
                sql = sql.Replace("$cc", cc.Replace("'", "''"));
                sql = sql.Replace("$internal", Util.BoolToString(internalOnly));

                return Convert.ToInt32(DbUtil.ExecuteScalar(sql));
            }

            return 0;
        }

        public static void SendNotifications(int insertOrUpdate, int bugid, ISecurity security,
            int justToThisUserId)
        {
            SendNotifications(insertOrUpdate,
                bugid,
                security,
                justToThisUserId,
                false, // status changed
                false, // assigend to changed
                0); // prev assigned to
        }

        public static void SendNotifications(int insertOrUpdate, int bugid, ISecurity security)
        {
            SendNotifications(insertOrUpdate,
                bugid,
                security,
                0, // just to this
                false, // status changed
                false, // assigend to changed
                0); // prev assigned to
        }

        // This used to send the emails, but not now.  Now it just queues
        // the emails to be sent, then spawns a thread to send them.
        public static void SendNotifications(int insertOrUpdate, // The implementation
            int bugid,
            ISecurity security,
            int justToThisUserid,
            bool statusChanged,
            bool assignedToChanged,
            int prevAssignedToUser)
        {
            // If there's something worth emailing about, then there's 
            // probably something worth updating the index about.
            // Really, though, we wouldn't want to update the index if it were
            // just the status that were changing...
            if (ApplicationSettings.EnableLucene) MyLucene.UpdateLuceneIndex(bugid);

            var notificationEmailEnabled = ApplicationSettings.NotificationEmailEnabled;

            if (!notificationEmailEnabled) return;
            // MAW -- 2006/01/27 -- Determine level of change detected
            var changeLevel = 0;
            if (insertOrUpdate == Insert)
                changeLevel = 1;
            else if (statusChanged)
                changeLevel = 2;
            else if (assignedToChanged)
                changeLevel = 3;
            else
                changeLevel = 4;

            string sql;

            if (justToThisUserid > 0)
            {
                sql = @"
/* get notification email for just one user  */
select us_email, us_id, us_admin, og.*
from bug_subscriptions
inner join users on bs_user = us_id
inner join orgs og on us_org = og_id
inner join bugs on bg_id = bs_bug
left outer join project_user_xref on pu_user = us_id and pu_project = bg_project
where us_email is not null
and us_enable_notifications = 1
-- $status_change
and us_active = 1
and us_email <> ''
and
case
when
    us_org <> bg_org
    and og_other_orgs_permission_level < 2
    and og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
        then og_other_orgs_permission_level
else
    isnull(pu_permission_level,$dpl)
end <> 0
and bs_bug = $id
and us_id = $just_this_usid";

                sql = sql.Replace("$just_this_usid", Convert.ToString(justToThisUserid));
            }
            else
            {
                // MAW -- 2006/01/27 -- Added different notifications if reported or assigned-to
                sql = @"
/* get notification emails for all subscribers */
select us_email, us_id, us_admin, og.*
from bug_subscriptions
inner join users on bs_user = us_id
inner join orgs og on us_org = og_id
inner join bugs on bg_id = bs_bug
left outer join project_user_xref on pu_user = us_id and pu_project = bg_project
where us_email is not null
and us_enable_notifications = 1
-- $status_change
and us_active = 1
and us_email <> ''
and (   ($cl <= us_reported_notifications and bg_reported_user = bs_user)
or ($cl <= us_assigned_notifications and bg_assigned_to_user = bs_user)
or ($cl <= us_assigned_notifications and $pau = bs_user)
or ($cl <= us_subscribed_notifications))
and
case
when
us_org <> bg_org
and og_other_orgs_permission_level < 2
and og_other_orgs_permission_level < isnull(pu_permission_level,$dpl)
    then og_other_orgs_permission_level
else
isnull(pu_permission_level,$dpl)
end <> 0
and bs_bug = $id
and (us_id <> $us or isnull(us_send_notifications_to_self,0) = 1)";
            }

            sql = sql.Replace("$cl", changeLevel.ToString());
            sql = sql.Replace("$pau", prevAssignedToUser.ToString());
            sql = sql.Replace("$id", Convert.ToString(bugid));
            sql = sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
            sql = sql.Replace("$us", Convert.ToString(security.User.Usid));

            var dsSubscribers = DbUtil.GetDataSet(sql);

            if (dsSubscribers.Tables[0].Rows.Count > 0)
            {
                var addedToQueue = false;

                // Get bug html
                var bugDr = GetBugDataRow(bugid, security);

                var from = ApplicationSettings.NotificationEmailFrom;

                // Format the subject line
                var subject = ApplicationSettings.NotificationSubjectFormat;

                subject = subject.Replace("$THING$",
                    Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel));

                var action = string.Empty;
                if (insertOrUpdate == Insert)
                    action = "added";
                else
                    action = "updated";

                subject = subject.Replace("$ACTION$", action);
                subject = subject.Replace("$BUGID$", Convert.ToString(bugid));
                subject = subject.Replace("$SHORTDESC$", (string)bugDr["short_desc"]);

                var trackingId = " (";
                trackingId += ApplicationSettings.TrackingIdString;
                trackingId += Convert.ToString(bugid);
                trackingId += ")";
                subject = subject.Replace("$TRACKINGID$", trackingId);

                subject = subject.Replace("$PROJECT$", (string)bugDr["current_project"]);
                subject = subject.Replace("$ORGANIZATION$", (string)bugDr["og_name"]);
                subject = subject.Replace("$CATEGORY$", (string)bugDr["category_name"]);
                subject = subject.Replace("$PRIORITY$", (string)bugDr["priority_name"]);
                subject = subject.Replace("$STATUS$", (string)bugDr["status_name"]);
                subject = subject.Replace("$ASSIGNED_TO$", (string)bugDr["assigned_to_username"]);

                // send a separate email to each subscriber
                foreach (DataRow dr in dsSubscribers.Tables[0].Rows)
                {
                    var to = (string)dr["us_email"];

                    // Create a fake response and let the code
                    // write the html to that response
                    var writer = new StringWriter();
                    var myResponse = new HttpResponse(writer);
                    myResponse.Write("<html>");
                    myResponse.Write("<base href=\"" +
                                     ApplicationSettings.AbsoluteUrlPrefix + "\"/>");

                    // create a security rec for the user receiving the email
                    var sec2 = new Security();

                    // fill in what we know is needed downstream
                    sec2.User.IsAdmin = Convert.ToBoolean(dr["us_admin"]);
                    sec2.User.ExternalUser = Convert.ToBoolean(dr["og_external_user"]);
                    sec2.User.TagsFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_tags_field_permission_level"];
                    sec2.User.CategoryFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_category_field_permission_level"];
                    sec2.User.PriorityFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_priority_field_permission_level"];
                    sec2.User.AssignedToFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_assigned_to_field_permission_level"];
                    sec2.User.StatusFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_status_field_permission_level"];
                    sec2.User.ProjectFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_project_field_permission_level"];
                    sec2.User.OrgFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_org_field_permission_level"];
                    sec2.User.UdfFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_udf_field_permission_level"];

                    var dsCustom = Util.GetCustomColumns();
                    foreach (DataRow drCustom in dsCustom.Tables[0].Rows)
                    {
                        var bgName = (string)drCustom["name"];
                        var ogName = "og_"
                                      + (string)drCustom["name"]
                                      + "_field_permission_level";

                        var obj = dr[ogName];
                        if (Convert.IsDBNull(obj))
                            sec2.User.DictCustomFieldPermissionLevel[bgName] = SecurityPermissionLevel.PermissionAll;
                        else
                            sec2.User.DictCustomFieldPermissionLevel[bgName] = (SecurityPermissionLevel)(int)dr[ogName];
                    }

                    var html = PrintBug.PrintBugNew(
                        bugDr,
                        sec2,
                        true, // include style 
                        false, // images_inline 
                        true, // history_inline
                        true); // internal_posts

                    myResponse.Write(html);
                    // at this point "writer" has the bug html

                    sql = @"
delete from queued_notifications where qn_bug = $bug and qn_to = N'$to'

insert into queued_notifications
(qn_date_created, qn_bug, qn_user, qn_status, qn_retries, qn_to, qn_from, qn_subject, qn_body, qn_last_exception)
values (getdate(), $bug, $user, N'not sent', 0, N'$to', N'$from', N'$subject', N'$body', N'')";

                    sql = sql.Replace("$bug", Convert.ToString(bugid));
                    sql = sql.Replace("$user", Convert.ToString(dr["us_id"]));
                    sql = sql.Replace("$to", to.Replace("'", "''"));
                    sql = sql.Replace("$from", from.Replace("'", "''"));
                    sql = sql.Replace("$subject", subject.Replace("'", "''"));
                    sql = sql.Replace("$body", writer.ToString().Replace("'", "''"));

                    DbUtil.ExecuteNonQueryWithoutLogging(sql);

                    addedToQueue = true;
                } // end loop through ds_subscribers

                if (addedToQueue)
                {
                    // spawn a worker thread to send the emails
                    var thread = new Thread(ThreadProcNotifications);
                    thread.Start();
                }
            } // if there are any subscribers
        }

        // Send the emails in the queue
        protected static void ActuallySendTheEmails()
        {
            Util.WriteToLog("actually_send_the_emails");

            var sql = @"select * from queued_notifications where qn_status = N'not sent' and qn_retries < 3";
            // create a new one, just in case there would be multithreading issues...

            // get the pending notifications
            var ds = DbUtil.GetDataSet(sql);
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var err = string.Empty;

                try
                {
                    var to = (string)dr["qn_to"];

                    Util.WriteToLog("sending email to " + to);

                    // try to send it
                    err = Email.SendEmail(
                        (string)dr["qn_to"],
                        (string)dr["qn_from"],
                        "", // cc
                        (string)dr["qn_subject"],
                        (string)dr["qn_body"],
                        BtnetMailFormat.Html);

                    if (string.IsNullOrEmpty(err)) sql = "delete from queued_notifications where qn_id = $qn_id";
                }
                catch (Exception e)
                {
                    err = e.Message;
                    if (e.InnerException != null)
                    {
                        err += "; ";
                        err += e.InnerException.Message;
                    }
                }

                if (!string.IsNullOrEmpty(err))
                {
                    sql =
                        "update queued_notifications  set qn_retries = qn_retries + 1, qn_last_exception = N'$ex' where qn_id = $qn_id";
                    sql = sql.Replace("$ex", err.Replace("'", "''"));
                }

                sql = sql.Replace("$qn_id", Convert.ToString(dr["qn_id"]));

                // update the row or delete the row
                DbUtil.ExecuteNonQuery(sql);
            }
        }

        // Send the emails in the queue
        public static void ThreadProcNotifications()
        {
            // just to be safe, make the worker threads wait for each other
            lock (Dummy)
            {
                try
                {
                    // Don't send emails right away, in case the guy is making a bunch of changes.
                    // Let's consolidate the notifications to one.
                    Thread.Sleep(1000 * 60);
                    ActuallySendTheEmails();
                }
                catch (ThreadAbortException)
                {
                    Util.WriteToLog("caught ThreadAbortException in threadproc_notifications");
                    ActuallySendTheEmails();
                }
            }
        } // end of notification thread proc

        public class BugPostAttachment
        {
            public Stream Content;
            public int ContentLength;
            public string ContentType;

            public string File;

            public BugPostAttachment(string file, Stream content, int contentLength, string contentType)
            {
                this.File = file;
                this.Content = content;
                this.ContentLength = contentLength;
                this.ContentType = contentType;
            }
        }

        public class NewIds
        {
            public int Bugid { get; set; }
            public int Postid { get; set; }

            public NewIds(int b, int p)
            {
                this.Bugid = b;
                this.Postid = p;
            }
        }
    }
}