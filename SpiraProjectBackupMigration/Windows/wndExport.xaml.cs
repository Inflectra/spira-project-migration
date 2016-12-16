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
using System.Windows.Shapes;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for wndExport.xaml
	/// </summary>
	public partial class wndExport : Window
	{
		private const string CLASS_NAME = "wndExport.";

		//Set our controls..
		private usercntSpiraConnect _page1 = null;
		private usercntSpiraSelProject _page2 = null;
		private usercntSelOutputFile _page3 = null;
		private usercntVerifyExport _page4 = null;
		private usercntProgressOutput _page5 = null;
		private int _pageNo = 0;

		public wndExport()
		{
			const string METHOD = CLASS_NAME + ".ctor()";

			InitializeComponent();

			//Log window run..
			Logger.LogTrace(METHOD, "Opened Project Export window.");

			//Create our controls..
			this._page1 = new usercntSpiraConnect();
			this._page1.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page2 = new usercntSpiraSelProject();
			this._page2.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page3 = new usercntSelOutputFile();
			this._page3.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page4 = new usercntVerifyExport();
			this._page4.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page5 = new usercntProgressOutput();
			this._page5.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);

			//Load up the content..
			this.grdContent.Children.Add(this._page1);
			this._pageNo = 1;
		}

		/// <summary>Hit when a page's Verfified changes.</summary>
		/// <param name="sender">A userControl for display.</param>
		/// <param name="e">EventArgs</param>
		private void page_onVerifiedChanged(object sender, EventArgs e)
		{
			dynamic page = (dynamic)sender;

			if (page.isVerified)
				this.btnNext.IsEnabled = true;
			else
				this.btnNext.IsEnabled = false;

			if (this._pageNo == 5)
				this.btnNext.Content = "Finish";
		}

		/// <summary>Hit when the user clicks the 'next' button.</summary>
		/// <param name="sender">btnNext</param>
		/// <param name="e">RoutedEventArgs</param>
		private void onNextClick(object sender, RoutedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "onNextClick()";

			//Depending on which page is displayed..
			switch (this._pageNo)
			{
				case 1:
					{
						try
						{
							//Set the controls on the form..
							this.grdForm.IsEnabled = false;
							this.Cursor = Cursors.Wait;
							this.btnNext.IsEnabled = false;

							//Try to connect to the server.
							SoapServiceClient client = SpiraClientFactory.CreateClient_Spira5(new Uri(this._page1.SelectedServerURL, UriKind.Absolute));
							client.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(client_Connection_Authenticate2Completed);
                            client.Connection_Authenticate2Async("administrator", this._page1.txtAdminPassword.Password, "ProjectExport");
							Logger.LogTrace(METHOD, "Screen #1: Validating user input.");
						}
						catch (Exception ex)
						{
							string msg = ((ex.InnerException != null) ? ex.InnerException.Message : ex.Message);
							MessageBox.Show("There was an error connecting to the server:" + Environment.NewLine + msg, "Server Unavailable", MessageBoxButton.OK, MessageBoxImage.Error);

							this.grdForm.IsEnabled = true;
							this.Cursor = Cursors.Arrow;
							this.btnNext.IsEnabled = true;
						}
					}
					break;

				case 2:
					{
						//No checking needed, we move on..
						//Advance the page.
						this.btnNext.IsEnabled = false;
						this._pageNo = 3;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page3);
						Logger.LogTrace(METHOD, "Screen #2: Moved to screen #3.");
					}
					break;

				case 3:
					{
						//No checking here, either, we jump to page 4.
						this.btnNext.IsEnabled = false;
						this._pageNo = 4;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page4);
						Logger.LogTrace(METHOD, "Screen #3: Moved to screen #4.");
					}
					break;

				case 4:
					{
						//The confirmed they want to continue. Start exporting.
						this._pageNo = 5;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page5);
						Logger.LogTrace(METHOD, "Screen #4: Moved to screen #5.");

						this._page5.setConnectionInfo(this._page2.SelectedProject, this._page1.SelectedPassword, this._page3.SelectedFile, new Uri(this._page1.SelectedServerURL));

						//Disable Back button.
						this.btnBack.IsEnabled = false;
						this.btnCancel.IsEnabled = false;
						this.btnNext.IsEnabled = false;
					}
					break;

				case 5:
					{
						//Clear the log (in case multiple errors, do it here.) And then close the window.
						Logger.ClearLogFile();
						this.Close();
					}
					break;
			}

		}

		/// <summary>Hit when the test for connection is finished.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void client_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "client_Connection_Authenticate2Completed()";

			if (e.Error == null && e.Result)
			{
				Logger.LogTrace(METHOD, "Screen #1: Logged in to server, getting project list.");

				//Load the projects..
				((SoapServiceClient)sender).Project_RetrieveCompleted += new EventHandler<Project_RetrieveCompletedEventArgs>(client_Project_RetrieveCompleted);
				((SoapServiceClient)sender).Project_RetrieveAsync();
			}
			else
			{
				if (e.Error != null)
				{
					if (e.Error.InnerException != null && e.Error.InnerException.GetType() == typeof(System.Net.WebException))
					{
						if (e.Error.InnerException.Message.Contains("could not be resolved"))
							MessageBox.Show("The server could not be found. IP Address or DNS name unreachable.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
						else if (e.Error.InnerException.Message.Contains("returned an error"))
							MessageBox.Show("The API could not be reached at that address. Please check the entered URL. Spira version 4.0 or newer is required.", "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
						else
							MessageBox.Show("There was an error connecting to the server:" + Environment.NewLine + e.Error.InnerException.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					else
						MessageBox.Show("There was an error connecting to the server:" + Environment.NewLine + e.Error.Message, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
					Logger.LogError(METHOD, e.Error, "While connecting to server:");
				}
				else
				{
					MessageBox.Show("Invalid password for the Administrator account!", "Incorrect Login", MessageBoxButton.OK, MessageBoxImage.Error);
					Logger.LogError(METHOD, null, "While connecting to server:" + Environment.NewLine + "Invalid password given for user.");
				}

				//Reset form.
				this.grdForm.IsEnabled = true;
				this.Cursor = Cursors.Arrow;
				this.btnNext.IsEnabled = true;
			}
		}

		/// <summary>Hit when we got the list of projects.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Project_RetrieveCompletedEventArgs</param>
		private void client_Project_RetrieveCompleted(object sender, Project_RetrieveCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "wndExport_Project_RetrieveCompleted()";

			if (e.Error == null)
			{
				Logger.LogTrace(METHOD, "Screen #1: Got project list.");

				//Set the data source..
				List<SpiraSoapService.RemoteProject> newList = new List<RemoteProject>();
				if (e.Result != null)
					newList = e.Result.ToList<SpiraSoapService.RemoteProject>();
				this._page2.setAvailableProjects(newList);

				//Advance the page.
				this.grdContent.Children.Clear();
				this.grdContent.Children.Add(this._page2);
				this._pageNo = 2;

				this.grdForm.IsEnabled = true;
				this.Cursor = Cursors.Arrow;
				this.btnBack.IsEnabled = true;

				//Log it..
				Logger.LogTrace(METHOD, "Screen #1: Moved to screen #2.");
			}
			else
			{
				string msg = ((e.Error.InnerException != null) ? e.Error.InnerException.Message : e.Error.Message);
				MessageBox.Show("There was an error retrieving projects from the system:" + Environment.NewLine + msg, "Server Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Logger.LogError(METHOD, e.Error);
			}
		}

		/// <summary>Hit when the user wants to jump back a page.</summary>
		/// <param name="sender">btnBack</param>
		/// <param name="e">RoutedEventArgs</param>
		private void onBackClick(object sender, RoutedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "onBackClick()";

			//Depending on which page is displayed..
			switch (this._pageNo)
			{
				case 2:
					{
						//Jump Back a Page.
						this._pageNo = 1;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page1);
						Logger.LogTrace(METHOD, "Screen #2: Moved to screen #1.");

						//Disable Back, since we can't go back past page 1.
						this.btnBack.IsEnabled = false;
						this.btnNext.IsEnabled = true;
					}
					break;

				case 3:
					{
						//Jump Back a Page.
						this.btnBack.IsEnabled = true;
						this.btnNext.IsEnabled = true;
						this._pageNo = 2;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page2);
						Logger.LogTrace(METHOD, "Screen #3: Moved to screen #2.");
					}
					break;

				case 4:
					{
						//No checking here, either, we jump to page 4.
						this.btnNext.IsEnabled = true;
						this._pageNo = 3;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page3);
						Logger.LogTrace(METHOD, "Screen #4: Moved to screen #3.");
					}
					break;
			}

		}

		/// <summary>Hit when the user wants to cancel.</summary>
		/// <param name="sender">btnCancel</param>
		/// <param name="e">RoutedEventArgs</param>
		private void onCancelClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
