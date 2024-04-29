using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Threading;

namespace AGSEventAdder
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// contains the game when open
		public AgsGame AgsGame
			{ get; set; } = null;

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

			MainWindow wnd = new();
			wnd.Show();
		}
	}
}
