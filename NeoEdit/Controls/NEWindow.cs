using System.Windows;

namespace NeoEdit.Program.Controls
{
	public class NEWindow : Window
	{
		public NEWindow()
		{
			WindowStyle = WindowStyle.ToolWindow;
		}

		public new bool ShowDialog()
		{
			var result = base.ShowDialog() == true;
			Owner?.Focus();
			return result;
		}
	}
}
