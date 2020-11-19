using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		public NEWindowData data { get; private set; }
		NEWindowData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public void ResetData(NEWindowData data)
		{
			result = null;
			this.data = data;
			SetAllFileDatas(AllFileDatas); // Will regenerate AllFiles
			AllFileDatas.ForEach(neFileData => neFileData.neFile.ResetData(neFileData));
		}

		public IReadOnlyOrderedHashSet<NEFile> AllFiles { get; private set; }

		public IReadOnlyOrderedHashSet<NEFileData> AllFileDatas
		{
			get => data.allFileDatas;
			set
			{
				editableData.allFileDatas = value;
				AllFiles = new OrderedHashSet<NEFile>(value.Select(neFile => neFile.neFile));
			}
		}

		public void SetAllFileDatas(IEnumerable<NEFileData> neFileDatas) => AllFileDatas = new OrderedHashSet<NEFileData>(neFileDatas);

		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get => data.activeFiles;
			set
			{
				editableData.activeFiles = value;
				if (!data.activeFiles.Contains(Focused))
					Focused = data.activeFiles.FirstOrDefault();
			}
		}

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();

		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };

		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		public NEFile Focused
		{
			get => data.focused;
			set => editableData.focused = value;
		}

		public WindowLayout WindowLayout
		{
			get => data.windowLayout;
			set => editableData.windowLayout = value;
		}

		public bool ActiveOnly
		{
			get => data.activeOnly;
			set => editableData.activeOnly = value;
		}

		public bool MacroVisualize
		{
			get => data.macroVisualize;
			set => editableData.macroVisualize = value;
		}

		NEWindowResult result;
		NEWindowResult CreateResult()
		{
			if (result == null)
				result = new NEWindowResult();
			return result;
		}

		public void AddNewFile(NEFile neFile) => CreateResult().AddNewFile(neFile);

		public NEWindowResult GetResult()
		{
			NEClipboard setClipboard = null;
			List<KeysAndValues>[] setKeysAndValues = null;
			List<string> dragFiles = null;

			var nextAllFileDatas = new List<NEFileData>();
			var newFileDatas = new List<NEFileData>();
			foreach (var neFile in AllFiles)
			{
				var result = neFile.GetResult();
				if (result == null)
				{
					nextAllFileDatas.Add(neFile.data);
					continue;
				}

				if (result.Files == null)
					nextAllFileDatas.Add(neFile.data);
				else
					nextAllFileDatas.AddRange(result.Files.ForEach(x => x.data));

				if (result.NewFiles != null)
					newFileDatas.AddRange(result.NewFiles.Select(x => x.data));

				if (result.Clipboard != null)
				{
					if (setClipboard == null)
						setClipboard = new NEClipboard();
					setClipboard.Add(result.Clipboard.Item1);
					setClipboard.IsCut = result.Clipboard.Item2;
				}

				if (result.KeysAndValues != null)
					for (var kvIndex = 0; kvIndex < KeysAndValuesCount; ++kvIndex)
						if (result.KeysAndValues[kvIndex] != null)
						{
							if (setKeysAndValues == null)
								setKeysAndValues = new List<KeysAndValues>[KeysAndValuesCount];
							if (setKeysAndValues[kvIndex] == null)
								setKeysAndValues[kvIndex] = new List<KeysAndValues>();
							setKeysAndValues[kvIndex].Add(result.KeysAndValues[kvIndex]);
						}

				if (result.DragFiles != null)
				{
					if (dragFiles == null)
						dragFiles = new List<string>();
					dragFiles.AddRange(result.DragFiles);
				}
			}

			nextAllFileDatas.AddRange(newFileDatas);

			if (result != null)
			{
				if (result.NewFiles != null)
					nextAllFileDatas.AddRange(result.NewFiles.ForEach(x => x.data));
			}

			if (!AllFileDatas.Matches(nextAllFileDatas))
			{
				var newlyAdded = nextAllFileDatas.Select(x => x.neFile).Except(AllFiles).ToList();

				SetAllFileDatas(nextAllFileDatas);

				if (newlyAdded.Any())
					SetActiveFiles(newlyAdded);
				else
				{
					SetActiveFiles(AllFiles.Intersect(ActiveFiles));
					if (!ActiveFiles.Any())
					{
						var newActive = AllFiles.OrderByDescending(file => file.LastActive).FirstOrDefault();
						if (newActive != null)
							SetActiveFile(newActive);
					}
				}

				var now = DateTime.Now;
				ActiveFiles.ForEach(neFile => neFile.LastActive = now);
			}

			if (setClipboard != null)
				CreateResult().SetClipboard(setClipboard);

			if (setKeysAndValues != null)
				CreateResult().SetKeysAndValues(setKeysAndValues);

			if (dragFiles != null)
				CreateResult().SetDragFiles(dragFiles);

			var ret = result;
			result = null;
			return ret;
		}
	}
}
