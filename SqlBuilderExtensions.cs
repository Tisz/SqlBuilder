using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBuilder
{
    public static class SqlBuilderExtensions
    {
        public static string GetListAsSQLSafeString<T>(IEnumerable<T> list)
        {
            StringBuilder builder = new StringBuilder();
            bool started = false;
            foreach (T o in list)
            {
                if (!started)
                    started = true;
                else
                    builder.Append(", ");

                builder.Append(GetLiteralStringForSQL(o));
            }

            return builder.ToString();
        }

        public static string GetLiteralStringForSQL(object o)
        {
            if (o == null)
                return "NULL";
            else if (o is Guid guidObj)
                return $"'{guidObj}'";
            else if (o is string stringObj)
                return $"'{GetSQLSafeString(stringObj)}'";
            else if (o is bool boolObj)
                return boolObj ? "1" : "0";
            else if (o is byte || o is short || o is int || o is long || o is decimal || o is float || o is double)
                return o.ToString();
            else if (o is DateTime dateObj)
                return string.Format("CONVERT(datetime, '{0}', 103)", dateObj.ToString("dd/MM/yyyy HH:mm:ss"));
            else if (o is char charObj)
                return $"'{GetSQLSafeString(charObj)}'";
            else if (o is Functions function)
                return function.Description() + "()";
            else
                throw new ArgumentException($"Type not handled for: {o} Type: {o.GetType()}");
        }

        public static string GetSQLSafeString(string s)
        {
            return s.Replace("'", "''");
        }

        public static string GetSQLSafeString(char c)
        {
            if (c == '\'')
                return "''";
            else
                return c.ToString();
        }
    }
}
