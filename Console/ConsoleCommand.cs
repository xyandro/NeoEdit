using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Console
{
	public enum ConsoleCommand
	{
		None,
		[KeyGesture(Key.N, ModifierKeys.Control)] [KeyGesture(Key.T, ModifierKeys.Control, false)] File_New,
	}
}
