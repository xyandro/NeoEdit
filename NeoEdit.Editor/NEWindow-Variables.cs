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
					Data = Data.Clone();
				return Data;
			}
		}

		public void ResetData(NEWindowData data)
		{
			var oldNEFiles = NEFiles;

			ResetResult();
			Data = data;
			RecreateNEFiles();
			NEFiles.Except(oldNEFiles).ForEach(neFile => neFile.Attach(this));
			oldNEFiles.Except(NEFiles).ForEach(neFile => neFile.Detach());
			NEFileDatas.ForEach(neFileData => neFileData.neFile.ResetData(neFileData));
		}

		public NEFile Focused { get => Data.focused; set => EditableData.focused = value; }
		public WindowLayout WindowLayout { get => Data.windowLayout; set => EditableData.windowLayout = value; }
		public bool ActiveOnly { get => Data.activeOnly; set => EditableData.activeOnly = value; }
		public bool MacroVisualize { get => Data.macroVisualize; set => EditableData.macroVisualize = value; }

		public IReadOnlyOrderedHashSet<NEFile> NEFiles { get; private set; }

		public void SetNEFileDatas(IEnumerable<NEFileData> neFileDatas) => NEFileDatas = new OrderedHashSet<NEFileData>(neFileDatas);

		public IReadOnlyOrderedHashSet<NEFileData> NEFileDatas
		{
			get => Data.neFileDatas;
			set
			{
				EditableData.neFileDatas = value;
				RecreateNEFiles();
			}
		}

		void RecreateNEFiles() => NEFiles = new OrderedHashSet<NEFile>(NEFileDatas.Select(neFile => neFile.neFile));

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

		public void ClearActiveFiles() => ActiveFiles = new OrderedHashSet<NEFile>();
		public void SetActiveFile(NEFile file) => ActiveFiles = new OrderedHashSet<NEFile> { file };
		public void SetActiveFiles(IEnumerable<NEFile> files) => ActiveFiles = new OrderedHashSet<NEFile>(files);

		public void AddNewNEFile(NEFile neFile) => CreateResult().AddNewNEFile(neFile);

		public NEGlobal NEGlobal { get; private set; }
		public void Attach(NEGlobal neGlobal)
		{
			if (NEGlobal != null)
				throw new Exception("Window already attached");
			NEGlobal = neGlobal;
			neWindowUI = INEWindowUIStatic.CreateNEWindowUI(this);
		}

		public void Detach()
		{
			if (NEGlobal == null)
				throw new Exception("Window not attached");
			NEGlobal = null;
			neWindowUI.CloseWindow();
			neWindowUI = null;
		}

		NEWindowResult result;
		public NEWindowResult CreateResult()
		{
			if (result == null)
			{
				NEGlobal.CreateResult();
				result = new NEWindowResult();
			}
			return result;
		}

		void ResetResult() => result = null;

		public NEWindowResult GetResult()
		{
			if (result == null)
				return null;

			NEClipboard setClipboard = null;
			List<KeysAndValues>[] keysAndValues = null;
			List<string> dragFiles = null;

			var nextNEFileDatas = new List<NEFileData>();
			var newFileDatas = new List<NEFileData>();
			foreach (var neFile in NEFiles)
			{
				var result = neFile.GetResult();
				if (result == null)
				{
					nextNEFileDatas.Add(neFile.Data);
					continue;
				}

				nextNEFileDatas.AddRange(result.NEFiles.ForEach(x => x.Data));

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

			nextNEFileDatas.AddRange(newFileDatas);

			if (result?.NewNEFiles != null)
				nextNEFileDatas.AddRange(result.NewNEFiles.ForEach(x => x.Data));

			if (!NEFileDatas.Matches(nextNEFileDatas))
			{
				var oldNEFiles = NEFiles;

				SetNEFileDatas(nextNEFileDatas);
				NEFiles.Except(oldNEFiles).ForEach(neFile => neFile.Attach(this));
				oldNEFiles.Except(NEFiles).ForEach(neFile => neFile.Detach());

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
			ResetResult();
			return ret;
		}
	}
}
