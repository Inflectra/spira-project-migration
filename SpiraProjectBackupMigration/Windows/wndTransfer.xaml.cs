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
	public partial class wndTransfer : Window
	{
		private const string CLASS_NAME = "wndTransfer.";

		//Set our controls..
		private trnsSpiraConnect1 _page1 = null;
		private trnsSelectProject _page2 = null;
		private trnsSpiraConnect2 _page3 = null;
		private trnsCreateProject _page4 = null;
		private trnsSpiraNewUser _page5 = null;
		private trnsVerifyTransfer _page6 = null;
		private trnsProgressOut _page7 = null;
		private trnsProgressIn _page8 = null;

		private bool _isError = false;

		private int _pageNo = 0;

		public wndTransfer()
		{
			const string METHOD = CLASS_NAME + ".ctor()";

			InitializeComponent();

			//Log window run..
			Logger.LogTrace(METHOD, "Opened Project Export window.");

			//Create our controls..
			this._page1 = new trnsSpiraConnect1();
			this._page1.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page2 = new trnsSelectProject();
			this._page2.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page3 = new trnsSpiraConnect2();
			this._page3.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page4 = new trnsCreateProject();
			this._page4.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page5 = new trnsSpiraNewUser();
			this._page5.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page6 = new trnsVerifyTransfer();
			this._page6.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page7 = new trnsProgressOut();
			this._page7.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page8 = new trnsProgressIn();
			this._page8.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);

			//Load up the content..
			this.grdContent.Children.Add(this._page1);
			this._pageNo = 1;
		}

		/// <summary>Hit when a page's Verfified changes.</summary>
		/// <param name="sender">A userControl for display.</param>
		/// <param name="e">EventArgs</param>
		private void page_onVerifiedChanged(object sender, EventArgs e)
		{
			const string METHOD = CLASS_NAME + "page_onVerifiedChanged()";

			dynamic page = (dynamic)sender;

			if (page.isVerified)
				this.btnNext.IsEnabled = true;
			else
				this.btnNext.IsEnabled = false;

			if (this._pageNo == 8 || (this._pageNo == 7 && this._page7.isError))
				this.btnNext.Content = "Finish";

			if (this._pageNo == 7 && page.isVerified && !this._page7.isError)
			{
				//Automatically advance..
				this._pageNo = 8;
				this.grdContent.Children.Clear();
				this.grdContent.Children.Add(this._page8);
				Logger.LogTrace(METHOD, "Screen #7: Automagically moved to screen #8.");

				this._page8.setConnectionInfo(
					this._page4.SelectedProjectName,
					this._page3.SelectedPassword,
					this._page7.SelectedOutputDir,
					new Uri(this._page3.SelectedServerURL),
					this._page3.SelectedPassword
					);
			}
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
							SoapServiceClient client1 = SpiraClientFactory.CreateClient_Spira5(new Uri(this._page1.SelectedServerURL, UriKind.Absolute));
							client1.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(client1_Connection_Authenticate2Completed);
							client1.Connection_Authenticate2Async("administrator", this._page1.txtAdminPassword.Text, "ProjectExport");
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
						try
						{
							//Set the controls on the form..
							this.grdForm.IsEnabled = false;
							this.Cursor = Cursors.Wait;
							this.btnNext.IsEnabled = false;

							//Check that the server is not the same as the input..
							Uri server1 = new Uri(this._page1.SelectedServerURL);
							Uri server2 = new Uri(this._page3.SelectedServerURL);
							bool server1Local = (server1.Host.ToLowerInvariant().Trim() == "localhost" || server1.Host.ToLowerInvariant().Trim() == "127.0.0.1");
							bool server2Local = (server2.Host.ToLowerInvariant().Trim() == "localhost" || server2.Host.ToLowerInvariant().Trim() == "127.0.0.1");
							if (!server1.Equals(server2) && (!server1Local || !server2Local))
							{
								//Try to connect to the server.
								SoapServiceClient client2 = SpiraClientFactory.CreateClient_Spira5(new Uri(this._page3.SelectedServerURL, UriKind.Absolute));
								client2.Connection_Authenticate2Completed += new EventHandler<Connection_Authenticate2CompletedEventArgs>(client2_Connection_Authenticate2Completed);
								client2.Connection_Authenticate2Async(Common.USER_NAME, this._page3.txtAdminPassword.Text, Common.APP_NAME);
								Logger.LogTrace(METHOD, "Screen #3: Validating user input.");
							}
							else
							{
								MessageBox.Show("Both your import and export servers are the same. If importing and exporting to the same server, we recommend you use the Project copy feature in Administration.", "Same Host", MessageBoxButton.OK, MessageBoxImage.Exclamation);

								//Rest form.
								this.grdForm.IsEnabled = true;
								this.Cursor = Cursors.Arrow;
								this.btnNext.IsEnabled = true;
							}
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

				case 4:
					{
						//No checking needed, we move on..
						//Advance the page.
						this.btnNext.IsEnabled = true;
						this._pageNo = 5;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page5);
						this._page5.setServerLogin(new Uri(this._page3.SelectedServerURL), Common.USER_NAME, this._page3.SelectedPassword);
						Logger.LogTrace(METHOD, "Screen #4: Moved to screen #5.");
					}
					break;

				case 5:
					{
						//Entered in project name, show summary screen.
						this._pageNo = 6;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page6);
						Logger.LogTrace(METHOD, "Screen #5: Moved to screen #6.");

						//Generate the URLs, so we can parse them..
						Uri server1 = new Uri(this._page1.SelectedServerURL);
						Uri server2 = new Uri(this._page3.SelectedServerURL);
						this._page6.setInformation(
							this._page1.SelectedServerURL,
							this._page2.SelectedProject.Name + " [PR:" + this._page2.SelectedProject.ProjectId.Value.ToString() + "]",
							this._page3.SelectedServerURL,
							this._page4.SelectedProjectName);

						//Set controls.						
						this.btnBack.IsEnabled = true;
						this.btnCancel.IsEnabled = true;
						this.btnNext.IsEnabled = true;
					}
					break;

				case 6:
					{
						//The confirmed they want to continue. Start exporting.
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page7);
						Logger.LogTrace(METHOD, "Screen #6: Moved to screen #7.");
						this._pageNo = 7;

						//Start export.
						this._page7.setConnectionInfo(this._page2.SelectedProject, this._page1.SelectedPassword, new Uri(this._page1.SelectedServerURL));

						//Disable Back button.
						this.btnBack.IsEnabled = false;
						this.btnCancel.IsEnabled = false;
						this.btnNext.IsEnabled = false;
					}
					break;

				case 7:
					{
						//Only close if the Export threw an error.
						if (this._page7.isError) this.Close();
					}
					break;

				case 8:
					{
						//Close the window.
						this.Close();
					}
					break;
			}

		}

		/// <summary>Hit when the test for connection is finished.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void client1_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "client1_Connection_Authenticate2Completed()";

			if (e.Error == null && e.Result)
			{
				Logger.LogTrace(METHOD, "Screen #1: Logged in to server, getting project list.");

				//Load the projects..
				((SoapServiceClient)sender).Project_RetrieveCompleted += new EventHandler<Project_RetrieveCompletedEventArgs>(client1_Project_RetrieveCompleted);
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
		private void client1_Project_RetrieveCompleted(object sender, Project_RetrieveCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "client1_Project_RetrieveCompleted()";

			if (e.Error == null)
			{
				Logger.LogTrace(METHOD, "Screen #1: Got project list.");

				//Set the data source..
				if (e.Result != null)
					this._page2.setAvailableProjects(e.Result);

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

		/// <summary>Hit when the test for connection is finished.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Connection_Authenticate2CompletedEventArgs</param>
		private void client2_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "client2_Connection_Authenticate2Completed()";

			if (e.Error == null && e.Result)
			{
				Logger.LogTrace(METHOD, "Screen #3: Moved to screen #4.");

				//Advance the page.
				this.grdContent.Children.Clear();
				this.grdContent.Children.Add(this._page4);
				this._page4.txtProjectName.Text = this._page2.SelectedProject.Name;
				this._pageNo = 4;

				this.grdForm.IsEnabled = true;
				this.Cursor = Cursors.Arrow;
				this.btnBack.IsEnabled = true;
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
						Logger.LogTrace(METHOD, "Screen #2: Moved back to screen #1.");

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
						Logger.LogTrace(METHOD, "Screen #3: Moved back to screen #2.");
					}
					break;

				case 4:
					{
						//No checking here, either, we jump to page 4.
						this.btnNext.IsEnabled = true;
						this._pageNo = 3;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page3);
						Logger.LogTrace(METHOD, "Screen #4: Moved back to screen #3.");
					}
					break;

				case 5:
					{
						//No checking here, either, we jump to page 4.
						this.btnNext.IsEnabled = true;
						this._pageNo = 4;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page4);
						Logger.LogTrace(METHOD, "Screen #5: Moved back to screen #4.");
					}
					break;

				case 6:
					{
						//No checking here, either, we jump to page 4.
						this.btnNext.IsEnabled = true;
						this._pageNo = 5;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page5);
						Logger.LogTrace(METHOD, "Screen #6: Moved back to screen #5.");
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
