using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public static class NEGlobal
	{
		static NEGlobal()
		{
			data = new NEGlobalData();
			SetNEWindowDatas(new OrderedHashSet<NEWindowData>());
		}

		public static NEGlobalData data { get; private set; }
		static NEGlobalData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public static void ResetData(NEGlobalData data)
		{
			result = null;
			NEGlobal.data = data;
			SetNEWindowDatas(NEWindowDatas); // Will regenerate NEWindows
			NEWindowDatas.ForEach(neWindowData => neWindowData.neWindow.ResetData(neWindowData));
		}

		public static IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; private set; }

		public static IReadOnlyOrderedHashSet<NEWindowData> NEWindowDatas
		{
			get => data.neWindowDatas;
			set
			{
				editableData.neWindowDatas = value;
				NEWindows = new OrderedHashSet<NEWindow>(value.Select(neWindowData => neWindowData.neWindow));
			}
		}

		public static void SetNEWindowDatas(IEnumerable<NEWindowData> neWindowDatas) => NEWindowDatas = new OrderedHashSet<NEWindowData>(neWindowDatas);

		public static NEGlobalResult GetResult()
		{
			foreach (var neWindow in NEWindows)
			{
				var result = neWindow.GetResult();
				if (result == null)
					continue;

				if (result.Clipboard != null)
					CreateResult().SetClipboard(result.Clipboard);

				if (result.KeysAndValues != null)
					CreateResult().SetKeysAndValues(result.KeysAndValues);

				if (result.DragFiles != null)
					CreateResult().SetDragFiles(result.DragFiles);
			}

			var nextNEWindowDatas = NEWindows.Select(x => x.data).ToList();
			if (!NEWindowDatas.Matches(nextNEWindowDatas))
				SetNEWindowDatas(nextNEWindowDatas);

			var ret = result;
			result = null;
			return ret;
		}

		static NEGlobalResult result;
		static NEGlobalResult CreateResult()
		{
			if (result == null)
				result = new NEGlobalResult();
			return result;
		}

		public static void AddNewFiles(NEWindow neWindow) => SetNEWindowDatas(NEWindowDatas.Concat(neWindow.data));

		public static void RemoveFiles(NEWindow neWindow) => SetNEWindowDatas(NEWindowDatas.Except(neWindow.data));
	}
}
