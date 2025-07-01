using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AgsEventAdder
{
	internal static class Extensions
	{
		internal static string ParenParenToEmpty(this string text)
			=> (text is null || text.Length < 2 || text.Substring(0, 2) != "((") ? text : "";

		/// <summary>
		/// Convert to an integer
		/// </summary>
		/// <param name="val"></param>
		/// <returns>The object, converted to an integer</returns>
		internal static int ToInt32(this object val)
		{
			if (val is null)
				return 0;
			if (val is bool bool_val)
				return bool_val ? 1 : 0;

			var string_val = val.ToString();
			if (string_val.Equals("false", StringComparison.OrdinalIgnoreCase))
				return 0;
			if (string_val.Equals("true", StringComparison.OrdinalIgnoreCase))
				return 1;

			return Convert.ToInt32(val);
		}
	}
}
