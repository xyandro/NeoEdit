using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	public class ExecuteState
	{
		IReadOnlyDictionary<TextEditor, Tuple<IReadOnlyList<string>, bool?>> ClipboardDataMap;
		IReadOnlyDictionary<TextEditor, KeysAndValues>[] KeysAndValuesMap = Enumerable.Repeat(default(IReadOnlyDictionary<TextEditor, KeysAndValues>), 10).ToArray();

		public TabsWindow TabsWindow;
		public NECommand Command;
		public object PreExecuteData;
		public object ConfigureExecuteData;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool Result;
		public bool? MultiStatus;
		public AnswerResult SavedAnswers = new AnswerResult();


		public ExecuteState(NECommand command) => Command = command;

		public Tuple<IReadOnlyList<string>, bool?> GetClipboardData(TextEditor textEditor)
		{
			if (ClipboardDataMap == null)
				lock (this)
					if (ClipboardDataMap == null)
						ClipboardDataMap = TabsWindow.GetClipboardDataMap();

			return ClipboardDataMap[textEditor];
		}

		public KeysAndValues GetKeysAndValues(int kvIndex, TextEditor textEditor)
		{
			if (KeysAndValuesMap[kvIndex] == null)
				lock (this)
					if (KeysAndValuesMap[kvIndex] == null)
						KeysAndValuesMap[kvIndex] = TabsWindow.GetKeysAndValuesMap(kvIndex);

			return KeysAndValuesMap[kvIndex][textEditor];
		}
	}
}
