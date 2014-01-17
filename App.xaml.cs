using System.Windows;
using NeoEdit.Records;
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
			new Browser(Root.AllRoot.GetRecord(@"E:\Dev\Misc\NeoEdit") as RecordList);
		}
	}
}
