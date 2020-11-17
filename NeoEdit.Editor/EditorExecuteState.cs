using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Editor.Models;
using NeoEdit.Editor.PreExecution;

namespace NeoEdit.Editor
{
	public class EditorExecuteState : ExecuteState
	{
		public IPreExecution PreExecution;
		public NEFilesHandler NEFiles;

		public EditorExecuteState(NECommand command = NECommand.None) : base(command) => Command = command;

		public EditorExecuteState(NEFilesHandler neFiles, ExecuteState state) : base(state.Command)
		{
			ShiftDown = state.ShiftDown;
			ControlDown = state.ControlDown;
			AltDown = state.AltDown;
			MultiStatus = state.MultiStatus;
			Key = state.Key;
			Text = state.Text;
			Configuration = state.Configuration;
			NEFiles = neFiles;
		}

		AnswerResult savedAnswers;
		public AnswerResult SavedAnswers
		{
			get
			{
				if (savedAnswers == null)
					lock (this)
						if (savedAnswers == null)
							savedAnswers = new AnswerResult();
				return savedAnswers;
			}
		}

		IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>> ClipboardDataMap;
		public Func<IReadOnlyDictionary<INEFile, Tuple<IReadOnlyList<string>, bool?>>> ClipboardDataMapFunc;
		public Tuple<IReadOnlyList<string>, bool?> GetClipboardData(INEFile neFile)
		{
			if (ClipboardDataMap == null)
				lock (this)
					if (ClipboardDataMap == null)
						ClipboardDataMap = ClipboardDataMapFunc();

			return ClipboardDataMap[neFile];
		}

		IReadOnlyDictionary<INEFile, KeysAndValues>[] KeysAndValuesMap;
		public Func<int, IReadOnlyDictionary<INEFile, KeysAndValues>> KeysAndValuesFunc;
		public KeysAndValues GetKeysAndValues(int kvIndex, INEFile neFile)
		{
			if (KeysAndValuesMap == null)
				lock (this)
					if (KeysAndValuesMap == null)
						KeysAndValuesMap = Enumerable.Repeat(default(IReadOnlyDictionary<INEFile, KeysAndValues>), 10).ToArray();
			if (KeysAndValuesMap[kvIndex] == null)
				lock (this)
					if (KeysAndValuesMap[kvIndex] == null)
						KeysAndValuesMap[kvIndex] = KeysAndValuesFunc(kvIndex);

			return KeysAndValuesMap[kvIndex][neFile];
		}

		Dictionary<string, NEExpression> expressions;

		public NEExpression GetExpression(string expression)
		{
			if (expressions == null)
				lock (this)
					if (expressions == null)
						expressions = new Dictionary<string, NEExpression>();

			lock (this)
				if (expressions.ContainsKey(expression))
					return expressions[expression];

			var neExpression = new NEExpression(expression);
			lock (this)
				expressions[expression] = neExpression;
			return neExpression;
		}

		public override string ToString() => Command.ToString();
	}
}
