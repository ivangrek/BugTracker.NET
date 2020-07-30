<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MainMenu.ascx.cs" Inherits="BugTracker.Web.Core.Controls.MainMenu" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<%--// topmost visible HTML--%>
<%= BugTracker.Web.Core.Util.CustomHeaderHtml %>

<span id="debug" style="position: absolute; top: 0; left: 0;"></span>
<script>
    function dbg(s) {
        document.getElementById('debug').innerHTML += (s + '<br>');
    }

    function on_submit_search() {
        var el = document.getElementById('lucene_input');

        if (el.value == '') {
            alert('Enter the words you are search for.');
            el.focus();

            return false;
        }
        else {
            return true;
        }
    }
</script>

<table border="0" width="100%" cellpadding="0" cellspacing="0" class="menubar">
    <tr>
        <%= BugTracker.Web.Core.Util.CustomLogoHtml %>
        <td width="20">&nbsp;</td>

        <td class="menu_td">
            <a href="<%= ResolveUrl("~/Bug") %>"><span class="<%= SelectedItem == ApplicationSettings.PluralBugLabel ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;"><%= ApplicationSettings.PluralBugLabel %></span></a>
        </td>

        <% if (Security.User.CanSearch)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Search") %>"><span class="<%= SelectedItem == "search" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">search</span></a>
            </td>
            
        <% } %>
        
        <% if (ApplicationSettings.EnableWhatsNewPage)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/News") %>"><span class="<%= SelectedItem == "news" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">news</span></a>
            </td>
            
        <% } %>
        
        <% if (!Security.User.IsGuest)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Query") %>"><span class="<%= SelectedItem == "queries" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">queries</span></a>
            </td>
            
        <% } %>

        <% if (Security.User.IsAdmin || Security.User.CanUseReports || Security.User.CanEditReports)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Report") %>"><span class="<%= SelectedItem == "reports" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">reports</span></a>
            </td>
            
        <% } %>
        
        <% if (ApplicationSettings.CustomMenuLinkLabel != string.Empty)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl(ApplicationSettings.CustomMenuLinkUrl) %>"><span class="<%= SelectedItem == ApplicationSettings.CustomMenuLinkLabel ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;"><%= ApplicationSettings.CustomMenuLinkLabel %></span></a>
            </td>
            
        <% } %>
        
        <% if (Security.User.IsAdmin)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Administration") %>"><span class="<%= SelectedItem == "admin" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">admin</span></a>
            </td>
            
        <% } else if(Security.User.IsProjectAdmin)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Admin/Users/List.aspx") %>"><span class="<%= SelectedItem == "users" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">users</span></a>
            </td>
        <% } %>
        
        <td nowrap valign="middle">
            <form style="margin: 0; padding: 0;" action="<%= ResolveUrl("~/Bugs/Edit.aspx?id=") %>" method="get">
                <input class="menubtn" type="submit" value="go to ID">
                <input class="menuinput txt" size="4" type="text" name="id" accesskey="g">
            </form>
        </td>

        <% if (ApplicationSettings.EnableLucene && Security.User.CanSearch)
            {
                var query = (string)HttpContext.Current.Session["query"] ?? string.Empty;
        %>
            <td nowrap valign="middle">
                <form style="margin: 0; padding: 0;" action="<%= ResolveUrl("~/Search/SearchText") %>" method="get" onsubmit="return on_submit_search()">
                    <input class="menubtn" type="submit" value="search text">
                    <input class="menuinput txt" id="lucene_input" size="24" type="text" value='<%= query.Replace("'", "")%>' name="query" accesskey="s">
                    <a href="<%= ResolveUrl("~/Content/lucene_syntax.html") %>" target="_blank" style="font-size: 7pt;">advanced</a>
                </form>
            </td>
            
        <% } %>
        
        <td nowrap valign="middle">
            <% if (Security.User.IsGuest && ApplicationSettings.AllowGuestWithoutLogin)
               { %>
                <span class="smallnote">using as<br><%= Security.User.Username %></span>
                
            <% } else
               { %>
                <span class="smallnote">logged in as<br><%= Security.User.Username %></span>
            <% } %>
        </td>

        <% if (Security.AuthMethod == "plain")
           {
               if (Security.User.IsGuest && ApplicationSettings.AllowGuestWithoutLogin)
               { %>
                    <td class="menu_td">
                        <a href="<%= ResolveUrl("~/Account/Login") %>"><span class="<%= SelectedItem == "login" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">login</span></a>
                    </td>
            <% }
               else
               { %>
                    <td class="menu_td">
                        <a href="<%= ResolveUrl("~/Account/Logoff") %>"><span class="<%= SelectedItem == "logoff" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">logoff</span></a>
                    </td>
            <% }
           } %>

        <%--// for guest account, suppress display of "edit_self--%>
        <% if (!Security.User.IsGuest)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Account/Settings") %>"><span class="<%= SelectedItem == "settings" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">settings</span></a>
            </td>
            
        <% } %>

        <td valign="middle" align="left">
            <a target="_blank" href="<%= ResolveUrl("~/Content/about.html") %>"><span class="menu_item" style="margin-left: 3px;">about</span></a>
        </td>

        <td nowrap valign="middle">
            <a target="_blank" href="http://ifdefined.com/README.html"><span class="menu_item" style="margin-left: 3px;">help</span></a>
        </td>
    </tr>
</table>
