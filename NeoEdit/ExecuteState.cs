using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	public class ExecuteState
	{
		IReadOnlyDictionary<TextEditor, IReadOnlyList<string>> ClipboardMap;
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

		public IReadOnlyList<string> GetClipboard(TextEditor textEditor)
		{
			if (ClipboardMap == null)
				lock (this)
					if (ClipboardMap == null)
						ClipboardMap = TabsWindow.GetClipboardMap();

			return ClipboardMap[textEditor];
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
