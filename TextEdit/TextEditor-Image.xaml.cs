using System.Data;
using System.Linq;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		ImageGrabColorDialog.Result Command_Image_GrabColor_Dialog() => ImageGrabColorDialog.Run(WindowParent, Selections.Select(range => GetString(range)).FirstOrDefault());

		void Command_Image_GrabColor(ImageGrabColorDialog.Result result) => ReplaceSelections(result.Color);
	}
}
