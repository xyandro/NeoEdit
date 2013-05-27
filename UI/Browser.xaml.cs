using NeoEdit.LocalDisk;
using System.Windows.Input;

namespace NeoEdit.UI
{
	public partial class Browser : UIWindow
	{
		[DepProp]
		public string DirectoryName { get { return GetProp<string>(); } set { SetProp(value); } }

		static Browser()
		{
			Register<Browser>();
		}

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
