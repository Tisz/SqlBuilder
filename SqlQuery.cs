using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SqlBuilder
{
    public class SqlQuery
    {
        #region Attributes
        private List<string> Sets = new List<string>();
        private List<string> Columns = new List<string>();
        private List<string> Froms = new List<string>();
        private List<Join> Joins = new List<Join>();
        private List<Filter> Filters = new List<Filter>();
        private List<string> Fors = new List<string>();
        private List<Tuple<string, FilterAddType>> Havings = new List<Tuple<string, FilterAddType>>();
        private List<string> GroupBys = new List<string>();
        private List<Tuple<string, bool>> OrderBys = new List<Tuple<string, bool>>();
        private List<Tuple<string, UnionsTypes>> Unions = new List<Tuple<string, UnionsTypes>>();
        private List<Tuple<string, object>> Parameters = new List<Tuple<string, object>>();
        private (long Offset, long? Fetch)? OrderByOffset;
        private string startQuery = "";

        private List<string> Declarations = new List<string>();
        private Dictionary<string, SqlQuery> Withs = new Dictionary<string, SqlQuery>();


        private List<Filter> PaginationFilters = new List<Filter>();

        //For figuring out the total rows in a query
        private List<string> TotalColumns = new List<string>();
        private List<string> TotalGroupBys = new List<string>();
        private List<Tuple<string, bool>> TotalOrderBys = new List<Tuple<string, bool>>();

        /// <summary>
        /// Insert table
        /// </summary>
        private string T_Insert = "";
        private List<string> InsertColumns = new List<string>();
        private List<string> InsertValues = new List<string>();

        /// <summary>
        /// Delete table
        /// </summary>
        private string T_Delete = "";

        /// <summary>
        /// Delete table alias
        /// </summary>
        private string T_Delete_Alias = "";

        /// <summary>
        /// Update table
        /// </summary>
        private string T_Update = "";
        private List<Tuple<string, string>> UpdateColumnValue = new List<Tuple<string, string>>();

        /// <summary>
        /// The query will only return distinct rows. Eg. SELECT DISTINCT C.ID FROM Cat C
        /// </summary>
        public bool Distinct;

        /// <summary>
        /// The query will only this amount of rows. Eg. SELECT TOP 3 C.* FROM Cat C
        /// </summary>
        public int Top;

        /// <summary>
        /// The complete built SQL query
        /// </summary>
        private string query;
        public string Query
        {
            get
            {
                BuildQuery();
                return query;
            }
            set
            {
                query = value;
            }
        }
        #endregion

        public SqlQuery()
        {
        }

        public SqlQuery(string table, params string[] columns)
        {
            From(table);

            foreach (string column in columns)
                AddColumn(column);
        }

        /// <summary>
        /// Constructor for SqlQuery
        /// </summary>
        /// <param name="ignoreNull">Filter out any null items from the column range</param>
        public SqlQuery(bool ignoreNull, string table, params string[] columns)
        {
            From(table);

            foreach (string column in columns)
            {
                AddColumn(column);

                if (ignoreNull)
                    WhereNull("", column, true);
            }
        }

        /// <summary>
        /// Return the format for a table alias. Eg. Cat C
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="alias">The alias for the table</param>
        public static string TableFormat(string tableName, string alias = "")
        {
            string Table_A = "";
            if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(alias))
                Table_A = tableName + (!string.IsNullOrEmpty(alias) ? " " + alias : "");

            return Table_A;
        }

        /// <summary>
        /// Return the format for a table field. Eg. C.Name
        /// </summary>
        /// <param name="tableAlias">The alias for the table</param>
        /// <param name="field">The field of the table</param>
        /// <param name="asName">The custom name for the column</param>
        public static string FieldFormat(string tableAlias, string field, string asName = "")
        {
            string Table_F = "";
            if (!string.IsNullOrEmpty(tableAlias) && !string.IsNullOrEmpty(field))
                Table_F = tableAlias + "." + field + (!string.IsNullOrEmpty(asName) ? " as " + asName : "");
            else if (!string.IsNullOrEmpty(field))
                Table_F = field + (!string.IsNullOrEmpty(asName) ? " as " + asName : "");

            return Table_F;
        }

        /// <summary>
        /// Return the format for multiple table fields. Eg. C.Id, C.Name
        /// </summary>
        /// <param name="tableAlias">The alias for the table</param>
        /// <param name="fields">The field of the table</param>
        public static string FieldFormat(string tableAlias, string[] fields)
        {
            StringBuilder Table_F = new StringBuilder();

            foreach (string field in fields)
            {
                if (!string.IsNullOrEmpty(tableAlias) && !string.IsNullOrEmpty(field.Trim()))
                    Table_F.Append(tableAlias + "." + field.Trim() + ", ");
                else if (!string.IsNullOrEmpty(field))
                    Table_F.Append(field + ", ");
            }

            return Table_F.ToString().Substring(0, Table_F.Length - 2);
        }

        /// <summary>
        /// Returns the format for a function column. Eg ISNULL(C.NAME)
        /// </summary>
        /// <param name="function">The type of function to use</param>
        /// <param name="fieldTable">The alias and field for the table</param>
        public static string FieldFunctionFormat(Functions function, string fieldTable)
        {
            string Table_F = "";
            if (!string.IsNullOrEmpty(fieldTable))
                Table_F = function.Description() + "(" + fieldTable + ")";

            return Table_F;
        }

        /// <summary>
        /// Return the format for a join Eg. Inner Join Cat C on Dog.CatFriend = Cat.ID
        /// </summary>
        /// <param name="table">The table we are creating a join to</param>
        /// <param name="tableAlias">The alias for the new table we are joining to</param>
        /// <param name="tableJoinField">The field from the new table on which we are using to join</param>
        /// <param name="relatedTableAlias">The table alias we are using to join to the new table</param>
        /// <param name="comparison">The comparison condition for the tables to join</param>
        /// <returns>A string which represents the join structure for SQL</returns>
        public static string JoinFormat(string table, string tableAlias, string tableJoinField, string relatedTableAlias, string relatedField, Comparisons comparison)
        {
            string Join = "";
            if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(tableAlias) && !string.IsNullOrEmpty(tableJoinField) && !string.IsNullOrEmpty(relatedTableAlias) && !string.IsNullOrEmpty(relatedField))
                Join = JoinFormat(TableFormat(table, tableAlias), FieldFormat(tableAlias, tableJoinField), FieldFormat(relatedTableAlias, relatedField), comparison);

            return Join;
        }

        /// <summary>
        /// Return the format for a join Eg. Inner Join Cat C on Dog.CatFriend = Cat.ID
        /// </summary>
        /// <param name="tableJoin">The table and alias we will be joining to</param>
        /// <param name="tableJoinField">The alias and field which we will use to join</param>
        /// <param name="relatedTableField">The existing table and field in the query we will use to join to the new table</param>
        /// <param name="comparison">The comparison condition for the tables to join</param>
        /// <returns>A string which represents the join structure for SQL</returns>
        public static string JoinFormat(string tableJoin, string tableJoinField, string relatedTableField, Comparisons comparison)
        {
            string Join = "";
            if (!string.IsNullOrEmpty(tableJoin) && !string.IsNullOrEmpty(tableJoinField) && !string.IsNullOrEmpty(relatedTableField))
                Join = tableJoin + " ON " + tableJoinField + " " + comparison.Description() + " " + relatedTableField;

            return Join;
        }

        /// <summary>
        /// Return the format for a filter. Eg. C.Name = 'Fluffy'
        /// </summary>
        /// <param name="alias">Alias of the field</param>
        /// <param name="field">Field name</param>
        /// <param name="condition"></param>
        /// <param name="comparison"></param>
        ///<param name="useQuotation">Add quotations around the condition object</param>
        /// <returns>A formatted filter string</returns>
        public static string FilterFormat(string alias, string field, string condition, Comparisons comparison = Comparisons.Equal, bool useQuotation = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(field))
                Filter = FilterFormat(FieldFormat(alias, field), condition, comparison, useQuotation);

            return Filter;
        }

        public static string FilterFormat(string alias, string field, int condition, Comparisons comparison = Comparisons.Equal, bool useQuotation = false)
        {
            return FilterFormat(alias, field, condition.ToString(), comparison, useQuotation);
        }

        /// <summary>
        /// Add a filter for a null value
        /// </summary>
        /// <param name="alias">Alias of the field</param>
        /// <param name="field">Field name</param>
        /// <param name="notNull">Where the field is not null</param>
        public static string FilterIsNullFormat(string alias, string field, bool notNull = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(field))
                Filter = FieldFormat(alias, field) + " IS " + (notNull ? "NOT " : "") + "NULL ";

            return Filter;
        }

        /// <summary>
        /// Return the format for a filter. Eg. C.Name = 'Fluffy'
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="comparison"></param>
        ///<param name="useQuotation">Add quotations around the condition object</param>
        public static string FilterFormat(string aliasField, string condition, Comparisons comparison = Comparisons.Equal, bool useQuotation = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(aliasField))
            {
                if (useQuotation)
                    condition = "'" + condition + "'";

                Filter = aliasField + " " + comparison.Description() + " " + condition;
            }
            return Filter;
        }

        /// <summary>
        /// Return the format for a IN Filter. Eg. C.ID IN (1, 2, 3, 4)
        /// </summary>
        /// <param name="alias">Alias for the table</param>
        /// <param name="field">Name of the field to be used in the filter</param>
        /// <param name="listConditions">List of the conditions to be put into the IN statement</param>
        /// <param name="notIn">Use NOT IN</param>
        /// <param name="useQuotation">Use quotations around the condition/param>
        /// <returns>Return the format for a IN Filter.</returns>
        public static string FilterInFormat(string alias, string field, List<string> listConditions, bool notIn = false, bool useQuotation = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(field))
            {
                listConditions = listConditions.Select(x => SQLSafeFormat(x)).ToList();

                if (useQuotation)
                    listConditions = listConditions.Select(x => "'" + x + "'").ToList();

                var list = string.Join(", ", listConditions);
                Filter = FilterInFormat(alias, field, list, notIn, false);
            }
            return Filter;
        }

        /// <summary>
        /// Return the format for a IN Filter. Eg. C.ID IN (1, 2, 3, 4)
        /// </summary>
        /// <param name="alias">Alias for the table</param>
        /// <param name="field">Name of the field to be used in the filter</param>
        /// <param name="condition">The condition to be put into the IN statement</param>
        /// <param name="notIn">Use NOT IN</param>
        /// <param name="useQuotation">Use quotations around the condition/param>
        /// <returns>Return the format for a IN Filter.</returns>
        public static string FilterInFormat(string alias, string field, string condition, bool notIn = false, bool useQuotation = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(field))
            {
                if (useQuotation)
                    condition = "'" + condition + "'";

                Filter = FieldFormat(alias, field) + (notIn ? " NOT" : "") + " IN (" + condition + ")";
            }
            return Filter;
        }

        /// <summary>
        /// Returns a filter format for a field to be between or outside of two fields
        /// </summary>
        /// <param name="aliasField">Alias and field for the table</param>
        /// <param name="conditionOne">The first condition to be compared to</param>
        /// <param name="conditionTwo">The second condition to be compared to</param>
        /// <param name="notBetween">Filter to values outside of the range</param>
        /// <param name="useQuotation">Use quotations around the conditions</param>
        /// <returns>The format for a between filter</returns>
        public static string FilterBetweenFormat(string aliasField, string conditionOne, string conditionTwo, bool notBetween = false, bool useQuotation = false)
        {
            string Filter = "";
            if (!string.IsNullOrEmpty(aliasField))
            {
                if (useQuotation)
                {
                    conditionOne = "'" + conditionOne + "'";
                    conditionTwo = "'" + conditionTwo + "'";
                }

                var firstCompare = notBetween ? " < " : " >= ";
                var secondCompare = notBetween ? " > " : " <= ";
                var compareType = notBetween ? " OR " : " AND ";
                Filter = aliasField + firstCompare + conditionOne + compareType + aliasField + secondCompare + conditionTwo;

                if (notBetween)
                    Filter = "(" + Filter + ")";
            }
            return Filter;
        }

        /// <summary>
        /// Return the format for a CASE WHEN statement. Eg. CASE WHEN C.Name = 'Fluffy' THEN 1 ELSE 0
        /// </summary>
        /// <param name="cases">The multiple cases. Tuples of (CONDITION, OUTPUT)</param>
        /// <param name="elseCase">The final ELSE case</param>
        /// <returns>A CASE WHEN statement</returns>
        public static string CaseWhenFormat(string elseCase, params Tuple<string, string>[] cases)
        {
            string CaseWhen = "";
            if (cases.Length > 0 && !string.IsNullOrEmpty(elseCase))
            {
                CaseWhen = "CASE";

                foreach (var caseStmt in cases)
                    CaseWhen += " WHEN " + caseStmt.Item1 + " THEN " + caseStmt.Item2;

                CaseWhen += " ELSE " + elseCase + " END";
            }
            return CaseWhen;
        }

        /// <summary>
        /// Returns the formatted sql Cast for given field as a type
        /// </summary>
        /// <param name="field"></param>
        /// <param name="type"></param>
        public static string CastFormat(string field, string type)
        {
            string Cast = "";
            if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(type))
            {
                Cast = "CAST( " + field + " AS " + type + " )";
            }
            return Cast;
        }

        /// <summary>
        /// Return the formatted string for a function column
        /// </summary>
        /// <param name="function"></param>
        /// <param name="tableAliasFields"></param>
        public static string FunctionFormat(Functions function, params string[] tableAliasFields)
        {
            return function.Description() + "(" + string.Join(", ", tableAliasFields) + ")";
        }

        /// <summary>
        /// Return the formatted string for an IsNull Format.
        /// </summary>
        public static string IsNullFormat(string alias, string field, string thenAliasField)
        {
            //This function just looks better in the code rather than using FunctionFormat when building queries
            return FunctionFormat(Functions.IsNull, FieldFormat(alias, field), thenAliasField);
        }

        /// <summary>
        /// Return a query string containing all the given queries
        /// </summary>
        public static string CombinedQuery(params SqlQuery[] queries)
        {
            return string.Join("; ", queries.Select(x => x.GetQuery()));
        }


        /// <summary>
        /// Add a column to the query.
        /// </summary>
        /// <param name="tableAlias">The alias of the table</param>
        /// <param name="field">The field for this column</param>
        /// <param name="asName">Optional name for the column</param>
        /// <param name="addGroupBy">Should this be included in the Group By?</param>
        public SqlQuery AddColumn(string tableAlias, string field, string asName = "", bool addGroupBy = false)
        {
            AddColumn(FieldFormat(tableAlias, field, asName));

            if (addGroupBy)
                GroupBy(FieldFormat(tableAlias, field));

            return this;
        }

        /// <summary>
        /// Add a column to the query.
        /// </summary>
        /// <param name="tableAlias">The alias of the table</param>
        /// <param name="addGroupBy">Should this be included in the Group By?</param>
        public SqlQuery AddColumns(string tableAlias, string[] fields, bool addGroupBy = false)
        {
            AddColumn(FieldFormat(tableAlias, fields));

            if (addGroupBy)
                GroupBy(FieldFormat(tableAlias, fields));

            return this;
        }

        public SqlQuery AddColumns(IEnumerable<string> fields)
        {
            foreach (var field in fields)
                AddColumn(field);

            return this;
        }

        /// <summary>
        /// Add a column to the query
        /// </summary>
        /// <param name="column"></param>
        public SqlQuery AddColumn(string column, bool addGroupBy = false, string asName = null)
        {
            if (!string.IsNullOrEmpty(column))
                Columns.Add(column + (asName != null ? " AS " + asName : ""));

            if (addGroupBy)
                GroupBy(column);

            return this;
        }

        /// <summary>
        /// Add a column to the query which has a function.
        /// </summary>
        /// <param name="function">The function for this column. Eg. SUM, COUNT, AVG</param>
        /// <param name="asName">Optional name for the column</param>
        public SqlQuery AddFunctionColumn(Functions function, string asName = "", params string[] tableAliasFields)
        {
            if (tableAliasFields.Count() > 0)
                AddColumn(function.Description() + "(" + string.Join(", ", tableAliasFields) + ")" + (!string.IsNullOrEmpty(asName) ? " AS " + asName : ""));

            return this;
        }

        /// <summary>
        /// Add a null column
        /// </summary>
        /// <param name="asName">Optional name for the column. Useful for documentation.</param>
        public SqlQuery AddNullColumn(string asName = "")
        {
            AddColumn("NULL" + (!string.IsNullOrEmpty(asName) ? " AS " + asName : ""));
            return this;
        }

        /// <summary>
        /// Add a column which formats a full name as joined - Common event in Z Code
        /// </summary>
        /// <param name="asName">Optional name for the column. Useful for documentation.</param>
        public SqlQuery AddFullNameColumn(string aliasFirstName, string fieldFirstName, string aliasLastName, string fieldLastName, string asName = null)
        {
            var firstNameFormat = SqlQuery.FunctionFormat(Functions.IsNull, SqlQuery.FieldFormat(aliasFirstName, fieldFirstName), "''");
            var lastNameFormat = SqlQuery.FunctionFormat(Functions.IsNull, "' ' + " + SqlQuery.FieldFormat(aliasLastName, fieldLastName), "''");
            AddColumn(firstNameFormat + " + " + lastNameFormat, asName: asName);
            return this;
        }

        /// <summary>
        /// Add a SET option to the query
        /// </summary>
        /// <param name="option">Full option text</param>
        public SqlQuery AddSet(string option)
        {
            if (!string.IsNullOrEmpty(option))
                Sets.Add(option);

            return this;
        }


        /// <summary>
        /// Add a FROM to the query
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="alias">an alias for the table</param>
        public SqlQuery From(string tableName, string alias = "")
        {
            return From(TableFormat(tableName, alias));
        }

        /// <summary>
        /// Add a FROM to the query
        /// </summary>
        /// <param name="from">The table we are 'FROMING' from</param>
        public SqlQuery From(string from)
        {
            if (!string.IsNullOrEmpty(from))
                Froms.Add(from);

            return this;
        }

        /// <summary>
        /// Add a FROM to the query, using a subquery as the FROM
        /// </summary>
        public SqlQuery From(SqlQuery from, string alias = "")
        {
            if (from != null)
                From("(" + from.Query + ")", alias);

            return this;
        }

        /// <summary>
        /// Add a join to the query
        /// </summary>
        /// <param name="joinType">The type of join (eg. Inner, Outer...)</param>
        /// <param name="table">The table we are joining to</param>
        /// <param name="tableAlias">The alias for the table we are joining to</param>
        /// <param name="tableJoinField">The field we are using to join, from the table we are joining to</param>
        /// <param name="relatedTableAlias">The table we are joining from</param>
        /// <param name="relatedField">The field we are using to join from</param>
        /// <param name="comparison">The comparison of the join between the fields</param>
        public SqlQuery Join(JoinTypes joinType, string table, string tableAlias, string tableJoinField, string relatedTableAlias, string relatedField, Comparisons comparison = Comparisons.Equal)
        {
            return Join(joinType, TableFormat(table, tableAlias), FieldFormat(tableAlias, tableJoinField), FieldFormat(relatedTableAlias, relatedField), comparison);
        }

        public SqlQuery Join(JoinTypes joinType, string table, string tableAlias, string tableJoinField, string relatedTableAlias, string relatedField, Comparisons comparison = Comparisons.Equal, params Filter[] moreFilters)
        {
            return Join(joinType, TableFormat(table, tableAlias), FieldFormat(tableAlias, tableJoinField), FieldFormat(relatedTableAlias, relatedField), comparison, moreFilters);
        }

        /// <summary>
        /// Add a join to the query
        /// </summary>
        /// <param name="joinType">The type of join (eg. Inner, Outer...)</param>
        /// <param name="table">The query we are joining to</param>
        /// <param name="tableAlias">The alias for the table we are joining to</param>
        /// <param name="tableJoinField">The field we are using to join, from the table we are joining to</param>
        /// <param name="relatedTableAlias">The table we are joining from</param>
        /// <param name="relatedField">The field we are using to join from</param>
        /// <param name="comparison">The comparison of the join between the fields</param>
        public SqlQuery Join(JoinTypes joinType, SqlQuery table, string tableAlias, string tableJoinField, string relatedTableAlias, string relatedField, Comparisons comparison = Comparisons.Equal)
        {
            return Join(joinType, TableFormat("(" + table.Query + ")", tableAlias), FieldFormat(tableAlias, tableJoinField), FieldFormat(relatedTableAlias, relatedField), comparison);
        }

        /// <summary>
        /// Add a join to the query
        /// </summary>
        /// <param name="joinType">The type of join (eg. Inner, Outer...</param>
        /// <param name="tableJoin">The table we are joining to. Eg Cats C</param>
        /// <param name="tableJoinField">The alias + field we are joining to. Eg C.ID</param>
        /// <param name="relatedTableField">The table + field we are joining from. Eg. D.CatID</param>
        /// <param name="comparison">The comparison of the join between the fields</param>
        public SqlQuery Join(JoinTypes joinType, string tableJoin, string tableJoinField, string relatedTableField, Comparisons comparison = Comparisons.Equal, params Filter[] moreFilters)
        {
            List<Filter> moreFiltersList = new List<Filter>();
            moreFiltersList.Add(new Filter(tableJoinField, relatedTableField, comparison)); //We want the main join condition first, for readability.
            if (moreFilters != null && moreFilters.Length > 0) moreFiltersList.AddRange(moreFilters.ToList());

            return Join(joinType, tableJoin, moreFiltersList.ToArray());
        }

        /// <summary>
        /// Add a join to the query
        /// </summary>
        /// <param name="joinType">The way we are joining</param>
        public SqlQuery Join(JoinTypes joinType, string joinTable, params Filter[] filters)
        {
            if (!string.IsNullOrEmpty(joinTable) && filters.Count() > 0)
                Joins.Add(new Join(joinType, joinTable, filters.ToList()));

            return this;
        }

        /// <summary>
        /// Add a join to the query
        /// </summary>
        /// <param name="join">The join we are adding</param>
        public SqlQuery Join(Join join)
        {
            if (join != null)
                Joins.Add(join);

            return this;
        }


        public SqlQuery Where(string alias, string field, string condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterFormat(alias, field, condition, comparison, useQuotes), addType, include);
        }

        public SqlQuery Where(string aliasField, string condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterFormat(aliasField, condition, comparison, useQuotes), addType, include);
        }

        public SqlQuery Where(string alias, string field, int condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterFormat(alias, field, condition.ToString(), comparison, useQuotes), addType, include);
        }

        /// <summary>
        /// I would recommend using an @Alias for the Guid instead of using this method, as SQL processes it faster.
        /// </summary>
        public SqlQuery Where(string aliasField, Guid condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            return Where(FilterFormat(aliasField, condition.ToString(), comparison, true), addType, include);
        }

        /// <summary>
        /// I would recommend using an @Alias for the Guid instead of using this method, as SQL processes it faster.
        /// </summary>
        public SqlQuery Where(string alias, string field, Guid condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            return Where(FilterFormat(alias, field, condition.ToString(), comparison, true), addType, include);
        }

        /// <summary>
        /// WHERE statement which uses a character as a condition - Useful for status filters
        /// </summary>
        public SqlQuery Where(string alias, string field, char condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            return Where(FilterFormat(alias, field, condition.ToString(), comparison, true), addType, include);
        }

        public SqlQuery Where(string aliasField, int condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterFormat(aliasField, condition.ToString(), comparison, useQuotes), addType, include);
        }

        /// <summary>
        /// Add a filter using a parameter and object. Will require the use of .GetDapperParameter() or .GetParameter() later to retreive the value.
        /// </summary>
        public SqlQuery Where(string aliasField, string parameter, object value, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            AddParameter(parameter, value);
            return Where(FilterFormat(aliasField, parameter, comparison, useQuotes), addType, include);
        }


        public SqlQuery WhereIn(string alias, string field, List<string> listConditions, bool notIn = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterInFormat(alias, field, listConditions, notIn, useQuotes), addType, include);
        }

        public SqlQuery WhereIn(string alias, string field, List<Guid> listConditions, bool notIn = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterInFormat(alias, field, SqlBuilderExtensions.GetListAsSQLSafeString(listConditions), notIn, useQuotes), addType, include);
        }

        public SqlQuery WhereIn(string alias, string field, string conditions, bool notIn = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterInFormat(alias, field, conditions, notIn, useQuotes), addType, include);
        }

        public SqlQuery WhereIn(string alias, string field, SqlQuery conditions, bool notIn = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterInFormat(alias, field, conditions.Query, notIn, useQuotes), addType, include);
        }

        public SqlQuery WhereBetween(string alias, string field, string conditionOne, string conditionTwo, bool notBetween = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return WhereBetween(FieldFormat(alias, field), conditionOne, conditionTwo, notBetween, addType, useQuotes, include);
        }

        public SqlQuery WhereBetween(string aliasField, string conditionOne, string conditionTwo, bool notBetween = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Where(FilterBetweenFormat(aliasField, conditionOne, conditionTwo, notBetween, useQuotes), addType, include);
        }

        public SqlQuery HavingBetween(string aliasField, string conditionOne, string conditionTwo, bool notBetween = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Having(FilterBetweenFormat(aliasField, conditionOne, conditionTwo, notBetween, useQuotes), addType, include);
        }

        public SqlQuery Having(string aliasField, string condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Having(FilterFormat(aliasField, condition, comparison, useQuotes), addType, include);
        }

        public SqlQuery Having(string alias, string field, string conditionOne, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            return Having(FilterFormat(alias, field, conditionOne, comparison, useQuotes), addType, include);
        }

        /// <summary>
        /// Add a filter for where the field is (or is not) null
        /// </summary>
        /// <param name="alias">The alias of the table</param>
        /// <param name="field">The field name</param>
        /// <param name="notNull">Should this field be not null?</param>
        /// <param name="addType">How this filter will be added</param>
        public SqlQuery WhereNull(string alias, string field, bool notNull = false, FilterAddType addType = FilterAddType.And)
        {
            return Where(FilterIsNullFormat(alias, field, notNull), addType);
        }

        /// <summary>
        /// Add a filter for EXISTS
        /// </summary>
        public SqlQuery WhereExists(string query, FilterAddType addType = FilterAddType.And)
        {
            return Where("EXISTS(" + query + ")", addType);
        }

        /// <summary>
        /// Add a filter for EXISTS
        /// </summary>
        public SqlQuery WhereExists(SqlQuery query, FilterAddType addType = FilterAddType.And)
        {
            return Where("EXISTS(" + query.Query + ")", addType);
        }

        /// <summary>
        /// Add a WHERE filter to the query
        /// </summary>
        /// <param name="filter">The filter to add</param>
        /// <param name="addType">How this filter will be added</param>
        public SqlQuery Where(string filter, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            if (include && !string.IsNullOrEmpty(filter))
            {
                Filters.Add(new Filter(filter, addType));
            }
            return this;
        }

        /// <summary>
        /// Add a WHERE filter to the query
        /// </summary>
        /// <param name="filter">The filter to add</param>
        public SqlQuery Where(Filter filter)
        {
            if (filter != null)
                Filters.Add(filter);

            return this;
        }

        /// <summary>
        /// Add multiple filters contained by brackets. Eg.  ... OR (CAT.Name = 'Fluffy' AND CAT.Personality = 'Nice')
        /// </summary>
        /// <param name="filters">The multiple filters to group and add</param>
        public SqlQuery WhereGroup(FilterAddType addType = FilterAddType.And, params Filter[] filters)
        {
            if (filters.Count() > 0)
                WhereGroup(filters.ToList(), addType);

            return this;
        }

        /// <summary>
        /// Add multiple filters contained by brackets. Eg.  ... OR (CAT.Name = 'Fluffy' AND CAT.Personality = 'Nice')
        /// </summary>
        /// <param name="filters">The multiple filters to group and add</param>
        public SqlQuery WhereGroup(List<Filter> filters, FilterAddType addType = FilterAddType.And)
        {
            if (filters?.Count() > 0)
                Filters.Add(new Filter(filters, addType));

            return this;
        }

        /// <summary>
        /// Add FOR statements. Eg. FOR XML PATH('')
        /// </summary>
        public SqlQuery For(string For)
        {
            if (Fors == null)
                Fors = new List<string>();

            if (!string.IsNullOrEmpty(For))
                Fors.Add(For);

            return this;
        }

        /// <summary>
        /// Add a parameter to be used for the query command. Eg. @DateStart and filter.Value1
        /// </summary>
        public SqlQuery AddParameter(string name, object value, bool addTag = false)
        {
            if (Parameters == null)
                Parameters = new List<Tuple<string, object>>();

            if (!string.IsNullOrEmpty(name) && value != null)
                Parameters.Add(new Tuple<string, object>((addTag ? "@" : "") + name, value));

            return this;
        }

        /// <summary>
        /// Add a parameter to be used for the query command. Eg. @DateStart and filter.Value1
        /// </summary>
        public SqlQuery AddParameters(Dictionary<string, object> parameters)
        {
            foreach (var p in parameters)
            {
                AddParameter(p.Key, p.Value);
            }

            return this;
        }


        /// <summary>
        /// Add a order by to the query
        /// </summary>
        /// <param name="orderBy">The order by we are adding</param>
        public SqlQuery OrderBy(string orderBy, bool descending = false)
        {
            if (!string.IsNullOrEmpty(orderBy))
                OrderBys.Add(new Tuple<string, bool>(orderBy, descending));

            return this;
        }


        public SqlQuery OffSetFetch(long offset, long? fetch = null)
        {
            OrderByOffset = (offset, fetch);
            return this;
        }

        /// <summary>
        /// Add a order by to the query
        /// </summary>
        public SqlQuery OrderBy(string tableAlias, string tableField, bool descending = false)
        {
            if (!string.IsNullOrEmpty(tableField))
                OrderBy(FieldFormat(tableAlias, tableField), descending);

            return this;
        }

        /// <summary>
        /// Add a group by to the query
        /// </summary>
        /// <param name="groupBy">The group by we are adding</param>
        public SqlQuery GroupBy(string groupBy)
        {
            if (!string.IsNullOrEmpty(groupBy))
                GroupBys.Add(groupBy);

            return this;
        }

        /// <summary>
        /// Add a group by to the query
        /// </summary>
        public SqlQuery GroupBy(string tableAlias, string tableField)
        {
            if (!string.IsNullOrEmpty(tableField))
                GroupBys.Add(FieldFormat(tableAlias, tableField));

            return this;
        }

        /// <summary>
        /// Add a HAVING filter to the query
        /// </summary>
        /// <param name="filter">The filter to add</param>
        /// <param name="addType">How this filter will be added</param>
        public SqlQuery Having(string filter, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            if (include)
            {
                if (!string.IsNullOrEmpty(filter))
                    Havings.Add(new Tuple<string, FilterAddType>(filter, addType));
            }
            return this;
        }


        /// <summary>
        /// Add a union to the query. The union will be checked for atleast some validity before being added.
        /// </summary>
        /// <param name="union">The query we are unioning to</param>
        /// <param name="unionType">The type of union we want. Defaults as just a normal union.</param>
        public SqlQuery Union(SqlQuery union, UnionsTypes unionType = UnionsTypes.Empty)
        {
            if (union.IsValidQuery())
                Union(union.Query, unionType);

            return this;
        }

        /// <summary>
        /// Add a union to the query. This is the overriding method, so the query won't be checked for any validity.
        /// </summary>
        /// <param name="union">The query we are unioning to</param>
        /// <param name="unionType">The type of union we want. Defaults as just a normal union.</param>
        public SqlQuery Union(string union, UnionsTypes unionType = UnionsTypes.Empty)
        {
            if (!string.IsNullOrEmpty(union))
                Unions.Add(new Tuple<string, UnionsTypes>(union, unionType));

            return this;
        }

        /// <summary>
        /// Insert values into a table. Values added by InsertInto().
        /// </summary>
        public SqlQuery InsertIntoTable(string table)
        {
            if (!string.IsNullOrEmpty(table))
                T_Insert = table;

            return this;
        }

        /// <summary>
        /// Add columns to be inserted into. Eg. INSERT INTO X (col1, col2). And if added as values, then also VALUES (@col1, @col2)
        /// </summary>
        /// <param name="addAsValue">Set these as the values, as a parameter @Value/param>
        public SqlQuery InsertInto(bool addAsValue = true, params string[] columns)
        {
            columns = columns.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (columns.Count() > 0)
            {
                InsertColumns.AddRange(columns);

                if (addAsValue)
                    InsertIntoValues(columns.Select(x => "@" + x).ToArray());
            }

            return this;
        }

        /// <summary>
        /// Add columns to be inserted into. Eg. INSERT INTO X (col1, col2). And if added as values, then also VALUES (@col1, @col2)
        /// </summary>
        /// <param name="addAsValue">Set these as the values, as a parameter @Value/param>
        public SqlQuery InsertInto(bool addAsValue = true, List<string> columns = null)
        {
            columns = columns.Where(x => !string.IsNullOrEmpty(x)).ToList();
            if (columns.Count() > 0)
            {
                InsertColumns.AddRange(columns);

                if (addAsValue)
                    InsertIntoValues(columns.Select(x => "@" + x).ToArray());
            }

            return this;
        }

        /// <summary>
        /// Add a column to be inserted into
        /// </summary>
        /// <param name="addAsValue">Set this as the value, as a parameter @Value</param>
        public SqlQuery InsertInto(string column, bool addAsValue = true)
        {
            if (!string.IsNullOrEmpty(column))
            {
                InsertColumns.Add(column);

                if (addAsValue)
                    InsertValues.Add("@" + column);
            }

            return this;
        }

        /// <summary>
        /// Insert multiple values to be inserted into columns, all entries will be as one grouping. Eg VALUES (item1, item2, item3)
        /// </summary>
        public SqlQuery InsertIntoValues(params string[] values)
        {
            if (values.Count() > 0)
                InsertValues.Add(string.Join(", ", values));

            return this;
        }

        /// <summary>
        /// Insert a value to be inserted into columns. This will count as one grouping! Eg VALUES (value)
        /// </summary>
        public SqlQuery InsertIntoValues(string value)
        {
            if (!string.IsNullOrEmpty(value))
                InsertValues.Add(value);

            return this;
        }

        /// <summary>
        /// Deletes rows from a table. Can be filted on using Where().
        /// </summary>
        public SqlQuery DeleteFromTable(string table)
        {
            if (!string.IsNullOrEmpty(table))
                T_Delete = table;

            return this;
        }

        /// <summary>
        /// Deletes rows from a table.
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="tableAlias">an alias for the table</param>
        public SqlQuery DeleteFromTable(string tableName, string tableAlias)
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                T_Delete = tableName;
                T_Delete_Alias = tableAlias;
            }

            return this;
        }

        /// <summary>
        /// Set the table to be updated by the query.
        /// </summary>
        public SqlQuery UpdateTable(string table)
        {
            if (!string.IsNullOrEmpty(table))
                T_Update = table;

            return this;
        }

        /// <summary>
        /// Update to this table, else insert if nothing is updated. For when you are too lazy to set insert and update table separately.
        /// </summary>
        public SqlQuery UpdateElseInsertTable(string table)
        {
            InsertIntoTable(table);
            UpdateTable(table);
            return this;
        }

        /// <summary>
        /// Insert a column to be updated, and what it should be updated to
        /// </summary>
        public SqlQuery UpdateColumn(string column, string value, bool useQuotes = false)
        {
            if (!string.IsNullOrEmpty(column) && !string.IsNullOrEmpty(value))
            {
                if (useQuotes)
                    value = "'" + value + "'";

                UpdateColumnValue.Add(new Tuple<string, string>(column, value));
            }
            return this;
        }

        public SqlQuery UpdateColumn(string column, Guid value)
        {
            if (!string.IsNullOrEmpty(column))
            {
                string guidValue = "'" + value.ToString() + "'";
                UpdateColumnValue.Add(new Tuple<string, string>(column, guidValue));
            }
            return this;
        }

        /// <summary>
        /// Insert a column to be updated, using the column name as a parameter. Eg. ID = @ID
        /// </summary>
        public SqlQuery UpdateColumnWithParam(string column)
        {
            if (!string.IsNullOrEmpty(column))
                UpdateColumnValue.Add(new Tuple<string, string>(column, "@" + column));
            return this;
        }

        /// <summary>
        /// Insert a column to be updated, using the column name as a parameter. Eg. ID = @ID
        /// </summary>
        public SqlQuery UpdateColumnWithParam(params string[] columns)
        {
            foreach (var col in columns)
                UpdateColumnWithParam(col);
            return this;
        }

        /// <summary>
        /// Insert a column to be updated with an alias, using the column name as a parameter. Eg. C.ID = @ID
        /// </summary>
        public SqlQuery UpdateColumnAliasWithParam(string alias, string column)
        {
            if (!string.IsNullOrEmpty(column))
                UpdateColumnValue.Add(new Tuple<string, string>(FieldFormat(alias, column), "@" + column));
            return this;
        }

        /// <summary>
        /// Insert multiple columns to be updated with an alias, using the column name as a parameter. Eg. C.ID = @ID
        /// </summary>
        public SqlQuery UpdateColumnAliasWithParam(string alias, params string[] columns)
        {
            foreach (var col in columns)
                UpdateColumnAliasWithParam(alias, col);
            return this;
        }


        /// <summary>
        /// Adds a Declare @Variable TYPE at the start of the query. Takes a string so you just declare it as the full line
        /// </summary>
        public SqlQuery AddDeclaration(string declaration)
        {
            Declarations.Add(declaration);
            return this;
        }


        /// <summary>
        /// Adds a With @withName as (@Query)
        /// </summary>
        public SqlQuery AddWith(string withName, SqlQuery query)
        {
            Withs[withName] = query;
            return this;
        }

        /// <summary>
        /// Just puts some SQL code at the start of the main query, for complex with statements etc
        /// </summary>
        public SqlQuery AddStartQuery(string query)
        {
            startQuery = query;
            return this;
        }

        /// <summary>
        /// Build the actual query from what has been given so far
        /// </summary>
        public SqlQuery BuildQuery()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(T_Insert) && !string.IsNullOrEmpty(T_Update)) //---Update if exists, else Insert----
            {
                BuildUpdate(sb);
                sb.Append(" IF @@ROWCOUNT=0 "); //Attempt to update, but if no rows updated then insert will be called
                BuildInsert(sb, append: true);
            }
            else if (!string.IsNullOrEmpty(T_Delete)) //---Delete---
            {
                BuildDelete(sb);
            }
            else if (!string.IsNullOrEmpty(T_Insert)) //---Insert---
            {
                BuildInsert(sb);
            }
            else if (!string.IsNullOrEmpty(T_Update)) //---Update---
            {
                BuildUpdate(sb);
            }
            else
            {
                sb.Clear();

                sb.Append(startQuery);

                //---Unions---                
                if (!IsValidQuery() && Unions.Count > 0) //If we don't have a valid query here, but we have unions, then output them. 
                {
                    //Idea: Maybe change unions to be SqlQuery, so we can check the validity of each union to decide to include it or not.
                    sb.Clear();
                    sb.Append(Unions.FirstOrDefault().Item1);
                    foreach (var union in Unions.Skip(1))
                        sb.Append(" UNION " + (union.Item2 != UnionsTypes.Empty ? union.Item2.Description() : "") + " " + union.Item1 + " ");

                    query = sb.ToString();
                    return this; //The rest is invalid, dont do it.
                }

                //----Declare----
                BuildDeclare(sb);

                //----With----
                BuildWith(sb);

                //----Set----
                BuildSet(sb);

                sb.Append("SELECT ");

                //---Distinct----
                if (Distinct)
                    sb.Append("DISTINCT ");

                //----Top----
                if (Top != 0)
                    sb.Append("TOP " + Top.ToString() + " ");

                //----Select----
                if (Columns.Count > 0)
                {
                    string selectQuery = string.Join(", ", Columns);
                    sb.Append(selectQuery + " ");
                }

                //----From----
                if (Froms.Count > 0)
                {
                    sb.Append("FROM ");
                    string fromQuery = string.Join(", ", Froms);
                    sb.Append(fromQuery + " ");
                }

                //----Join----
                BuildJoins(sb);

                //----Where----
                BuildFilter(sb);

                //----For----
                BuildFor(sb);

                //----Group By----
                if (GroupBys.Count > 0)
                {
                    string groupByQuery = "GROUP BY " + string.Join(", ", GroupBys);
                    sb.Append(groupByQuery + " ");
                }

                //----Having----
                BuildHaving(sb);

                //----Order By----
                if (OrderBys.Count > 0)
                {
                    string orderByQuery = "ORDER BY " + string.Join(", ", OrderBys.Select(x => x.Item1 + (x.Item2 ? " DESC" : "")));
                    sb.Append(orderByQuery + " ");

                    if (OrderByOffset.HasValue)
                    {
                        sb.Append($@"OFFSET {OrderByOffset.Value.Offset} ROWS ");

                        if (OrderByOffset.Value.Fetch.HasValue)
                        {
                            sb.Append($@"FETCH NEXT {OrderByOffset.Value.Fetch.Value} ROWS ONLY ");
                        }
                    }
                }

                //----Union----
                if (Unions.Count > 0)
                {
                    string unionQuery = "";
                    foreach (var union in Unions)
                        unionQuery += "UNION " + (union.Item2 != UnionsTypes.Empty ? union.Item2.Description() : "") + " " + union.Item1 + " ";

                    sb.Append(unionQuery);
                }
            }


            query = sb.ToString();

            return this;
        }

        /// <summary>
        /// Get the query. Should be used if we want the debugger to print the query for easier debugging.
        /// </summary>
        /// <returns>The SQL Query as a string</returns>
        public string GetQuery()
        {
#if DEBUG
            Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + ": " + Query);
