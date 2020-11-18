using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public static class NEAllFiles
	{
		static NEAllFiles()
		{
			data = new NEAllFilesData();
			SetAllNEFilesDatas(new OrderedHashSet<NEFilesData>());
		}

		public static NEAllFilesData data { get; private set; }
		static NEAllFilesData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public static void ResetData(NEAllFilesData data)
		{
			result = null;
			NEAllFiles.data = data;
			SetAllNEFilesDatas(AllNEFilesDatas); // Will regenerate AllNEFiles
			AllNEFilesDatas.ForEach(neFilesData => neFilesData.neFiles.ResetData(neFilesData));
		}

		public static IReadOnlyOrderedHashSet<NEFiles> AllNEFiles { get; private set; }

		public static IReadOnlyOrderedHashSet<NEFilesData> AllNEFilesDatas
		{
			get => data.allNEFilesData;
			set
			{
				editableData.allNEFilesData = value;
				AllNEFiles = new OrderedHashSet<NEFiles>(value.Select(neFilesData => neFilesData.neFiles));
			}
		}

		public static void SetAllNEFilesDatas(IEnumerable<NEFilesData> allNEFilesDatas) => AllNEFilesDatas = new OrderedHashSet<NEFilesData>(allNEFilesDatas);

		public static NEAllFilesResult GetResult()
		{
			foreach (var neFiles in AllNEFiles)
			{
				var result = neFiles.GetResult();
				if (result == null)
					continue;

				if (result.Clipboard != null)
					CreateResult().SetClipboard(result.Clipboard);

				if (result.KeysAndValues != null)
					CreateResult().SetKeysAndValues(result.KeysAndValues);

				if (result.DragFiles != null)
					CreateResult().SetDragFiles(result.DragFiles);
			}

			var nextAllNEFilesDatas = AllNEFiles.Select(x => x.data).ToList();
			if (!AllNEFilesDatas.Matches(nextAllNEFilesDatas))
				SetAllNEFilesDatas(nextAllNEFilesDatas);

			var ret = result;
			result = null;
			return ret;
		}

		static NEAllFilesResult result;
		static NEAllFilesResult CreateResult()
		{
			if (result == null)
				result = new NEAllFilesResult();
			return result;
		}

		public static void AddNewFiles(NEFiles neFiles) => SetAllNEFilesDatas(AllNEFilesDatas.Concat(neFiles.data));

		public static void RemoveFiles(NEFiles neFiles) => SetAllNEFilesDatas(AllNEFilesDatas.Except(neFiles.data));
	}
}
