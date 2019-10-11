<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="BugTracker.Web.Administration.Priorities.Edit" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <%--TODO <body onload="change_sample_color()">--%>

    <script>
        function change_sample_color() {
            var sample = document.getElementById("sample");
            var color = document.getElementById("color");

            try {
                sample.style.background = color.value;
            } catch (e) {
            }
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <div class="main">
        <a href="<%= ResolveUrl("~/Admin/Priorities/List.aspx")%>">back to priorities</a>
        <div>&nbsp;</div>

        <table style="border-collapse: collapse; border: 0;">
            <tr>
                <td>
                    <form class="frm" runat="server">
                        <table border="0">

                            <tr>
                                <td class="lbl">Description:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="name" maxlength="20" size="20" />
                                </td>
                                <td runat="server" class="err" id="name_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="3">
                                    <span class="smallnote">Sort Sequence controls the sort order in the dropdowns.</span>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Sort Sequence:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="sortSeq" maxlength="2" size="2" />
                                </td>
                                <td runat="server" class="err" id="sortSeqErr">&nbsp;</td>
                            </tr>

                            <tr>
                                <td colspan="3">
                                    <span class="smallnote">Background Color and CSS Class can be used to control the look of lists.<br>
                                        See the example queries.</span>
                                </td>
                            </tr>

                            <tr>
                                <td class="lbl">Background Color:</td>
                                <td>
                                    <input onkeyup="change_sample_color()" runat="server" type="text" class="txt" id="color" value="#ffffff" maxlength="7" size="7" />
                                    &nbsp;&nbsp;&nbsp;&nbsp;<span style="padding: 3px;" id="sample">&nbsp;&nbsp;Sample&nbsp;&nbsp;</span>
                                </td>
                                <td runat="server" class="err" id="color_err">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">CSS Class:</td>
                                <td>
                                    <input runat="server" type="text" class="txt" id="style" value="" maxlength="10" size="10" />
                                    &nbsp;&nbsp;<a target="_blank" href="<%= ResolveUrl("~/Admin/EditStyles.aspx")%>">more CSS info...</a>
                                </td>
                                <td runat="server" class="err" id="styleErr">&nbsp;</td>
                            </tr>

                            <tr>
                                <td class="lbl">Default Selection:</td>
                                <td>
                                    <asp:CheckBox runat="server" class="cb" ID="defaultSelection" />
                                </td>
                                <td>&nbsp</td>
                            </tr>

                            <tr>
                                <td colspan="3" align="left">
                                    <span runat="server" class="err" id="msg">&nbsp;</span>
                                </td>
                            </tr>

                            <tr>
                                <td colspan="2" align="center">
                                    <input runat="server" class="btn" type="submit" id="sub" value="Create or Edit" />
                                </td>
                                <td>&nbsp;</td>
                            </tr>
                        </table>
                    </form>
                </td>
            </tr>
        </table>
    </div>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
    <% Response.Write(Application["custom_footer"]); %>
</asp:Content>
