using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlBuilder
{
    public enum FilterAddType
    {
        [Description("")]
        Empty,
        [Description("AND")]
        And,
        [Description("OR")]
        Or,
    }

    public enum JoinTypes
    {
        [Description("INNER JOIN")]
        Inner,
        [Description("OUTER JOIN")]
        Outer,
        [Description("LEFT JOIN")]
        Left,
        [Description("LEFT OUTER JOIN")]
        LeftOuter,
    };

    public enum Comparisons
    {
        [Description("=")]
        Equal,
        [Description("!=")]
        NotEqual,
        [Description(">")]
        Greater,
        [Description(">=")]
        GreaterEqual,
        [Description("<")]
        Lesser,
        [Description("<=")]
        LesserEqual,
        [Description("<>")]
        Except,
        [Description("LIKE")]
        Like,
    }

    public enum UnionsTypes
    {
        [Description("")]
        Empty,
        [Description("ALL")]
        All,
    }

    public enum Functions
    {
        /// <summary>
        /// Count the number of rows for this field
        /// </summary>
        [Description("COUNT")]
        Count,
        /// <summary>
        /// Add all values found of this field
        /// </summary>
        [Description("SUM")]
        Sum,
        /// <summary>
        /// Average of field
        /// </summary>
        [Description("AVG")]
        Average,
        /// <summary>
        /// Removes white space from a string
        /// </summary>
        [Description("TRIM")]
        Trim,
        /// <summary>
        /// Removes white space from the left of a string
        /// </summary>
        [Description("LTRIM")]
        LeftTrim,
        /// <summary>
        /// Removes white space from the right of a string
        /// </summary>
        [Description("RTRIM")]
        RightTrim,
        /// <summary>
        /// Sets all characters to be lower case
        /// </summary>
        [Description("LOWER")]
        Lower,
        /// <summary>
        /// Sets all characters to be UPPER case
        /// </summary>
        [Description("UPPER")]
        Upper,
        /// <summary>
        /// Returns the character of the ASCII Code
        /// </summary>
        [Description("CHAR")]
        Char,
        /// <summary>
        /// Returns the position of a character in a string.
        /// </summary>
        [Description("CHARINDEX")]
        CharIndex,
        /// <summary>
        /// Returns the number of character bytes in a string/expression
        /// </summary>
        [Description("DATALENGTH")]
        DataLength,
        /// <summary>
        /// Returns the length of a string
        /// </summary>
        [Description("LEN")]
        Length,
        /// <summary>
        /// Adds multiple strings together
        /// </summary>
        [Description("CONCAT")]
        Concat,
        /// <summary>
        /// Reverses the string
        /// </summary>
        [Description("REVERSE")]
        Reverse,
        /// <summary>
        /// Returns the day of the date
        /// </summary>
        [Description("DAY")]
        Day,
        /// <summary>
        /// Returns the day of the date
        /// </summary>
        [Description("MONTH")]
        Month,
        /// <summary>
        /// Returns the day of the date
        /// </summary>
        [Description("YEAR")]
        Year,
        /// <summary>
        /// If the first value is null, use the second
        /// </summary>
        [Description("ISNULL")]
        IsNull,
        /// <summary>
        /// Use the first non-null value from left to right
        /// </summary>
        [Description("COALESCE")]
        Coalesce,
        /// <summary>
        /// Use the largest value
        /// </summary>
        [Description("MAX")]
        Max,
        /// <summary>
        /// Use the minimum value
        /// </summary>
        [Description("MIN")]
        Min,
        /// <summary>
        /// Place a string into another string at a given position
        /// </summary>
        [Description("STUFF")]
        Stuff,
        /// <summary>
        /// Return the current date
        /// </summary>
        [Description("GETDATE")]
        GetDate,
    }
}