#endif
            return Query;
        }

        /// <summary>
        /// Get the parameters for this query
        /// </summary>
        public List<Tuple<string, object>> GetParameters()
        {
            if (Parameters == null)
                Parameters = new List<Tuple<string, object>>();

            return Parameters;
        }

        /// <summary>
        /// Return the parameters as a dictonary which can be directly input into a Dapper parameter
        /// </summary>
        public IDictionary<string, object> GetDapperParameters()
        {
            IDictionary<string, object> convParameters = new ExpandoObject();
            Parameters?.ForEach(p => convParameters.Add(p.Item1.Replace("@", ""), p.Item2 == DBNull.Value ? null : p.Item2));
            return convParameters;
        }

        /// <summary>
        /// Add a list of parameters to this queries parameters (useful when combining queries)
        /// </summary>
        public SqlQuery CombineParameters(List<Tuple<string, object>> otherParams)
        {
            if (Parameters == null)
                Parameters = new List<Tuple<string, object>>();

            if (otherParams != null)
                Parameters.AddRange(otherParams);

            return this;
        }

        /// <summary>
        /// Build the filter part of the query
        /// </summary>
        public StringBuilder BuildFilter(StringBuilder sb, bool includePagination = true)
        {
            bool whereAdded = false;
            if (Filters.Count > 0)
            {
                string filterQuery = "WHERE " + GetFilterPart();
                sb.Append(filterQuery);
                whereAdded = true;
            }


            if (PaginationFilters.Count > 0 && includePagination)
            {
                if (!whereAdded)
                    sb.Append("WHERE ");
                sb.Append(GetPaginationFilterPart(!whereAdded));
            }

            return sb;
        }

        /// <summary>
        /// Build the FOR part of the query
        /// </summary>
        public StringBuilder BuildFor(StringBuilder sb)
        {
            if (Fors?.Any() == true)
                sb.Append("FOR " + string.Join(", ", Fors));

            return sb;
        }

        public StringBuilder BuildInsert(StringBuilder sb, bool includeSelects = true, bool append = false)
        {
            if (!append)
                sb.Clear();

            BuildSet(sb);
            sb.Append(" INSERT INTO " + T_Insert + " (" + string.Join(", ", InsertColumns) + ") VALUES ");

            var containedInsertValues = InsertValues.Select(x => "(" + x + ")");
            sb.Append(string.Join(", ", containedInsertValues));

            if (includeSelects && Columns.Count > 0)
            {
                sb.Append(Environment.NewLine + " SELECT ");

                string selectQuery = string.Join(", ", Columns);
                sb.Append(selectQuery + " ");
            }

            return sb;
        }

        public StringBuilder BuildUpdate(StringBuilder sb)
        {
            sb.Clear();
            BuildSet(sb);
            sb.Append("UPDATE " + T_Update + " SET ");
            sb.Append(string.Join(", ", UpdateColumnValue.Select(x => x.Item1 + " = " + x.Item2)) + " ");

            if (Froms.Count > 0)
            {
                sb.Append(" FROM ");
                string fromQuery = string.Join(", ", Froms);
                sb.Append(fromQuery + " ");

                BuildJoins(sb);
            }

            BuildFilter(sb);

            return sb;
        }

        public StringBuilder BuildSet(StringBuilder sb)
        {
            foreach (string set in Sets)
                sb.Append("SET " + set + " ");

            return sb;
        }

        public StringBuilder BuildDeclare(StringBuilder sb)
        {
            foreach (string declare in Declarations)
                sb.Append(declare + "; ");

            return sb;
        }

        public StringBuilder BuildWith(StringBuilder sb)
        {
            var firstWith = true;
            foreach (var with in Withs)
            {
                var withLine = firstWith ? "WITH " : ", ";

                sb.Append(withLine + with.Key + " AS (" + with.Value.GetQuery() + " )");

                firstWith = false;
            }

            return sb;
        }

        public StringBuilder BuildDelete(StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(T_Delete))
            {
                sb.Clear();
                sb.Append("DELETE " + T_Delete_Alias + " FROM " + T_Delete + " " + T_Delete_Alias);
            }

            BuildJoins(sb);

            BuildFilter(sb);

            return sb;
        }

        /// <summary>
        /// Return the filter part of the query without the 'where' statement. Useful for enclosed filter statements. Eg. WHERE C.ID = 1 AND (C.TYPE = 'A' OR C.Name = 'Bee')
        /// </summary>
        public string GetFilterPart()
        {
            string filterPart = " ";
            bool firstfilter = true;
            foreach (var filter in Filters)
            {
                filterPart += filter.BuildFilter(firstfilter);
                firstfilter = false;
            }

            return filterPart;
        }

        /// <summary>
        /// Return the filter part of the query without the 'where' statement. Useful for enclosed filter statements. Eg. WHERE C.ID = 1 AND (C.TYPE = 'A' OR C.Name = 'Bee')
        /// </summary>
        public string GetPaginationFilterPart(bool firstfilter = true)
        {
            string filterPart = " ";
            foreach (var filter in PaginationFilters)
            {
                filterPart += filter.BuildFilter(firstfilter);
                firstfilter = false;
            }

            return filterPart;
        }

        /// <summary>
        /// Build the join part of the query
        /// </summary>
        public StringBuilder BuildJoins(StringBuilder sb)
        {
            if (Joins.Count > 0)
                sb.Append(" " + GetJoinPart());

            return sb;
        }

        /// <summary>
        /// Return the join part of the query
        /// </summary>
        public string GetJoinPart()
        {
            string joinPart = "";
            foreach (var join in Joins)
            {
                if (!string.IsNullOrEmpty(join.OverrideJoin))
                    joinPart += " " + join.OverrideJoin + " ";
                else
                    joinPart += join.JoinType.Description() + " " + join.JoinTable + " ON " + join.BuildJoinFilter();
            }

            return joinPart;
        }

        /// <summary>
        /// Build the Having part of the query
        /// </summary>
        public SqlQuery BuildHaving(StringBuilder sb)
        {
            if (Havings.Count > 0)
            {
                bool firstHaving = true;
                string havingQuery = "HAVING ";
                foreach (var having in Havings)
                {
                    havingQuery += (!firstHaving ? having.Item2.Description() : "") + " " + having.Item1 + " ";
                    firstHaving = false;
                }

                sb.Append(havingQuery);
            }

            return this;
        }

        /// <summary>
        /// Build the Having part of the query
        /// </summary>
        public SqlQuery BuildUnion(StringBuilder sb)
        {
            if (Unions.Count > 0)
            {
                string unionQuery = "";
                if (string.IsNullOrEmpty(sb.ToString())) //If something has gone bad in the main query, just the first join is now the main query.
                {
                    foreach (var union in Unions)
                        unionQuery += (string.IsNullOrEmpty(unionQuery) ? "" : "UNION ") + (union.Item2 != UnionsTypes.Empty ? union.Item2.Description() : "") + " " + union.Item1 + " ";
                }
                else
                {
                    foreach (var union in Unions)
                        unionQuery += "UNION " + (union.Item2 != UnionsTypes.Empty ? union.Item2.Description() : "") + " " + union.Item1 + " ";
                }

                sb.Append(unionQuery);
            }

            return this;
        }

        /// <summary>
        /// Format the string to be SQL safe as best we can
        /// </summary>
        public static string SQLSafeFormat(string s)
        {
            s = s.Replace("'", "''");
            s = s.Replace("\'", "''");
            return s;
        }

        /// <summary>
        /// Return a array of objects as a SQL Safe literal string array
        /// </summary>
        public static string[] GetSQLSafeArray(object[] items)
        {
            string[] output = new string[items.Count()];
            for (int i = 0; i < items.Count(); i++)
            {
                output[i] = SqlBuilderExtensions.GetLiteralStringForSQL(items[i]);
            }

            return output;
        }

        public override string ToString()
        {
            return Query;
        }

        /// <summary>
        /// Get a string list from a guid list
        /// </summary>
        /// <param name="guidList">The list of guids to convert</param>
        public static List<string> GuidToStringList(List<Guid> guidList)
        {
            List<string> convertedList = new List<string>();

            if (guidList != null && guidList.Count > 0)
                convertedList = guidList.Select(x => x.ToString()).ToList();

            return convertedList;
        }

        /// <summary>
        /// Return the date as a valid SQL data
        /// </summary>
        public static DateTime GetSqlLimitedDateTime(DateTime dateTime)
        {
            if (dateTime < Constants.SQL_MIN_DATE)
                return Constants.SQL_MIN_DATE;

            else if (dateTime > Constants.SQL_MAX_DATE)
                return Constants.SQL_MAX_DATE;

            return dateTime;
        }

        /// <summary>
        /// Check for some minimal requirements for a valid query.
        /// </summary>
        public bool IsValidQuery()
        {
            return Columns.Count > 0 && Froms.Count > 0;
        }

        /// <summary>
        /// Clear out all the filters. Useful when doing mass BATCH queries so you dont need to recreate a query each loop. 
        /// </summary>
        public void ClearFilters()
        {
            Filters.Clear();
        }


        #region Totals

        public SqlQuery WherePagination(string filter, FilterAddType addType = FilterAddType.And, bool include = true)
        {
            if (include && !string.IsNullOrEmpty(filter))
            {
                PaginationFilters.Add(new Filter(filter, addType));
            }
            return this;
        }

        /// <summary>
        /// Add a column to the query.
        /// </summary>
        /// <param name="tableAlias">The alias of the table</param>
        /// <param name="field">The field for this column</param>
        /// <param name="asName">Optional name for the column</param>
        /// <param name="addGroupBy">Should this be included in the Group By?</param>
        public SqlQuery AddTotalColumn(string tableAlias, string field, string asName = "", bool addGroupBy = false)
        {
            AddTotalColumn(FieldFormat(tableAlias, field, asName));

            if (addGroupBy)
                TotalGroupBy(FieldFormat(tableAlias, field));

            return this;
        }

        /// <summary>
        /// Add a column to the query.
        /// </summary>
        /// <param name="tableAlias">The alias of the table</param>
        /// <param name="addGroupBy">Should this be included in the Group By?</param>
        public SqlQuery AddTotalColumns(string tableAlias, string[] fields, bool addGroupBy = false)
        {
            AddTotalColumn(FieldFormat(tableAlias, fields));

            if (addGroupBy)
                TotalGroupBy(FieldFormat(tableAlias, fields));

            return this;
        }

        public SqlQuery AddTotalColumns(IEnumerable<string> fields)
        {
            foreach (var field in fields)
                AddTotalColumn(field);

            return this;
        }

        /// <summary>
        /// Add a column to the query
        /// </summary>
        /// <param name="column"></param>
        public SqlQuery AddTotalColumn(string column, bool addGroupBy = false, string asName = null)
        {
            if (!string.IsNullOrEmpty(column))
                TotalColumns.Add(column + (asName != null ? " AS " + asName : ""));

            if (addGroupBy)
                TotalGroupBy(column);

            return this;
        }

        /// <summary>
        /// Add a group by to the query
        /// </summary>
        /// <param name="groupBy">The group by we are adding</param>
        public SqlQuery TotalGroupBy(string groupBy)
        {
            if (!string.IsNullOrEmpty(groupBy))
                TotalGroupBys.Add(groupBy);

            return this;
        }

        /// <summary>
        /// Add a group by to the query
        /// </summary>
        public SqlQuery TotalGroupBy(string tableAlias, string tableField)
        {
            if (!string.IsNullOrEmpty(tableField))
                TotalGroupBys.Add(FieldFormat(tableAlias, tableField));

            return this;
        }

        /// <summary>
        /// Add a order by to the query
        /// </summary>
        /// <param name="orderBy">The order by we are adding</param>
        public SqlQuery TotalOrderBy(string orderBy, bool descending = false)
        {
            if (!string.IsNullOrEmpty(orderBy))
                TotalOrderBys.Add(new Tuple<string, bool>(orderBy, descending));

            return this;
        }


        /// <summary>
        /// Add a order by to the query
        /// </summary>
        public SqlQuery TotalOrderBy(string tableAlias, string tableField, bool descending = false)
        {
            if (!string.IsNullOrEmpty(tableField))
                TotalOrderBy(FieldFormat(tableAlias, tableField), descending);

            return this;
        }

        /// <summary>
        /// Build the actual query from what has been given so far
        /// </summary>
        public SqlQuery BuildTotalQuery()
        {
            var sb = new StringBuilder();

            sb.Clear();

            sb.Append(startQuery);

            //----Declare----
            BuildDeclare(sb);

            //----Set----
            BuildSet(sb);

            sb.Append("SELECT ");

            //---Distinct----
            if (Distinct)
                sb.Append("DISTINCT ");

            //----Top----
            if (Top != 0)
                sb.Append("TOP " + Top.ToString() + " ");

            //----Select----
            if (TotalColumns.Count > 0)
            {
                string selectQuery = string.Join(", ", TotalColumns);
                sb.Append(selectQuery + " ");
            }

            //----From----
            if (Froms.Count > 0)
            {
                sb.Append("FROM ");
                string fromQuery = string.Join(", ", Froms);
                sb.Append(fromQuery + " ");
            }

            //----Join----
            BuildJoins(sb);

            //----Where----
            BuildFilter(sb, false);

            //----For----
            BuildFor(sb);

            //----Group By----
            if (TotalGroupBys.Count > 0)
            {
                string groupByQuery = "GROUP BY " + string.Join(", ", TotalGroupBys);
                sb.Append(groupByQuery + " ");
            }

            //----Having----
            BuildHaving(sb);

            //----Order By----
            if (TotalOrderBys.Count > 0)
            {
                string orderByQuery = "ORDER BY " + string.Join(", ", TotalOrderBys.Select(x => x.Item1 + (x.Item2 ? " DESC" : "")));
                sb.Append(orderByQuery + " ");
            }

            //----Union----
            if (Unions.Count > 0)
            {
                string unionQuery = "";
                foreach (var union in Unions)
                    unionQuery += "UNION " + (union.Item2 != UnionsTypes.Empty ? union.Item2.Description() : "") + " " + union.Item1 + " ";

                sb.Append(unionQuery);
            }

            totalQuery = sb.ToString();
            return this;
        }

        /// <summary>
        /// The complete built SQL query
        /// </summary>
        private string totalQuery;
        public string TotalQuery
        {
            get
            {
                BuildTotalQuery();
                return totalQuery;
            }
            set
            {
                totalQuery = value;
            }
        }

        /// <summary>
        /// Get the query. Should be used if we want the debugger to print the query for easier debugging.
        /// </summary>
        /// <returns>The SQL Query as a string</returns>
        public string GetTotalQuery()
        {
#if DEBUG
            Debug.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + ": " + TotalQuery);
