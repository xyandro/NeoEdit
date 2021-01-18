using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		public INEGlobalData Data { get; private set; }
		NEGlobalData EditableData
		{
			get
			{
				if (Data.NESerial != NESerialTracker.NESerial)
					Data = new NEGlobalData(Data);
				return Data as NEGlobalData;
			}
		}

		public void SetData(INEGlobalData data)
		{
			if (Data == data)
				return;

			Data = data;
			NEWindowDatas.ForEach(neWindowData => neWindowData.NEWindow.SetData(neWindowData));
			UpdateAttachments();
			ClearResult();
		}

		IEnumerable<INEWindow> INEGlobal.NEWindows => Data.NEWindows;

		public IReadOnlyOrderedHashSet<NEWindow> NEWindows => Data.NEWindows;

		public IReadOnlyOrderedHashSet<INEWindowData> NEWindowDatas
		{
			get => Data.NEWindowDatas;
			private set
			{
				EditableData.NEWindowDatas = value;
				EditableData.NEWindows = new OrderedHashSet<NEWindow>(value.Select(neWindowData => neWindowData.NEWindow));
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

			var nextNEWindowDatas = new OrderedHashSet<INEWindowData>();
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

		INEGlobalData oldAttachments = new NEGlobalData();
		void UpdateAttachments()
		{
			if (oldAttachments == Data)
				return;

			var oldAttachmentsMap = oldAttachments.NEWindowDatas.ToDictionary(neWindowData => neWindowData.NEWindow);

			// Closed windows: detach all files, then the window
			oldAttachments.NEWindows.Except(Data.NEWindows).ForEach(neWindow =>
			{
				oldAttachmentsMap[neWindow].NEFiles.ForEach(neFile => neFile.Detach());
				neWindow.Detach();
			});

			// Existing windows: detach removed files
			oldAttachments.NEWindows.Intersect(Data.NEWindows).ForEach(neWindow => oldAttachmentsMap[neWindow].NEFiles.Except(neWindow.NEFiles).ForEach(neFile => neFile.Detach()));

			// Existing windows: attach new files
			oldAttachments.NEWindows.Intersect(Data.NEWindows).ForEach(neWindow => neWindow.NEFiles.Except(oldAttachmentsMap[neWindow].NEFiles).ForEach(neFile => neFile.Attach(neWindow)));

			// New windows: attach the window, then all files
			Data.NEWindows.Except(oldAttachments.NEWindows).ForEach(neWindow =>
			{
				neWindow.Attach(this);
				neWindow.NEFiles.ForEach(neFile => neFile.Attach(neWindow));
			});

			oldAttachments = Data;
		}
	}
}
