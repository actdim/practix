using System;
using System.Text.RegularExpressions;

namespace ActDim.Practix.Extensions // ActDim.Practix.Linq
{
	/// <summary>
	/// String and StringBuilder extensions
	/// </summary>
	public static class StringExtensions
	{
		public static bool Contains(this string source, string value, StringComparison comparisonType) //valueToFind
		{
			return source.IndexOf(value, comparisonType) >= 0;
		}

		public static bool IsNullOrEmpty(this string value)
		{
			//return string.IsNullOrEmpty(value);
			return (value == null || value.Length == 0);
		}

		#region Split

		public static string[] Split(this string expression, string delimiter, string qualifier)
	    {
		    return Split(expression, delimiter, qualifier, true);
	    }

	    public static string[] Split(this string expression, string delimiter)
	    {
		    return Split(expression, delimiter, "\"", true);
	    }

	    public static string[] Split(this string expression, string delimiter, string qualifier, bool ignoreCase)
	    {
		    if (qualifier == null)
		    {
			    qualifier = "\"";
		    }
		    string statement = String.Format("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))", Regex.Escape(delimiter), Regex.Escape(qualifier));
		    //\s+(?=(?:[^"]*"[^"]*")*(?![^"]*"))

		    var options = RegexOptions.Multiline; //RegexOptions.Compiled | RegexOptions.Multiline
			if (ignoreCase)
			{
				options = options | RegexOptions.IgnoreCase;
			}
		    return new Regex(statement, options).Split(expression);
	    }

		#endregion
	}
}
