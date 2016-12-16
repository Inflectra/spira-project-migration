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
	public partial class usercntProgressOutput : UserControl
	{
		private const string CLASS_NAME = "usercntProgressOutput.";

		private bool _isVerified = false;

		//Conenction information..
		private string _name = "ProjectExport";
		private string _password = "";
		private string _user = "administrator";
		private string _file = "";
		private Uri _server;
		private RemoteProject _project;

		//Values for Progress bar.
		private int maxValue = 25;

		//Our three clients and tracking data.
		SpiraSoapService.SoapServiceClient _client1;
		private int _client1Act = 0;
		private int _client1Num = 0;

		public usercntProgressOutput()
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
		/// <param name="exportFile">The file that we're saving the data to.</param>
		/// <param name="userPassword">The administrator's password.</param>
		public void setConnectionInfo(RemoteProject spiraProject, string userPassword, string exportFile, Uri serverUrl)
		{
			const string METHOD = CLASS_NAME + "setConnectionInfo()";

			this._project = spiraProject;
			this._password = userPassword;
			this._server = serverUrl;
			this._file = exportFile;

			//Set initial labels:
			this.txtProjectName.Text = this._project.Name;
			this.barProgress.IsIndeterminate = false;

			//Create the Thread Class & Thread
			Thread_Export prcExport = new Thread_Export(this._project, this._password, this._file, this._server);
			prcExport.ProgressFinished += new EventHandler<Thread_Events.FinishedArgs>(prcExport_ProgressFinished);
			prcExport.ProgressUpdate += new EventHandler<Thread_Events.ProgressArgs>(prcExport_ProgressUpdate);
			Thread thExport = new Thread(prcExport.StartProcess);
			thExport.Name = "Project Export";

			//Run the thread.
			thExport.Start();
		}

		/// <summary>Updates the user when there is a progress update.</summary>
		/// <param name="sender">The thread.</param>
		/// <param name="e">ProgressArgs</param>
		private void prcExport_ProgressUpdate(object sender, Thread_Events.ProgressArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				//Update progress bar..
				this.barProgress.Value = e.PercentageDone;
				Debug.WriteLine("Percentage: " + e.PercentageDone.ToString());
				if (e.ActivityMessage != null) this.txtAction.Text = e.ActivityMessage;
			}
			else
			{
				prcExport_ProgressUpdateCallback callB = new prcExport_ProgressUpdateCallback(this.prcExport_ProgressUpdate);
				this.Dispatcher.Invoke(callB, new object[] { sender, e });
			}
		}

		/// <summary>Updates the user when there's a notification that the thread is finished.</summary>
		/// <param name="sender">The thread.</param>
		/// <param name="e">FinishedArgs</param>
		private void prcExport_ProgressFinished(object sender, Thread_Events.FinishedArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Status == Thread_Events.FinishedArgs.FinishedStatusEnum.OK)
				{
					//Update progress bar..
					this.barProgress.Value = 1;
					this.barProgress.IsIndeterminate = false;
					Debug.WriteLine("Percentage: " + e.PercentageDone.ToString());
					this.txtAction.Text = "Export Finished!";
					this.isVerified = true;
				}
				else
				{
					//There was an error. Display message.
					this.txtAction.Text = "Error in export!";
					this.isVerified = true;
					this.txtError.Visibility = System.Windows.Visibility.Visible;
					this.txtErrorMessage.Text = e.Message;
				}
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
			if (e.NewValue < 0)
			{
				this.txtPercentage.Text = "";
			}
			else
			{
				string perTxt = ((double)e.NewValue * (double)100).ToString("##0.0");
				this.txtPercentage.Text = perTxt + "%";
			}
		}

		/// <summary>Delegate for handling Progress Updates.</summary>
		/// <param name="sender">The thread.</param>
		/// <param name="e">ProgressArgs</param>
		private delegate void prcExport_ProgressUpdateCallback(object sender, Thread_Events.ProgressArgs e);

		/// <summary>The delegate for handling a Finished Progress event.</summary>
		/// <param name="sender">The thread.</param>
		/// <param name="e">FinishedArgs</param>
		private delegate void prcExport_ProgressFinishedCallback(object sender, Thread_Events.FinishedArgs e);
	}
}
