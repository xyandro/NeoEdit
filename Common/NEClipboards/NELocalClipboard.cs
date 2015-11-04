using System.Collections.Generic;

namespace NeoEdit.Common.NEClipboards
{
	public class NELocalClipboard
	{
		readonly IClipboardEnabled control;
		public NELocalClipboard(IClipboardEnabled control) { this.control = control; }

		public string Text { get { return NEClipboard.GetTextInternal(control); } set { NEClipboard.SetTextInternal(control, value); } }
		public string CopiedFile { set { NEClipboard.SetFileInternal(control, value, false); } }
		public string CutFile { set { NEClipboard.SetFileInternal(control, value, true); } }
		public List<string> CopiedFiles { set { NEClipboard.SetFilesInternal(control, value, false); } }
		public List<string> CutFiles { set { NEClipboard.SetFilesInternal(control, value, true); } }
		public List<string> Strings { get { return NEClipboard.GetStringsInternal(control); } set { NEClipboard.SetStringsInternal(control, value); } }
		public List<object> Objects { get { return NEClipboard.GetObjectsInternal(control); } set { NEClipboard.SetObjectsInternal(control, value); } }
	}
}
