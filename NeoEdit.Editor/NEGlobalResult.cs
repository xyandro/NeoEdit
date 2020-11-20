using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobalResult
	{
		public List<NEWindow> AddNEWindows { get; private set; }
		public List<NEWindow> RemoveNEWindows { get; private set; }
		public NEClipboard Clipboard { get; private set; }
		public IReadOnlyList<KeysAndValues>[] KeysAndValues { get; private set; }
		public IReadOnlyList<string> DragFiles { get; private set; }

		public void AddNEWindow(NEWindow neWindow)
		{
			if (AddNEWindows == null)
				AddNEWindows = new List<NEWindow>();
			AddNEWindows.Add(neWindow);
			if (RemoveNEWindows != null)
				RemoveNEWindows.Remove(neWindow);
		}

		public void RemoveNEWindow(NEWindow neWindow)
		{
			if (RemoveNEWindows == null)
				RemoveNEWindows = new List<NEWindow>();
			RemoveNEWindows.Add(neWindow);
			if (AddNEWindows != null)
				AddNEWindows.Remove(neWindow);
		}

		public void SetClipboard(NEClipboard clipboard) => Clipboard = clipboard;

		public void SetKeysAndValues(IReadOnlyList<KeysAndValues>[] keysAndValues) => KeysAndValues = keysAndValues;

		public void SetDragFiles(IReadOnlyList<string> dragFiles) => DragFiles = dragFiles;
	}
}
