using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SaberSongPatcher.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			Logger.Debug("Application started");
			// Create the startup window
			MainWindow wnd = new MainWindow();
			// Do stuff here, e.g. to the window
			//wnd.Title = "Something else";
			// Show the window
			wnd.Show();
		}
	}
}
