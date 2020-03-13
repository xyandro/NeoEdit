﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program
{
	public class ExecuteState
	{
		[XMLConverter.NoXML] IReadOnlyDictionary<TextEditor, Tuple<IReadOnlyList<string>, bool?>> ClipboardDataMap;
		public Func<IReadOnlyDictionary<TextEditor, Tuple<IReadOnlyList<string>, bool?>>> ClipboardDataMapFunc;

		[XMLConverter.NoXML] IReadOnlyDictionary<TextEditor, KeysAndValues>[] KeysAndValuesMap = Enumerable.Repeat(default(IReadOnlyDictionary<TextEditor, KeysAndValues>), 10).ToArray();
		public Func<int, IReadOnlyDictionary<TextEditor, KeysAndValues>> KeysAndValuesFunc;

		[XMLConverter.NoXML] public AnswerResult SavedAnswers = new AnswerResult();

		public NECommand Command;
		public object PreExecuteData;
		public object ConfigureExecuteData;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public bool Unhandled;
		public Key Key;
		public string Text;

		public ExecuteState(NECommand command) => Command = command;

		public Tuple<IReadOnlyList<string>, bool?> GetClipboardData(TextEditor textEditor)
		{
			if (ClipboardDataMap == null)
				lock (this)
					if (ClipboardDataMap == null)
						ClipboardDataMap = ClipboardDataMapFunc();

			return ClipboardDataMap[textEditor];
		}

		public KeysAndValues GetKeysAndValues(int kvIndex, TextEditor textEditor)
		{
			if (KeysAndValuesMap[kvIndex] == null)
				lock (this)
					if (KeysAndValuesMap[kvIndex] == null)
						KeysAndValuesMap[kvIndex] = KeysAndValuesFunc(kvIndex);

			return KeysAndValuesMap[kvIndex][textEditor];
		}
	}
}
