namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public interface IDbUtil
    {
        [Obsolete("Use ExecuteScalar(SqlString sql)")]
        object ExecuteScalar(string sql);

        object ExecuteScalar(SqlString sql);

        [Obsolete("Use ExecuteNonQueryWithoutLogging(SqlString sql)")]
        void ExecuteNonQueryWithoutLogging(string sql);

        void ExecuteNonQueryWithoutLogging(SqlString sql);

        [Obsolete("Use ExecuteNonQuery(SqlString sql)")]
        void ExecuteNonQuery(string sql);

        void ExecuteNonQuery(SqlString sql);

        void ExecuteNonQuery(SqlCommand cmd);

        [Obsolete("Use ExecuteReader(SqlString sql, CommandBehavior behavior)")]
        SqlDataReader ExecuteReader(string sql, CommandBehavior behavior);

        SqlDataReader ExecuteReader(SqlString sql, CommandBehavior behavior);

        SqlDataReader ExecuteReader(SqlCommand cmd, CommandBehavior behavior);

        [Obsolete("Use GetDataSet(SqlString sql)")]
        DataSet GetDataSet(string sql);

        DataSet GetDataSet(SqlString sql);

        [Obsolete("Use GetDataView(SqlString sql)")]
        DataView GetDataView(string sql);

        DataView GetDataView(SqlString sql);

        [Obsolete("Use GetDataRow(SqlString sql)")]
        DataRow GetDataRow(string sql);

        DataRow GetDataRow(SqlString sql);

        SqlConnection GetSqlConnection();
    }

    internal sealed class DbUtil : IDbUtil
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IApplicationLogger applicationLogger;

        public DbUtil(
            IApplicationSettings applicationSettings,
            IApplicationLogger applicationLogger)
        {
            this.applicationSettings = applicationSettings;
            this.applicationLogger = applicationLogger;
        }

        [Obsolete("Use ExecuteScalar(SqlString sql)")]
        public object ExecuteScalar(string sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            {
                object returnValue;
                var cmd = new SqlCommand(sql, conn);
                returnValue = cmd.ExecuteScalar();

                return returnValue;
            }
        }

        public object ExecuteScalar(SqlString sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());

                return cmd.ExecuteScalar();
            }
        }

        [Obsolete("Use ExecuteNonQueryWithoutLogging(SqlString sql)")]
        public void ExecuteNonQueryWithoutLogging(string sql)
        {
            using (var conn = GetSqlConnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQueryWithoutLogging(SqlString sql)
        {
            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        [Obsolete("Use ExecuteNonQuery(SqlString sql)")]
        public void ExecuteNonQuery(string sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            {
                var cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQuery(SqlString sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            using (var conn = GetSqlConnection())
            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());
                cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQuery(SqlCommand cmd)
        {
            LogCommand(cmd);

            using (var conn = GetSqlConnection())
            {
                try
                {
                    cmd.Connection = conn;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Connection = null;
                }
            }
        }

        [Obsolete("Use ExecuteReader(SqlString sql, CommandBehavior behavior)")]
        public SqlDataReader ExecuteReader(string sql, CommandBehavior behavior)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            var conn = GetSqlConnection();

            using (var cmd = new SqlCommand(sql, conn))
            {
                return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
            }
        }

        public SqlDataReader ExecuteReader(SqlString sql, CommandBehavior behavior)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            var conn = GetSqlConnection();

            using (var cmd = new SqlCommand(sql.ToString(), conn))
            {
                cmd.Parameters.AddRange(sql.GetParameters().ToArray());

                return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
            }
        }

        public SqlDataReader ExecuteReader(SqlCommand cmd, CommandBehavior behavior)
        {
            LogCommand(cmd);

            var conn = GetSqlConnection();
            try
            {
                cmd.Connection = conn;
                return cmd.ExecuteReader(behavior | CommandBehavior.CloseConnection);
            }
            finally
            {
                cmd.Connection = null;
            }
        }

        [Obsolete("Use GetDataSet(SqlString sql)")]
        public DataSet GetDataSet(string sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
            }

            var ds = new DataSet();

            using (var conn = GetSqlConnection())
            using (var da = new SqlDataAdapter(sql, conn))
            {
                var stopwatch = new Stopwatch();

                stopwatch.Start();
                da.Fill(ds);
                stopwatch.Stop();
                LogStopwatchTime(stopwatch);

                return ds;
            }
        }

        public DataSet GetDataSet(SqlString sql)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger
                    .WriteToLog("sql=\n" + sql);
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

        [Obsolete("Use GetDataView(SqlString sql)")]
        public DataView GetDataView(string sql)
        {
            var ds = GetDataSet(sql);
            return new DataView(ds.Tables[0]);
        }

        public DataView GetDataView(SqlString sql)
        {
            var ds = GetDataSet(sql);

            return new DataView(ds.Tables[0]);
        }

        [Obsolete("Use GetDataRow(SqlString sql)")]
        public DataRow GetDataRow(string sql)
        {
            var ds = GetDataSet(sql);
            if (ds.Tables[0].Rows.Count != 1)
                return null;
            return ds.Tables[0].Rows[0];
        }

        public DataRow GetDataRow(SqlString sql)
        {
            var ds = GetDataSet(sql);

            if (ds.Tables[0].Rows.Count != 1)
            {
                return null;
            }

            return ds.Tables[0].Rows[0];
        }

        public SqlConnection GetSqlConnection()
        {
            var connectionString = this.applicationSettings.ConnectionString == " ?"
                ? "MISSING CONNECTION STRING"
                : this.applicationSettings.ConnectionString;

            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        private void LogCommand(SqlCommand cmd)
        {
            if (this.applicationSettings.LogSqlEnabled)
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

                this.applicationLogger
                    .WriteToLog(sb.ToString());
            }
        }

        private void LogStopwatchTime(Stopwatch stopwatch)
        {
            if (this.applicationSettings.LogSqlEnabled)
            {
                this.applicationLogger.WriteToLog("elapsed milliseconds:" + stopwatch.ElapsedMilliseconds.ToString("0000"));
            }
        }
    }
}
