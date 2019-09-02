/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Administration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Persistence;
    using Persistence.Models;

    internal interface IStatusService
    {
        DataSet LoadList();

        Status LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class StatusService : IStatusService
    {
        private readonly ApplicationContext context;

        public StatusService(ApplicationContext context)
        {
            this.context = context;
        }

        public DataSet LoadList()
        {
            var statuses = this.context.Statuses
                .OrderBy(x => x.SortSequence)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("status");
            dataTable.Columns.Add("sort seq");
            dataTable.Columns.Add("css<br>class");
            dataTable.Columns.Add("default");
            dataTable.Columns.Add("hidden");

            foreach (var status in statuses)
            {
                dataTable.Rows.Add(status.Id, status.Name, status.SortSequence, status.Style, status.Default, status.Id);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public Status LoadOne(int id)
        {
            var status = this.context.Statuses
                .First(x => x.Id == id);

            return status;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var status = new Status
            {
                Name = parameters["$na"],
                SortSequence = Convert.ToInt32(parameters["$ss"]),
                Style = parameters["$st"],
                Default = Convert.ToInt32(parameters["$df"])
            };

            this.context.Statuses
                .Add(status);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var status = this.context.Statuses
                .First(x => x.Id == id);

            status.Name = parameters["$na"];
            status.SortSequence = Convert.ToInt32(parameters["$ss"]);
            status.Style = parameters["$st"];
            status.Default = Convert.ToInt32(parameters["$df"]);


            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_status = $1
                select st_name, @cnt [cnt] from statuses where st_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (Convert.ToInt32(dataRow["cnt"]) > 0, Convert.ToString(dataRow["st_name"]));
        }

        public void Delete(int id)
        {
            var status = this.context.Statuses
                .First(x => x.Id == id);

            this.context.Statuses
                .Remove(status);

            this.context
                .SaveChanges();
        }
    }
}