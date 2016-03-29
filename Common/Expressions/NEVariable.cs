using System;
using System.Collections.Generic;
using System.Linq;
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
	}
}
