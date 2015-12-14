using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Tools
{
	public enum ToolsCommand
	{
		None,
		File_Exit,
		[KeyGesture(Key.N, ModifierKeys.Control)] Tools_NSRLTool
	}
}
