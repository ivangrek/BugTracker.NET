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

    public interface ICategoryService
    {
        DataSet LoadList();

        Category LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class CategoryService : ICategoryService
    {
        private readonly ApplicationContext context;

        public CategoryService(ApplicationContext context)
        {
            this.context = context;
        }

        public DataSet LoadList()
        {
            var categories = this.context.Categories
                .OrderBy(x => x.Name)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("category");
            dataTable.Columns.Add("sort seq");
            dataTable.Columns.Add("default");
            dataTable.Columns.Add("hidden");

            foreach (var category in categories)
            {
                var defaultValue = category.Default == 1
                    ? "Y"
                    : "N";

                dataTable.Rows.Add(category.Id, category.Name, category.SortSequence, defaultValue, category.Id);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public Category LoadOne(int id)
        {
            var category = this.context.Categories
                .First(x => x.Id == id);

            return category;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var category = new Category
            {
                Name = parameters["$na"],
                SortSequence = Convert.ToInt32(parameters["$ss"]),
                Default = Convert.ToInt32(parameters["$df"])
            };

            this.context.Categories
                .Add(category);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var category = this.context.Categories
                .First(x => x.Id == id);

            category.Name = parameters["$na"];
            category.SortSequence = Convert.ToInt32(parameters["$ss"]);
            category.Default = Convert.ToInt32(parameters["$df"]);

            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_category = $1
                select ct_name, @cnt [cnt] from categories where ct_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (Convert.ToInt32(dataRow["cnt"]) > 0, Convert.ToString(dataRow["ct_name"]));
        }

        public void Delete(int id)
        {
            var category = this.context.Categories
                .First(x => x.Id == id);

            this.context.Categories
                .Remove(category);

            this.context
                .SaveChanges();
        }
    }
}