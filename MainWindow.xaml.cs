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
			if (changed_text == prompt && app.AgsGame is null)
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
			RoomCbB.ItemsSource = game.Rooms;
			RoomCbB.SelectedIndex = 0;
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
			if (app.AgsGame is null)
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
			// TODO: Check whether there are pending changes, if so then
			// ask to confirm closing
		}

		private void RoomCbB_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			App app = Application.Current as App;
			if (app is null || app.AgsGame is null || app.AgsGame.Rooms is null)
			{
				RoomCbB.SelectedIndex = -1;
				return;
			}
			ComboBox cbb = sender as ComboBox;
			int index = cbb.SelectedIndex;
			if (index >= 0 && app.AgsGame.Rooms[index].Room == AgsGame.kNoRoom)
				cbb.SelectedIndex = 0;
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
			return null;
		}
	}

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
			
			return null;
		}
	}

	public class RoomConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			String strval = value.ToString();
			if (strval == "-1")
				strval = "";
			return strval;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{

			return null;
		}
	}
}
