/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Web.UI;
    using Core;

    public partial class View : Page
    {
        public int Scale = 1;
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            if (security.User.IsAdmin || security.User.CanUseReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var stringId = Util.SanitizeInteger(Request["id"]);
            var view = Request["view"];
            // parent_iframe = Request["parent_iframe"];  // this didn't work

            var scaleString = Request["scale"];

            if (string.IsNullOrEmpty(scaleString))
                this.Scale = 1;
            else
                this.Scale = Convert.ToInt32(scaleString);

            this.Sql = @"select rp_desc, rp_sql, rp_chart_type
		from reports
		where rp_id = $id";

            this.Sql = this.Sql.Replace("$id", stringId);

            var dr = DbUtil.GetDataRow(this.Sql);

            var rpSql = (string) dr["rp_sql"];
            var chartType = (string) dr["rp_chart_type"];
            var desc = (string) dr["rp_desc"];

            // replace the magic pseudo variable
            rpSql = rpSql.Replace("$ME", Convert.ToString(security.User.Usid));

            var ds = DbUtil.GetDataSet(rpSql);

            if (ds.Tables[0].Rows.Count > 0)
            {
                if (view == "data")
                {
                    create_table(desc, ds);
                }
                else
                {
                    if (chartType == "pie")
                    {
                        create_pie_chart(desc, ds);
                    }
                    else if (chartType == "bar")
                    {
                        create_bar_chart(desc, ds);
                    }
                    else if (chartType == "line")
                    {
                        // we need at least two values to draw a line
                        if (ds.Tables[0].Rows.Count > 1)
                            create_line_chart(desc, ds);
                        else
                            write_no_data_message(desc, ds);
                    }
                    else
                    {
                        create_table(desc, ds);
                    }
                }
            }
            else
            {
                if (view == "data")
                {
                    create_table(desc, ds);
                }
                else
                {
                    if (chartType == "pie"
                        || chartType == "bar"
                        || chartType == "line")
                        write_no_data_message(desc, ds);
                    else
                        create_table(desc, ds);
                }
            }
        }

        public void create_line_chart(string title, DataSet ds)
        {
            var chartWidth = 640 / this.Scale;
            var chartHeight = 300 / this.Scale;
            var chartTopMargin = 10 / this.Scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / this.Scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / this.Scale; // gape between chart and top of page

            var maxGridLines = 20 / this.Scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // find the max of the y axis so we know how to scale the data
            var max = 0.0F;
            float tmp;
            int i;
            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(ds.Tables[0].Rows[i][1]);
                if (tmp > max) max = tmp;
                ;
            }

            var verticalScaleFactor = (chartHeight - chartTopMargin) / max;

            // determine how the horizontal grid lines should be

            var gridLineInterval = 1;
            if (max > 1)
                while (max / gridLineInterval > maxGridLines)
                    gridLineInterval *= 10 / this.Scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                pageLeftMargin + chartWidth, // far left
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

            // gray chart background
            objGraphics.FillRectangle(
                new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                pageLeftMargin, pageTopMargin + fontTitle.Height,
                pageLeftMargin + chartWidth,
                chartHeight);

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title,
                fontTitle,
                blackBrush,
                xAxisTextOffset,
                fontTitle.Height / 2);

            int y;
            var chartBottom = pageTopMargin + fontTitle.Height + chartHeight;

            var blackPen = new Pen(Color.Black, 1);

            for (i = 0; i < max; i += gridLineInterval)
            {
                y = (int) (i * verticalScaleFactor);

                // y axis label
                objGraphics.DrawString(
                    Convert.ToString(i),
                    fontLegend,
                    blackBrush,
                    xAxisTextOffset, chartBottom - y - fontLegend.Height / 2);

                // grid line
                objGraphics.DrawLine(
                    blackPen,
                    pageLeftMargin,
                    chartBottom - y,
                    pageLeftMargin + chartWidth,
                    chartBottom - y);
            }

            // draw lines
            var lineLength = chartWidth / (ds.Tables[0].Rows.Count - 1);
            var x = pageLeftMargin;

            var xAxisTextY = chartBottom + pageBottomMargin / 2 - fontLegend.Height / 2;

            var bluePen = new Pen(Color.FromArgb(0, 0, 204), 2);
            var blueBrush = new SolidBrush(Color.FromArgb(0, 0, 204));
            var prevXAxisLabel = -99999;

            for (i = 1; i < ds.Tables[0].Rows.Count; i++)
            {
                var data1 = Convert.ToSingle((int) ds.Tables[0].Rows[i - 1][1]);
                var data2 = Convert.ToSingle((int) ds.Tables[0].Rows[i][1]);

                var valueY1 = (int) (data1 * verticalScaleFactor);
                var valueY2 = (int) (data2 * verticalScaleFactor);

                objGraphics.DrawLine(
                    bluePen,
                    x, chartBottom - valueY1,
                    x + lineLength, chartBottom - valueY2);

                objGraphics.FillEllipse(
                    blueBrush,
                    x + lineLength - 3, chartBottom - valueY2 - 3,
                    6, 6);

                // draw x axis labels

                var xVal = "";

                try
                {
                    xVal = Convert.ToString((int) ds.Tables[0].Rows[i][0]);
                }
                catch (Exception)
                {
                    xVal = Convert.ToString(ds.Tables[0].Rows[i][0]);
                }

                if (x - prevXAxisLabel > 50) // space them apart, so they don't bump into each other
                {
                    // the little line so that the label points to the the data point
                    objGraphics.DrawLine(
                        blackPen,
                        x, chartBottom,
                        x, chartBottom + 14);

                    objGraphics.DrawString(
                        xVal,
                        fontLegend,
                        blackBrush,
                        x, xAxisTextY);

                    prevXAxisLabel = x;
                }

                x += lineLength;
            }

            // Since we are outputting a Gif, set the ContentType appropriately
            Response.ContentType = "image/gif";

            // Save the image to a file
            objBitmap.Save(Response.OutputStream, ImageFormat.Gif);

            // clean up...
            objGraphics.Dispose();
            objBitmap.Dispose();
        }

        public void create_bar_chart(string title, DataSet ds)
        {
            var chartWidth = 640 / this.Scale;
            var chartHeight = 300 / this.Scale;
            var chartTopMargin = 10 / this.Scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / this.Scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / this.Scale; // gape between chart and top of page

            var maxGridLines = 20 / this.Scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // find the max of the y axis so we know how to scale the data
            var max = 0.0F;
            float tmp;
            int i;
            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(ds.Tables[0].Rows[i][1]);
                if (tmp > max) max = tmp;
                ;
            }

            var verticalScaleFactor = (chartHeight - chartTopMargin) / max;

            // determine how the horizontal grid lines should be

            var gridLineInterval = 1;
            if (max > 1)
                while (max / gridLineInterval > maxGridLines)
                    gridLineInterval *= 10 / this.Scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                pageLeftMargin + chartWidth, // far left
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

            // gray chart background
            objGraphics.FillRectangle(
                new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                pageLeftMargin, pageTopMargin + fontTitle.Height,
                pageLeftMargin + chartWidth,
                chartHeight);

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title,
                fontTitle,
                blackBrush,
                xAxisTextOffset,
                fontTitle.Height / 2);

            int y;
            var chartBottom = pageTopMargin + fontTitle.Height + chartHeight;

            var blackPen = new Pen(Color.Black, 1);

            for (i = 0; i < max; i += gridLineInterval)
            {
                y = (int) (i * verticalScaleFactor);

                // y axis label
                objGraphics.DrawString(
                    Convert.ToString(i),
                    fontLegend,
                    blackBrush,
                    xAxisTextOffset, chartBottom - y - fontLegend.Height / 2);

                // grid line
                objGraphics.DrawLine(
                    blackPen,
                    pageLeftMargin,
                    chartBottom - y,
                    pageLeftMargin + chartWidth,
                    chartBottom - y);
            }

            /*
            // draw high water mark
            y = (int)(max * vertical_scale_factor);

            objGraphics.DrawString(
                Convert.ToString(i),
                fontLegend,
                blackBrush,
                x_axis_text_offset, (chart_bottom-y) - (fontLegend.Height/2));

            // grid line
            objGraphics.DrawLine(
                black_pen,
                page_left_margin,
                chart_bottom-y,
                page_left_margin + chart_width,
                chart_bottom-y);
        */

            // draw bars
            var barSpace = chartWidth / ds.Tables[0].Rows.Count;
            var barWidth = (int) (.70F * barSpace);
            var x = (int) (.30F * barSpace);
            x += pageLeftMargin;

            var xAxisTextY = chartBottom + pageBottomMargin / 2 - fontLegend.Height / 2;
            Brush blueBrush = new SolidBrush(Color.FromArgb(0, 0, 204));

            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                var data = Convert.ToSingle((int) ds.Tables[0].Rows[i][1]);

                var barHeight = (int) (data * verticalScaleFactor);

                objGraphics.FillRectangle(
                    blueBrush,
                    x, chartBottom - barHeight,
                    barWidth,
                    barHeight);

                objGraphics.DrawString(
                    Convert.ToString(ds.Tables[0].Rows[i][0]),
                    fontLegend,
                    blackBrush,
                    x, xAxisTextY);

                x += barWidth;
                x += (int) (.30F * barSpace);
            }

            // Since we are outputting a Gif, set the ContentType appropriately
            Response.ContentType = "image/gif";

            // Save the image to a file
            objBitmap.Save(Response.OutputStream, ImageFormat.Gif);

            // clean up...
            objGraphics.Dispose();
            objBitmap.Dispose();
        }

        public void create_pie_chart(string title, DataSet ds)
        {
            var width = 240;
            var pageTopMargin = 15;

            // [corey] - I downloaded this code from MSDN, the URL below, and modified it.
            // http://msdn.microsoft.com/msdnmag/issues/02/02/ASPDraw/default.aspx

            // We need to connect to the database and grab information for the
            // particular columns for the particular table

            // find the total of the numeric data
            float total = 0.0F, tmp;
            int i;
            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(ds.Tables[0].Rows[i][1]);
                total += tmp;
            }

            // we need to create fonts for our legend and title
            var fontLegend = new Font("Verdana", 10);

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);
            var titleHeight = fontTitle.Height + pageTopMargin;

            // We need to create a legend and title, how big do these need to be?
            // Also, we need to resize the height for the pie chart, respective to the
            // height of the legend and title

            var rowGap = 6;
            var startOfRect = 8;
            var rectWidth = 14;
            var rectHeight = 16;

            int rowHeight;
            if (rectHeight > fontLegend.Height) rowHeight = rectHeight;
            else rowHeight = fontLegend.Height;
            rowHeight += rowGap;

            var legendHeight = rowHeight * (ds.Tables[0].Rows.Count + 1);
            var height = width + legendHeight + titleHeight + pageTopMargin;
            var pieHeight = width; // maintain a one-to-one ratio

            // Create a rectange for drawing our pie
            var pieRect = new Rectangle(0, titleHeight, width, pieHeight);

            // Create our pie chart, start by creating an ArrayList of colors
            var colors = new ArrayList();

            colors.Add(new SolidBrush(Color.FromArgb(204, 204, 255)));
            colors.Add(new SolidBrush(Color.FromArgb(051, 051, 255)));
            colors.Add(new SolidBrush(Color.FromArgb(204, 204, 204)));
            colors.Add(new SolidBrush(Color.FromArgb(153, 153, 255)));
            colors.Add(new SolidBrush(Color.FromArgb(153, 153, 153)));
            colors.Add(new SolidBrush(Color.FromArgb(000, 204, 000)));

            var rnd = new Random();
            for (i = 0; i < ds.Tables[0].Rows.Count - 6; i++)
                colors.Add(new SolidBrush(Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255))));

            var currentDegree = 0.0F;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(width, height);
            var objGraphics = Graphics.FromImage(objBitmap);

            var blackBrush = new SolidBrush(Color.Black);

            // Put a white backround in
            objGraphics.FillRectangle(new SolidBrush(Color.White), 0, 0, width, height);
            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                objGraphics.FillPie(
                    (SolidBrush) colors[i],
                    pieRect,
                    currentDegree,
                    Convert.ToSingle(ds.Tables[0].Rows[i][1]) / total * 360);

                // increment the currentDegree
                currentDegree += Convert.ToSingle(ds.Tables[0].Rows[i][1]) / total * 360;
            }

            // Create the title, centered
            var stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            objGraphics.DrawString(title, fontTitle, blackBrush,
                new Rectangle(0, 0, width, titleHeight), stringFormat);

            // Create the legend
            objGraphics.DrawRectangle(
                new Pen(Color.Gray, 1),
                0,
                height - legendHeight,
                width - 4,
                legendHeight - 1);

            var y = height - legendHeight + rowGap;

            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                objGraphics.FillRectangle(
                    (SolidBrush) colors[i],
                    startOfRect, // x
                    y,
                    rectWidth,
                    rectHeight);

                objGraphics.DrawString(
                    Convert.ToString(ds.Tables[0].Rows[i][0])
                    + " - " +
                    Convert.ToString(ds.Tables[0].Rows[i][1]),
                    fontLegend,
                    blackBrush,
                    startOfRect + rectWidth + 4,
                    y);

                y += rectHeight + rowGap;
            }

            // display the total
            objGraphics.DrawString(
                "Total: " + Convert.ToString(total),
                fontLegend,
                blackBrush,
                startOfRect + rectWidth + 4,
                y);

            // Since we are outputting a Gif, set the ContentType appropriately
            Response.ContentType = "image/gif";

            // Save the image to a file
            objBitmap.Save(Response.OutputStream, ImageFormat.Gif);

            // clean up...
            objGraphics.Dispose();
            objBitmap.Dispose();
        }

        public void create_table(string title, DataSet ds)
        {
            Response.Write("<link rel=StyleSheet href=Content/btnet.css type=text/css>");
            Response.Write("<s" + "cript");
            Response.Write(" type=text/javascript language=JavaScript src=Scripts/sortable.js>");
            Response.Write("</s" + "cript>");

            Response.Write("\n<body style='background: white;' ");
            // this didn't work
            //if (!string.IsNullOrEmpty(parent_iframe))
            //{ 
            //    Response.Write(" onload=\"parent.document.getElementById('" + parent_iframe + "').height = 20 + document['body'].offsetHeight\"");  
            //}
            Response.Write(">\n<div class=align><table border=0><tr><td>");

            Response.Write("<h2>" + title + "</h2>");

            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                SortableHtmlTable.CreateFromDataSet(
                    Response, ds, "", "");
            else
                Response.Write("<font size=+1>The database query for this report returned zero rows.</font>");
        }

        public void write_no_data_message(string title, DataSet ds)
        {
            var chartWidth = 640 / this.Scale;
            var chartHeight = 300 / this.Scale;
            var chartTopMargin = 10 / this.Scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / this.Scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / this.Scale; // gape between chart and top of page

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);
            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                pageLeftMargin + chartWidth, // far left
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title + " (no data to chart)",
                fontTitle,
                blackBrush,
                xAxisTextOffset,
                fontTitle.Height / 2);

            // Since we are outputting a Gif, set the ContentType appropriately
            Response.ContentType = "image/gif";

            // Save the image to a file
            objBitmap.Save(Response.OutputStream, ImageFormat.Gif);

            // clean up...
            objGraphics.Dispose();
            objBitmap.Dispose();
        }
    }
}