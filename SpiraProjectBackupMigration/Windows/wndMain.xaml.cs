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

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	/// <summary>
	/// Interaction logic for wndMain.xaml
	/// </summary>
	public partial class wndMain : Window
	{
		public wndMain()
		{
			InitializeComponent();
		}

		/// <summary>Hit when the user wants to export a project.</summary>
		/// <param name="sender">btnExport</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnExport_Click(object sender, RoutedEventArgs e)
		{
			this.Hide();
//			try
//			{
				wndExport windowExport = new wndExport();
				windowExport.ShowDialog();
//			}
//			catch (Exception ex)
//			{ }

			this.Show();
		}

		/// <summary>Hit when the user wants to import a project.</summary>
		/// <param name="sender">btnImport</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnImport_Click(object sender, RoutedEventArgs e)
		{
			this.Hide();
			try
			{
				wndImport windowImport = new wndImport();
				windowImport.ShowDialog();
			}
			catch (Exception ex)
			{
			}
			this.Show();
		}

		/// <summary>Hit when the user wants to transfer a project.</summary>
		/// <param name="sender">btnTransfer</param>
		/// <param name="e">RoutedEventArgs</param>
		private void btnTransfer_Click(object sender, RoutedEventArgs e)
		{
			this.Hide();
			try
			{
				wndTransfer windowTransfer = new wndTransfer();
				windowTransfer.ShowDialog();
			}
			catch (Exception ex)
			{
			}
			this.Show();
		}
	}
}
