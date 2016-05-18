using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class NEVariables : IEnumerable<NEVariable>
	{
		readonly Dictionary<string, NEVariable> varDict = new Dictionary<string, NEVariable>();

		public NEVariables() { }

		public NEVariables(IEnumerable<NEVariable> variables) { AddRange(variables); }

		public NEVariables(params NEVariable[] variables) { AddRange(variables); }

		public void Add(NEVariable variable) => varDict[variable.Name] = variable;

		public void AddRange(IEnumerable<NEVariable> variables) => variables.ForEach(variable => Add(variable));

		public bool Contains(string variable) => varDict.ContainsKey(variable);

		NEVariable GetVariable(string variable)
		{
			if (!varDict.ContainsKey(variable))
				throw new ArgumentException($"Variable {variable} doesn't exist");
			return varDict[variable];
		}

		public object GetValue(string variable, int rowNum) => GetVariable(variable).GetValue(rowNum);

		public List<object> GetValues(string variable) => GetVariable(variable).GetValues();

		public List<object> GetValues(string variable, int rowCount) => Enumerable.Range(0, rowCount).Select(row => GetVariable(variable).GetValue(row)).ToList();

		public int? ResultCount(HashSet<string> variables) => variables.Min(variable => GetVariable(variable).Count());

		//Class implements ienumerable so it can be the source of the variable help dialog
		public IEnumerator<NEVariable> GetEnumerator() => varDict.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
