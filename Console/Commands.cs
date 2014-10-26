using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Console
{
	enum ConsoleCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control)] File_New,
		View_Tiles,
	}
}
