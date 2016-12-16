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
	/// Interaction logic for usercntSpiraConnect.xaml
	/// </summary>
	public partial class usercntSpiraSelProject : UserControl
	{
		private bool _isVerified = false;

		public usercntSpiraSelProject()
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

		/// <summary>Gets the entered-in URL.</summary>
		/// <returns>String containing the full service path.</returns>
		public SpiraSoapService.RemoteProject SelectedProject
		{
			get
			{
				return (SpiraSoapService.RemoteProject)this.cmbProject.SelectedItem;
			}
		}

		/// <summary>Set the data source for selecting the projects.</summary>
		/// <param name="projectList">The list of RemoteProjects to select from.</param>
		public void setAvailableProjects(List<SpiraSoapService.RemoteProject> projectList)
		{
			this.cmbProject.ItemsSource = projectList;
		}

		public event EventHandler onVerifiedChanged;

		/// <summary>Hit when the project ID is changed.</summary>
		/// <param name="sender">cmbProject</param>
		/// <param name="e">SelectionChangedEventArgs</param>
		private void onSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
				this.isVerified = true;
			else
				this.isVerified = false;
		}
	}
}
