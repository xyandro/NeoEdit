using System.Windows;
using NeoEdit.UI;

namespace NeoEdit
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			new Browser(new Records.Disk.DiskRoot(Records.Root.AllRoot));
		}
	}
}
