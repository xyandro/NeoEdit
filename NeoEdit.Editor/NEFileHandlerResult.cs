using System;
using System.Collections.Generic;

namespace NeoEdit.Editor
{
	public class NEFileHandlerResult
	{
		public KeysAndValues[] KeysAndValues { get; private set; }
		public List<(NEFileHandler neFile, int? index)> FilesToAdd { get; private set; }
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

		public void AddNewFile((NEFileHandler neFile, int? index) data)
		{
			if (FilesToAdd == null)
				FilesToAdd = new List<(NEFileHandler neFile, int? index)>();
			FilesToAdd.Add(data);
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
