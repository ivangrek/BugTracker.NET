namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Identification;
    using Microsoft.AspNetCore.Http;
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
        private readonly BtNetDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ReportService(
            BtNetDbContext dbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
        }

        public DataSet LoadList()
        {
            var reports = this.dbContext.Reports
                .OrderBy(x => x.Name)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("report");
            dataTable.Columns.Add("view<br>chart");
            dataTable.Columns.Add("view<br>data");

            var user = this.httpContextAccessor.HttpContext.User;

            if (user.IsInRole(BtNetRole.Administrator) || user.Identity.GetCanEditReports())
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
                        viewChartValue = $"<a target='_blank' href='{$"~/Report/Show?view=chart&id={report.Id}"}'>{report.ChartType}</a>";
                        break;
                    default:
                        viewChartValue = "&nbsp;";
                        break;
                }

                var viewDataValue = $"<a target='_blank' href='{$"~/Report/Show?view=data&id={report.Id}"}'>data</a>";

                if (user.IsInRole(BtNetRole.Administrator) || user.Identity.GetCanEditReports())
                {
                    var editValue = $"<a href='{$"~/Report/Update/{report.Id}"}'>edit</a>";
                    var deleteValue = $"<a href='{$"~/Report/Delete/{report.Id}"}'>delete</a>";

                    dataTable.Rows.Add(report.Id, report.Name, viewChartValue, viewDataValue, editValue, deleteValue);
                }
                else
                {
                    dataTable.Rows.Add(report.Id, report.Name, viewChartValue, viewDataValue);
                }
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public DataSet LoadSelectList()
        {
            var reports = this.dbContext.Reports
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
            var report = this.dbContext.Reports
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

            this.dbContext.Reports
                .Add(report);

            this.dbContext
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var report = this.dbContext.Reports
                .First(x => x.Id == id);

            report.Name = parameters["$de"];
            report.Sql = parameters["$sq"];
            report.ChartType = parameters["$ct"];

            this.dbContext
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var report = this.dbContext.Reports
                 .First(x => x.Id == id);

            return (true, report.Name);
        }

        public void Delete(int id)
        {
            var report = this.dbContext.Reports
                .First(x => x.Id == id);

            this.dbContext.Reports
                .Remove(report);

            var dashboardItem = this.dbContext.DashboardItems
                .FirstOrDefault(x => x.ReportId == id);

            if (dashboardItem != null)
            {
                this.dbContext.DashboardItems
                .Remove(dashboardItem);
            }

            this.dbContext
                .SaveChanges();
        }
    }
}