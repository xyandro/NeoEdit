using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip
{
	public enum RipCommand
	{
		None,
		File_Exit,
		[KeyGesture(Key.D, ModifierKeys.Control)] Add_CD,
	}
}
