using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.Common.Expressions
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

		public int ResultCount(params IEnumerable<string>[] variableLists)
		{
			var mins = variableLists.Select(list => list.Min(variable => GetVariable(variable).Count())).NonNull().Distinct().ToList();
			if (!mins.Any())
				return 1;
			if (mins.Count > 1)
				throw new Exception("Expressions must have the same numbers of results");
			return mins.Single();
		}

		public int ResultCount(params NEExpression[] expressions) => ResultCount(expressions.Select(expression => expression.Variables).ToArray());

		//Class implements ienumerable so it can be the source of the variable help dialog
		public IEnumerator<NEVariable> GetEnumerator() => varDict.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
