using NeoEdit.UI;
using System.Windows;

namespace NeoEdit
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			new Browser(Records.Disk.DiskRoot.RootName);
		}
	}
}
