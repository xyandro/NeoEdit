using System;
using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFileResult
	{
		public List<NEFile> NEFiles { get; private set; }
		public List<NEFile> NewNEFiles { get; private set; }
		public Tuple<IReadOnlyList<string>, bool?> Clipboard { get; private set; }
		public KeysAndValues[] KeysAndValues { get; private set; }
		public List<string> DragFiles { get; private set; }

		public NEFileResult(NEFile neFile) => NEFiles = new List<NEFile> { neFile };

		public void ClearNEFiles() => NEFiles.Clear();

		public void AddNEFile(NEFile neFile) => NEFiles.Add(neFile);

		public void AddNewNEFile(NEFile neFile)
		{
			if (NewNEFiles == null)
				NewNEFiles = new List<NEFile>();
			NewNEFiles.Add(neFile);
		}

		public void SetClipboard(Tuple<IReadOnlyList<string>, bool?> clipboard) => Clipboard = clipboard;

		public void SetKeysAndValues(int kvIndex, KeysAndValues keysAndValues)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (KeysAndValues == null)
				KeysAndValues = new KeysAndValues[10];
			KeysAndValues[kvIndex] = keysAndValues;
		}

		public void AddDragFile(string fileName)
		{
			if (DragFiles == null)
				DragFiles = new List<string>();
			DragFiles.Add(fileName);
		}
	}
}
