/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Report;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class ReportController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IReportService reportService;

        public ReportController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IReportService reportService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.reportService = reportService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - reports",
                SelectedItem = MainMenuSections.Reports
            };

            var model = new SortableTableModel
            {
                DataSet = this.reportService.LoadList(),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int id, string view, int scale = 1)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var dataRow = this.reportService.LoadOne(id);
            var rpSql = dataRow.Sql
                // replace the magic pseudo variable
                .Replace("$ME", Convert.ToString(this.security.User.Usid));

            var dataSet = DbUtil.GetDataSet(rpSql);
            var needDrawGraph = dataRow.ChartType == "pie"
                || dataRow.ChartType == "bar"
                || dataRow.ChartType == "line";

            if (view == "data" || !needDrawGraph)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = dataRow.Name,
                    SelectedItem = MainMenuSections.Reports
                };

                var model = new SortableTableModel
                {
                    DataSet = dataSet
                };

                return View(model);
            }

            if (dataSet.Tables[0].Rows.Count > 0)
            {
                if (dataRow.ChartType == "pie")
                {
                    using (var bitmap = CreatePieChart(dataRow.Name, dataSet))
                    using (var memoryStream = new MemoryStream())
                    {
                        // Save the image to a stream
                        bitmap.Save(memoryStream, ImageFormat.Gif);

                        return File(memoryStream.ToArray(), "image/gif");
                    }
                }
                else if (dataRow.ChartType == "bar")
                {
                    using (var bitmap = CreateBarChart(dataRow.Name, dataSet, scale))
                    using (var memoryStream = new MemoryStream())
                    {
                        // Save the image to a stream
                        bitmap.Save(memoryStream, ImageFormat.Gif);

                        return File(memoryStream.ToArray(), "image/gif");
                    }
                }
                else if (dataRow.ChartType == "line")
                {
                    // we need at least two values to draw a line
                    if (dataSet.Tables[0].Rows.Count > 1)
                    {
                        using (var bitmap = CreateLineChart(dataRow.Name, dataSet, scale))
                        using (var memoryStream = new MemoryStream())
                        {
                            // Save the image to a stream
                            bitmap.Save(memoryStream, ImageFormat.Gif);

                            return File(memoryStream.ToArray(), "image/gif");
                        }
                    }
                    else
                    {
                        using (var bitmap = WriteNoDataMessage(dataRow.Name, scale))
                        using (var memoryStream = new MemoryStream())
                        {
                            // Save the image to a stream
                            bitmap.Save(memoryStream, ImageFormat.Gif);

                            return File(memoryStream.ToArray(), "image/gif");
                        }
                    }
                }
            }
            else
            {
                using (var bitmap = WriteNoDataMessage(dataRow.Name, scale))
                using (var memoryStream = new MemoryStream())
                {
                    // Save the image to a stream
                    bitmap.Save(memoryStream, ImageFormat.Gif);

                    return File(memoryStream.ToArray(), "image/gif");
                }
            }

            return Content(string.Empty);
        }

        [HttpGet]
        public ActionResult Select()
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - select report",
                SelectedItem = MainMenuSections.Reports
            };

            var model = new SortableTableModel
            {
                DataSet = this.reportService.LoadSelectList(),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - new report",
                SelectedItem = MainMenuSections.Reports
            };

            var model = new EditModel
            {
                ChartType = "Table",
                SqlText = Request.Form["sql_text"] // if coming from Search.aspx
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - new report",
                    SelectedItem = MainMenuSections.Reports
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$de", model.Name },
                { "$sq", Server.HtmlDecode(model.SqlText) },
                { "$ct", model.ChartType.ToLower() },
            };

            this.reportService.Create(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit report",
                SelectedItem = MainMenuSections.Reports
            };

            // Get this entry's data from the db and fill in the form
            var dataRow = this.reportService
                .LoadOne(id);

            var model = new EditModel
            {
                Id = dataRow.Id,
                Name = dataRow.Name,
                ChartType = dataRow.ChartType,
                SqlText = dataRow.Sql
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - edit report",
                    SelectedItem = MainMenuSections.Reports
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$de", model.Name/*this.desc.Value.Replace("'", "''")*/ },
                { "$sq", model.SqlText/*Server.HtmlDecode(this.sqlText.Value.Replace("'", "''"))*/ },
                { "$ct", model.ChartType.ToLower() },
            };

            this.reportService.Update(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, name) = this.reportService.CheckDeleting(id);

            if (!valid)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete report",
                SelectedItem = MainMenuSections.Reports
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, _) = this.reportService.CheckDeleting(model.Id);

            if (!valid)
            {
                return Content("You are not allowed to use this page.");
            }

            // do delete here
            this.reportService.Delete(model.Id);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Administrator)]
        public ActionResult Dashboard()
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - dashboard",
                SelectedItem = MainMenuSections.Reports
            };

            // TODO to service
            var sql = @"
