﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Program;

namespace NeoEdit.Program.Expressions
{
	public class NEVariables : IEnumerable<NEVariable>
	{
		readonly Dictionary<string, NEVariable> varDict = new Dictionary<string, NEVariable>();

		public NEVariables() { }

		public NEVariables(params object[] values) { AddRange(values.Select((value, index) => NEVariable.Constant($"p{index}", $"Param {index}", value))); }

		public NEVariables(IEnumerable<NEVariable> variables) { AddRange(variables); }

		public NEVariables(params NEVariable[] variables) { AddRange(variables); }

		public void Add(NEVariable variable) => varDict[variable.Name] = variable;

		public void AddRange(IEnumerable<NEVariable> variables) => variables.ForEach(variable => Add(variable));

		public void Remove(string name) => varDict.Remove(name);

		public bool Contains(string variable) => varDict.ContainsKey(variable);

		NEVariable GetVariable(string variable)
		{
			if (!varDict.ContainsKey(variable))
				throw new ArgumentException($"Variable {variable} doesn't exist");
			return varDict[variable];
		}

		public object GetValue(string variable, int rowNum) => GetVariable(variable).GetValue(rowNum);

		public List<object> GetValues(string variable, int rowCount) => Enumerable.Range(0, rowCount).Select(row => GetVariable(variable).GetValue(row)).ToList();

		public int? ResultCount(params IEnumerable<string>[] variableLists)
		{
			int? resultCount = null;
			string resultVariable = null;

			foreach (var list in variableLists)
				foreach (var variable in list)
				{
					var count = GetVariable(variable).Count();
					if (!count.HasValue)
						continue;

					if (!resultCount.HasValue)
					{
						resultCount = count;
						resultVariable = variable;
					}
					else if (resultCount != count)
						throw new Exception($"Result count mismatch: {resultVariable} ({resultCount}) vs {variable} ({count})");
				}
			return resultCount;
		}

		public int? ResultCount(params NEExpression[] expressions) => ResultCount(expressions.Select(expression => expression.Variables).ToArray());

		//Class implements ienumerable so it can be the source of the variable help dialog
		public IEnumerator<NEVariable> GetEnumerator() => varDict.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
