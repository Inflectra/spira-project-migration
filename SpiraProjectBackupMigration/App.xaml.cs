using System;
using System.Windows;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		/// <summary>Hit on Application Startup, reads any command-line arguments.</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void app_Startup(object sender, StartupEventArgs e)
		{
			//Loop through our arguments and set any given options..
			for (int i = 0; i < e.Args.Length; i++)
			{
				string argument = e.Args[i].ToLowerInvariant().Trim();

				//Split the option from our value..
				string[] parts = argument.Split('=');
				string option = parts[0].Trim();
				string value = "";
				if (parts.Length > 0)
					value = parts[1].Trim();

				switch (option)
				{
					case "pagesize":
						//Get the number..
						int pageSize = 0;
						if (int.TryParse(value, out pageSize))
						{
							//Check we have valid values, first. Minimum = 5, Maximum = 500
							if (pageSize >= 5 && pageSize <= 500)
								Common.PAGE_NUM = pageSize;
						}
						break;
				}
			}
		}

		/// <summary>Catches an exeption.</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Logger.SaveLogToFile("C:\\ProjectExport.log");

			string msg = "Error:" + Environment.NewLine + ((Exception)e.ExceptionObject).Message + Environment.NewLine + Environment.NewLine +
				((Exception)e.ExceptionObject).StackTrace;
			MessageBox.Show(msg, "DEBUG ERROR", MessageBoxButton.OK, MessageBoxImage.Error);

			App.Current.Shutdown();
		}
	}
}
