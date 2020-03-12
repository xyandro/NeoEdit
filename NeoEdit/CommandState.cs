using System;
using System.Collections.Generic;

namespace NeoEdit.Program
{
	public class CommandState
	{
		Dictionary<TextEditor, IReadOnlyList<string>> ClipboardMap;

		public Func<Dictionary<TextEditor, IReadOnlyList<string>>> GetClipboardMap;

		public TabsWindow TabsWindow;
		public NECommand Command;
		public object Parameters;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public object PreHandleData;
		public bool Result;
		public bool? MultiStatus;

		public CommandState(NECommand command) => Command = command;

		public IReadOnlyList<string> GetClipboard(TextEditor textEditor)
		{
			if (ClipboardMap == null)
				lock (this)
				{
					if (ClipboardMap == null)
						ClipboardMap = GetClipboardMap();
				}

			return ClipboardMap[textEditor];
		}
	}
}
