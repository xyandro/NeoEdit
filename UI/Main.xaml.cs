using System.Windows;

namespace NeoEdit.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class Main : Window
	{
		public Main()
		{
			InitializeComponent();
			Show();
			new Browser(new NeoEdit.DiskModule.Local.Disk(), null);
		}
	}
}
