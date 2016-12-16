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
using Ionic.Zip;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for usercntSpiraConnect.xaml
	/// </summary>
	public partial class usercntSelInputFile : UserControl
	{
		private bool _isVerified = false;

		public usercntSelInputFile()
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
				if (this.onVerifiedChanged != null)
				{
					this._isVerified = value;
					this.onVerifiedChanged(this, new EventArgs());
				}
			}
		}

		public event EventHandler onVerifiedChanged;

		/// <summary>Hit when the user goes to select their file.</summary>
		/// <param name="sender">btnFileSelect</param>
		/// <param name="e">RoutedEventArgs</param>
		private void onFileSelect(object sender, RoutedEventArgs e)
		{
			//Create a save file dialog..
			OpenFileDialog selectFile = new OpenFileDialog();
			selectFile.AddExtension = true;
			selectFile.CheckFileExists = true;
			selectFile.CheckPathExists = true;
			selectFile.DefaultExt = Common.FILEDIALOG_EXT;
			selectFile.DereferenceLinks = true;
			selectFile.Filter = Common.FILEDIALOG_FILTER;
			selectFile.FilterIndex = 0;
			selectFile.Title = "Select Project File";
			selectFile.ValidateNames = true;

			//Need to get the window..
			DependencyObject parent = null;
			DependencyObject tmp = this;
			while ((tmp = VisualTreeHelper.GetParent(tmp)) != null)
			{
				parent = tmp;
			}

			//Display the dialog.
			bool? isSelected = selectFile.ShowDialog((Window)parent);
			if (isSelected.HasValue && isSelected.Value)
			{
				bool isOkay = false;

				try
				{
					//Verify the file's a real project and not some random ZIP..
					using (ZipFile zip = ZipFile.Read(selectFile.FileName))
					{
						try
						{
							ZipEntry zipFile = zip[Common.PROJECT_FILE];
							if (zipFile != null) isOkay = true;
						}
						catch
						{ }
					}
				}
				catch
				{ }

				if (isOkay)
				{
					this.txtFileName.Text = selectFile.FileName;
					this.isVerified = true;
				}
				else
					MessageBox.Show("The selected file is not a valid project export.", "Invalid File", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);

			}
		}

		/// <summary>Checks to make sure the path is good.</summary>
		/// <param name="sender">txtFileName</param>
		/// <param name="e">TextChangedEventArgs</param>
		private void onTextChanged(object sender, TextChangedEventArgs e)
		{
			this.isVerified = (File.Exists(this.txtFileName.Text.Trim()));
		}

		/// <summary>The filename the user selected.</summary>
		public string SelectedFile
		{
			get
			{
				return this.txtFileName.Text.Trim();
			}
		}
	}
}
