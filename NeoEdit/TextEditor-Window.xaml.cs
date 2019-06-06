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

		void Command_Window_TabIndex(bool activeOnly)
		{
			ReplaceSelections((TabsParent.GetIndex(this, activeOnly) + 1).ToString());
		}
	}
}
