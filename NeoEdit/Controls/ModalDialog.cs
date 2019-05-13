using System.Windows;

namespace NeoEdit.Controls
{
	public class ModalDialog : Window
	{
		public ModalDialog()
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
