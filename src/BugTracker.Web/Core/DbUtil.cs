/*
    Copyright 2002-2011 Corey Trager

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Text;

    public class DbUtil
    {
        public static object execute_scalar(string sql)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1") Util.write_to_log("sql=\n" + sql);

            using (var conn = get_sqlconnection())
            {
                object returnValue;
                var cmd = new SqlCommand(sql, conn);
                returnValue = cmd.ExecuteScalar();
                conn.Close(); // redundant, but just to be clear
                return returnValue;
            }
        }

        public static void execute_nonquery_without_logging(string sql)
        {
            using (var conn = get_sqlconnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close(); // redundant, but just to be clear
            }
        }

        public static void execute_nonquery(string sql)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1") Util.write_to_log("sql=\n" + sql);

            using (var conn = get_sqlconnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close(); // redundant, but just to be clear
            }
        }

        public static void execute_nonquery(SqlCommand cmd)
        {
            log_command(cmd);

            using (var conn = get_sqlconnection())
            {
                try
                {
                    cmd.Connection = conn;
                    cmd.ExecuteNonQuery();
                    conn.Close(); // redundant, but just to be clear
                }
                finally
                {
                    conn.Close(); // redundant, but just to be clear
                    cmd.Connection = null;
                }
            }
        }

        public static SqlDataReader execute_reader(string sql, CommandBehavior behavior)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1") Util.write_to_log("sql=\n" + sql);

            var conn = get_sqlconnection();
            try
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
                }
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        public static SqlDataReader execute_reader(SqlCommand cmd, CommandBehavior behavior)
        {
            log_command(cmd);

            var conn = get_sqlconnection();
            try
            {
                cmd.Connection = conn;
                return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
            }
            catch
            {
                conn.Close();
                throw;
            }
            finally
            {
                cmd.Connection = null;
            }
        }

        public static DataSet get_dataset(string sql)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1") Util.write_to_log("sql=\n" + sql);

            var ds = new DataSet();
            using (var conn = get_sqlconnection())
            {
                using (var da = new SqlDataAdapter(sql, conn))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    da.Fill(ds);
                    stopwatch.Stop();
                    log_stopwatch_time(stopwatch);
                    conn.Close(); // redundant, but just to be clear
                    return ds;
                }
            }
        }

        public static void log_stopwatch_time(Stopwatch stopwatch)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1")
                Util.write_to_log("elapsed milliseconds:" + stopwatch.ElapsedMilliseconds.ToString("0000"));
        }

        public static DataView get_dataview(string sql)
        {
            var ds = get_dataset(sql);
            return new DataView(ds.Tables[0]);
        }

        public static DataRow get_datarow(string sql)
        {
            var ds = get_dataset(sql);
            if (ds.Tables[0].Rows.Count != 1)
                return null;
            return ds.Tables[0].Rows[0];
        }

        public static SqlConnection get_sqlconnection()
        {
            var connection_string = Util.get_setting("ConnectionString", "MISSING CONNECTION STRING");
            var conn = new SqlConnection(connection_string);
            conn.Open();
            return conn;
        }

        private static void log_command(SqlCommand cmd)
        {
            if (Util.get_setting("LogSqlEnabled", "1") == "1")
            {
                var sb = new StringBuilder();
                sb.Append("sql=\n" + cmd.CommandText);
                foreach (SqlParameter param in cmd.Parameters)
                {
                    sb.Append("\n  ");
                    sb.Append(param.ParameterName);
                    sb.Append("=");
                    if (param.Value == null || Convert.IsDBNull(param.Value))
                    {
                        sb.Append("null");
                    }
                    else if (param.SqlDbType == SqlDbType.Text || param.SqlDbType == SqlDbType.Image)
                    {
                        sb.Append("...");
                    }
                    else
                    {
                        sb.Append("\"");
                        sb.Append(param.Value);
                        sb.Append("\"");
                    }
                }

                Util.write_to_log(sb.ToString());
            }
        }
    } // end DbUtil
} // end namespace