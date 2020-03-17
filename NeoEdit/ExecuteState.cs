using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	public class ExecuteState
	{
		public IEnumerable<Tab> ActiveTabs;
		public bool Handled = true;
		public TabsWindow TabsWindow;

		public NECommand Command;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public Key Key;
		public string Text;
		public object Configuration;

		public ExecuteState(NECommand command) => Command = command;

		public ModifierKeys Modifiers
		{
			set
			{
				ShiftDown = value.HasFlag(ModifierKeys.Shift);
				ControlDown = value.HasFlag(ModifierKeys.Control);
				AltDown = value.HasFlag(ModifierKeys.Alt);
			}
		}

		AnswerResult savedAnswers;
		public AnswerResult SavedAnswers
		{
			get
			{
				if (savedAnswers == null)
					savedAnswers = new AnswerResult();
				return savedAnswers;
			}
		}

		IReadOnlyDictionary<Tab, Tuple<IReadOnlyList<string>, bool?>> ClipboardDataMap;
		public Func<IReadOnlyDictionary<Tab, Tuple<IReadOnlyList<string>, bool?>>> ClipboardDataMapFunc;
		public Tuple<IReadOnlyList<string>, bool?> GetClipboardData(Tab tab)
		{
			if (ClipboardDataMap == null)
				lock (this)
					if (ClipboardDataMap == null)
						ClipboardDataMap = ClipboardDataMapFunc();

			return ClipboardDataMap[tab];
		}

		IReadOnlyDictionary<Tab, KeysAndValues>[] KeysAndValuesMap;
		public Func<int, IReadOnlyDictionary<Tab, KeysAndValues>> KeysAndValuesFunc;
		public KeysAndValues GetKeysAndValues(int kvIndex, Tab tab)
		{
			if (KeysAndValuesMap == null)
				KeysAndValuesMap = Enumerable.Repeat(default(IReadOnlyDictionary<Tab, KeysAndValues>), 10).ToArray();
			if (KeysAndValuesMap[kvIndex] == null)
				lock (this)
					if (KeysAndValuesMap[kvIndex] == null)
						KeysAndValuesMap[kvIndex] = KeysAndValuesFunc(kvIndex);

			return KeysAndValuesMap[kvIndex][tab];
		}
	}
}
