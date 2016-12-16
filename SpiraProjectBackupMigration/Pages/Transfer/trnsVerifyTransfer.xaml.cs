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

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for usercntSpiraConnect.xaml
	/// </summary>
	public partial class trnsVerifyTransfer: UserControl
	{
		private bool _isVerified = false;

		public trnsVerifyTransfer()
		{
			InitializeComponent();
			this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(usercntVerifyExport_IsVisibleChanged);
		}

		/// <summary>Sets the information to display to the user.</summary>
		/// <param name="fileName">The filename we're expoting to.</param>
		/// <param name="newProject">The new project's name.</param>
		public void setInformation(string inServer, string inProject, string outServer, string outProject)
		{
			this.txtInServer.Text = inServer;
			this.txtInProject.Text = inProject;
			this.txtOutServer.Text = outServer;
			this.txtOutProject.Text = outProject;
		}

		/// <summary>Hit when we become visible, to automatically set the 'Next' button visible.</summary>
		/// <param name="sender">This</param>
		/// <param name="e">DependencyPropertyChangedEventArgs</param>
		private void usercntVerifyExport_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (this.onVerifiedChanged != null)
				this.onVerifiedChanged(this, new EventArgs());
		}

		/// <summary>Specifies whether or not this page is good to go on to the next.</summary>
		public bool isVerified
		{
			get
			{
				return true;
			}
			set
			{ }
		}

		public event EventHandler onVerifiedChanged;
	}
}
