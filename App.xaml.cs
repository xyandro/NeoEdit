using System.Windows;
using System.IO;

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
			var file = Path.Combine(dir, "TestData", "Combined.txt");
			if (!File.Exists(file))
				new Test.UnicodeGenerator().Generate();

			var data = File.ReadAllBytes(file);
			new NeoEdit.UI.BinaryEditorUI.BinaryEditor(data);

			//new Test.ConverterTest().Run();
		}
	}
}
