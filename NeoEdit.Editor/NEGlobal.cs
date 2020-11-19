using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class NEGlobal : INEGlobal
	{
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

		IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>> GetClipboardDataMap()
		{
			var empty = Tuple.Create(new List<string>() as IReadOnlyList<string>, default(bool?));
			var activeFiles = EditorExecuteState.CurrentState.NEWindow.ActiveFiles;
			var clipboardDataMap = activeFiles.ToDictionary(x => x as INEFile, x => empty);

			if (NEClipboard.Current.Count == activeFiles.Count)
				NEClipboard.Current.ForEach((cb, index) => clipboardDataMap[activeFiles.GetIndex(index)] = Tuple.Create(cb, NEClipboard.Current.IsCut));
			else if (NEClipboard.Current.ChildCount == activeFiles.Count)
				NEClipboard.Current.Strings.ForEach((str, index) => clipboardDataMap[activeFiles.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(new List<string> { str }, NEClipboard.Current.IsCut));
			else if (((NEClipboard.Current.Count == 1) || (NEClipboard.Current.Count == NEClipboard.Current.ChildCount)) && (NEClipboard.Current.ChildCount == activeFiles.Sum(neFile => neFile.Selections.Count)))
				NEClipboard.Current.Strings.Take(activeFiles.Select(neFile => neFile.Selections.Count)).ForEach((obj, index) => clipboardDataMap[activeFiles.GetIndex(index)] = new Tuple<IReadOnlyList<string>, bool?>(obj.ToList(), NEClipboard.Current.IsCut));
			else
			{
				var strs = NEClipboard.Current.Strings;
				activeFiles.ForEach(neFile => clipboardDataMap[neFile] = new Tuple<IReadOnlyList<string>, bool?>(strs, NEClipboard.Current.IsCut));
			}

			return clipboardDataMap;
		}

		IReadOnlyList<KeysAndValues>[] keysAndValues = Enumerable.Repeat(new List<KeysAndValues>(), 10).ToArray();
		Dictionary<INEFile, KeysAndValues> GetKeysAndValuesMap(int kvIndex)
		{
			var empty = new KeysAndValues(new List<string>(), kvIndex == 0);
			var allFiles = EditorExecuteState.CurrentState.NEWindow.AllFiles;
			var activeFiles = EditorExecuteState.CurrentState.NEWindow.ActiveFiles;
			var keysAndValuesMap = allFiles.ToDictionary(x => x as INEFile, x => empty);

			if (keysAndValues[kvIndex].Count == 1)
				allFiles.ForEach(neFile => keysAndValuesMap[neFile] = keysAndValues[kvIndex][0]);
			else if (keysAndValues[kvIndex].Count == activeFiles.Count)
				activeFiles.ForEach((neFile, index) => keysAndValuesMap[neFile] = keysAndValues[kvIndex][index]);

			return keysAndValuesMap;
		}

		public void HandleCommand(ExecuteState state, Func<bool> skipDraw = null)
		{
			EditorExecuteState.SetState(this, state);
			if (state.Command == NECommand.Internal_CommandLine)
				NEFile.PreExecute_Internal_CommandLine(); // HACK, but EditorExecuteState.CurrentState.NEWindow is null
			else
				EditorExecuteState.CurrentState.NEWindow.HandleCommand(state, skipDraw);
		}

		public bool StopTasks() => EditorExecuteState.CurrentState.NEWindow.StopTasks();

		public bool KillTasks() => EditorExecuteState.CurrentState.NEWindow.KillTasks();
	}
}
