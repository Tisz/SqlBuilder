using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBuilder
{
    public class Join
    {
        public Join(JoinTypes joinType, string table, string tableAlias, string tableJoinField, string relatedTableAlias, string relatedField, Comparisons comparison = Comparisons.Equal)
        {
            JoinTable = SqlQuery.FieldFormat(tableAlias, table);
            Filters = new List<Filter>();
            Filters.Add(new Filter(tableAlias, tableJoinField, SqlQuery.FieldFormat(relatedTableAlias, relatedField), comparison));
            JoinType = joinType;
        }

        public Join(JoinTypes joinType, string joinTable, Filter filter)
        {
            JoinTable = joinTable;
            JoinType = joinType;
            Filters = new List<Filter>() { filter };
        }

        public Join(JoinTypes joinType, string joinTable, List<Filter> filters)
        {
            JoinTable = joinTable;
            JoinType = joinType;
            Filters = filters;
        }

        public Join(JoinTypes joinType, string table, string tableAlias, List<Filter> filters)
        {
            JoinTable = SqlQuery.FieldFormat(tableAlias, table);
            Filters = filters;
            JoinType = joinType;
        }

        public Join(string join)
        {
            OverrideJoin = join;
        }

        /// <summary>
        /// The table being joined to.
        /// </summary>
        public string JoinTable { get; set; }

        /// <summary>
        /// The way this join will be added
        /// </summary>
        public JoinTypes JoinType { get; set; }

        /// <summary>
        /// List of filters to add in the join expression
        /// </summary>
        public List<Filter> Filters { get; set; }

        /// <summary>
        /// Join string which will override any other configuration when added to the query
        /// </summary>
        public string OverrideJoin { get; set; }


        /// <summary>
        /// Returns the structured join ready for a query
        /// </summary>
        public string BuildJoin()
        {
            if (!string.IsNullOrEmpty(OverrideJoin))
                return OverrideJoin;
            else
                return JoinType.Description() + " " + JoinTable + " ON " + BuildJoinFilter();
        }

        /// <summary>
        /// Returns the structured join filter ready for a query
        /// </summary>
        public string BuildJoinFilter()
        {
            string joinFilter = "";
            if (Filters != null && Filters.Count > 0)
            {
                bool firstFilter = true;
                foreach (var filter in Filters)
                {
                    joinFilter += filter.BuildFilter(firstFilter);
                    firstFilter = false;
                }
            }
            return joinFilter;
        }
    }
}
