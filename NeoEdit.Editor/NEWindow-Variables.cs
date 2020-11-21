using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		public NEWindowData Data { get; private set; }
		NEWindowData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
				{
					CreateResult();
					Data = new NEWindowData(Data);
				}
				return Data;
			}
		}

		public void SetData(NEWindowData data)
		{
			ClearResult();
			Data = data;
			NEFileDatas.ForEach(neFileData => neFileData.neFile.SetData(neFileData));
		}

		public NEFile Focused { get => Data.focused; set => EditableData.focused = value; }
		public WindowLayout WindowLayout { get => Data.windowLayout; set => EditableData.windowLayout = value; }
		public bool ActiveOnly { get => Data.activeOnly; set => EditableData.activeOnly = value; }
		public bool MacroVisualize { get => Data.macroVisualize; set => EditableData.macroVisualize = value; }

		public IReadOnlyOrderedHashSet<NEFile> NEFiles => Data.neFiles;

		public IReadOnlyOrderedHashSet<NEFileData> NEFileDatas
		{
			get => Data.neFileDatas;
			private set
			{
				EditableData.neFileDatas = value;
				EditableData.neFiles = new OrderedHashSet<NEFile>(value.Select(neFileData => neFileData.neFile));
			}
		}

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();
		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };
		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		public IReadOnlyOrderedHashSet<NEFile> ActiveFiles
		{
			get => Data.activeFiles;
			set
			{
				EditableData.activeFiles = value;
				if (!ActiveFiles.Contains(Focused))
					Focused = ActiveFiles.OrderByDescending(neFile => neFile.LastActive).FirstOrDefault();
			}
		}

		public void ClearNEWindows() => CreateResult().ClearNEWindows();
		public void AddNewNEFile(NEFile neFile) => CreateResult().AddNewNEFile(neFile);

		NEWindowResult result;
		public NEWindowResult CreateResult()
		{
			if (result == null)
			{
				NEGlobal?.CreateResult();
				result = new NEWindowResult(this);
			}
			return result;
		}

		public NEWindowResult GetResult()
		{
			if (result == null)
				return null;

			NEClipboard setClipboard = null;
			List<KeysAndValues>[] keysAndValues = null;
			List<string> dragFiles = null;

			var nextNEFileDatas = new OrderedHashSet<NEFileData>();
			var newFileDatas = new List<NEFileData>();
			foreach (var neFile in NEFiles)
			{
				var result = neFile.GetResult();
				if (result == null)
				{
					nextNEFileDatas.Add(neFile.Data);
					continue;
				}

				result.NEFiles.ForEach(x => nextNEFileDatas.Add(x.Data));

				if (result.NewNEFiles != null)
					newFileDatas.AddRange(result.NewNEFiles.Select(x => x.Data));

				if (result.Clipboard != null)
				{
					if (setClipboard == null)
						setClipboard = new NEClipboard();
					setClipboard.Add(result.Clipboard.Item1);
					setClipboard.IsCut = result.Clipboard.Item2;
				}

				if (result.KeysAndValues != null)
					for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
						if (result.KeysAndValues[kvIndex] != null)
						{
							if (keysAndValues == null)
								keysAndValues = new List<KeysAndValues>[10];
							if (keysAndValues[kvIndex] == null)
								keysAndValues[kvIndex] = new List<KeysAndValues>();
							keysAndValues[kvIndex].Add(result.KeysAndValues[kvIndex]);
						}

				if (result.DragFiles != null)
				{
					if (dragFiles == null)
						dragFiles = new List<string>();
					dragFiles.AddRange(result.DragFiles);
				}
			}

			newFileDatas.ForEach(nextNEFileDatas.Add);

			if (result?.NewNEFiles != null)
			{
				result.NewNEFiles.ForEach(neFile => neFile.GetResult());
				result.NewNEFiles.ForEach(neFile => nextNEFileDatas.Add(neFile.Data));
			}

			if (!NEFileDatas.Matches(nextNEFileDatas))
			{
				var oldNEFiles = NEFiles;

				NEFileDatas = nextNEFileDatas;

				var nextActiveFiles = NEFiles.Except(oldNEFiles).ToList(); // New files
				if (!nextActiveFiles.Any())
					nextActiveFiles = NEFiles.Intersect(ActiveFiles).ToList(); // Currently active files
				if ((!nextActiveFiles.Any()) && (NEFiles.Any()))
				{
					// All files with max LastActive
					var maxLastActive = NEFiles.Max(neFile => neFile.LastActive);
					nextActiveFiles = NEFiles.Where(neFile => neFile.LastActive == maxLastActive).ToList();
				}

				SetActiveFiles(nextActiveFiles);

				var now = DateTime.Now;
				ActiveFiles.ForEach(neFile => neFile.LastActive = now);
			}

			if (setClipboard != null)
				result.SetClipboard(setClipboard);

			if (keysAndValues != null)
				result.SetKeysAndValues(keysAndValues);

			if (dragFiles != null)
				result.SetDragFiles(dragFiles);

			var ret = result;
			ClearResult();
			return ret;
		}

		void ClearResult() => result = null;

		public NEGlobal NEGlobal { get; private set; }
		public void Attach(NEGlobal neGlobal)
		{
			if (NEGlobal != null)
				throw new Exception("Window already attached");
			if (result != null)
				throw new Exception("Can't attach, window being modified");

			NEGlobal = neGlobal;
			neWindowUI = INEWindowUIStatic.CreateNEWindowUI(this);
		}

		public void Detach()
		{
			if (NEGlobal == null)
				throw new Exception("Window not attached");
			if (result != null)
				throw new Exception("Can't detach, window being modified");

			neWindowUI.CloseWindow();
			neWindowUI = null;
			NEGlobal = null;
		}
	}
}
