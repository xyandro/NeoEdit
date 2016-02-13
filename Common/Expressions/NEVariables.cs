using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	public class NEVariables
	{
		readonly Dictionary<string, NEVariable> varDict = new Dictionary<string, NEVariable>();
		readonly Dictionary<string, List<object>> varValues = new Dictionary<string, List<object>>();

		public int RowCount { get; private set; }

		public NEVariables() { }

		public NEVariables(IEnumerable<NEVariable> variables) { AddRange(variables); }

		public NEVariables(params NEVariable[] variables) { AddRange(variables); }

		public void Add(NEVariable variable) => varDict[variable.Name] = variable;

		public void AddRange(IEnumerable<NEVariable> variables) => variables.ForEach(variable => Add(variable));

		public bool Contains(string variable) => varDict.ContainsKey(variable);

		HashSet<Action> initialized = new HashSet<Action>();
		void RunActions(IEnumerable<Action> actions)
		{
			var newActions = actions.Distinct().Where(action => !initialized.Contains(action)).ToList();
			newActions.Execute();
			newActions.ForEach(action => initialized.Add(action));
		}

		public void Prepare(NEExpression expression, int? rowCount = null) => Prepare(expression.Variables, rowCount);

		public int Prepare(IEnumerable<string> variables, int? rowCount = null)
		{
			if (rowCount < 0)
				throw new ArgumentException($"{nameof(NEVariables.RowCount)} invalid");

			if (!variables.Any())
				return RowCount = rowCount ?? 1;

			if ((rowCount.HasValue) && (varValues != null) && (variables.All(variable => (varValues.ContainsKey(variable)) && (varValues[variable].Count >= rowCount))))
				return RowCount = rowCount.Value;

			var missing = variables.FirstOrDefault(variable => !varDict.ContainsKey(variable));
			if (missing != null)
				throw new Exception($"Variable {missing} is undefined");

			lock(this)
			{
				if ((rowCount.HasValue) && (varValues != null) && (variables.All(variable => (varValues.ContainsKey(variable)) && (varValues[variable].Count >= rowCount))))
					return RowCount = rowCount.Value;

				RunActions(variables.Select(variable => varDict[variable].Initializer).NonNull().SelectMany(initializer => initializer.AllActions()).Distinct());

				for (var pass = 0; pass < 2; ++pass)
				{
					var data = variables.Where(variable => (varDict[variable].Infinite) == (pass == 1)).AsParallel().ToDictionary(variable => variable, variable => varDict[variable].Value().Take(rowCount ?? int.MaxValue).ToList());
					if (!rowCount.HasValue)
						rowCount = data.Any() ? data.Min(pair => pair.Value.Count) : 1;
					data.ForEach(pair => varValues[pair.Key] = pair.Value);
				}

				var tooFew = variables.FirstOrDefault(variable => varValues[variable].Count < rowCount);
				if (tooFew != null)
					throw new Exception($"Variable {tooFew} doesn't have enough values");

				return RowCount = rowCount.Value;
			}
		}

		public object GetValue(string variable, int rowNum)
		{
			Prepare(new[] { variable }, RowCount);
			if (varValues == null)
				throw new Exception($"Must call {nameof(Prepare)} first");
			if (!varValues.ContainsKey(variable))
				throw new Exception($"Variable {variable} is undefined");
			if (rowNum >= varValues[variable].Count)
				throw new Exception($"Variable {variable} doesn't have enough values");
			return varValues[variable][rowNum];
		}

		public List<object> GetValues(string variable)
		{
			if (varValues == null)
				throw new Exception($"Must call {nameof(Prepare)} first");
			return varValues[variable];
		}

		public Dictionary<string, List<object>> GetAllValues()
		{
			if (varValues == null)
				throw new Exception($"Must call {nameof(Prepare)} first");
			return varValues;
		}
	}
}
