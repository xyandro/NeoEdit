using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Processes
{
	enum ProcessesCommand
	{
		None,
		[KeyGesture(Key.F5)] View_Refresh,
		[KeyGesture(Key.H, ModifierKeys.Control)] View_Handles,
		[KeyGesture(Key.M, ModifierKeys.Control)] View_Memory,
		View_Modules,
		[KeyGesture(Key.S, ModifierKeys.Control)] Process_Suspend,
		[KeyGesture(Key.R, ModifierKeys.Control)] Process_Resume,
		[KeyGesture(Key.Delete)] Process_Kill,
	}
}
