using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobal : INEGlobal
	{
		static EditorExecuteState state => EditorExecuteState.CurrentState;

		public NEGlobal()
		{
			data = new NEGlobalData();
			SetNEWindowDatas(new OrderedHashSet<NEWindowData>());
		}

		public NEGlobalData data { get; private set; }
		NEGlobalData editableData
		{
			get
			{
				if (data.NESerial != NESerialTracker.NESerial)
					data = data.Clone();
				return data;
			}
		}

		public void ResetData(NEGlobalData data)
		{
			result = null;
			this.data = data;
			SetNEWindowDatas(NEWindowDatas); // Will regenerate NEWindows
			NEWindowDatas.ForEach(neWindowData => neWindowData.neWindow.ResetData(neWindowData));
		}

		IEnumerable<INEWindow> INEGlobal.NEWindows => NEWindows;

		public IReadOnlyOrderedHashSet<NEWindow> NEWindows { get; private set; }

		public IReadOnlyOrderedHashSet<NEWindowData> NEWindowDatas
		{
			get => data.neWindowDatas;
			set
			{
				editableData.neWindowDatas = value;
				NEWindows = new OrderedHashSet<NEWindow>(value.Select(neWindowData => neWindowData.neWindow));
			}
		}

		public void SetNEWindowDatas(IEnumerable<NEWindowData> neWindowDatas) => NEWindowDatas = new OrderedHashSet<NEWindowData>(neWindowDatas);

		public NEGlobalResult GetResult()
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

		NEGlobalResult result;
		NEGlobalResult CreateResult()
		{
			if (result == null)
				result = new NEGlobalResult();
			return result;
		}

		public void AddNewFiles(NEWindow neWindow) => SetNEWindowDatas(NEWindowDatas.Concat(neWindow.data));

		public void RemoveFiles(NEWindow neWindow) => SetNEWindowDatas(NEWindowDatas.Except(neWindow.data));

		public bool HandlesKey(ModifierKeys modifiers, Key key)
		{
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Escape:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
				case Key.Home:
				case Key.End:
				case Key.PageUp:
				case Key.PageDown:
				case Key.Tab:
				case Key.Enter:
					return true;
			}

			return false;
		}

		public void HandleCommand(ExecuteState executeState, Func<bool> skipDraw = null)
		{
			EditorExecuteState.SetState(this, executeState);
			switch (state.Command)
			{
				case NECommand.Internal_CommandLine: NEFile.PreExecute_Internal_CommandLine(); break;
				default: state.NEWindow.HandleCommand(state, skipDraw); break;
			}
		}

		public bool StopTasks() => state.NEWindow.StopTasks();

		public bool KillTasks() => state.NEWindow.KillTasks();
	}
}
