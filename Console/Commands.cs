using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Console
{
	class ConsoleMenuItem : NEMenuItem<ConsoleCommand> { }

	enum ConsoleCommand
	{
		None,
		[Header("_New")] [KeyGesture(Key.N, ModifierKeys.Control)] File_New,
		[Header("_Tiles")] [KeyGesture(Key.T, ModifierKeys.Control)] View_Tiles,
	}
}
