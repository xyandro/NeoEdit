using System.Windows;

namespace NeoEdit.UI.Windows
{
	public partial class BinaryEditor : Window
	{
		public BinaryEditor(byte[] data)
		{
			InitializeComponent();
			Show();

			view.Data = data;
		}
	}
}
