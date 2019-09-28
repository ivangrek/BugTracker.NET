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

    public interface IPriorityService
    {
        DataSet LoadList();

        Priority LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class PriorityService : IPriorityService
    {
        private readonly ApplicationContext context;

        public PriorityService(ApplicationContext context)
        {
            this.context = context;
        }

        public DataSet LoadList()
        {
            var priorities = this.context.Priorities
                .OrderBy(x => x.SortSequence)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("description");
            dataTable.Columns.Add("sort seq");
            dataTable.Columns.Add("background<br>color");
            dataTable.Columns.Add("css<br>class");
            dataTable.Columns.Add("default");
            dataTable.Columns.Add("hidden");

            foreach (var priority in priorities)
            {
                var backgroundColorValue = $"<div style='background:{priority.BackgroundColor};'>{priority.BackgroundColor}</div>";
                var defaultValue = priority.Default == 1
                    ? "Y"
                    : "N";

                dataTable.Rows.Add(priority.Id, priority.Name, priority.SortSequence, backgroundColorValue, priority.Style, defaultValue, priority.Id);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public Priority LoadOne(int id)
        {
            var priority = this.context.Priorities
                .First(x => x.Id == id);

            return priority;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var priority = new Priority
            {
                Name = parameters["$na"],
                SortSequence = Convert.ToInt32(parameters["$ss"]),
                BackgroundColor = parameters["$co"],
                Style = parameters["$st"],
                Default = Convert.ToInt32(parameters["$df"])
            };

            this.context.Priorities
                .Add(priority);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var priority = this.context.Priorities
                .First(x => x.Id == id);

            priority.Name = parameters["$na"];
            priority.SortSequence = Convert.ToInt32(parameters["$ss"]);
            priority.BackgroundColor = parameters["$co"];
            priority.Style = parameters["$st"];
            priority.Default = Convert.ToInt32(parameters["$df"]);

            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_priority = $1
                select pr_name, @cnt [cnt] from priorities where pr_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (Convert.ToInt32(dataRow["cnt"]) > 0, Convert.ToString(dataRow["pr_name"]));
        }

        public void Delete(int id)
        {
            var priority = this.context.Priorities
                .First(x => x.Id == id);

            this.context.Priorities
                .Remove(priority);

            this.context
                .SaveChanges();
        }
    }
}