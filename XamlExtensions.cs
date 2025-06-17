using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace AgsEventAdder
{
	internal static class XamlExtensions
	{
		public static T FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			DependencyObject parent = VisualTreeHelper.GetParent(child);
			while (parent != null)
			{
				if (parent is T correctlyTyped)
					return correctlyTyped;

				parent = VisualTreeHelper.GetParent(parent);
			}
			return null;
		}

		public static bool IsInteractiveElement(DependencyObject element)
		{
			while (element is not null)
			{
				if (element is TextBox or Button or CheckBox or ComboBox or Slider or ListBoxItem)
					// Found an interactive element
					return true;

				// 'element' might be something like a 'run', in which case 'GetParent()' will bomb
				if (element is not (Visual or Visual3D))
					return false;

				element = VisualTreeHelper.GetParent(element);
			}
			return false;
		}

		/// <summary>
		/// Generate a new random name that can be used for the 'x:Name' attribute 
		/// </summary>
		/// <returns></returns>
		public static string NewRandomXamlName() => "_" + Guid.NewGuid().ToString().Replace("-", "_");

		public static ContextMenu Clone(this ContextMenu context_menu)
		{
			var menu_clone = new ContextMenu();
			foreach (MenuItem item in context_menu.Items)
			{
				MenuItem item_clone = new()
				{
					Header = item.Header,
					Command = item.Command,
					CommandParameter = item.CommandParameter
				};
				_ = menu_clone.Items.Add(item_clone);
			}
			return menu_clone;
		}
	}
}
