using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;
using System.Threading;
using System.Diagnostics;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for usercntSpiraConnect.xaml
	/// </summary>
	public partial class trnsProgressIn : UserControl
	{
		private const string CLASS_NAME = "trnsProgressIn.";

		private bool _isVerified = false;

		//Conenction information..
		private string _name = "ProjectExport";
		private string _adminPass = "";
		private string _userPass = "";
		private string _user = "administrator";
		private string _file = "";
		private Uri _server;
		private string _project;

		//Values for Progress bar.
		private int maxValue = 25;

		//Our three clients and tracking data.
		SpiraSoapService.SoapServiceClient _client1;
		private int _client1Act = 0;
		private int _client1Num = 0;

		public trnsProgressIn()
		{
			InitializeComponent();
		}

		/// <summary>Specifies whether or not this page is good to go on to the next.</summary>
		public bool isVerified
		{
			get
			{
				return this._isVerified;
			}
			set
			{
				if (value != this._isVerified && this.onVerifiedChanged != null)
				{
					this._isVerified = value;
					this.onVerifiedChanged(this, new EventArgs());
				}
			}
		}

		public event EventHandler onVerifiedChanged;

		/// <summary>Set the remote project and starts the export.</summary>
		/// <param name="spiraProject">The project to export.</param>
		/// <param name="importFile">The file that we're saving the data to.</param>
		/// <param name="adminPass">The administrator's password.</param>
		public void setConnectionInfo(string spiraProject, string adminPass, string importFile, Uri serverUrl, string newUserPass)
		{
			const string METHOD = CLASS_NAME + "setConnectionInfo()";

			this._project = spiraProject;
			this._adminPass = adminPass;
			this._server = serverUrl;
			this._file = importFile;
			this._userPass = newUserPass;

			//Set initial labels:
			this.txtProjectName.Text = this._project;
			this.barProgress.IsIndeterminate = false;

			//Create the Thread Class & Thread
			Thread_Import prcExport = new Thread_Import(spiraProject, adminPass, importFile, serverUrl, newUserPass);
			prcExport.ProgressFinished += new EventHandler<Thread_Events.FinishedArgs>(prcExport_ProgressFinished);
			prcExport.ProgressUpdate += new EventHandler<Thread_Events.ProgressArgs>(prcExport_ProgressUpdate);
			Thread thExport = new Thread(prcExport.Run);
			thExport.Name = "Project Import";

			//Run the thread.
			thExport.Start();
		}

		private delegate void prcExport_ProgressUpdateCallback(object sender, Thread_Events.ProgressArgs e);
		private void prcExport_ProgressUpdate(object sender, Thread_Events.ProgressArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				//Update progress bar..
				if (e.PercentageDone < 0)
				{
					this.barProgress.IsIndeterminate = true;
					this.barProgress.Value = 0;
				}
				else
				{
					this.barProgress.Value = e.PercentageDone;
					if (this.barProgress.IsIndeterminate) this.barProgress.IsIndeterminate = false;
				}
				Debug.WriteLine("Percentage: " + e.PercentageDone.ToString());
				if (e.ActivityMessage != null) this.txtAction.Text = e.ActivityMessage;
			}
			else
			{
				prcExport_ProgressUpdateCallback callB = new prcExport_ProgressUpdateCallback(this.prcExport_ProgressUpdate);
				this.Dispatcher.Invoke(callB, new object[] { sender, e });
			}
		}

		private delegate void prcExport_ProgressFinishedCallback(object sender, Thread_Events.FinishedArgs e);
		private void prcExport_ProgressFinished(object sender, Thread_Events.FinishedArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				//Update progress bar..
				this.barProgress.Value = 1;
				Debug.WriteLine("Percentage: " + e.PercentageDone.ToString());
				this.txtAction.Text = "Transfer Finished!";
				this.isVerified = true;
			}
			else
			{
				prcExport_ProgressFinishedCallback callB = new prcExport_ProgressFinishedCallback(this.prcExport_ProgressFinished);
				this.Dispatcher.Invoke(callB, new object[] { sender, e });
			}
		}

		/// <summary>Hit when the percentage changes on the progress bar.</summary>
		/// <param name="sender">barProgress</param>
		/// <param name="e">RoutedPropertyChangedEventArgs</param>
		private void barProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!this.barProgress.IsIndeterminate)
			{
				string perTxt = ((double)e.NewValue * (double)100).ToString("##0.0");
				this.txtPercentage.Text = perTxt + "%";
			}
			else
			{
				this.txtPercentage.Text = "";
			}
		}
	}
}
