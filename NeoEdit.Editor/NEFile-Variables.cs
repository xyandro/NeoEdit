using System;
using System.Collections.Generic;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEFile
	{
		public INEFileData Data { get; private set; }
		NEFileData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
				{
					CreateResult();
					Data = new NEFileData(Data);
				}
				return Data as NEFileData;
			}
		}

		public void SetData(INEFileData data)
		{
			ClearResult();
			Data = data;
			Text.MoveToTextPoint(NETextPoint);
			EnsureVisible();
			SetModifiedFlag();
		}

		void SetData(int serial)
		{
			var data = Data;
			while ((data.Undo != null) && (data.NESerial > serial))
				data = data.Undo;
			while ((data.Redo != null) && (data.Redo.NESerial <= serial))
				data = data.Redo;
			if (data != Data)
				SetData(data);
		}

		NETextPoint NETextPoint { get => Data.NETextPoint; set => EditableData.NETextPoint = value; }

		public IReadOnlyList<NERange> Selections
		{
			get => Data.Selections;
			set
			{
				if (AllowOverlappingSelections)
					EditableData.Selections = Sort(value);
				else
					EditableData.Selections = DeOverlap(value);
				CurrentSelection = CurrentSelection;
				EnsureVisible();
			}
		}

		public IReadOnlyList<NERange> GetRegions(int region)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			return Data.Regions[region - 1];
		}

		void SetRegions(int region, IReadOnlyList<NERange> regions)
		{
			if ((region < 1) || (region > 9))
				throw new IndexOutOfRangeException($"Invalid region: {region}");
			EditableData.Regions[region - 1] = DeOverlap(regions);
			Data.Regions[region - 1] = DeOverlap(regions);
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
