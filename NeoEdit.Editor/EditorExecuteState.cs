﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.Editor.Models;
using NeoEdit.Editor.PreExecution;

namespace NeoEdit.Editor
{
	public class EditorExecuteState
	{
		public NECommand Command;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public Key Key;
		public string Text;
		public IConfiguration Configuration;
		public IPreExecution PreExecution;
		public Tabs Tabs;

		public EditorExecuteState(NECommand command) => Command = command;

		public EditorExecuteState(Tabs tabs, ExecuteState state)
		{
			Command = state.Command;
			ShiftDown = state.ShiftDown;
			ControlDown = state.ControlDown;
			AltDown = state.AltDown;
			MultiStatus = state.MultiStatus;
			Key = state.Key;
			Text = state.Text;
			Configuration = state.Configuration;
			Tabs = tabs;
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
				lock (this)
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