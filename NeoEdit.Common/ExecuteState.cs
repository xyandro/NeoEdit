using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;

namespace NeoEdit.Common
{
	public class ExecuteState
	{
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

		IReadOnlyDictionary<ITab, Tuple<IReadOnlyList<string>, bool?>> ClipboardDataMap;
		public Func<IReadOnlyDictionary<ITab, Tuple<IReadOnlyList<string>, bool?>>> ClipboardDataMapFunc;
		public Tuple<IReadOnlyList<string>, bool?> GetClipboardData(ITab tab)
		{
			if (ClipboardDataMap == null)
				lock (this)
					if (ClipboardDataMap == null)
						ClipboardDataMap = ClipboardDataMapFunc();

			return ClipboardDataMap[tab];
		}

		IReadOnlyDictionary<ITab, KeysAndValues>[] KeysAndValuesMap;
		public Func<int, IReadOnlyDictionary<ITab, KeysAndValues>> KeysAndValuesFunc;
		public KeysAndValues GetKeysAndValues(int kvIndex, ITab tab)
		{
			if (KeysAndValuesMap == null)
				KeysAndValuesMap = Enumerable.Repeat(default(IReadOnlyDictionary<ITab, KeysAndValues>), 10).ToArray();
			if (KeysAndValuesMap[kvIndex] == null)
				lock (this)
					if (KeysAndValuesMap[kvIndex] == null)
						KeysAndValuesMap[kvIndex] = KeysAndValuesFunc(kvIndex);

			return KeysAndValuesMap[kvIndex][tab];
		}

		Dictionary<string, NEExpression> expressions;

		public NEExpression GetExpression(string expression)
		{
			if (expressions == null)
				expressions = new Dictionary<string, NEExpression>();
			if (!expressions.ContainsKey(expression))
				expressions[expression] = new NEExpression(expression);
			return expressions[expression];
		}

		public override string ToString() => Command.ToString();
	}
}
