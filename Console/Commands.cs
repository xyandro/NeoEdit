using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	enum ConsoleCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control, false)] File_New,
		View_Tiles,
	}
}
