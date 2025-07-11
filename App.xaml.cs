﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Windows;

namespace AgsEventAdder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application, INotifyPropertyChanged
	{

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
		private AgsGame _ags_game = null;

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
		private int _changes_pending = 0;

		public bool SaveIsPending
		{
			get => _save_is_pending;
			set
			{
				if (_save_is_pending == value)
					return;
				_save_is_pending = value;
				OnPropertyChanged(nameof(SaveIsPending));
			}
		}
		private bool _save_is_pending = false;

		/// <summary>
		/// Notify UI elements whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

			ShutdownMode = ShutdownMode.OnMainWindowClose;
			Exit += App_Exit;
			MainWindow wnd = new();
			wnd.Show();
		}
		private void App_Exit(object sender, ExitEventArgs e)
		{
			AgsGame?.Unlock();
		}
	}
}
