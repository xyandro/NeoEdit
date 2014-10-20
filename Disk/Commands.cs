using System.Windows.Input;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	class DiskMenuItem : NEMenuItem<DiskCommand> { }

	enum DiskCommand
	{
		None,
		[Header("_Rename")] [KeyGesture(Key.F2)] File_Rename,
		[Header("_Identify")] [KeyGesture(Key.I, ModifierKeys.Control)] File_Identify,
		[Header("_MD5")] [KeyGesture(Key.M, ModifierKeys.Control)] File_MD5,
		[Header("_SHA1")] [KeyGesture(Key.S, ModifierKeys.Control)] File_SHA1,
		[Header("_Delete")] [KeyGesture(Key.Delete)] File_Delete,
		[Header("Cu_t")] [KeyGesture(Key.X, ModifierKeys.Control)] Edit_Cut,
		[Header("_Copy")] [KeyGesture(Key.C, ModifierKeys.Control)] Edit_Copy,
		[Header("_Paste")] [KeyGesture(Key.V, ModifierKeys.Control)] Edit_Paste,
		[Header("_Refresh")] [KeyGesture(Key.F5)] View_Refresh,
	}
}
