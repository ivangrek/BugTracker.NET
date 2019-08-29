<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="add_attachment.aspx.cs" Inherits="BugTracker.Web.add_attachment" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script>
        function set_msg(s) {
            document.getElementById("msg").innerHTML = s;
            //document.getElementById("file_input").innerHTML =
            //    '<input type=file class=txt name="attached_file" id="attached_file" maxlength=255 size=60>';
        }

        function waiting() {
            document.getElementById("msg").innerHTML = "Uploading...";
            return true;
        }
    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <iframe name="hiddenframe" style="display: none">x</iframe>

    <div class="align">
        Add attachment to <% Response.Write(Convert.ToString(this.bugid)); %>
        <p>
            <table border="0">
                <tr>
                    <td>
                        <form target="hiddenframe" class="frm" runat="server" enctype="multipart/form-data" onsubmit="return waiting()">
                            <table border="0">

                                <tr>
                                    <td class="lbl">Description:</td>
                                    <td>
                                        <input runat="server" type="text" class="txt" id="desc" maxlength="80" size="80" />
                                    </td>
                                    <td runat="server" class="err" id="desc_err">&nbsp;</td>
                                </tr>

                                <tr>
                                    <td class="lbl">File:</td>
                                    <td>
                                        <div id="file_input">
                                            <input runat="server" type="file" class="txt" id="attached_file" maxlength="255" size="60" />
                                        </div>
                                    </td>
                                    <td runat="server" class="err" id="attached_file_err">&nbsp;</td>
                                </tr>

                                <tr>
                                    <td colspan="3">
                                        <asp:CheckBox runat="server" class="cb" ID="internal_only" />
                                        <span runat="server" id="internal_only_label">Visible to internal users only</span>
                                    </td>
                                </tr>

                                <tr>
                                    <td colspan="3" align="left">
                                        <span runat="server" class="err" id="msg">&nbsp;</span>
                                    </td>
                                </tr>

                                <tr>
                                    <td colspan="2" align="center">
                                        <input runat="server" class="btn" type="submit" id="sub" value="Upload" />
                                    </td>
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
