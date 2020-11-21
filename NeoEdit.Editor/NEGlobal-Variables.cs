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

		public void SetData(NEGlobalData data)
		{
			ClearResult();
			Data = data;
			NEWindowDatas.ForEach(neWindowData => neWindowData.neWindow.SetData(neWindowData));
			UpdateAttachments();
		}

		IEnumerable<INEWindow> INEGlobal.NEWindows => Data.neWindows;

		public IReadOnlyOrderedHashSet<NEWindow> NEWindows => Data.neWindows;

		public IReadOnlyOrderedHashSet<NEWindowData> NEWindowDatas
		{
			get => Data.neWindowDatas;
			private set
			{
				EditableData.neWindowDatas = value;
				EditableData.neWindows = new OrderedHashSet<NEWindow>(value.Select(neWindowData => neWindowData.neWindow));
			}
		}

		public void AddNewNEWindow(NEWindow neWindow) => CreateResult().AddNewNEWindow(neWindow);

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

			var nextNEWindowDatas = new OrderedHashSet<NEWindowData>();
			foreach (var neWindow in NEWindows)
			{
				var result = neWindow.GetResult();
				if (result == null)
				{
					nextNEWindowDatas.Add(neWindow.Data);
					continue;
				}

				result.NEWindows.ForEach(x => nextNEWindowDatas.Add(x.Data));

				if (result.Clipboard != null)
					CreateResult().SetClipboard(result.Clipboard);

				if (result.KeysAndValues != null)
					CreateResult().SetKeysAndValues(result.KeysAndValues);

				if (result.DragFiles != null)
					CreateResult().SetDragFiles(result.DragFiles);
			}

			if (result?.NewNEWindows != null)
			{
				result.NewNEWindows.ForEach(neWindow => neWindow.GetResult());
				result.NewNEWindows.ForEach(neWindow => nextNEWindowDatas.Add(neWindow.Data));
			}

			if (!NEWindowDatas.Matches(nextNEWindowDatas))
				NEWindowDatas = nextNEWindowDatas;

			var ret = result;
			ClearResult();
			return ret;
		}

		void ClearResult() => result = null;

		NEGlobalData oldAttachments = new NEGlobalData();
		void UpdateAttachments()
		{
			if (oldAttachments == Data)
				return;

			var oldAttachmentsMap = oldAttachments.neWindowDatas.ToDictionary(neWindowData => neWindowData.neWindow);

			// Closed windows: detach all files, then the window
			oldAttachments.neWindows.Except(Data.neWindows).ForEach(neWindow =>
			{
				oldAttachmentsMap[neWindow].neFiles.ForEach(neFile => neFile.Detach());
				neWindow.Detach();
			});

			// Existing windows: detach removed files
			oldAttachments.neWindows.Intersect(Data.neWindows).ForEach(neWindow => oldAttachmentsMap[neWindow].neFiles.Except(neWindow.NEFiles).ForEach(neFile => neFile.Detach()));

			// Existing windows: attach new files
			oldAttachments.neWindows.Intersect(Data.neWindows).ForEach(neWindow => neWindow.NEFiles.Except(oldAttachmentsMap[neWindow].neFiles).ForEach(neFile => neFile.Attach(neWindow)));

			// New windows: attach the window, then all files
			Data.neWindows.Except(oldAttachments.neWindows).ForEach(neWindow =>
			{
				neWindow.Attach(this);
				neWindow.NEFiles.ForEach(neFile => neFile.Attach(neWindow));
			});

			oldAttachments = Data;
		}
	}
}
