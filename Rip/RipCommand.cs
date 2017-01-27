using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Rip
{
	public enum RipCommand
	{
		None,
		File_Exit,
		Edit_CopyTitles,
		[KeyGesture(Key.C, ModifierKeys.Control)] Edit_CopyFileNames,
		[KeyGesture(Key.D, ModifierKeys.Control)] Add_CD,
		[KeyGesture(Key.Y, ModifierKeys.Control)] Add_YouTube,
	}
}
