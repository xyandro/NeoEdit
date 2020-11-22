using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		public NEFileData Data { get; private set; }
		NEFileData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
				{
					CreateResult();
					Data = new NEFileData(Data);
				}
				return Data;
			}
		}

		public void SetData(NEFileData data)
		{
			ClearResult();
			Data = data;
			Text.MoveToTextPoint(NETextPoint);
			EnsureVisible();
			SetModifiedFlag();
		}

		NETextPoint NETextPoint { get => Data.neTextPoint; set => EditableData.neTextPoint = value; }

		public NEFile DiffTarget
		{
			get => Data.diffTarget;
			set
			{
				if (DiffTarget != null)
				{
					Text.ClearDiff();
					DiffTarget.Text.ClearDiff();
					DiffTarget.EditableData.diffTarget = null;
					EditableData.diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					EditableData.diffTarget = value;
					value.EditableData.diffTarget = this;
					CalculateDiff();
				}
			}
		}

		public IReadOnlyList<Range> Selections
		{
			get => Data.selections;
			set
			{
				EditableData.selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<Range> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return Data.regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<Range> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			EditableData.regions[region - 1] = DeOverlap(regions);
			Data.regions[region - 1] = DeOverlap(regions);
		}

		void ClearNEFiles() => CreateResult().ClearNEFiles();
		void AddNEFile(NEFile neFile) => CreateResult().AddNEFile(neFile);
		void AddNewNEFile(NEFile neFile) => CreateResult().AddNewNEFile(neFile);

		Tuple<IReadOnlyList<string>, bool?> clipboardData;
		Tuple<IReadOnlyList<string>, bool?> ClipboardData
		{
			get
			{
				if (clipboardData == null)
					clipboardData = state.GetClipboardData(this);

				return clipboardData;
			}

			set
			{
				clipboardData = value;
				CreateResult().SetClipboard(value);
			}
		}

		IReadOnlyList<string> Clipboard { get => ClipboardData.Item1; set => ClipboardData = Tuple.Create(value, default(bool?)); }
		IReadOnlyList<string> ClipboardCopy { set => ClipboardData = Tuple.Create(value, (bool?)false); }
		IReadOnlyList<string> ClipboardCut { set => ClipboardData = Tuple.Create(value, (bool?)true); }

		readonly KeysAndValues[] keysAndValues = new KeysAndValues[10];
		KeysAndValues GetKeysAndValues(int kvIndex)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			if (keysAndValues[kvIndex] == null)
				keysAndValues[kvIndex] = state.GetKeysAndValues(kvIndex, this);

			return keysAndValues[kvIndex];
		}

		void SetKeysAndValues(int kvIndex, IReadOnlyList<string> values, bool matchCase = false)
		{
			if ((kvIndex < 0) || (kvIndex > 9))
				throw new IndexOutOfRangeException($"Invalid kvIndex: {kvIndex}");

			var newKeysAndValues = new KeysAndValues(values, kvIndex == 0, matchCase);
			keysAndValues[kvIndex] = newKeysAndValues;
			CreateResult().SetKeysAndValues(kvIndex, newKeysAndValues);
		}

		void AddDragFile(string fileName) => CreateResult().AddDragFile(fileName);

		NEFileResult result;
		NEFileResult CreateResult()
		{
			if (result == null)
			{
				NEWindow?.CreateResult();
				result = new NEFileResult(this);
			}
			return result;
		}

		public NEFileResult GetResult()
		{
			if (result == null)
				return null;

			var ret = result;
			ClearResult();
			return ret;
		}

		void ClearResult()
		{
			clipboardData = null;
			for (var kvIndex = 0; kvIndex < 10; ++kvIndex)
				keysAndValues[kvIndex] = null;
			result = null;
		}

		public NEWindow NEWindow { get; private set; }
		public void Attach(NEWindow neWindow)
		{
			if (NEWindow != null)
				throw new Exception("File already attached");
			if (result != null)
				throw new Exception("Can't attach, file being modified");

			NEWindow = neWindow;
			SetAutoRefresh();
		}

		public void Detach()
		{
			if (NEWindow == null)
				throw new Exception("File not attached");
			if (result != null)
				throw new Exception("Can't detach, file being modified");

			NEWindow = null;
			SetAutoRefresh();
		}
	}
}
