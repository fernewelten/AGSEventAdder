using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace AgsEventAdder
{
	/// <summary>
	/// Empty strings convert to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenEmptyConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (String.IsNullOrWhiteSpace(value as String)) ?
				Visibility.Collapsed : Visibility.Visible;
		}
	}

	/// <summary>
	/// 'false' converts to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenFalseConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				bool v = (bool)value;
				return v ? Visibility.Visible : Visibility.Collapsed;
			}
			catch
			{
				return Visibility.Visible;
			}
		}
	}

	/// <summary>
	/// 'True' converts to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenTrueConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				bool v = (bool)value;
				return v ? Visibility.Collapsed : Visibility.Visible;
			}
			catch
			{
				return Visibility.Visible;
			}
		}
	}

	/// <summary>
	/// 'True' converts to Hidden, all others to Visible
	/// </summary>
	public class HiddenWhenTrueConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				bool v = (bool)value;
				return v ? Visibility.Hidden : Visibility.Visible;
			}
			catch
			{
				return Visibility.Visible;
			}
		}
	}

	/// <summary>
	/// Null objects converted to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenNullConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is null) ? Visibility.Collapsed : Visibility.Visible;
		}
	}

	/// <summary>
	/// Zero converted to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhen0Converter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				int v = (int)value;
				return v == 0 ? Visibility.Collapsed : Visibility.Visible;
			}
			catch
			{
				return Visibility.Visible;
			}
		}
	}

	public class HiddenWhenInCodeMConverter : OneWayMultiConverter, IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2)
				try
				{
					bool roster_value_is_in_code = (bool)values[0];
					string roster_value = values[1] as string;

					if (roster_value_is_in_code || String.IsNullOrWhiteSpace(roster_value.ParenParenToEmpty()))
						return Visibility.Hidden;
				}
				catch { }

			return Visibility.Visible;
		}

	}

	public class CollapsedWhenEqualMConverter : OneWayMultiConverter, IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2)
				try
				{
					string string1 = values[0] as string;
					string string2 = values[1] as string;

					if (string1?.Trim().ParenParenToEmpty() == string2?.Trim().ParenParenToEmpty())
						return Visibility.Collapsed;
				}
				catch { }

			return Visibility.Visible;
		}
	}

	public class CollapsedWhenBothZeroMConverter: OneWayMultiConverter, IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2)
				try
				{
					int val1 = to_int(values[0]);
					int val2 = to_int(values[1]);

					if (val1 == 0 && val2 == 0)
						return Visibility.Collapsed;
				}
				catch { }

			return Visibility.Visible;

			int to_int(object val)
			{
				if (val is null)
					return 0;
				if (val is bool)
					return (bool) val ? 1 : 0;
				try
				{
					return (int)val;
				}
				catch 
				{
					return 1;
				}
			}
		}
	}

	/// <summary>
	/// Converter where the way back isn't implemented
	/// </summary>
	public abstract class OneWayConverter
	{
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}

	/// <summary>
	/// MultiConverter where the way back isn't implemented
	/// </summary>
	public abstract class OneWayMultiConverter
	{
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}


}
