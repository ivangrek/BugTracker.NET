/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Administration
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    internal static class StatusService
    {
        internal static DataSet LoadList()
        {
            return DbUtil.GetDataSet(
                @"select st_id [id],
                st_name [status],
                st_sort_seq [sort seq],
                st_style [css<br>class],
                case when st_default = 1 then 'Y' else 'N' end [default],
                st_id [hidden]
                from statuses order by st_sort_seq");
        }

        internal static DataRow LoadOne(int id)
        {
            var sql = @"select st_name, st_sort_seq, isnull(st_style,'') [st_style], st_default from statuses where st_id = $1"
                .Replace("$1", Convert.ToString(id));

            return DbUtil.GetDataRow(sql);
        }

        internal static void Create(Dictionary<string, string> parameters)
        {
            var sql = @"insert into statuses (st_name, st_sort_seq, st_style, st_default) values (N'$na', $ss, N'$st', $df)"
                .Replace("$na", parameters["$na"])
                .Replace("$ss", parameters["$ss"])
                .Replace("$st", parameters["$st"])
                .Replace("$df", parameters["$df"]);

            DbUtil.ExecuteNonQuery(sql);
        }

        internal static void Update(Dictionary<string, string> parameters)
        {
            var sql = @"update statuses set
                st_name = N'$na',
                st_sort_seq = $ss,
                st_style = N'$st',
                st_default = $df
                where st_id = $id"
                .Replace("$id", parameters["$id"])
                .Replace("$na", parameters["$na"])
                .Replace("$ss", parameters["$ss"])
                .Replace("$st", parameters["$st"])
                .Replace("$df", parameters["$df"]);

            DbUtil.ExecuteNonQuery(sql);
        }

        internal static (bool Valid, string Name) CheckDeleting(int id)
        {
            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_status = $1
                select st_name, @cnt [cnt] from statuses where st_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dataRow = DbUtil.GetDataRow(sql);

            return (Convert.ToInt32(dataRow["cnt"]) > 0, Convert.ToString(dataRow["st_name"]));
        }

        internal static void Delete(int id)
        {
            var sql = @"delete statuses where st_id = $1"
                .Replace("$1", Convert.ToString(id));

            DbUtil.ExecuteNonQuery(sql);
        }
    }
}