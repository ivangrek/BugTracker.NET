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
    using Identification;
    using Persistence;
    using Persistence.Models;

    public interface IQueryService
    {
        DataSet LoadList(bool showAll);

        Query LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class QueryService : IQueryService
    {
        private readonly ISecurity security;
        private readonly ApplicationContext context;

        public QueryService(
            ISecurity security,
            ApplicationContext context)
        {
            this.security = security;
            this.context = context;
        }

        public DataSet LoadList(bool showAll)
        {
            var dataTable = new DataTable();
            var queries = Array.Empty<Query>();

            if (this.security.User.IsAdmin || this.security.User.CanEditSql)
            {
                queries = this.context.Queries
                    .Where(x => showAll || x.UserId == null || x.UserId.Value == 0 || x.UserId == this.security.User.Usid)
                    .OrderBy(x => x.Name)
                    .ToArray();

                dataTable.Columns.Add("query");
                dataTable.Columns.Add("visibility");
                dataTable.Columns.Add("$no_sort_view list");
                dataTable.Columns.Add("$no_sort_print list");
                dataTable.Columns.Add("$no_sort_export as excel");
                dataTable.Columns.Add("$no_sort_print list<br>with detail");
                dataTable.Columns.Add("$no_sort_rename");
                dataTable.Columns.Add("$no_sort_delete");
                dataTable.Columns.Add("$no_sort_sql");

                foreach (var query in queries)
                {
                    var visibilityValue = " ";

                    if ((query.UserId == null || query.UserId == 0) && (query.OrganizationId == null || query.OrganizationId == 0))
                    {
                        visibilityValue = "everybody";
                    }
                    else if (query.UserId != null && query.UserId != 0)
                    {
                        var user = this.context.Users
                            .First(x => x.Id == query.UserId);

                        visibilityValue = $"user:{user.Name}";
                    }
                    else if (query.OrganizationId != null && query.OrganizationId != 0)
                    {
                        var organization = this.context.Organizations
                            .First(x => x.Id == query.OrganizationId);

                        visibilityValue = $"org:{organization.Name}";
                    }

                    var viewListValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug?qu_id={query.Id}")}'>view list</a>";
                    var printListValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/Print?queryId={query.Id}")}'>print list</a>";
                    var exportValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/Print?format=excel&queryId={query.Id}")}'>export as excel</a>";
                    var printListDetailValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/PrintDetail?queryId={query.Id}")}'>print detail</a>";
                    var renameValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Query/Update/{query.Id}")}'>edit</a>";
                    var deleteValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Query/Delete/{query.Id}")}'>delete</a>";
                    var sqlValue = query.Sql.Replace("\n", "<br>");

                    dataTable.Rows.Add(query.Name, visibilityValue, viewListValue,
                        printListValue, exportValue, printListDetailValue,
                        renameValue, deleteValue, sqlValue);
                }
            }
            else
            {
                queries = this.context.Queries
                    .Where(x => x.UserId == this.security.User.Usid)
                    .OrderBy(x => x.Name)
                    .ToArray();

                dataTable.Columns.Add("query");
                dataTable.Columns.Add("view list");
                dataTable.Columns.Add("$no_sort_print list");
                dataTable.Columns.Add("$no_sort_export as excel");
                dataTable.Columns.Add("$no_sort_print list<br>with detail");
                dataTable.Columns.Add("$no_sort_rename");
                dataTable.Columns.Add("$no_sort_delete");

                foreach (var query in queries)
                {
                    var viewListValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug?qu_id={query.Id}")}'>view list</a>";
                    var printListValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/Print?queryId={query.Id}")}'>print list</a>";
                    var exportValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/Print?format=excel&queryId={query.Id}")}'>export as excel</a>";
                    var printListDetailValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Bug/PrintPrintDetail?queryId={query.Id}")}'>print detail</a>";
                    var renameValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Query/Update/{query.Id}")}'>edit</a>";
                    var deleteValue = $"<a href='{VirtualPathUtility.ToAbsolute($"~/Query/Delete/{query.Id}")}'>delete</a>";

                    dataTable.Rows.Add(query.Name, viewListValue,
                        printListValue, exportValue, printListDetailValue,
                        renameValue, deleteValue);
                }
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public Query LoadOne(int id)
        {
            var query = this.context.Queries
                .First(x => x.Id == id);

            return query;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var query = new Query
            {
                Name = parameters["$de"],
                Sql = parameters["$sq"],
                Default = 0,
                UserId = Convert.ToInt32(parameters["$us"]),
                OrganizationId = Convert.ToInt32(parameters["$rl"])
            };

            this.context.Queries
                .Add(query);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var query = this.context.Queries
                .First(x => x.Id == id);

            query.Name = parameters["$de"];
            query.Sql = parameters["$sq"];
            query.UserId = Convert.ToInt32(parameters["$us"]);
            query.OrganizationId = Convert.ToInt32(parameters["$rl"]);

            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"select qu_desc, isnull(qu_user,0) qu_user from queries where qu_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return ((int)dataRow["qu_user"] == this.security.User.Usid, Convert.ToString(dataRow["qu_desc"]));
        }

        public void Delete(int id)
        {
            var query = this.context.Queries
                .First(x => x.Id == id);

            this.context.Queries
                .Remove(query);

            this.context
                .SaveChanges();
        }
    }
}