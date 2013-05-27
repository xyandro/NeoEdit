using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.LocalDisk;

namespace NeoEdit.UI
{
	/// <summary>
	/// Interaction logic for Directory.xaml
	/// </summary>
	public partial class Browser : Window
	{
		public static readonly DependencyProperty DirectoryNameProperty = DependencyProperty.Register("DirectoryName", typeof(string), typeof(Browser));
		public string DirectoryName { get { return (string)GetValue(DirectoryNameProperty); } set { SetValue(DirectoryNameProperty, value); } }

		Disk Disk;
		Dir Directory;
		public Browser(Disk disk, string directory)
		{
			InitializeComponent();
			Disk = disk;
			SetDirectory(directory);
		}

		public void SetDirectory(string directory)
		{
			Directory = Disk.GetDirectory(directory);
			DirectoryName = Directory.Name;
			files.ItemsSource = Directory.Files;
		}

		private void files_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				SetDirectory(files.SelectedItem as string);
			}
		}
	}
}
