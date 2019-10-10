<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" AutoEventWireup="true" CodeBehind="Search.aspx.cs" Inherits="BugTracker.Web.Search" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="on_change()">--%>

    <link rel="StyleSheet" href="<%= ResolveUrl("~/Scripts/jquery/jquery-ui-1.7.2.custom.css") %>" type="text/css">
    <!-- use btnet_edit_bug.css to control positioning on edit_bug.asp.  use btnet_search.css to control position on Search.aspx  -->
    <link rel="StyleSheet" href="<%= ResolveUrl("~/Content/custom/btnet_search.css") %>" type="text/css">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/jquery/jquery-ui-1.7.2.custom.min.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/bug_list.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/suggest.js") %>"></script>
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/datejs/date.js") %>"></script>

    <script>
        search_suggest_min_chars = <% Response.Write(ApplicationSettings.SearchSuggestMinChars); %>

        // start of mass edit javascript
        <% if (Security.User.IsAdmin || Security.User.CanMassEditBugs)
        { %>

            function select_all(sel) {
                var frm = document.getElementById("massform");
                for (var i = 0; i < frm.elements.length; i++) {
                    var varname = frm.elements[i].name;
                    if (!isNaN(parseInt(varname))) {
                        frm.elements[i].checked = sel;
                    }
                }
            };

        function validate_mass() {

            var at_least_one_selected = false;

            // make sure at least one item is selected
            var frm = document.getElementById("massform");
            for (var i = 0; i < frm.elements.length; i++) {
                var varname = frm.elements[i].name;
                if (!isNaN(parseInt(varname))) {
                    var checkbox = frm.elements[i];
                    if (checkbox.checked == true) {
                        at_least_one_selected = true;
                        break;
                    }
                }
            }

            if (!at_least_one_selected) {
                alert("No items selected for mass update/delete.");
                return false;
            }

            if (frm.mass_project.selectedIndex == 0 &&
                frm.mass_org.selectedIndex == 0 &&
                frm.mass_category.selectedIndex == 0 &&
                frm.mass_priority.selectedIndex == 0 &&
                frm.mass_assigned_to.selectedIndex == 0 &&
                frm.mass_status.selectedIndex == 0 &&
                frm.mass_reported_by.selectedIndex == 0) {
                if (!frm.mass_delete.checked) {
                    alert("No updates were specified and delete wasn't checked.  Please specify updates or delete.");
                    return false;
                }
            } else {
                if (frm.mass_delete.checked) {
                    alert("Both updates and delete were specified.   Please select one or the other.");
                    return false;
                }
            }

            return true;
        }

        function load_one_massedit_select(from_id, to_id) {
            var from;
            var to;
            var option;

            from = document.getElementById(from_id);
            to = document.getElementById(to_id);

            option = document.createElement('option');
            option.value = -1;
            option.text = "[do not update]";
            try {
                to.add(option, null); // standards compliant; doesn't work in IE
            } catch (ex) {
                to.add(option); // IE only
            }

            for (var i = 0; i < from.options.length; i++) {
                option = document.createElement('option');
                option.value = from.options[i].value;
                option.text = from.options[i].text;
                try {
                    to.add(option, null); // standards compliant; doesn't work in IE
                } catch (ex) {
                    to.add(option); // IE only
                }
            }

        }

        function load_massedit_selects() {

            load_one_massedit_select("project", "mass_project");
            load_one_massedit_select("org", "mass_org");
            load_one_massedit_select("category", "mass_category");
            load_one_massedit_select("priority", "mass_priority");
            load_one_massedit_select("assigned_to", "mass_assigned_to");
            load_one_massedit_select("status", "mass_status");
            load_one_massedit_select("reported_by", "mass_reported_by");
        }

    <% } %> // end of mass edit javascript

        function build_where(where, clause) {
            if (clause == "") return where;

            var sql = "";

            if (where == "") {
                sql = "where ";
                sql += clause;
            } else {
                sql = where;
                and_or = document.getElementById("and").checked ? "and " : "or ";
                sql += and_or;
                sql += clause;
            }

            return sql;
        }


        function build_clause_from_options(options, column_name) {

            var clause = "";
            for (i = 0; i < options.length; i++) {
                if (options[i].selected) {
                    if (clause == "") {
                        clause = " " + column_name + " in (";
                    } else {
                        clause += ",";
                    }

                    clause += options[i].value;
                }
            }
            if (clause != "") clause += ")\n";

            return clause;
        }


        function in_not_in_vals(el) {

            var vals = "";

            if (el.tagName == "INPUT") {
                if (el.value == "") {
                    return vals;
                }
                vals = "(";

                var opts = "";
                val_array = el.value.split(",");
                for (i = 0; i < val_array.length; i++) {
                    if (opts != "") {
                        opts += ",";
                    }

                    opts += "N'";
                    opts += val_array[i].replace(/'/ig, "''");
                    opts += "'"; // "
                }
                vals += opts;
                vals += ")\n";

            } else if (el.tagName == "SELECT") {
                if (el.selectedIndex == -1) {
                    return vals;
                }
                vals = "(";

                var opts = "";
                for (i = 0; i < el.options.length; i++) {
                    if (el.options[i].selected) {
                        if (opts != "") {
                            opts += ",";
                        }

                        var one_opt = "N'";
                        one_opt += el.options[i].text.replace(/'/ig, "''");
                        one_opt += "'";

                        opts += one_opt;
                    }
                }
                vals += opts;
                vals += ")\n";
            }

            //alert(vals)
            return vals;
        }

        function format_to_date_for_db(s) {
            // convert the date for sql
            // Uses date.js, 
            try {
                return Date.parse(s).toString("yyyyMMdd 23:59:59");
            } catch (err) {
                return "";
            }

        }


        function format_from_date_for_db(s) {
            // convert the date for sql
            // Uses date.js, 
            try {
                return Date.parse(s).toString("yyyyMMdd");
            } catch (err) {
                return "";
            }

        }

        var asp_form_id = '<% Response.Write(ApplicationSettings.AspNetFormId); %>';


        function on_change() {
            var frm = document.getElementById(asp_form_id);


            // Build "WHERE" clause

            var where = "";

            var reported_by_clause = build_clause_from_options(frm.reported_by.options, "bg_reported_user");
            var assigned_to_clause = build_clause_from_options(frm.assigned_to.options, "bg_assigned_to_user");
            var project_clause = build_clause_from_options(frm.project.options, "bg_project");

            var project_custom_dropdown1_clause = build_clause_from_options(
                frm.project_custom_dropdown1.options,
                "bg_project_custom_dropdown_value1");
            var project_custom_dropdown2_clause = build_clause_from_options(
                frm.project_custom_dropdown2.options,
                "bg_project_custom_dropdown_value2");
            var project_custom_dropdown3_clause = build_clause_from_options(
                frm.project_custom_dropdown3.options,
                "bg_project_custom_dropdown_value3");

        <%
        if (Security.User.OtherOrgsPermissionLevel != 0)
        {
        %>
            var org_clause = build_clause_from_options(frm.org.options, "bg_org");
        <%
        }
        else
        {
        %>
            var org_clause = "";
        <%
        }
        %>
            var category_clause = build_clause_from_options(frm.category.options, "bg_category");
            var priority_clause = build_clause_from_options(frm.priority.options, "bg_priority");
            var status_clause = build_clause_from_options(frm.status.options, "bg_status");
            var udf_clause = "";

        <%
        if (this.ShowUdf)

        {
        %>
            udf_clause = build_clause_from_options(frm.udf.options, "bg_user_defined_attribute");
        <%
        }
        %>


            // SQL "LIKE" uses [, %, and _ in a special way

            like_string = frm.like.value.replace(/'/gi, "''");
            like_string = like_string.replace(/\[/gi, "[[]");
            like_string = like_string.replace(/%/gi, "[%]");
            like_string = like_string.replace(/_/gi, "[_]");

            like2_string = frm.like2.value.replace(/'/gi, "''");
            like2_string = like2_string.replace(/\[/gi, "[[]");
            like2_string = like2_string.replace(/%/gi, "[%]");
            like2_string = like2_string.replace(/_/gi, "[_]");

            // "    this line is only here to help unconfuse the syntax coloring in my editor

            var desc_clause = "";
            if (frm.like.value != "") {
                desc_clause = " bg_short_desc like";
                desc_clause += " N'%" + like_string + "%'\n";
            }

            var comments_clause = "";
            if (frm.like2.value != "") {
                comments_clause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and isnull(bp_comment_search,bp_comment) like";
                comments_clause += " N'%" + like2_string + "%'";
            <% if (Security.User.ExternalUser)
        { %>
                comments_clause += " and bp_hidden_from_external_users = 0";
            <% } %>
                comments_clause += ")\n";
            }

            var comments_since_clause = "";
            if (frm.comments_since.value != "") {
                comments_since_clause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and bp_date > '";
                comments_since_clause += frm.comments_since.value + "')\n";
            }

            var from_clause = "";
            if (frm.from_date.value != "") {
                from_clause = " bg_reported_date >= '" + frm.from_date.value + "'\n";
            }

            var to_clause = "";
            if (frm.to_date.value != "") {
                to_clause = " bg_reported_date <= '" + frm.to_date.value + " 23:59:59'\n";
            }

            var lu_from_clause = "";
            if (frm.lu_from_date.value != "") {
                lu_from_clause = " bg_last_updated_date >= '" + frm.lu_from_date.value + "'\n";
            }

            var lu_to_clause = "";
            if (frm.lu_to_date.value != "") {
                lu_to_clause = " bg_last_updated_date <= '" + frm.lu_to_date.value + " 23:59:59'\n";
            }

        <%
        // echo the custom input columns as the user types them
        var customCount = 1;
        foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
        {
            var columnName = (string)drcc["name"];
            if (Security.User.DictCustomFieldPermissionLevel[columnName] == SecurityPermissionLevel.PermissionNone)
            {
                continue;
            }

            var clause = "custom_clause_" + Convert.ToString(customCount++);
            var customColId = columnName.Replace(" ", "");
            var datatype = (string)drcc["datatype"];

            Response.Write("var " + clause + " = \"\";\n");
            Response.Write("el = document.getElementById('" + customColId + "')\n");

            if ((datatype == "varchar" || datatype == "nvarchar" || datatype == "char" || datatype == "nchar")
                && (string)drcc["dropdown type"] == "")
            {
                // my_text_field like '%val%'
                Response.Write("if (el.value != \"\")\n");
                Response.Write("{\n\t");
                Response.Write(clause + " = \" [" + columnName + "] like '%\" + el.value.replace(/'/gi,\"''\") + \"%'\\n\"\n");
                Response.Write("\twhere = build_where(where, " + clause + ");\n");
                Response.Write("}\n\n");
            }
            else if (datatype == "datetime")
            {
                Response.Write("if (el.value != \"\")\n");
                Response.Write("{\n\t");
                Response.Write(clause + " = \" [" + columnName + "] >=  '\" + format_from_date_for_db(el.value) + \"'\\n\"\n");
                Response.Write("\twhere = build_where(where, " + clause + ");\n");
                Response.Write("}\n\n");

                Response.Write("el = document.getElementById('to__" + customColId + "')\n");

                Response.Write("if (el.value != \"\")\n");
                Response.Write("{\n\t");
                Response.Write(clause + " = \" [" + columnName + "] <=  '\" + format_to_date_for_db(el.value) + \"'\\n\"\n");
                Response.Write("\twhere = build_where(where, " + clause + ");\n");
                Response.Write("}\n\n");
            }
            else
            {
                // my_field in (val1, val2, val3)
                Response.Write("vals = in_not_in_vals(el)\n");
                Response.Write("if (vals != \"\")\n");
                Response.Write("{\n\t");
                Response.Write(clause + " = \" [" + columnName + "] in \" + vals\n");
                Response.Write("\twhere = build_where(where, " + clause + ");\n");
                Response.Write("}\n\n");
            }
        }
        %>

            where = build_where(where, reported_by_clause);
            where = build_where(where, assigned_to_clause);
            where = build_where(where, project_clause);
            where = build_where(where, project_custom_dropdown1_clause);
            where = build_where(where, project_custom_dropdown2_clause);
            where = build_where(where, project_custom_dropdown3_clause);
            where = build_where(where, org_clause);
            where = build_where(where, category_clause);
            where = build_where(where, priority_clause);
            where = build_where(where, status_clause);
            where = build_where(where, desc_clause);
            where = build_where(where, comments_clause);
            where = build_where(where, comments_since_clause);
            where = build_where(where, from_clause);
            where = build_where(where, to_clause);
            where = build_where(where, lu_from_clause);
            where = build_where(where, lu_to_clause);
            where = build_where(where, udf_clause);

        <%
        var searchSql = ApplicationSettings.SearchSQL;

        if (searchSql == "")
        {
        %>
            var select = "select isnull(pr_background_color,'#ffffff') [color], bg_id [id]";
            select += ",\nbg_short_desc [desc]";

        <%
        if (this.UseFullNames)
        {
        %>
            select += ",\nisnull(rpt.us_lastname + ', ' + rpt.us_firstname,'') [reported by]";
        <%
        }
        else
        {
        %>
            select += ",\nisnull(rpt.us_username,'') [reported by]";
        <%
        }
        %>		
            select += ",\nbg_reported_date [reported on]";
        <%
        if (this.UseFullNames)
        {
        %>
            select += ",\nisnull(lu.us_lastname + ', ' + lu.us_firstname,'') [last updated by]";
        <%
        }
        else
        {
        %>
            select += ",\nisnull(lu.us_username,'') [last updated by]";
        <%
        }
        %>		
            select += ",\nbg_last_updated_date [last updated on]";

        <%
        if (Security.User.TagsFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(bg_tags,'') [tags]";
        <%
        }

        if (Security.User.ProjectFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(pj_name,'') [project]";
        <%
        }

        if (Security.User.OrgFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(og_name,'') [organization]";
        <%
        }

        if (Security.User.CategoryFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(ct_name,'') [category]";
        <%
        }

        if (Security.User.PriorityFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(pr_name,'') [priority]";
        <%
        }

        if (Security.User.AssignedToFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
            if (this.UseFullNames)
            {
        %>
            select += ",\nisnull(asg.us_lastname + ', ' + asg.us_firstname,'') [assigned to]";
        <%
        }
        else
        {
        %>
            select += ",\nisnull(asg.us_username,'') [assigned to]";
        <%
            }
        }

        if (Security.User.StatusFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
        %>
            select += ",\nisnull(st_name,'') [status]";
        <%
        }

        if (Security.User.UdfFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
        {
            if (this.ShowUdf)
            {
                var udfName = ApplicationSettings.UserDefinedBugAttributeName;
                Response.Write("select += \",\\nisnull(udf_name,'') [" + udfName + "]\"");
            }
        }

        // add the custom fields to the columns
        var userDropdownCnt = 1;
        foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
        {
            var columnName = (string)drcc["name"];
            if (Security.User.DictCustomFieldPermissionLevel[columnName] == SecurityPermissionLevel.PermissionNone)
            {
                continue;
            }

            if (Convert.ToString(drcc["dropdown type"]) == "users")
            {
                Response.Write("\nselect += \", \\nisnull(users"
                               + Convert.ToString(userDropdownCnt)
                               + ".us_username,'') ["
                               + columnName
                               + "]\"");
                userDropdownCnt++;
            }
            else
            {
                if (Convert.ToString(drcc["datatype"]) == "decimal")
                {
                    Response.Write("\nselect += \", \\nisnull(["
                                   + columnName
                                   + "],0) ["
                                   + columnName
                                   + "]\"");
                }
                else
                {
                    Response.Write("\nselect += \", \\nisnull(["
                                   + columnName
                                   + "],'') ["
                                   + columnName
                                   + "]\"");
                }
            }
        }

        Response.Write("\nselect += \"" + this.ProjectDropdownSelectCols + "\"");
        %>

            select += "\nfrom bugs\n";
            select += "left outer join users rpt on rpt.us_id = bg_reported_user\n";
            select += "left outer join users lu on lu.us_id = bg_last_updated_user\n";
            select += "left outer join users asg on asg.us_id = bg_assigned_to_user\n";
            select += "left outer join projects on pj_id = bg_project\n";
            select += "left outer join orgs on og_id = bg_org\n";
            select += "left outer join categories on ct_id = bg_category\n";
            select += "left outer join priorities on pr_id = bg_priority\n";
            select += "left outer join statuses on st_id = bg_status\n";

        <%
        // do the joins related to "user" dropdowns
        userDropdownCnt = 1;
        foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
        {
            var columnName = (string)drcc["name"];
            if (Security.User.DictCustomFieldPermissionLevel[columnName] == SecurityPermissionLevel.PermissionNone)
            {
                continue;
            }

            if (Convert.ToString(drcc["dropdown type"]) == "users")
            {
                Response.Write("select += \"left outer join users users");
                Response.Write(Convert.ToString(userDropdownCnt));
                Response.Write(" on users");
                Response.Write(Convert.ToString(userDropdownCnt));
                Response.Write(".us_id = bugs.");
                Response.Write("[");
                Response.Write(columnName);
                Response.Write("]\\n\"\n");
                userDropdownCnt++;
            }
        }

        if (this.ShowUdf)
        {
        %>
            select += "left outer join user_defined_attribute on udf_id = bg_user_defined_attribute\n";
        <%
        }
        %>

            frm.query.value = select + where + 'order by bg_id desc';
        <%
        } // else use sql from web config
        else
        {
        %>
            var search_sql = "<% Response.Write(searchSql.Replace("\r\n", "")); %>";
            search_sql = search_sql.replace(/\[br\]/g, "\n");
            frm.query.value = search_sql.replace(/\$WHERE\$/, where);
        <%
        }
        %>

            // I don't understand why this doesn't work in IE.   Did it used to work?
            document.getElementById("visible_sql_text").firstChild.nodeValue = frm.query.value;
            //document.getElementById("visible_sql_text").innerHTML = frm.query.value;
        }

        function set_hit_submit_button() {
            document.getElementById(asp_form_id).hit_submit_button.value = "1";
        }


        var shown = true;

        function showhide_form() {
            var frm = document.getElementById("<% Response.Write(ApplicationSettings.AspNetFormId); %>");
            if (shown) {
                frm.style.display = "none";
                shown = false;
                showhide.firstChild.nodeValue = "show form";
            } else {
                frm.style.display = "block";
                shown = true;
                showhide.firstChild.nodeValue = "hide form";
            }
        }

        function set_project_changed() {
            on_change();
            document.getElementById(asp_form_id).project_changed.value = "1";
        }


        $(document).ready(do_doc_ready);


        function show_calendar(el) {
            $("#" + el).datepicker("show");
        }

        function do_doc_ready() {
            date_format = '<% Response.Write(ApplicationSettings.DatepickerDateFormat); %>';
            $('.date').datepicker({ dateFormat: date_format, duration: 'fast' });
            $('.date').change(on_change);
            $('.filter').click(on_invert_filter);
            $('.filter_selected').click(on_invert_filter);
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div id="suggest_popup" style="display: none; position: absolute; z-index: 1000;"></div>

    <div class="main">

        <% if (!Security.User.AddsNotAllowed)
            { %>
        <a href="<%= ResolveUrl("~/Bugs/Edit.aspx") %>">
            <img src="<%= ResolveUrl("~/Content/images/add.png") %>" border="0" align="top">&nbsp;add new <% Response.Write(ApplicationSettings.SingularBugLabel); %></a>
        <% } %>

        <a style="margin-left: 40px;" href="javascript:showhide_form()" id="showhide">hide form</a>

        <table border="0">
            <tr>
                <td>
                <tr>
                    <td>
                        <div id="searchfrom">
                            <form class="frm" action="Search.aspx" method="POST" runat="server" onmouseover="hide_suggest()">

                                <table border="0" cellpadding="6" cellspacing="0">
                                    <tr>
                                        <td colspan="10">
                                            <span class="smallnote">Hold down Ctrl key to select multiple items.</span>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td nowrap>
                                            <span class="lbl">reported by:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="reported_by" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="category_label" runat="server">category:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="category" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="priority_label" runat="server">priority:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="priority" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="assigned_to_label" runat="server">assigned to:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="assigned_to" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="status_label" runat="server">status:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="status" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>
                                    </tr>

                                </table>
                                <table border="0" cellpadding="3" cellspacing="0">
                                    <tr>

                                        <td nowrap>
                                            <span class="lbl" id="org_label" runat="server">organization:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="org" runat="server" onchange="on_change()"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="project_label" runat="server">project:</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="project" runat="server" onchange="set_project_changed()"
                                                AutoPostBack="true"></asp:ListBox>
                                        </td>

                                        <td nowrap>
                                            <span class="lbl" id="project_custom_dropdown1_label" runat="server" style="display: none">?</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="project_custom_dropdown1" runat="server" Style="display: none" onchange="on_change()"></asp:ListBox>
                                        </td>
                                        <td nowrap>
                                            <span class="lbl" id="project_custom_dropdown2_label" runat="server" style="display: none">?</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="project_custom_dropdown2" runat="server" Style="display: none" onchange="on_change()"></asp:ListBox>
                                        </td>
                                        <td nowrap>
                                            <span class="lbl" id="project_custom_dropdown3_label" runat="server" style="display: none">?</span><br>
                                            <asp:ListBox Rows="6" SelectionMode="Multiple" ID="project_custom_dropdown3" runat="server" Style="display: none" onchange="on_change()"></asp:ListBox>
                                        </td>
                                    </tr>

                                </table>
                                <br>
                                <table border="0" cellpadding="3" cellspacing="0">
                                    <tr>
                                        <td><span class="lbl"><% Response.Write(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)); %> description contains:&nbsp;</span>
                                        <td colspan="2">
                                        <input type="text" class="txt" id="like" runat="server" onkeydown="search_criteria_onkeydown(this,event)" onkeyup="search_criteria_onkeyup(this,event)" size="50" autocomplete="off"/>


                                            <% if (this.ShowUdf)
                                                {
                                            %>
                                        <td nowrap rowspan="2">
                                            <span class="lbl" id="udf_label" runat="server"><% Response.Write(ApplicationSettings.UserDefinedBugAttributeName); %></span><br>
                                            <asp:ListBox Rows="4" SelectionMode="Multiple" ID="udf" runat="server" onchange="on_change()"></asp:ListBox>

                                            <%
                                                }
                                            %>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td><span class="lbl"><% Response.Write(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)); %> comments contain:&nbsp;</span>
                                        <td colspan="2">
                                            <input type="text" class="txt" id="like2" runat="server" onkeyup="on_change()" size="50" autocomplete="off"/>
                                        </td>
                                    </tr>


                                    <tr>
                                        <td nowrap><span class="lbl"><% Response.Write(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)); %> comments since:&nbsp;</span>
                                        <td colspan="2">
                                            <input type="text" class="txt date" id="comments_since" runat="server" onkeyup="on_change()" size="10"/>
                                            <a style="font-size: 8pt;"
                                                href="javascript:show_calendar('comments_since')">[select]
                                            </a>
                                        </td>
                                    </tr>


                                    <tr>
                                        <td nowrap><span class="lbl">"Reported on" from date:&nbsp;</span>
                                        <td colspan="2">
                                            <input runat="server" type="text" class="txt date" id="from_date" maxlength="10" size="10" onchange="on_change()"/>
                                            <a style="font-size: 8pt;"
                                                href="javascript:show_calendar('from_date')">[select]
                                            </a>

                                            &nbsp;&nbsp;&nbsp;&nbsp;
        <span class="lbl">to:&nbsp;</span>
                                            <input runat="server" type="text" class="txt date" id="to_date" maxlength="10" size="10" onchange="on_change()"/>
                                            <a style="font-size: 8pt;"
                                                href="javascript:show_calendar('to_date')">[select]
                                            </a>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td nowrap><span class="lbl">"Last updated on" from date:&nbsp;</span>
                                        <td colspan="2">
                                            <input runat="server" type="text" class="txt date" id="lu_from_date" maxlength="10" size="10" onchange="on_change()"/>
                                            <a style="font-size: 8pt;"
                                                href="javascript:show_calendar('lu_from_date')">[select]
                                            </a>

                                            &nbsp;&nbsp;&nbsp;&nbsp;
        <span class="lbl">to:&nbsp;</span>
                                            <input runat="server" type="text" class="txt date" id="lu_to_date" maxlength="10" size="10" onchange="on_change()"/>
                                            <a style="font-size: 8pt;"
                                                href="javascript:show_calendar('lu_to_date')">[select]
                                            </a>
                                        </td>
                                    </tr>


                                    <%

                                        var minTextAreaSize = ApplicationSettings.TextAreaThreshold;
                                        var maxTextAreaRows = ApplicationSettings.MaxTextAreaRows;

                                        // Create the custom column INPUT elements
                                        foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
                                        {
                                            var columnName = (string)drcc["name"];
                                            if (Security.User.DictCustomFieldPermissionLevel[columnName] == SecurityPermissionLevel.PermissionNone)
                                            {
                                                continue;
                                            }

                                            var fieldId = columnName.Replace(" ", "");
                                            var datatype = drcc["datatype"].ToString();
                                            var dropdownType = Convert.ToString(drcc["dropdown type"]);

                                            Response.Write("<tr>");
                                            Response.Write("<td><span class=lbl id=\"" + fieldId + "_label\">");
                                            Response.Write(columnName);

                                            if ((datatype == "nvarchar" || datatype == "varchar" || datatype == "char" || datatype == "nchar")
                                                && dropdownType == "")
                                            {
                                                Response.Write(" contains");
                                            }

                                            Response.Write(":&nbsp;</span>");

                                            Response.Write("<td colspan=3>");

                                            var fieldLength = int.Parse(drcc["length"].ToString());

                                            var dropdownOptions = Convert.ToString(drcc["vals"]);

                                            if (dropdownType != "" || dropdownOptions != "")
                                            {
                                                // create dropdown here

                                                Response.Write("<select multiple=multiple size=3 onchange='on_change()' ");

                                                Response.Write(" id=\"" + fieldId + "\"");
                                                Response.Write(" name=\"" + columnName + "\"");
                                                Response.Write(">");

                                                var selectedVals = Request[columnName];
                                                if (selectedVals == null)
                                                {
                                                    selectedVals = "$Q6Q6Q6$"; // the point here is, don't select anything in the dropdowns
                                                }
                                                var selectedValsArray = Util.SplitStringUsingCommas(selectedVals);

                                                if (dropdownType != "users")
                                                {
                                                    var options = Util.SplitDropdownVals(dropdownOptions);
                                                    for (var j = 0; j < options.Length; j++)
                                                    {
                                                        Response.Write("<option ");

                                                        // reselect vals
                                                        for (var k = 0; k < selectedValsArray.Length; k++)
                                                        {
                                                            if (options[j] == selectedValsArray[k])
                                                            {
                                                                Response.Write(" selected ");
                                                                break;
                                                            }
                                                        }

                                                        Response.Write(">");
                                                        Response.Write(options[j]);
                                                        Response.Write("</option>");
                                                    }
                                                }
                                                else
                                                {
                                                    var dvUsers = new DataView(this.DtUsers);
                                                    foreach (DataRowView drv in dvUsers)
                                                    {
                                                        var userId = Convert.ToString(drv[0]);
                                                        var userName = Convert.ToString(drv[1]);

                                                        Response.Write("<option value=");
                                                        Response.Write(userId);

                                                        // reselect vals
                                                        for (var k = 0; k < selectedValsArray.Length; k++)
                                                        {
                                                            if (userId == selectedValsArray[k])
                                                            {
                                                                Response.Write(" selected ");
                                                                break;
                                                            }
                                                        }

                                                        Response.Write(">");
                                                        Response.Write(userName);
                                                        Response.Write("</option>");
                                                    }
                                                }

                                                Response.Write("</select>");
                                            }
                                            else
                                            {
                                                if (datatype == "datetime")
                                                {
                                                    write_custom_date_controls(columnName);
                                                }
                                                else
                                                {
                                                    Response.Write("<input type=text class=txt");
                                                    Response.Write("  onkeyup=\"on_change()\" ");

                                                    // match the size of the text field to the size of the database field

                                                    var size = Convert.ToInt32(drcc["length"]);

                                                    // adjust the size
                                                    if (size > 60)
                                                    {
                                                        size = 60;
                                                    }
                                                    else if (datatype == "int" || datatype == "decimal")
                                                    {
                                                        size = 30;
                                                    }

                                                    var sizeString = Convert.ToString(size);

                                                    Response.Write(" size=" + sizeString);
                                                    Response.Write(" maxlength=" + sizeString);

                                                    Response.Write(" name=\"" + columnName + "\"");
                                                    Response.Write(" id=\"" + fieldId + "\"");

                                                    Response.Write(" value=\"");
                                                    if (Request[columnName] != "")
                                                    {
                                                        Response.Write(HttpUtility.HtmlEncode(Request[columnName]));
                                                    }
                                                    Response.Write("\"");
                                                    Response.Write(">");

                                                    if ((datatype == "nvarchar" || datatype == "varchar" || datatype == "char" || datatype == "nchar")
                                                        && dropdownType == "")
                                                    {
                                                        //
                                                    }
                                                    else
                                                    {
                                                        Response.Write("&nbsp;&nbsp;<span class=smallnote>Enter multiple values using commas, no spaces: 1,2,3</span>");
                                                    }
                                                }
                                            }
                                        }
                                    %>

                                    <tr>
                                        <td colspan="10" nowrap>Use "and" logic:<input type="radio" runat="server" name="and_or" value="and" id="and" onchange="on_change()" checked/>
                                            &nbsp;&nbsp;
                                            Use "or" logic:<input type="radio" runat="server" name="and_or" value="or" id="or" onchange="on_change()"/>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td colspan="10" align="center">
                                            <input type="hidden" runat="server" id="project_changed" value="0"/>
                                            <input type="hidden" runat="server" id="hit_submit_button" value="0"/>
                                            <input type="hidden" runat="server" id="hit_save_query_button" value="0"/>
                                            <input class="btn" type="submit" onclick="set_hit_submit_button()" value="&nbsp;&nbsp;&nbsp;Search&nbsp;&nbsp;&nbsp;" runat="server"/>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td colspan="10" align="right">
                                            <script>
                                                function on_save_query() {
                                                    var frm2 = document.getElementById("save_query_form");
                                                    frm2.sql_text.value =
                                                        document.getElementById("visible_sql_text").innerHTML;
                                                    frm2.submit();
                                                }
                                            </script>
                                            <% if (Security.User.IsGuest) /* can't save search */
                                                { %>
                                            <span style="color: Gray; font-size: 7pt;">Save Search not available to "guest" user</span>
                                            <% }
                                                else
                                                { %>
                                            <a href="javascript:on_save_query()">Save search criteria as query</a>
                                            <% } %>
                                        </td>
                                    </tr>

                                </table>

                            <input type="hidden" name="new_page" id="new_page" runat="server" value="0"/>
                            <input type="hidden" name="actn" id="actn" runat="server" value=""/>
                            <input type="hidden" name="filter" id="filter" runat="server" value=""/>
                            <input type="hidden" name="sort" id="sort" runat="server" value="-1"/>
                            <input type="hidden" name="prev_sort" id="prev_sort" runat="server" value="-1"/>
                            <input type="hidden" name="prev_dir" id="prev_dir" runat="server" value="ASC"/>
                            <input type="hidden" name="tags" id="tags" value=""/>

                                <script>
                                    var enable_popups = <% Response.Write(Security.User.EnablePopups ? "1" : "0"); %>;
                                    var asp_form_id = '<% Response.Write(ApplicationSettings.AspNetFormId); %>';
                                </script>

                                <div id="popup" class="buglist_popup"></div>

                            <input type="hidden" id="query" runat="server" value=""/>
                            </form>
                        </div>

                    </td>
                </tr>
        </table>
    </div>

    <%
        if (this.Dv == null)
        {
        }
        else
        {
            if (this.Dv.Table.Rows.Count > 0)
            {
                Response.Write("<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + ">print list</a>");
                Response.Write("&nbsp;&nbsp;&nbsp;<a target=_blank href=" + ResolveUrl("~/Bugs/Print2.aspx") + ">print detail</a>");
                Response.Write("&nbsp;&nbsp;&nbsp;<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + "?format=excel>export to excel</a><br>");

                if (ApplicationSettings.EnableTags)
                {
                    BugList.DisplayBugListTagsLine(Response, Security);
                }

                if (!Security.User.IsGuest && (Security.User.IsAdmin || Security.User.CanMassEditBugs))
                {
                    Response.Write("<form id=massform onsubmit='return validate_mass()' method=get action=MassEdit.aspx>");
                    display_bugs(true, Security);
                    Response.Write("<p><table class=frm><tr><td colspan=5 class=smallnote>Update or delete all checked items");
                    Response.Write("<tr><td colspan=5>");
                    Response.Write("<a href=javascript:select_all(true)>select all</a>&nbsp;&nbsp;&nbsp;&nbsp;");
                    Response.Write("<a href=javascript:select_all(false)>deselect all</a>");
                    Response.Write("<tr>");
                    Response.Write("<td><span class=lbl>project:</span><br><select name=mass_project id=mass_project></select>");
                    Response.Write("<td><span class=lbl>organization:</span><br><select name=mass_org id=mass_org></select>");
                    Response.Write("<td><span class=lbl>category:</span><br><select name=mass_category id=mass_category></select>");
                    Response.Write("<td><span class=lbl>priority:</span><br><select name=mass_priority id=mass_priority></select>");
                    Response.Write("<td><span class=lbl>assigned to:</span><br><select name=mass_assigned_to id=mass_assigned_to></select>");
                    Response.Write("<td><span class=lbl>status:</span><br><select name=mass_status id=mass_status></select>");
                    Response.Write("<td><span class=lbl>reported by:</span><br><select name=mass_reported_by id=mass_reported_by></select>");
                    Response.Write("<tr><td colspan=5>OR DELETE:&nbsp;<input type=checkbox class=cb name=mass_delete>");
                    Response.Write("<tr><td colspan=5 align=center><input type=submit value='Update/Delete All'>");
                    Response.Write("</table></form><p><script>load_massedit_selects()</script>");
                }
                else
                {
                    // no checkboxes
                    display_bugs(false, Security);
                }
            }
            else
            {
                Response.Write("<p>No ");
                Response.Write(ApplicationSettings.PluralBugLabel);
                Response.Write("<p>");
            }
        }
    %>

    <p>
        <span id="visible_sql_label" runat="server">SQL:</span>
    </p>
    <pre style="font-family: courier new; font-size: 8pt" id="visible_sql_text" runat="server">&nbsp;</pre>

    <!-- form 3 -->
    <form id="save_query_form" target="_blank" method="post" action="<%= ResolveUrl("~/Queries/Edit.aspx") %>">
        <input type="hidden" name="sql_text" value="">
    </form>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>
