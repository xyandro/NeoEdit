using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	public class NEVariable
	{
		public string Name { get; }
		public string Description { get; }
		public Func<IEnumerable<object>> Value { get; }
		public NEVariableInitializer Initializer { get; }
		public bool Infinite { get; }

		public NEVariable(string name, string description, Func<IEnumerable<object>> value, NEVariableInitializer initializer = null, bool infinite = false)
		{
			Name = name;
			Description = description;
			Initializer = initializer;
			Value = value;
			Infinite = infinite;
		}

		public static NEVariable Constant(string name, string description, Func<object> value, NEVariableInitializer initializer = null) => new NEVariable(name, description, () => System.Linq.Enumerable.Repeat(value(), int.MaxValue), initializer, true);
		public static NEVariable Enumerable<T>(string name, string description, Func<IEnumerable<T>> values, NEVariableInitializer initializer = null, bool infinite = false) => new NEVariable(name, description, () => values().Cast<object>(), initializer, infinite);
		public static NEVariable InterpretEnumerable(string name, string description, Func<IEnumerable<string>> values, NEVariableInitializer initializer = null) => new NEVariable(name, description, () => InterpretValues(values()), initializer);
		public static NEVariable InterpretConstant(string name, string description, Func<string> value, NEVariableInitializer initializer = null) => Constant(name, description, () => InterpretValues(new[] { value() })[0], initializer);

		delegate bool TryParse<T>(string str, out T value);
		static List<object> InterpretType<T>(IEnumerable<string> strs, TryParse<T> tryParse)
		{
			var result = new List<object>();
			T value;
			foreach (var str in strs)
			{
				if (!tryParse(str, out value))
					return null;
				result.Add(value);
			}
			return result;
		}

		static protected List<object> InterpretValues(IEnumerable<string> strs) => InterpretType<bool>(strs, bool.TryParse) ?? InterpretType<BigInteger>(strs, BigInteger.TryParse) ?? InterpretType<double>(strs, double.TryParse) ?? strs.Cast<object>().ToList();
	}
}
