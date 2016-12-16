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

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for usercntSpiraCrtProject.xaml
	/// </summary>
	public partial class trnsCreateProject : UserControl
	{
		private bool _isVerified = false;

		public trnsCreateProject()
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
				this._isVerified = value;
				this.onVerifiedChanged(this, new EventArgs());
			}
		}

		public event EventHandler onVerifiedChanged;

		/// <summary>Hit when the user types into the Project Name textbox.</summary>
		/// <param name="sender">txtProjectName</param>
		/// <param name="e">TextChangedEventArgs</param>
		private void txtProjectName_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.isVerified = (this.txtProjectName.Text.Length > 3);
		}

		/// <summary>The selected project name.</summary>
		public string SelectedProjectName
		{
			get
			{
				return this.txtProjectName.Text.Trim();
			}
		}
	}
}
