<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="MainMenu.ascx.cs" Inherits="BugTracker.Web.Core.Controls.MainMenu" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<%--// topmost visible HTML--%>
<%= Util.Context.Application["custom_header"] %>

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
        <%= Util.Context.Application["custom_logo"] %>
        <td width="20">&nbsp;</td>

        <td class="menu_td">
            <a href="<%= ResolveUrl("~/Bugs/List.aspx") %>"><span class="<%= SelectedItem == Util.GetSetting("PluralBugLabel", "bugs") ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;"><%= Util.GetSetting("PluralBugLabel", "bugs") %></span></a>
        </td>

        <% if (Security.User.CanSearch)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Search.aspx") %>"><span class="<%= SelectedItem == "search" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">search</span></a>
            </td>
            
        <% } %>
        
        <% if (Util.GetSetting("EnableWhatsNewPage", "0") == "1")
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/ViewWhatsNew.aspx") %>"><span class="<%= SelectedItem == "news" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">news</span></a>
            </td>
            
        <% } %>
        
        <% if (!Security.User.IsGuest)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Queries/List.aspx") %>"><span class="<%= SelectedItem == "queries" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">queries</span></a>
            </td>
            
        <% } %>

        <% if (Security.User.IsAdmin || Security.User.CanUseReports || Security.User.CanEditReports)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Reports/List.aspx") %>"><span class="<%= SelectedItem == "reports" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">reports</span></a>
            </td>
            
        <% } %>
        
        <% if (Util.GetSetting("CustomMenuLinkLabel", string.Empty) != string.Empty)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl(Util.GetSetting("CustomMenuLinkUrl", string.Empty)) %>"><span class="<%= SelectedItem == Util.GetSetting("CustomMenuLinkLabel", "") ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;"><%= Util.GetSetting("CustomMenuLinkLabel", string.Empty) %></span></a>
            </td>
            
        <% } %>
        
        <% if (Security.User.IsAdmin)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Administration/Home.aspx") %>"><span class="<%= SelectedItem == "admin" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">admin</span></a>
            </td>
            
        <% } else if(Security.User.IsProjectAdmin)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/Administration/Users/List.aspx") %>"><span class="<%= SelectedItem == "users" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">users</span></a>
            </td>
        <% } %>
        
        <td nowrap valign="middle">
            <form style="margin: 0; padding: 0;" action="<%= ResolveUrl("~/Bugs/Edit.aspx?id=") %>" method="get">
                <input class="menubtn" type="submit" value="go to ID">
                <input class="menuinput txt" size="4" type="text" name="id" accesskey="g">
            </form>
        </td>

        <% if (Util.GetSetting("EnableLucene", "1") == "1" && Security.User.CanSearch)
            {
                var query = (string)HttpContext.Current.Session["query"] ?? string.Empty;
        %>
            <td nowrap valign="middle">
                <form style="margin: 0; padding: 0;" action="<%= ResolveUrl("~/SearchText.aspx") %>" method="get" onsubmit="return on_submit_search()">
                    <input class="menubtn" type="submit" value="search text">
                    <input class="menuinput txt" id="lucene_input" size="24" type="text" value='<%= query.Replace("'", "")%>' name="query" accesskey="s">
                    <a href="<%= ResolveUrl("~/Content/lucene_syntax.html") %>" target="_blank" style="font-size: 7pt;">advanced</a>
                </form>
            </td>
            
        <% } %>
        
        <td nowrap valign="middle">
            <% if (Security.User.IsGuest && Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
               { %>
                <span class="smallnote">using as<br><%= Security.User.Username %></span>
                
            <% } else
               { %>
                <span class="smallnote">logged in as<br><%= Security.User.Username %></span>
            <% } %>
        </td>

        <% if (Security.AuthMethod == "plain")
           {
               if (Security.User.IsGuest && Util.GetSetting("AllowGuestWithoutLogin", "0") == "1")
               { %>
                    <td class="menu_td">
                        <a href="<%= ResolveUrl("~/Home.aspx") %>"><span class="<%= SelectedItem == "login" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">login</span></a>
                    </td>
            <% }
               else
               { %>
                    <td class="menu_td">
                        <a href="<%= ResolveUrl("~/Logoff.aspx") %>"><span class="<%= SelectedItem == "logoff" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">logoff</span></a>
                    </td>
            <% }
           } %>

        <%--// for guest account, suppress display of "edit_self--%>
        <% if (!Security.User.IsGuest)
           { %>
            <td class="menu_td">
                <a href="<%= ResolveUrl("~/EditSelf.aspx") %>"><span class="<%= SelectedItem == "settings" ? "selected_menu_item" : "menu_item" %> warn" style="margin-left:3px;">settings</span></a>
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
<br>
