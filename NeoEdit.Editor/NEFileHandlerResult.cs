using System;
using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFileHandlerResult
	{
		public KeysAndValues[] KeysAndValues { get; private set; }
		public List<NEFileHandler> Files { get; private set; }
		public List<NEFileHandler> NewFiles { get; private set; }
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

		public void ClearFiles()
		{
			if (Files == null)
				Files = new List<NEFileHandler>();
			Files.Clear();
		}

		public void AddFile(NEFileHandler neFile)
		{
			if (Files == null)
				Files = new List<NEFileHandler>();
			Files.Add(neFile);
		}

		public void AddNewFile(NEFileHandler neFile)
		{
			if (NewFiles == null)
				NewFiles = new List<NEFileHandler>();
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