#endif
            return TotalQuery;
        }


        /// <summary>
        /// Check for some minimal requirements for a valid query.
        /// </summary>
        public bool IsValidTotalQuery()
        {
            return TotalColumns.Count > 0 && Froms.Count > 0;
        }
        #endregion

        #region Clone
        public object Clone()
        {
            SqlQuery clonedSQL = new SqlQuery();

            clonedSQL.Sets = Sets.ToList();
            clonedSQL.Top = Top;
            clonedSQL.Distinct = Distinct;
            clonedSQL.Columns = Columns.ToList();
            clonedSQL.Froms = Froms.ToList();
            clonedSQL.Joins = Joins.ToList();
            clonedSQL.Filters = Filters.ToList();
            clonedSQL.Fors = Fors.ToList();
            clonedSQL.Parameters = Parameters.ToList();
            clonedSQL.Havings = Havings.ToList();
            clonedSQL.GroupBys = GroupBys.ToList();
            clonedSQL.OrderBys = OrderBys.ToList();
            clonedSQL.Unions = Unions.ToList();
            clonedSQL.T_Insert = T_Insert;
            clonedSQL.InsertColumns = InsertColumns.ToList();
            clonedSQL.InsertValues = InsertValues.ToList();
            clonedSQL.T_Delete = T_Delete;
            clonedSQL.T_Update = T_Update;
            clonedSQL.UpdateColumnValue = UpdateColumnValue.ToList();
            clonedSQL.Distinct = Distinct;

            return clonedSQL;
        }

        public SqlQuery CloneSqlQuery()
        {
            return (SqlQuery)Clone();
        }
        #endregion
    }
}