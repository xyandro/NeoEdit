using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Network
{
	public enum NetworkCommand
	{
		None,
		File_Exit,
		[KeyGesture(Key.F, ModifierKeys.Control)] Socket_Forward,
	}
}
