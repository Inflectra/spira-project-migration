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
	public partial class usercntVerifyExport : UserControl
	{
		private bool _isVerified = false;

		public usercntVerifyExport()
		{
			InitializeComponent();
			this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(usercntVerifyExport_IsVisibleChanged);
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
