<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Notifications.aspx.cs" Inherits="BugTracker.Web.Administration.Notifications" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script type="text/javascript" src="<%= ResolveUrl("~/Scripts/sortable.js")%>"></script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div style="width: 600px;"
        class="smallnote">
        Email notifications are put into a table into the database and then the system attempts to send them.
        If the system fails to send the notification, it records the reason for the failure with the row.
        <br>
        <br>
        The system makes 3 attempts to send the notification. After the 3rd attempt,
        you can either give up and delete the unsent notifications
        or you can reset the retry count and let the system continue trying.

    </div>

    <p>
        <div class="align">
            <a href="EditQueuedNotifications.aspx?actn=delete&ses=" <% Response.Write(this.Ses); %>>Delete unsent notifications</a>
            <br>
            <br>
            <a href="EditQueuedNotifications.aspx?actn=reset&ses=" <% Response.Write(this.Ses); %>>Reset retry count to zero</a>
            <br>
            <br>
            <a href="EditQueuedNotifications.aspx?actn=resend&ses=" <% Response.Write(this.Ses); %>>Try to resend</a>
            <br>
            <br>

            <%

                if (this.Ds.Tables[0].Rows.Count > 0)
                    SortableHtmlTable.CreateFromDataSet(
                        Response, this.Ds, "", "");
                else
                    Response.Write("No queued email notifications in the database.");
            %>
        </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>
