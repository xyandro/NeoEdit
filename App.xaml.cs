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
			//var data = System.IO.File.ReadAllBytes(@"E:\Dev\Misc\NeoEdit\bin\Debug\magic.mgc");
			//var data = System.IO.File.ReadAllBytes(@"E:\Dev\Misc\NeoEdit\bin\Debug\NeoEdit.exe.config");
			var data = System.IO.File.ReadAllBytes(@"C:\Docs\Cpp\NeoEdit\bin\Debug\TestData\Combined.txt");
			new NeoEdit.UI.BinaryEditorUI.BinaryEditor(data);

			//new Test.UnicodeGenerator().Generate();
			//new Test.ConverterTest().Run();
		}
	}
}
