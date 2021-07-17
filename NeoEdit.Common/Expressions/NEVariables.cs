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

		public NEVariables(IEnumerable<NEVariable> variables) => AddRange(variables);

		public NEVariables(params NEVariable[] variables) => AddRange(variables);

		public static NEVariables FromConstants(params object[] values) => new NEVariables(values.Select((value, index) => NEVariable.Constant($"p{index}", $"Param {index}", () => value)));

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

		public object GetValue(NEVariableUse variableUse, int index, int rowCount)
		{
			var variable = GetVariable(variableUse.Name);
			return variable.GetValue(index, variableUse.Repeat, rowCount);
		}

		public List<object> GetValues(NEVariableUse variable, int count, int rowCount) => GetVariable(variable.Name).GetValues(variable.Repeat, count, rowCount);

		public List<object> GetValues(NEVariableUse variable, int rowCount) => GetValues(variable, rowCount, rowCount);

		public int RowCount(params NEExpression[] expressions) => RowCount(expressions.SelectMany(x => x.VariableUses));

		public int RowCount(IEnumerable<NEVariableUse> variableUses, int? rowCount = null)
		{
			var variables = variableUses.Select(x => (variable: GetVariable(x.Name), repeat: x.Repeat)).ToList();
			var count = rowCount ?? variables.Select(x => x.variable.Count ?? 1).DefaultIfEmpty(1).Max();

			foreach ((var variable, var repeat) in variables)
			{
				if (variable.Count == null) // Series, doesn't factor into count
					continue;

				if (variable.Count == count)
					continue;

				if (repeat == NEVariableRepeat.None)
					throw new Exception($"Variable count mismatch: {variable.Name} ({variable.Count}) vs {count}");

				if (count % variable.Count != 0)
					throw new Exception($"Non-multiple variable: {variable.Name} ({variable.Count}) vs {count}");
			}

			return count;
		}

		//Class implements ienumerable so it can be the source of the variable help dialog
		public IEnumerator<NEVariable> GetEnumerator() => varDict.Values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
