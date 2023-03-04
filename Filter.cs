using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBuilder
{
    public class Filter
    {
        public Filter(string alias, string field, string condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotation = false)
        {
            FilterExpression = SqlQuery.FilterFormat(alias, field, condition, comparison, useQuotation);
            AddType = addType;
        }

        public Filter(string alias, string field, Guid condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And)
        {
            FilterExpression = SqlQuery.FilterFormat(alias, field, condition.ToString(), comparison, true);
            AddType = addType;
        }

        public Filter(string alias, string field, int condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotation = false)
        {
            FilterExpression = SqlQuery.FilterFormat(alias, field, condition, comparison, useQuotation);
            AddType = addType;
        }

        public Filter(string aliasTable, string condition, Comparisons comparison = Comparisons.Equal, FilterAddType addType = FilterAddType.And, bool useQuotation = false)
        {
            FilterExpression = SqlQuery.FilterFormat(aliasTable, condition, comparison, useQuotation);
            AddType = addType;
        }

        /// <summary>
        /// Filter with a format for a value to be between two conditions
        /// </summary>
        public Filter(string alias, string field, string conditionOne, string conditionTwo, bool notBetween = false, FilterAddType addType = FilterAddType.And, bool useQuotes = false, bool include = true)
        {
            FilterExpression = SqlQuery.FilterBetweenFormat(SqlQuery.FieldFormat(alias, field), conditionOne, conditionTwo, notBetween, useQuotes);
            AddType = addType;
        }

        public Filter(string filter, FilterAddType addType = FilterAddType.And)
        {
            FilterExpression = filter;
            AddType = addType;
        }

        public Filter(List<Filter> filters, FilterAddType addType = FilterAddType.And)
        {
            SubFilters = filters;
            AddType = addType;
        }

        /// <summary>
        /// The filter without the AddType. Eg C.Name = 'Kitty'
        /// </summary>
        public string FilterExpression { get; set; }

        /// <summary>
        /// The way this filter will be added (AND, OR.. etc)
        /// </summary>
        public FilterAddType AddType { get; set; }

        /// <summary>
        /// List of filters to add in contained brackets
        /// </summary>
        public List<Filter> SubFilters { get; set; }

        /// <summary>
        /// Filter string which will override any other configuration when added to the query
        /// </summary>
        public string OverrideFilter { get; set; }

        /// <summary>
        /// Returns the structured filter ready for a query
        /// </summary>
        public string BuildFilter(bool firstFilter = true)
        {
            string filterString = "";

            if (!string.IsNullOrEmpty(FilterExpression))
            {
                if (!string.IsNullOrEmpty(OverrideFilter))
                    filterString += $" {OverrideFilter} ";
                else
                    filterString += (!firstFilter ? AddType.Description() : "") + " " + FilterExpression + " ";
            }

            //Build the sub filters contained by brackets
            if (SubFilters != null && SubFilters.Count > 0)
            {
                bool firstSubFilter = true;
                string subFilterString = "";
                foreach (var subfilter in SubFilters)
                {
                    subFilterString += subfilter.BuildFilter(firstSubFilter);
                    firstSubFilter = false;
                }

                if (!string.IsNullOrWhiteSpace(subFilterString))
                    filterString += (!firstFilter ? AddType.Description() : "") + " (" + subFilterString + ") ";
            }

            return filterString;
        }
    }
}
