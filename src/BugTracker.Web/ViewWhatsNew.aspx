<%--
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
--%>

<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ViewWhatsNew.aspx.cs" Inherits="BugTracker.Web.ViewWhatsNew" MasterPageFile="~/Site.Master" ClientIDMode="Static" %>
<%@ Register TagPrefix="BugTracker" TagName="MainMenu" Src="~/Core/Controls/MainMenu.ascx" %>
<%@ Import Namespace="BugTracker.Web.Core" %>

<asp:Content ContentPlaceHolderID="Head" runat="server">
    <script>

        var json = {};
        var since = 0;
        var table_start =
            "<table border=1 class=datat><tr><td class=datah>when<td class=datah>id<td class=datah>desc<td class=datah>action<td class=datah>user";
        var table_end = "</table>";
        var seconds_in_a_day = 86400;
        var seconds_in_an_hour = 3600;
        var my_news_list = new Array();

        function how_long_ago(seconds) {

            // turn seconds ago into a friendly piece of text

            var days = Math.floor(seconds / seconds_in_a_day);
            seconds -= days * seconds_in_a_day;
            var hours = Math.floor(seconds / seconds_in_an_hour);
            seconds -= hours * seconds_in_an_hour;
            var minutes = Math.floor(seconds / 60);
            seconds -= minutes * 60;

            if (days > 0) {
                if (days == 1) {
                    if (hours > 2) {
                        return "1 day and " + hours + " hours ago";
                    } else {
                        return "1 day ago";
                    }
                } else {
                    return days + " days ago";
                }
            } else if (hours > 0) {
                if (hours == 1) {
                    if (minutes > 5) {
                        return "1 hour and " + minutes + " minutes ago";
                    } else {
                        return "1 hour ago";
                    }
                } else {
                    return hours + " hours ago";
                }
            } else if (minutes > 0) {
                if (minutes == 1) {
                    return "1 minute ago";
                } else {
                    return minutes + " minutes ago";
                }
            } else {
                return seconds + " seconds ago";
            }
        }


        function get_color(secondsAgo) {
            if (secondsAgo < 90) {
                return "red";
            } else if (secondsAgo < 180) {
                return "orange";
            } else if (secondsAgo < 300) {
                return "yellow";
            } else {
                return "white";
            }
        }

        function process_json(json) {

            for (i = 0; i < json.news_list.length; i++) {

                var news = json.news_list[i];

                my_news_list.push(news);

                if (news.seconds > since) {
                    since = news.seconds;
                }

            }

            // iterate backwards through all the news retrieved, updating the "how long ago"

            var tableRows = "";

            for (i = my_news_list.length - 1; i > -1; i--) {

                news = my_news_list[i];

                var secondsAgo = json.now - news.seconds;

                var tr = "<tr><td class=datad style='background:" +
                    get_color(secondsAgo) +
                    "'>" +
                    how_long_ago(secondsAgo) +
                    "<td class=datad>" +
                    news.bugid +
                    "<td class=datad><a href=<%= ResolveUrl("~/Bugs/Edit.aspx?id=") %>" +
                    news.bugid +
                    ">" +
                    news.desc +
                    "</a>" +
                    "<td class=datad>" +
                    news.action +
                    "<td class=datad>" +
                    news.who;

                tableRows += tr;

            }

            el = document.getElementById("news_table");
            el.innerHTML = table_start + tableRows + table_end;

        }

        function get_news() {

            $.ajax(
                {
                    type: "GET",
                    url: "WhatsNew.aspx?since=" + since,

                    cache: false,
                    timeout: 20000,
                    dataType: "json",

                    success: function (data) {
                        //alert(data)
                        process_json(data);
                        setTimeout(get_news,
                            1000 *
                        <% Response.Write(Util.GetSetting("WhatsNewPageIntervalInSeconds", "20"));%> );
                    },

                    error: function (xmlHttpRequest, textStatus, errorThrown) {
                        setTimeout(get_news, 60000);
                    }
                }
            );
        };


        $(document).ready(function () {
            get_news();
        });

    </script>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyHeader" runat="server">
    <BugTracker:MainMenu runat="server" ID="MainMenu"/>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyContent" runat="server">
    <table border="0" cellspacing="0" cellpadding="10">
        <tr>
            <td valign="top">

                <div id="news_table">&nbsp</div>

    </table>
</asp:Content>

<asp:Content ContentPlaceHolderID="BodyFooter" runat="server">
</asp:Content>
