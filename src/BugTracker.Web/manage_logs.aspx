<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" ValidateRequest="false" EnableEventValidation="false" AutoEventWireup="true" CodeBehind="manage_logs.aspx.cs" Inherits="BugTracker.Web.manage_logs" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <% this.security.write_menu(Response, "admin"); %>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="align">
        <table border="0">
            <tr>
                <td>
                    <form runat="server">
                        <asp:DataGrid ID="MyDataGrid" runat="server" BorderColor="black" CssClass="datat"
                            CellPadding="3" AutoGenerateColumns="false" OnItemCommand="my_button_click">
                            <HeaderStyle CssClass="datah"></HeaderStyle>
                            <ItemStyle CssClass="datad"></ItemStyle>
                            <Columns>
                                <asp:BoundColumn HeaderText="File" DataField="file" />
                                <asp:HyperLinkColumn HeaderText="Download" Text="Download" DataNavigateUrlField="url"
                                    Target="_blank" />
                                <asp:ButtonColumn HeaderText="Delete" ButtonType="LinkButton" Text="Delete" CommandName="dlt" />
                            </Columns>
                        </asp:DataGrid>
                        <div class="err" id="msg" runat="server">&nbsp;</div>

                    </form>
        </table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>
