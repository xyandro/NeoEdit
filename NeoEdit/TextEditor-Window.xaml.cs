using System.IO;
using System.Linq;
using System.Text;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Transform;
using NeoEdit.Dialogs;

namespace NeoEdit
{
	partial class TextEditor
	{

		static public void Command_Window_TabIndex(ITextEditor te, bool activeOnly)
		{
			te.ReplaceSelections((te.TabsParent.GetIndex(te, activeOnly) + 1).ToString());
		}
	}
}