select ds.*, rp_desc
from dashboard_items ds
inner join reports on rp_id = ds_report
where ds_user = $us
order by ds_col, ds_row";

            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

            var model = new DashboardModel
            {
                DataSet = DbUtil.GetDataSet(sql)
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult EditDashboard()
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit dashboard",
                SelectedItem = MainMenuSections.Reports
            };

            var sql = @"
                select ds_id, ds_col, ds_row, ds_chart_type, rp_desc
                from dashboard_items ds
                inner join reports on rp_id = ds_report
                where ds_user = $user
                order by ds_col, ds_row"
                .Replace("$user", Convert.ToString(this.security.User.Usid));

            var model = new EditDashboardModel
            {
                DataSet = DbUtil.GetDataSet(sql)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDashboard(string action, int? dashboardId, int? reportId, int? column, string chartType)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var sql = string.Empty;

            if (string.IsNullOrEmpty(action))
            {
                return Content("?");
            }

            if (action == "add")
            {
                sql = @"
                    declare @last_row int
                    set @last_row = -1

                    select @last_row = max(ds_row) from dashboard_items
                    where ds_user = $user
                    and ds_col = $col

                    if @last_row = -1 or @last_row is null
                        set @last_row = 1
                    else
                        set @last_row = @last_row + 1

                    insert into dashboard_items
                    (ds_user, ds_report, ds_chart_type, ds_col, ds_row)
                    values ($user, $report, '$chart_type', $col, @last_row)"
                    .Replace("$user", Convert.ToString(this.security.User.Usid))
                    .Replace("$report", Convert.ToString(reportId))
                    .Replace("$chart_type", chartType.Replace("'", "''"))
                    .Replace("$col", Convert.ToString(column));
            }
            else if (action == "delete")
            {
                sql = "delete from dashboard_items where ds_id = $ds_id and ds_user = $user"
                    .Replace("$ds_id", Convert.ToString(dashboardId))
                    .Replace("$user", Convert.ToString(this.security.User.Usid));
            }
            else if (action == "moveup" || action == "movedown")
            {
                sql = @"
                    /* swap positions */
                    declare @other_row int
                    declare @this_row int
                    declare @col int

                    select @this_row = ds_row, @col = ds_col
                    from dashboard_items
                    where ds_id = $ds_id and ds_user = $user

                    set @other_row = @this_row + $delta

                    update dashboard_items
                    set ds_row = @this_row
                    where ds_user = $user
                    and ds_col = @col
                    and ds_row = @other_row

                    update dashboard_items
                    set ds_row = @other_row
                    where ds_user = $user
                    and ds_id = $ds_id";

                if (action == "moveup")
                {
                    sql = sql.Replace("$delta", "-1");
                }
                else
                {
                    sql = sql.Replace("$delta", "1");
                }

                sql = sql.Replace("$ds_id", Convert.ToString(dashboardId));
                sql = sql.Replace("$user", Convert.ToString(this.security.User.Usid));
            }

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(EditDashboard));
        }

        private static Bitmap CreatePieChart(string title, DataSet dataSet)
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
            for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(dataSet.Tables[0].Rows[i][1]);
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

            var legendHeight = rowHeight * (dataSet.Tables[0].Rows.Count + 1);
            var height = width + legendHeight + titleHeight + pageTopMargin;
            var pieHeight = width; // maintain a one-to-one ratio

            // Create a rectange for drawing our pie
            var pieRect = new Rectangle(0, titleHeight, width, pieHeight);

            // Create our pie chart, start by creating an ArrayList of colors
            var colors = new ArrayList
            {
                new SolidBrush(Color.FromArgb(204, 204, 255)),
                new SolidBrush(Color.FromArgb(051, 051, 255)),
                new SolidBrush(Color.FromArgb(204, 204, 204)),
                new SolidBrush(Color.FromArgb(153, 153, 255)),
                new SolidBrush(Color.FromArgb(153, 153, 153)),
                new SolidBrush(Color.FromArgb(000, 204, 000))
            };

            var rnd = new Random();

            for (i = 0; i < dataSet.Tables[0].Rows.Count - 6; i++)
            {
                colors.Add(new SolidBrush(Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255))));
            }

            var currentDegree = 0.0F;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(width, height);

            using (var objGraphics = Graphics.FromImage(objBitmap))
            using (var blackBrush = new SolidBrush(Color.Black))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                // Put a white backround in
                objGraphics.FillRectangle(whiteBrush, 0, 0, width, height);
                for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    objGraphics.FillPie(
                        (SolidBrush)colors[i],
                        pieRect,
                        currentDegree,
                        Convert.ToSingle(dataSet.Tables[0].Rows[i][1]) / total * 360);

                    // increment the currentDegree
                    currentDegree += Convert.ToSingle(dataSet.Tables[0].Rows[i][1]) / total * 360;
                }

                // Create the title, centered
                using (var stringFormat = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    objGraphics.DrawString(title, fontTitle, blackBrush, new Rectangle(0, 0, width, titleHeight), stringFormat);
                }

                // Create the legend
                objGraphics.DrawRectangle(
                    new Pen(Color.Gray, 1),
                    0,
                    height - legendHeight,
                    width - 4,
                    legendHeight - 1);

                var y = height - legendHeight + rowGap;

                for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    objGraphics.FillRectangle(
                        (SolidBrush)colors[i],
                        startOfRect, // x
                        y,
                        rectWidth,
                        rectHeight);

                    objGraphics.DrawString(
                        Convert.ToString(dataSet.Tables[0].Rows[i][0])
                        + " - " +
                        Convert.ToString(dataSet.Tables[0].Rows[i][1]),
                        fontLegend,
                        blackBrush,
                        startOfRect + rectWidth + 4,
                        y);

                    y += rectHeight + rowGap;
                }

                // display the total
                objGraphics.DrawString(
                    $"Total: {total}",
                    fontLegend,
                    blackBrush,
                    startOfRect + rectWidth + 4,
                    y);

                return objBitmap;
            }
        }

        private static Bitmap CreateBarChart(string title, DataSet dataSet, int scale)
        {
            var chartWidth = 640 / scale;
            var chartHeight = 300 / scale;
            var chartTopMargin = 10 / scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / scale; // gape between chart and top of page

            var maxGridLines = 20 / scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // find the max of the y axis so we know how to scale the data
            var max = 0.0F;
            float tmp;
            int i;
            for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(dataSet.Tables[0].Rows[i][1]);
                if (tmp > max) max = tmp;
            }

            var verticalScaleFactor = (chartHeight - chartTopMargin) / max;

            // determine how the horizontal grid lines should be

            var gridLineInterval = 1;
            if (max > 1)
                while (max / gridLineInterval > maxGridLines)
                    gridLineInterval *= 10 / scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            using (var objGraphics = Graphics.FromImage(objBitmap))
            using (var blackBrush = new SolidBrush(Color.Black))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                // white overall background
                objGraphics.FillRectangle(
                    whiteBrush, // yellow
                    0, 0,
                    pageLeftMargin + chartWidth, // far left
                    pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

                // gray chart background
                objGraphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                    pageLeftMargin, pageTopMargin + fontTitle.Height,
                    pageLeftMargin + chartWidth,
                    chartHeight);

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
                    y = (int)(i * verticalScaleFactor);

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
                var barSpace = chartWidth / dataSet.Tables[0].Rows.Count;
                var barWidth = (int)(.70F * barSpace);
                var x = (int)(.30F * barSpace);
                x += pageLeftMargin;

                var xAxisTextY = chartBottom + pageBottomMargin / 2 - fontLegend.Height / 2;
                Brush blueBrush = new SolidBrush(Color.FromArgb(0, 0, 204));

                for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    var data = Convert.ToSingle((int)dataSet.Tables[0].Rows[i][1]);

                    var barHeight = (int)(data * verticalScaleFactor);

                    objGraphics.FillRectangle(
                        blueBrush,
                        x, chartBottom - barHeight,
                        barWidth,
                        barHeight);

                    objGraphics.DrawString(
                        Convert.ToString(dataSet.Tables[0].Rows[i][0]),
                        fontLegend,
                        blackBrush,
                        x, xAxisTextY);

                    x += barWidth;
                    x += (int)(.30F * barSpace);
                }

                return objBitmap;
            }
        }

        private static Bitmap CreateLineChart(string title, DataSet dataSet, int scale)
        {
            var chartWidth = 640 / scale;
            var chartHeight = 300 / scale;
            var chartTopMargin = 10 / scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / scale; // gape between chart and top of page

            var maxGridLines = 20 / scale;

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);

            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // find the max of the y axis so we know how to scale the data
            var max = 0.0F;
            float tmp;
            int i;
            for (i = 0; i < dataSet.Tables[0].Rows.Count; i++)
            {
                tmp = Convert.ToSingle(dataSet.Tables[0].Rows[i][1]);
                if (tmp > max) max = tmp;
            }

            var verticalScaleFactor = (chartHeight - chartTopMargin) / max;

            // determine how the horizontal grid lines should be

            var gridLineInterval = 1;
            if (max > 1)
                while (max / gridLineInterval > maxGridLines)
                    gridLineInterval *= 10 / scale;

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            using (var objGraphics = Graphics.FromImage(objBitmap))
            using (var blackBrush = new SolidBrush(Color.Black))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                // white overall background
                objGraphics.FillRectangle(
                    whiteBrush, // yellow
                    0, 0,
                    pageLeftMargin + chartWidth, // far left
                    pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

                // gray chart background
                objGraphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(204, 204, 204)), // gray
                    pageLeftMargin, pageTopMargin + fontTitle.Height,
                    pageLeftMargin + chartWidth,
                    chartHeight);

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
                    y = (int)(i * verticalScaleFactor);

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
                var lineLength = chartWidth / (dataSet.Tables[0].Rows.Count - 1);
                var x = pageLeftMargin;

                var xAxisTextY = chartBottom + pageBottomMargin / 2 - fontLegend.Height / 2;

                var bluePen = new Pen(Color.FromArgb(0, 0, 204), 2);
                var blueBrush = new SolidBrush(Color.FromArgb(0, 0, 204));
                var prevXAxisLabel = -99999;

                for (i = 1; i < dataSet.Tables[0].Rows.Count; i++)
                {
                    var data1 = Convert.ToSingle((int)dataSet.Tables[0].Rows[i - 1][1]);
                    var data2 = Convert.ToSingle((int)dataSet.Tables[0].Rows[i][1]);

                    var valueY1 = (int)(data1 * verticalScaleFactor);
                    var valueY2 = (int)(data2 * verticalScaleFactor);

                    objGraphics.DrawLine(
                        bluePen,
                        x, chartBottom - valueY1,
                        x + lineLength, chartBottom - valueY2);

                    objGraphics.FillEllipse(
                        blueBrush,
                        x + lineLength - 3, chartBottom - valueY2 - 3,
                        6, 6);

                    // draw x axis labels

                    var xVal = string.Empty;

                    try
                    {
                        xVal = Convert.ToString((int)dataSet.Tables[0].Rows[i][0]);
                    }
                    catch (Exception)
                    {
                        xVal = Convert.ToString(dataSet.Tables[0].Rows[i][0]);
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

                return objBitmap;
            }
        }

        private static Bitmap WriteNoDataMessage(string title, int scale)
        {
            var chartWidth = 640 / scale;
            var chartHeight = 300 / scale;
            var chartTopMargin = 10 / scale; // gap between highest bar and border of chart

            var xAxisTextOffset = 8 / scale; // gap between edge and start of x axis text
            var pageTopMargin = 40 / scale; // gape between chart and top of page

            var fontTitle = new Font("Verdana", 12, FontStyle.Bold);
            var fontLegend = new Font("Verdana", 8);
            var pageBottomMargin = 3 * fontLegend.Height;
            var pageLeftMargin = 4 * fontLegend.Height + xAxisTextOffset; // where the y axis text goes

            // Create a Bitmap instance
            var objBitmap = new Bitmap(
                pageLeftMargin + chartWidth, // total width
                pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // total height

            using (var objGraphics = Graphics.FromImage(objBitmap))
            using (var blackBrush = new SolidBrush(Color.Black))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                // white overall background
                objGraphics.FillRectangle(
                    whiteBrush, // yellow
                    0, 0,
                    pageLeftMargin + chartWidth, // far left
                    pageTopMargin + fontTitle.Height + chartHeight + pageBottomMargin); // bottom

                // draw title
                objGraphics.DrawString(
                    title + " (no data to chart)",
                    fontTitle,
                    blackBrush,
                    xAxisTextOffset,
                    fontTitle.Height / 2);

                return objBitmap;
            }
        }
    }
}