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
	public partial class usercntSpiraConnect : UserControl
	{
		private bool _isVerified = false;

		public usercntSpiraConnect()
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
		public string SelectedServerURL
		{
			get
			{
				if (this.isVerified)
				{
					string retString = "";
					retString += this.cmbHttps.Text;
					retString += this.txtServerName.Text.Trim();

					return retString;
				}
				else
				{
					throw new Exception("There are errors on this page.");
				}
			}
		}

		/// <summary>Gets the entered-in Admin password.</summary>
		/// <returns>The password.</returns>
		public string SelectedPassword
		{
			get
			{
				return this.txtAdminPassword.Password;
			}
		}

		public event EventHandler onVerifiedChanged;

        private void onTextChanged(object sender, TextChangedEventArgs e)
        {
            //Check both fields to verify they're okay.
            if (!String.IsNullOrWhiteSpace(this.txtServerName.Text.Trim()) && !String.IsNullOrWhiteSpace(this.txtAdminPassword.Password.Trim()))
                this.isVerified = true;
            else
                this.isVerified = false;
        }

        private void onPasswordChanged(object sender, RoutedEventArgs e)
		{
			//Check both fields to verify they're okay.
            if (!String.IsNullOrWhiteSpace(this.txtServerName.Text.Trim()) && !String.IsNullOrWhiteSpace(this.txtAdminPassword.Password.Trim()))
				this.isVerified = true;
			else
				this.isVerified = false;
		}
	}
}
