/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public static class DbUtil
    {
        private static IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        [Obsolete("Use ExecuteScalar(SqlString sql)")]
        public static object ExecuteScalar(string sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            {
                object returnValue;
                var cmd = new SqlCommand(sql, conn);
                returnValue = cmd.ExecuteScalar();
                conn.Close(); // redundant, but just to be clear
                return returnValue;
            }
        }

        public static object ExecuteScalar(SqlString sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());

                return cmd.ExecuteScalar();
            }
        }

        [Obsolete("Use ExecuteNonQueryWithoutLogging(SqlString sql)")]
        public static void ExecuteNonQueryWithoutLogging(string sql)
        {
            using (var conn = GetSqlConnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close(); // redundant, but just to be clear
            }
        }

        public static void ExecuteNonQueryWithoutLogging(SqlString sql)
        {
            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        [Obsolete("Use ExecuteNonQuery(SqlString sql)")]
        public static void ExecuteNonQuery(string sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close(); // redundant, but just to be clear
            }
        }

        public static void ExecuteNonQuery(SqlString sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        public static void ExecuteNonQuery(SqlCommand cmd)
        {
            LogCommand(cmd);

            using (var conn = GetSqlConnection())
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

        [Obsolete("Use ExecuteReader(SqlString sql, CommandBehavior behavior)")]
        public static SqlDataReader ExecuteReader(string sql, CommandBehavior behavior)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            var conn = GetSqlConnection();
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

        public static SqlDataReader ExecuteReader(SqlString sql, CommandBehavior behavior)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            var conn = GetSqlConnection();

            try
            {
                using (var cmd = new SqlCommand(sql.ToString(), conn))
                {
                    cmd.Parameters.AddRange(sql.GetParameters().ToArray());

                    return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
                }
            }
            catch
            {
                conn.Close();
                throw;
            }
        }

        public static SqlDataReader ExecuteReader(SqlCommand cmd, CommandBehavior behavior)
        {
            LogCommand(cmd);

            var conn = GetSqlConnection();
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

        [Obsolete("Use GetDataSet(SqlString sql)")]
        public static DataSet GetDataSet(string sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            var ds = new DataSet();
            using (var conn = GetSqlConnection())
            {
                using (var da = new SqlDataAdapter(sql, conn))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    da.Fill(ds);
                    stopwatch.Stop();
                    LogStopwatchTime(stopwatch);
                    conn.Close(); // redundant, but just to be clear
                    return ds;
                }
            }
        }

        public static DataSet GetDataSet(SqlString sql)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("sql=\n" + sql);
            }

            var ds = new DataSet();

            using (var conn = GetSqlConnection())
            using (var da = new SqlDataAdapter(sql.ToString(), conn))
            {
                da.SelectCommand.Parameters.AddRange(sql.GetParameters().ToArray());

                var stopwatch = new Stopwatch();

                stopwatch.Start();
                da.Fill(ds);
                stopwatch.Stop();
                LogStopwatchTime(stopwatch);

                return ds;
            }
        }

        public static void LogStopwatchTime(Stopwatch stopwatch)
        {
            if (ApplicationSettings.LogSqlEnabled)
            {
                Util.WriteToLog("elapsed milliseconds:" + stopwatch.ElapsedMilliseconds.ToString("0000"));
            }
        }

        [Obsolete("Use GetDataView(SqlString sql)")]
        public static DataView GetDataView(string sql)
        {
            var ds = GetDataSet(sql);
            return new DataView(ds.Tables[0]);
        }

        public static DataView GetDataView(SqlString sql)
        {
            var ds = GetDataSet(sql);

            return new DataView(ds.Tables[0]);
        }

        [Obsolete("Use GetDataRow(SqlString sql)")]
        public static DataRow GetDataRow(string sql)
        {
            var ds = GetDataSet(sql);
            if (ds.Tables[0].Rows.Count != 1)
                return null;
            return ds.Tables[0].Rows[0];
        }

        public static DataRow GetDataRow(SqlString sql)
        {
            var ds = GetDataSet(sql);

            if (ds.Tables[0].Rows.Count != 1)
            {
                return null;
            }

            return ds.Tables[0].Rows[0];
        }

        public static SqlConnection GetSqlConnection()
        {
            var connectionString = ApplicationSettings.ConnectionString == " ?"
                ? "MISSING CONNECTION STRING"
                : ApplicationSettings.ConnectionString;

            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        private static void LogCommand(SqlCommand cmd)
        {
            if (ApplicationSettings.LogSqlEnabled)
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

                Util.WriteToLog(sb.ToString());
            }
        }
    }
}
