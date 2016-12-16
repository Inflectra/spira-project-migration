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
	public partial class wndImport : Window
	{
		private const string CLASS_NAME = "wndImport.";

		//Set our controls..
		private usercntSpiraConnect _page1 = null;
		private usercntSpiraCrtProject _page2 = null;
		private usercntSpiraNewUser _page3 = null;
		private usercntSelInputFile _page4 = null;
		private usercntVerifyImport _page5 = null;
		private usercntProgressImport _page6 = null;
		private int _pageNo = 0;

		public wndImport()
		{
			const string METHOD = CLASS_NAME + ".ctor()";

			InitializeComponent();

			//Log window run..
			Logger.LogTrace(METHOD, "Opened Project Export window.");

			//Create our controls..
			this._page1 = new usercntSpiraConnect();
			this._page1.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page2 = new usercntSpiraCrtProject();
			this._page2.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page3 = new usercntSpiraNewUser();
			this._page3.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page4 = new usercntSelInputFile();
			this._page4.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page5 = new usercntVerifyImport();
			this._page5.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);
			this._page6 = new usercntProgressImport();
			this._page6.onVerifiedChanged += new EventHandler(page_onVerifiedChanged);

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

			this.btnNext.IsEnabled = page.isVerified;

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
                            client.Connection_Authenticate2Async(Common.USER_NAME, this._page1.txtAdminPassword.Password, "ProjectExport");
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
						this.btnNext.IsEnabled = true;
						this._pageNo = 3;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page3);
						this._page3.setServerLogin(new Uri(this._page1.SelectedServerURL), Common.USER_NAME, this._page1.SelectedPassword);
						Logger.LogTrace(METHOD, "Screen #2: Moved to screen #3.");
					}
					break;

				case 3:
					{
						//No checking needed, we move on..
						//Advance the page.
						this.btnNext.IsEnabled = false;
						this._pageNo = 4;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page4);
						Logger.LogTrace(METHOD, "Screen #3: Moved to screen #4.");
					}
					break;

				case 4:
					{
						//No checking needed, we move on..
						//Advance the page.
						this.btnNext.IsEnabled = false;
						this._pageNo = 5;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page5);
						this._page5.setInformation(System.IO.Path.GetFileName(this._page4.SelectedFile), this._page2.SelectedProjectName, this._page3.SelectedUserPass);
						Logger.LogTrace(METHOD, "Screen #3: Moved to screen #4.");
					}
					break;

				case 5:
					{
						//The confirmed they want to continue. Start exporting.
						this._pageNo = 6;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page6);
						Logger.LogTrace(METHOD, "Screen #5: Moved to screen #6.");

						this._page6.setConnectionInfo(
							this._page2.SelectedProjectName, 
							this._page1.SelectedPassword, 
							this._page4.SelectedFile, 
							new Uri(this._page1.SelectedServerURL),
							this._page3.SelectedUserPass);

						//Disable Back button.
						this.btnBack.IsEnabled = false;
						this.btnCancel.IsEnabled = false;
						this.btnNext.IsEnabled = false;
					}
					break;

				case 6:
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
		private void client_Connection_Authenticate2Completed(object sender, Connection_Authenticate2CompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "client_Connection_Authenticate2Completed()";

			if (e.Error == null && e.Result)
			{
				Logger.LogTrace(METHOD, "Screen #1: Moved to screen #2.");

				//Advance the page.
				this.grdContent.Children.Clear();
				this.grdContent.Children.Add(this._page2);
				this._pageNo = 2;

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
						Logger.LogTrace(METHOD, "Screen #2: Moved to screen #1.");

						//Disable Back, since we can't go back past page 1.
						this.btnBack.IsEnabled = false;
						this.btnNext.IsEnabled = true;
					}
					break;
				case 3:
					{
						//Jump Back a Page.
						this._pageNo = 2;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page2);
						Logger.LogTrace(METHOD, "Screen #3: Moved to screen #2.");

						//Disable Back, since we can't go back past page 1.
						this.btnNext.IsEnabled = true;
					}
					break;

				case 4:
					{
						//Jump Back a Page.
						this._pageNo = 3;
						this.grdContent.Children.Clear();
						this.grdContent.Children.Add(this._page4);
						Logger.LogTrace(METHOD, "Screen #4: Moved to screen #3.");

						//Disable Back, since we can't go back past page 1.
						this._page5.setInformation(null, null, null);
						this.btnNext.IsEnabled = true;
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
