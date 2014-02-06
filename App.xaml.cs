using System.IO;
using System.Windows;

namespace NeoEdit
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		App()
		{
			var dir = Directory.GetCurrentDirectory();
			//var file = Path.Combine(dir, "magic.mgc");
			//var file = Path.Combine(dir, "NeoEdit.exe.config");
			//var file = Path.Combine(dir, "TestData", "Combined.txt");
			//if (!File.Exists(file))
			//	new Test.UnicodeGenerator().Generate();

			//var data = new NeoEdit.Common.BinaryData(File.ReadAllBytes(file));
			//new NeoEdit.BinaryEditorUI.BinaryEditor(data);

			//new Test.ConverterTest().Run();
			new NeoEdit.BrowserUI.Browser();
		}
	}
}
