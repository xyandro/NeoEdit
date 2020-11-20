using System;
using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFileResult
	{
		public KeysAndValues[] KeysAndValues { get; private set; }
		public List<NEFile> Files { get; private set; }
		public List<NEFile> NewFiles { get; private set; }
		public Tuple<IReadOnlyList<string>, bool?> Clipboard { get; private set; }
		public List<string> DragFiles { get; private set; }

		public void SetKeysAndValues(int kvIndex, KeysAndValues keysAndValues)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (KeysAndValues == null)
				KeysAndValues = new KeysAndValues[10];
			KeysAndValues[kvIndex] = keysAndValues;
		}

		public void ClearNEFiles()
		{
			if (Files == null)
				Files = new List<NEFile>();
			Files.Clear();
		}

		public void AddNEFile(NEFile neFile)
		{
			if (Files == null)
				Files = new List<NEFile>();
			Files.Add(neFile);
		}

		public void AddNewNEFile(NEFile neFile)
		{
			if (NewFiles == null)
				NewFiles = new List<NEFile>();
			NewFiles.Add(neFile);
		}

		public void SetClipboard(Tuple<IReadOnlyList<string>, bool?> clipboard) => Clipboard = clipboard;

		internal void AddDragFile(string fileName)
		{
			if (DragFiles == null)
				DragFiles = new List<string>();
			DragFiles.Add(fileName);
		}
	}
}
