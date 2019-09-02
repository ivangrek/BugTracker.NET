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

    internal interface IUserDefinedAttributeService
    {
        DataSet LoadList();

        UserDefinedAttribute LoadOne(int id);

        void Create(Dictionary<string, string> parameters);

        void Update(Dictionary<string, string> parameters);

        (bool Valid, string Name) CheckDeleting(int id);

        void Delete(int id);
    }

    internal class UserDefinedAttributeService : IUserDefinedAttributeService
    {
        private readonly ApplicationContext context;

        public UserDefinedAttributeService(ApplicationContext context)
        {
            this.context = context;
        }

        public DataSet LoadList()
        {
            var userDefinedAttributes = this.context.UserDefinedAttributes
                .OrderBy(x => x.Name)
                .ToArray();

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("user defined attribute value");
            dataTable.Columns.Add("sort seq");
            dataTable.Columns.Add("default");
            dataTable.Columns.Add("hidden");

            foreach (var userDefinedAttribute in userDefinedAttributes)
            {
                var defaultValue = userDefinedAttribute.Default == 1
                    ? "Y"
                    : "N";

                dataTable.Rows.Add(userDefinedAttribute.Id, userDefinedAttribute.Name, userDefinedAttribute.SortSequence, defaultValue, userDefinedAttribute.Id);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        public UserDefinedAttribute LoadOne(int id)
        {
            var userDefinedAttribute = this.context.UserDefinedAttributes
                .First(x => x.Id == id);

            return userDefinedAttribute;
        }

        public void Create(Dictionary<string, string> parameters)
        {
            var userDefinedAttribute = new UserDefinedAttribute
            {
                Name = parameters["$na"],
                SortSequence = Convert.ToInt32(parameters["$ss"]),
                Default = Convert.ToInt32(parameters["$df"])
            };

            this.context.UserDefinedAttributes
                .Add(userDefinedAttribute);

            this.context
                .SaveChanges();
        }

        public void Update(Dictionary<string, string> parameters)
        {
            var id = Convert.ToInt32(parameters["$id"]);
            var userDefinedAttribute = this.context.UserDefinedAttributes
                .First(x => x.Id == id);

            userDefinedAttribute.Name = parameters["$na"];
            userDefinedAttribute.SortSequence = Convert.ToInt32(parameters["$ss"]);
            userDefinedAttribute.Default = Convert.ToInt32(parameters["$df"]);

            this.context
                .SaveChanges();
        }

        public (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_user_defined_attribute = $1
                select udf_name, @cnt [cnt] from user_defined_attribute where udf_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (Convert.ToInt32(dataRow["cnt"]) > 0, Convert.ToString(dataRow["udf_name"]));
        }

        public void Delete(int id)
        {
            var userDefinedAttribute = this.context.UserDefinedAttributes
                .First(x => x.Id == id);

            this.context.UserDefinedAttributes
                .Remove(userDefinedAttribute);

            this.context
                .SaveChanges();
        }
    }
}