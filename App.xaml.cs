using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Threading;
using System.ComponentModel;

namespace AgsEventAdder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, INotifyPropertyChanged
	{

		private AgsGame _ags_game = null;

		/// <summary>
		/// Contains the game when open
		/// </summary>
		public AgsGame AgsGame
		{ 
			get => _ags_game;
			set
			{
				if (_ags_game == value)
					return; // all done

				_ags_game = value;
				OnPropertyChanged(nameof(AgsGame));
			}
		}

		private int _changes_pending = 0;

		public int ChangesPending
		{
			get => _changes_pending;
			set
			{
				if (_changes_pending == value)
					return;
				_changes_pending = value;
				OnPropertyChanged(nameof(ChangesPending));
			}
		}


		/// <summary>
		/// Notify UI elements whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

			MainWindow wnd = new();
			wnd.Show();
		}
	}
}
