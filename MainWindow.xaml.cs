using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Runtime.CompilerServices;

namespace AgsEventAdder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			// Set the data context from code behind because WPF will not
			// allow this for 'App' specifically on the XAML side, making
			// up lame excuses.
			DataContext = Application.Current as App;

			GamePathTxt.Text = GamePathTxt.FindResource("Prompt") as String;

			GamePathOFD = new OpenFileDialog()
			{
				AddExtension = true,
				CheckFileExists = true,
				CheckPathExists = true,
				ValidateNames = true,
				DefaultExt = ".ags",
				Filter = "AGS Games (*.agf)|*.agf|All files (*.*)|*.*",
				FilterIndex = 0,
			};

		}

		/// <summary>
		/// For Open File dialogs that ask for a game path
		/// </summary>
		private readonly OpenFileDialog GamePathOFD = null;

		private void GamePathBrowseBtn_Click(object sender, RoutedEventArgs e)
		{
			if (GamePathOFD.ShowDialog()?? false)
				GamePathTxt.Text = GamePathOFD.FileName;
		}

		private void GamePathTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			// When this element has the focus, assume that the content is still
			// being edited, so no validation here
			if (GamePathTxt.IsFocused)
				return;

			GamePathTxt_HandleChanged(GamePathTxt.Text);
		}

		/// <summary>
		/// Whenever the user has done changing the GamePathTxt field
		/// </summary>
		/// <param name="changed_text">Text that has been entered</param>
		private void GamePathTxt_HandleChanged(String changed_text)
		{
			GamePathErrorTxt.Text = "";
			GameDescBlock.Text = "";
			App app = Application.Current as App;
			// The prompt text is displayed in lieu of "" 
			String prompt = GamePathTxt.FindResource("Prompt") as String;
			if (changed_text == prompt && app?.AgsGame is null)
				// All ready and done. Still no game selected
				return;

			if (String.IsNullOrWhiteSpace(changed_text))
			{
				changed_text = "";
				if (GamePathTxt.Text != prompt)
					GamePathTxt.Text = prompt;
			}

			bool cancel = GamePathTxt_HandleAnyOpenGame(changed_text);
			if (cancel)
			{
				GamePathTxt.Text = (app.AgsGame is null) ? prompt : app.AgsGame.Path;
				return;
			}

			AgsGame.Factory(GamePathTxt.Text, out AgsGame game, out String errtext);
			if (game is null)
			{
				if (String.IsNullOrEmpty(errtext))
					errtext = GamePathErrorTxt.FindResource("Default") as String;
				GamePathErrorTxt.Text = errtext;
				return;
			}

			GamePathErrorTxt.Text = "";
			app.AgsGame = game;
			GameDescBlock.Text = game.Desc;
			OverviewTV.ItemsSource = game.Overview.Root.Items;
		}


		/// <summary>
		/// If a game is open, ask user how to handle that game
		/// </summary>
		/// <returns>
		/// Whether this operation should be cancelled 
		/// </returns>
		private bool GamePathTxt_HandleAnyOpenGame(String changed_path)
		{
			App app = Application.Current as App;
			if (app?.AgsGame is null)
				// All done because no game is open
				return String.IsNullOrEmpty(changed_path);
			
			if ((bool)this.FindResource("ChangesPending")) 
				return MessageBox.Show(
					"Discard all pending changes?", 
					"Game is already open", 
					MessageBoxButton.OKCancel, 
					MessageBoxImage.Exclamation) == MessageBoxResult.Cancel;

			app.AgsGame.Unlock();
			app.AgsGame = null;
			return false;
		}

		private void GamePathTxt_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (GamePathTxt.Text.Trim() == GamePathTxt.FindResource("Prompt") as String)
				GamePathTxt.Text = "";
		}

		private void GamePathTxt_LostFocus(object sender, RoutedEventArgs e)
		{
			GamePathTxt_HandleChanged(GamePathTxt.Text);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (Application.Current is not App app || app.ChangesPending == 0)
				return;
			// TODO Ask to confirm closing
		}

		private void OverviewTV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var lvi = sender as ListViewItem;
			if (lvi?.Content is not OverviewItem selected)
				return;
			// Work out what has been selected
			if (selected is CharacterTable)
			{
				OverviewTV_Characters_MouseDoubleClick();
			}
			switch (selected.EventCarrier)
			{
				default:
					return;
				case EventCarrier.Guis:
					OverviewTV_Guis_MouseDoubleClick();
					return;
				case EventCarrier.Hotspots:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Hotspots_MouseDoubleClick(room);
						return;
					}
				case EventCarrier.InvItems:
					OverviewTV_InvItems_MouseDoubleClick();
					return;
				case EventCarrier.Objects:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Objects_MouseDoubleClick(room);
							return;
					}
				case EventCarrier.Regions:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Regions_MouseDoubleClick(room);
						return;
					}
				case EventCarrier.Rooms:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Rooms_MouseDoubleClick(room);
						return;
					}
			}
		}

		int GetRoomFromOverviewItem(in OverviewItem oit)
		{
			for (OverviewCompo compo = oit; compo is not null; compo = compo.Parent)
				if (compo is OverviewRoom)
					return (compo as OverviewRoom).Number;

			return -1;
		}

		private void OverviewTV_Characters_MouseDoubleClick()
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Guis_MouseDoubleClick()
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Hotspots_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_InvItems_MouseDoubleClick()
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Objects_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Regions_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Rooms_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}


		private void CommitAll_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void RejectAll_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Empty strings convert to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenEmptyConverter: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (String.IsNullOrWhiteSpace(value as String)) ?
				Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// 'false' converts to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenFalseConverter : IValueConverter
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

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Null objects converted to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhenNullConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is null) ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException(); 
		}
	}
	
	/// <summary>
	/// Zero converted to Collapsed, all others to Visible
	/// </summary>
	public class CollapsedWhen0Converter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				int v = (int)value;
				return v == 0? Visibility.Collapsed : Visibility.Visible;
			}
			catch
			{
				return Visibility.Visible;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException(); 
		}
	}
}
