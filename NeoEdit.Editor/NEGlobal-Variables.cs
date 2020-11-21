using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		public NEGlobalData Data { get; private set; }
		NEGlobalData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
					Data = new NEGlobalData(Data);
				return Data;
			}
		}

		public void ResetData(NEGlobalData data)
		{
			var oldNEWindows = NEWindows;

			ResetResult();
			Data = data;
			RecreateNEWindows();
			NEWindows.Except(oldNEWindows).ForEach(neWindow => neWindow.Attach(this));
			oldNEWindows.Except(NEWindows).ForEach(neWindow => neWindow.Detach());
			NEWindowDatas.ForEach(neWindowData => neWindowData.neWindow.ResetData(neWindowData));
		}

		IEnumerable<INEWindow> INEGlobal.NEWindows => NEWindows;

		public IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; private set; }

		public void SetNEWindowDatas(IEnumerable<NEWindowData> neWindowDatas) => NEWindowDatas = new OrderedHashSet<NEWindowData>(neWindowDatas);

		public IReadOnlyOrderedHashSet<NEWindowData> NEWindowDatas
		{
			get => Data.neWindowDatas;
			private set
			{
				EditableData.neWindowDatas = value;
				RecreateNEWindows();
			}
		}

		void RecreateNEWindows() => NEWindows = new OrderedHashSet<NEWindow>(Data.neWindowDatas.Select(neWindowData => neWindowData.neWindow));

		public void AddNEWindow(NEWindow neWindow) => CreateResult().AddNEWindow(neWindow);
		public void RemoveNEWindow(NEWindow neWindow) => CreateResult().RemoveNEWindow(neWindow);

		NEGlobalResult result;
		public NEGlobalResult CreateResult()
		{
			if (result == null)
				result = new NEGlobalResult();
			return result;
		}

		public NEGlobalResult GetResult()
		{
			if (result == null)
				return null;

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

			IEnumerable<NEWindow> nextNEWindowsItr = NEWindows;
			if (result != null)
			{
				if (result.RemoveNEWindows != null)
					nextNEWindowsItr = nextNEWindowsItr.Except(result.RemoveNEWindows);
				if (result.AddNEWindows != null)
					nextNEWindowsItr = nextNEWindowsItr.Concat(result.AddNEWindows);
			}
			var nextNEWindowDatas = nextNEWindowsItr.Select(x => x.Data).ToList();

			if (!NEWindowDatas.Matches(nextNEWindowDatas))
			{
				var oldNEWindows = NEWindows;
				SetNEWindowDatas(nextNEWindowDatas);
				NEWindows.Except(oldNEWindows).ForEach(neWindow => neWindow.Attach(this));
				oldNEWindows.Except(NEWindows).ForEach(neWindow => neWindow.Detach());
			}

			var ret = result;
			ResetResult();
			return ret;
		}

		void ResetResult()
		{
			result = null;
		}
	}
}
