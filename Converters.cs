using System;
using System.Collections.Generic;
using static System.Convert;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

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
	/// parameter converts to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				int int_value = value.ToInt32();
				int int_parameter = parameter.ToInt32();
				if (int_value == int_parameter)
					return Visibility.Collapsed;
			}
			catch
			{ }
			return Visibility.Visible;
		}
	}


	/// <summary>
	/// 'True' converts to Hidden, all others to Visible
	/// </summary>
	public class HiddenWhenConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				int int_value = value.ToInt32();
				int int_parameter = parameter.ToInt32();
				if (int_value == int_parameter)
					return Visibility.Hidden;
			}
			catch
			{ }
			return Visibility.Visible;
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
	/// Stretches the input by the parameter
	/// </summary>
	public class StretchConverter : OneWayConverter, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				return ToDouble(value) * ToDouble(parameter);
			}
			catch
			{
				return value;
			}
		}
	}

	public class HiddenWhenInCodeMConverter : OneWayConverter, IMultiValueConverter
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

	public class CollapsedWhenEqualMConverter : OneWayConverter, IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2)
				try
				{
					string string1 = values[0].ToString();
					string string2 = values[1].ToString();

					if (string1?.Trim().ParenParenToEmpty() == string2?.Trim().ParenParenToEmpty())
						return Visibility.Collapsed;
				}
				catch { }

			return Visibility.Visible;
		}
	}

	public class CollapsedWhenBothZeroMConverter: OneWayConverter, IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2)
				try
				{
					int val1 = values[0].ToInt32();
					int val2 = values[1].ToInt32();

					if (val1 == 0 && val2 == 0)
						return Visibility.Collapsed;
				}
				catch { }

			return Visibility.Visible;
		}
	}

	/// <summary>
	/// Converter or MultiConverter where the way back isn't implemented
	/// </summary>
	public abstract class OneWayConverter
	{
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> throw new NotImplementedException();
	}
}
