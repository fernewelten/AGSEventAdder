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
		public static string ParenParenToEmpty(this string text)
			=> (text is null || text.Length < 2 || text.Substring(0, 2) != "((") ? text : "";
	}
}
