/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class view_report : Page
    {
        public int scale = 1;

        public Security security;

        public string sql;
        //string parent_iframe;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (this.security.user.is_admin || this.security.user.can_use_reports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var string_id = Util.sanitize_integer(Request["id"]);
            var view = Request["view"];
            // parent_iframe = Request["parent_iframe"];  // this didn't work

            var scale_string = Request["scale"];

            if (string.IsNullOrEmpty(scale_string))
                this.scale = 1;
            else
                this.scale = Convert.ToInt32(scale_string);

            this.sql = @"select rp_desc, rp_sql, rp_chart_type
		from reports
		where rp_id = $id";

            this.sql = this.sql.Replace("$id", string_id);

            var dr = DbUtil.get_datarow(this.sql);

            var rp_sql = (string) dr["rp_sql"];
            var chart_type = (string) dr["rp_chart_type"];
            var desc = (string) dr["rp_desc"];

            // replace the magic pseudo variable
            rp_sql = rp_sql.Replace("$ME", Convert.ToString(this.security.user.usid));

            var ds = DbUtil.get_dataset(rp_sql);

            if (ds.Tables[0].Rows.Count > 0)
            {
                if (view == "data")
                {
                    create_table(desc, ds);
                }
                else
                {
                    if (chart_type == "pie")
                    {
                        create_pie_chart(desc, ds);
                    }
                    else if (chart_type == "bar")
                    {
                        create_bar_chart(desc, ds);
                    }
                    else if (chart_type == "line")
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
                    if (chart_type == "pie"
                        || chart_type == "bar"
                        || chart_type == "line")
                        write_no_data_message(desc, ds);
                    else
                        create_table(desc, ds);
                }
            }
        }

        public void create_line_chart(string title, DataSet ds)
        {
            var chart_width = 640 / this.scale;
            var chart_height = 300 / this.scale;
            var chart_top_margin = 10 / this.scale; // gap between highest bar and border of chart

            var x_axis_text_offset = 8 / this.scale; // gap between edge and start of x axis text
            var page_top_margin = 40 / this.scale; // gape between chart and top of page

            var max_grid_lines = 20 / this.scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var page_bottom_margin = 3 * fontLegend.Height;
            var page_left_margin = 4 * fontLegend.Height + x_axis_text_offset; // where the y axis text goes

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

            var vertical_scale_factor = (chart_height - chart_top_margin) / max;

            // determine how the horizontal grid lines should be

            var grid_line_interval = 1;
            if (max > 1)
                while (max / grid_line_interval > max_grid_lines)
                    grid_line_interval *= 10 / this.scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                page_left_margin + chart_width, // total width
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                page_left_margin + chart_width, // far left
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // bottom

            // gray chart background
            objGraphics.FillRectangle(
                new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                page_left_margin, page_top_margin + fontTitle.Height,
                page_left_margin + chart_width,
                chart_height);

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title,
                fontTitle,
                blackBrush,
                x_axis_text_offset,
                fontTitle.Height / 2);

            int y;
            var chart_bottom = page_top_margin + fontTitle.Height + chart_height;

            var black_pen = new Pen(Color.Black, 1);

            for (i = 0; i < max; i += grid_line_interval)
            {
                y = (int) (i * vertical_scale_factor);

                // y axis label
                objGraphics.DrawString(
                    Convert.ToString(i),
                    fontLegend,
                    blackBrush,
                    x_axis_text_offset, chart_bottom - y - fontLegend.Height / 2);

                // grid line
                objGraphics.DrawLine(
                    black_pen,
                    page_left_margin,
                    chart_bottom - y,
                    page_left_margin + chart_width,
                    chart_bottom - y);
            }

            // draw lines
            var line_length = chart_width / (ds.Tables[0].Rows.Count - 1);
            var x = page_left_margin;

            var x_axis_text_y = chart_bottom + page_bottom_margin / 2 - fontLegend.Height / 2;

            var blue_pen = new Pen(Color.FromArgb(0, 0, 204), 2);
            var blue_brush = new SolidBrush(Color.FromArgb(0, 0, 204));
            var prev_x_axis_label = -99999;

            for (i = 1; i < ds.Tables[0].Rows.Count; i++)
            {
                var data1 = Convert.ToSingle((int) ds.Tables[0].Rows[i - 1][1]);
                var data2 = Convert.ToSingle((int) ds.Tables[0].Rows[i][1]);

                var value_y1 = (int) (data1 * vertical_scale_factor);
                var value_y2 = (int) (data2 * vertical_scale_factor);

                objGraphics.DrawLine(
                    blue_pen,
                    x, chart_bottom - value_y1,
                    x + line_length, chart_bottom - value_y2);

                objGraphics.FillEllipse(
                    blue_brush,
                    x + line_length - 3, chart_bottom - value_y2 - 3,
                    6, 6);

                // draw x axis labels

                var x_val = "";

                try
                {
                    x_val = Convert.ToString((int) ds.Tables[0].Rows[i][0]);
                }
                catch (Exception)
                {
                    x_val = Convert.ToString(ds.Tables[0].Rows[i][0]);
                }

                if (x - prev_x_axis_label > 50) // space them apart, so they don't bump into each other
                {
                    // the little line so that the label points to the the data point
                    objGraphics.DrawLine(
                        black_pen,
                        x, chart_bottom,
                        x, chart_bottom + 14);

                    objGraphics.DrawString(
                        x_val,
                        fontLegend,
                        blackBrush,
                        x, x_axis_text_y);

                    prev_x_axis_label = x;
                }

                x += line_length;
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
            var chart_width = 640 / this.scale;
            var chart_height = 300 / this.scale;
            var chart_top_margin = 10 / this.scale; // gap between highest bar and border of chart

            var x_axis_text_offset = 8 / this.scale; // gap between edge and start of x axis text
            var page_top_margin = 40 / this.scale; // gape between chart and top of page

            var max_grid_lines = 20 / this.scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var page_bottom_margin = 3 * fontLegend.Height;
            var page_left_margin = 4 * fontLegend.Height + x_axis_text_offset; // where the y axis text goes

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

            var vertical_scale_factor = (chart_height - chart_top_margin) / max;

            // determine how the horizontal grid lines should be

            var grid_line_interval = 1;
            if (max > 1)
                while (max / grid_line_interval > max_grid_lines)
                    grid_line_interval *= 10 / this.scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                page_left_margin + chart_width, // total width
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                page_left_margin + chart_width, // far left
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // bottom

            // gray chart background
            objGraphics.FillRectangle(
                new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                page_left_margin, page_top_margin + fontTitle.Height,
                page_left_margin + chart_width,
                chart_height);

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title,
                fontTitle,
                blackBrush,
                x_axis_text_offset,
                fontTitle.Height / 2);

            int y;
            var chart_bottom = page_top_margin + fontTitle.Height + chart_height;

            var black_pen = new Pen(Color.Black, 1);

            for (i = 0; i < max; i += grid_line_interval)
            {
                y = (int) (i * vertical_scale_factor);

                // y axis label
                objGraphics.DrawString(
                    Convert.ToString(i),
                    fontLegend,
                    blackBrush,
                    x_axis_text_offset, chart_bottom - y - fontLegend.Height / 2);

                // grid line
                objGraphics.DrawLine(
                    black_pen,
                    page_left_margin,
                    chart_bottom - y,
                    page_left_margin + chart_width,
                    chart_bottom - y);
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
            var bar_space = chart_width / ds.Tables[0].Rows.Count;
            var bar_width = (int) (.70F * bar_space);
            var x = (int) (.30F * bar_space);
            x += page_left_margin;

            var x_axis_text_y = chart_bottom + page_bottom_margin / 2 - fontLegend.Height / 2;
            Brush blue_brush = new SolidBrush(Color.FromArgb(0, 0, 204));

            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                var data = Convert.ToSingle((int) ds.Tables[0].Rows[i][1]);

                var bar_height = (int) (data * vertical_scale_factor);

                objGraphics.FillRectangle(
                    blue_brush,
                    x, chart_bottom - bar_height,
                    bar_width,
                    bar_height);

                objGraphics.DrawString(
                    Convert.ToString(ds.Tables[0].Rows[i][0]),
                    fontLegend,
                    blackBrush,
                    x, x_axis_text_y);

                x += bar_width;
                x += (int) (.30F * bar_space);
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
            var page_top_margin = 15;

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
            var titleHeight = fontTitle.Height + page_top_margin;

            // We need to create a legend and title, how big do these need to be?
            // Also, we need to resize the height for the pie chart, respective to the
            // height of the legend and title

            var row_gap = 6;
            var start_of_rect = 8;
            var rect_width = 14;
            var rect_height = 16;

            int row_height;
            if (rect_height > fontLegend.Height) row_height = rect_height;
            else row_height = fontLegend.Height;
            row_height += row_gap;

            var legendHeight = row_height * (ds.Tables[0].Rows.Count + 1);
            var height = width + legendHeight + titleHeight + page_top_margin;
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

            var y = height - legendHeight + row_gap;

            for (i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                objGraphics.FillRectangle(
                    (SolidBrush) colors[i],
                    start_of_rect, // x
                    y,
                    rect_width,
                    rect_height);

                objGraphics.DrawString(
                    Convert.ToString(ds.Tables[0].Rows[i][0])
                    + " - " +
                    Convert.ToString(ds.Tables[0].Rows[i][1]),
                    fontLegend,
                    blackBrush,
                    start_of_rect + rect_width + 4,
                    y);

                y += rect_height + row_gap;
            }

            // display the total
            objGraphics.DrawString(
                "Total: " + Convert.ToString(total),
                fontLegend,
                blackBrush,
                start_of_rect + rect_width + 4,
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
            Response.Write("<link rel=StyleSheet href=btnet.css type=text/css>");
            Response.Write("<s" + "cript");
            Response.Write(" type=text/javascript language=JavaScript src=sortable.js>");
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
                SortableHtmlTable.create_from_dataset(
                    Response, ds, "", "");
            else
                Response.Write("<font size=+1>The database query for this report returned zero rows.</font>");
        }

        public void write_no_data_message(string title, DataSet ds)
        {
            var chart_width = 640 / this.scale;
            var chart_height = 300 / this.scale;
            var chart_top_margin = 10 / this.scale; // gap between highest bar and border of chart

            var x_axis_text_offset = 8 / this.scale; // gap between edge and start of x axis text
            var page_top_margin = 40 / this.scale; // gape between chart and top of page

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);
            var fontLegend = new Font("Verdana", 8);
            var page_bottom_margin = 3 * fontLegend.Height;
            var page_left_margin = 4 * fontLegend.Height + x_axis_text_offset; // where the y axis text goes

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                page_left_margin + chart_width, // total width
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // total height

            var objGraphics = Graphics.FromImage(objBitmap);

            // white overall background
            objGraphics.FillRectangle(
                new SolidBrush(Color.White), // yellow
                0, 0,
                page_left_margin + chart_width, // far left
                page_top_margin + fontTitle.Height + chart_height + page_bottom_margin); // bottom

            var blackBrush = new SolidBrush(Color.Black);

            // draw title
            objGraphics.DrawString(
                title + " (no data to chart)",
                fontTitle,
                blackBrush,
                x_axis_text_offset,
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