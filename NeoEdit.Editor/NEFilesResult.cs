using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEFilesResult
	{
		public List<NEFile> NewFiles { get; private set; }
		public NEClipboard Clipboard { get; private set; }
		public IReadOnlyList<KeysAndValues>[] KeysAndValues { get; private set; }
		public IReadOnlyList<string> DragFiles { get; private set; }

		public void AddNewFile(NEFile neFile)
		{
			if (NewFiles == null)
				NewFiles = new List<NEFile>();
			NewFiles.Add(neFile);
		}

		public void SetClipboard(NEClipboard clipboard) => Clipboard = clipboard;

		public void SetKeysAndValues(IReadOnlyList<KeysAndValues>[] keysAndValues) => KeysAndValues = keysAndValues;

		public void SetDragFiles(IReadOnlyList<string> dragFiles) => DragFiles = dragFiles;
	}
}
