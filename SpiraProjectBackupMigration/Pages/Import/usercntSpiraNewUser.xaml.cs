using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>Interaction logic for usercntSpiraCrtProject.xaml</summary>
	public partial class usercntSpiraNewUser: UserControl
	{
		private bool _isVerified = false;
		private Uri serverUrl = null;
		private string serverUser;
		private string serverPass;

		/// <summary>Creates a new instance of the class.</summary>
		public usercntSpiraNewUser()
		{
			InitializeComponent();

			//Set the security question & answer text..
			this.txtQuestion.Text = Common.SECURITY_QUESTION;
			this.txtAnswer.Text = Common.SECURITY_ANSWER;
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

		/// <summary>The selected new user's password.</summary>
		public string SelectedUserPass
		{
			get
			{
				return this.txtDefaultPassword.Text;
			}
		}

		/// <summary>Sets connection infromation for testing user's password.</summary>
		/// <param name="serverUri">The server's URL.</param>
		/// <param name="user">User to log in as (administrator)</param>
		/// <param name="password">The user's password.</param>
		public void setServerLogin(Uri serverUri, string user, string password)
		{
			this.serverUrl = serverUri;
			this.serverUser = user;
			this.serverPass = password;
		}

		#region Testing User's Password
		/// <summary>Tests the importer's password.</summary>
		/// <param name="sender">Button</param>
		/// <param name="e">RoutedEventArgs</param>
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			//Hide the controls..
			this.divTesting.Cursor = Cursors.Wait;
			this.divTesting.Visibility = System.Windows.Visibility.Visible;
			this.txtTesting.Text = "Testing.";
			this.txtTesting.Foreground = Brushes.Black;
			this.spnSpinner.Visibility = System.Windows.Visibility.Visible;
			this.divTesting.MouseDown -= divTesting_MouseDown;



			SoapServiceClient client = SpiraClientFactory.CreateClient_Spira5(this.serverUrl);
			client.Connection_Authenticate2Completed += client_Connection_Authenticate2Completed;
			client.Project_RetrieveCompleted += client_Project_RetrieveCompleted;
			client.Connection_ConnectToProjectCompleted += client_Connection_ConnectToProjectCompleted;
			client.User_CreateCompleted += client_User_CreateCompleted;
			client.User_DeleteCompleted += client_User_DeleteCompleted;

			//Now connect..
			client.Connection_Authenticate2Async(this.serverUser, this.serverPass, Common.APP_NAME, client);
		}

		/// <summary>Hit when the testing client connects to the server..</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void client_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			this.txtTesting.Text += ".";

			if (e.Error == null)
			{
				//Get a list of projects..
				((SoapServiceClient)e.UserState).Project_RetrieveAsync(e.UserState);
			}
			else
			{
				//We didn't connect. This should never happen.
				Logger.LogError("", e.Error, "Testing new user password. Trying to connect to system.");
				MessageBox.Show("Could not connect to the server. This could mean all licenses are in use.", "Error Testing", MessageBoxButton.OK, MessageBoxImage.Warning);
				this.divTesting.Visibility = System.Windows.Visibility.Collapsed;
			}
		}

		/// <summary>Hit after we logged in. We have to select a project before creating a user. D'oh.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Project_RetrieveCompletedEventArgs</param>
		void client_Project_RetrieveCompleted(object sender, Project_RetrieveCompletedEventArgs e)
		{
			this.txtTesting.Text += ".";

			if (e.Error == null)
			{
				//We got out projects.  Get the first one and connect to it.
				RemoteProject tmpProj = e.Result.Where(prj => prj.Active).FirstOrDefault();
				if (tmpProj != null)
					((SoapServiceClient)e.UserState).Connection_ConnectToProjectAsync(tmpProj.ProjectId.Value, e.UserState);
				else
				{
					Exception ex = new Exception("No active projects to select to!");
					Logger.LogError("", ex, "Testing new user password. Trying to retrieve projects.");
					MessageBox.Show("Could not connect to an existing project. To test the password, there must be an existing active project to connect you. You can continue without testing the password, but if the import fails you will need to re-run it.", "Error Testing", MessageBoxButton.OK, MessageBoxImage.Warning);
					this.divTesting.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		/// <summary>Hit after we connected to a project.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Connection_ConnectToProjectCompletedEventArgs</param>
		private void client_Connection_ConnectToProjectCompleted(object sender, Connection_ConnectToProjectCompletedEventArgs e)
		{
			this.txtTesting.Text += ".";

			if (e.Error == null)
			{
				//We connected successfully. Create the fake user..
				RemoteUser tmpUser = new RemoteUser();
				tmpUser.Active = true;
				tmpUser.Admin = false;
				tmpUser.EmailAddress = "test@" + Common.APP_NAME.Replace(" ", ".") + ".com";
				tmpUser.FirstName = "Test";
				tmpUser.LastName = "User";
				tmpUser.UserName = Common.APP_NAME.Replace(" ", ".") + ".test";

				((SoapServiceClient)e.UserState).User_CreateAsync(tmpUser, this.txtDefaultPassword.Text, "1=1", "2", null, e.UserState);
			}
			else
			{
				Logger.LogError("", e.Error, "Testing new user password. Trying to connect to to Project");
				MessageBox.Show("Could not connect to an existing project. To test the password, there must be an existing project to connect you. You can continue without testing the password, but if the import fails you will need to re-run it.", "Error Testing", MessageBoxButton.OK, MessageBoxImage.Warning);
				this.divTesting.Visibility = System.Windows.Visibility.Collapsed;
			}
		}

		/// <summary>Called when the user is created.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">User_CreateCompletedEventArgs</param>
		private void client_User_CreateCompleted(object sender, User_CreateCompletedEventArgs e)
		{
			this.txtTesting.Text += ".";

			if (e.Error == null)
			{
				((SoapServiceClient)e.UserState).User_DeleteAsync(e.Result.UserId.Value, e.UserState);
			}
			else
			{
				//We couldn't create the new user.
				this.spnSpinner.Visibility = System.Windows.Visibility.Hidden;
				this.txtTesting.Text = "Password not long enough, or does not meet security settings.";
				this.txtTesting.Foreground = Brushes.DarkRed;
				this.divTesting.MouseDown += divTesting_MouseDown;
				this.divTesting.Cursor = Cursors.Cross;
			}
		}

		/// <summary>Hit when we're deleting the user.</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void client_User_DeleteCompleted(object sender, AsyncCompletedEventArgs e)
		{
			this.txtTesting.Text += ".";

			//Always disconnect and log success.
			((SoapServiceClient)e.UserState).Connection_DisconnectAsync();

			//Show that we succeeded..
			this.spnSpinner.Visibility = System.Windows.Visibility.Hidden;
			this.txtTesting.Text = "Valid Password!";
			this.txtTesting.Foreground = Brushes.DarkGreen;
			this.divTesting.MouseDown += divTesting_MouseDown;
			this.divTesting.Cursor = Cursors.Cross;

			if (e.Error != null)
			{
				//Log message to the log file stating that the user couldn't be deleted.
				Logger.LogError("", e.Error, "Testing new user password. Trying to remove test user failed.");
			}
		}
		#endregion

		/// <summary>Hit when the user wants to clear the div.. only enabled after we succeeded or failed.</summary>
		/// <param name="sender">Border</param>
		/// <param name="e">MouseButtonEventArgs</param>
		private void divTesting_MouseDown(object sender, MouseButtonEventArgs e)
		{
			this.divTesting.Visibility = System.Windows.Visibility.Collapsed;
		}
	}
}
