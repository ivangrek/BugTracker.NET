/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Web;
    using Persistence;
    using Persistence.Models;

    public interface IReportService
    {
        DataSet LoadList();

        DataSet LoadSelectList();

        Report LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class ReportService : IReportService
    {
        private readonly ISecurity security;
        private readonly ApplicationContext context;

        public ReportService(
            ISecurity security,
            ApplicationContext context)
        {
            this.security = security;
            this.context = context;
        }

        public DataSet LoadList()
        {
            var reports = this.context.Reports
                .OrderBy(x => x.Name)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("report");
            dataTable.Columns.Add("view<br>chart");
            dataTable.Columns.Add("view<br>data");

            if (this.security.User.IsAdmin || this.security.User.CanEditReports)
            {
                dataTable.Columns.Add("edit");
                dataTable.Columns.Add("delete");
            }

            foreach (var report in reports)
            {
                var viewChartValue = "&nbsp;";

                switch (report.ChartType)
                {
                    case "pie":
                    case "line":
                    case "bar":
                        viewChartValue = $"<a target='_blank' href='{VirtualPathUtility.ToAbsolute($"~/Reports/View.aspx?view=chart&id={report.Id}")}'>{report.ChartType}</a>";
                        break;
                    default:
                        viewChartValue = "&nbsp;";
                        break;
                }

                var viewDataValue = $"<a target='_blank' href='{VirtualPathUtility.ToAbsolute($"~/Reports/View.aspx?view=data&id={report.Id}")}'>data</a>";

                if (this.security.User.IsAdmin || this.security.User.CanEditReports)
                {
                    var editValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Reports/Edit.aspx?id={report.Id}")}'>edit</a>";
                    var deleteValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Reports/Delete.aspx?id={report.Id}")}'>delete</a>";

                    dataTable.Rows.Add(report.Name, viewChartValue, viewDataValue, editValue, deleteValue);
                }
                else
                {
                    dataTable.Rows.Add(report.Name, viewChartValue, viewDataValue);
                }
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public DataSet LoadSelectList()
        {
            var reports = this.context.Reports
                .OrderBy(x => x.Name)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("report");
            dataTable.Columns.Add("chart");
            dataTable.Columns.Add("data");

            foreach (var report in reports)
            {
                var chartValue = "&nbsp;";

                switch (report.ChartType)
                {
                    case "pie":
                    case "line":
                    case "bar":
                        chartValue = $"<a href='javascript:select_report(\"{report.ChartType}\", {report.Id})'>select {report.ChartType}</a>";
                        break;
                    default:
                        chartValue = "&nbsp;";
                        break;
                }

                var dataValue = $"<a href='javascript:select_report(\"data\", {report.Id})'>select data</a>";

                dataTable.Rows.Add(report.Name, chartValue, dataValue);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public Report LoadOne(int id)
        {
            var report = this.context.Reports
                .First(x => x.Id == id);

            return report;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var report = new Report
            {
                Name = parameters["$de"],
                Sql = parameters["$sq"],
                ChartType = parameters["$ct"]
            };

            this.context.Reports
                .Add(report);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var report = this.context.Reports
                .First(x => x.Id == id);

            report.Name = parameters["$na"];
            report.Sql = parameters["$sq"];
            report.ChartType = parameters["$ct"];

            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"select rp_desc from reports where rp_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (true, Convert.ToString(dataRow["rp_desc"]));
        }

        public void Delete(int id)
        {
            var report = this.context.Reports
                .First(x => x.Id == id);

            this.context.Reports
                .Remove(report);

            var dashboardItem = this.context.DashboardItems
                .FirstOrDefault(x => x.ReportId == id);

            if (dashboardItem != null)
            {
                this.context.DashboardItems
                .Remove(dashboardItem);
            }

            this.context
                .SaveChanges();
        }
    }
}