namespace BugTracker.Web.Core
{
    using System;
    using System.Data.SqlClient;
    using System.Collections.Generic;
    using System.Text;

    public class SqlString
    {
        private readonly StringBuilder sqlStringBuilder = new StringBuilder();
        private readonly List<SqlParameter> parameters = new List<SqlParameter>();

        public SqlString(string value)
        {
            this.sqlStringBuilder
                .Append(value);
        }

        public SqlString(string value, IList<SqlParameter> parameters)
        {
            this.sqlStringBuilder
                .Append(value);

            this.parameters
                .AddRange(parameters);
        }

        public override string ToString()
        {
            return this.sqlStringBuilder
                .ToString();
        }

        public SqlString AddParameterWithValue(string parameter, object value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }

            if (!parameter.StartsWith("@"))
            {
                parameter = $"@{parameter}";
            }

            this.parameters
                .Add(new SqlParameter
                {
                    ParameterName = parameter,
                    Value = value
                });

            return this;
        }

        public SqlString AddParameterWithValue(string parameter, int value)
        {
            if (!parameter.StartsWith("@"))
            {
                parameter = $"@{parameter}";
            }

            this.parameters
                .Add(new SqlParameter
                {
                    ParameterName = parameter,
                    Value = value
                });

            return this;
        }

        public SqlString Append(string toAppend)
        {
            this.sqlStringBuilder
                .Append(toAppend);

            return this;
        }

        public SqlString Append(SqlString toAppend)
        {
            this.sqlStringBuilder
                .Append(toAppend);

            foreach (var param in toAppend.GetParameters())
            {
                this.parameters
                    .Add(param);
            }

            return this;
        }

        public IList<SqlParameter> GetParameters()
        {
            return this.parameters;
        }
    }
}