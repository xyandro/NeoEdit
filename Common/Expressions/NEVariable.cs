using System;
using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Common.Expressions
{
	abstract public class NEVariable
	{
		public string Name { get; }
		public string Description { get; }

		public NEVariable(string name, string description)
		{
			Name = name;
			Description = description;
		}

		abstract public object GetValue(int index);
		virtual public int? Count() => null;

		public static NEVariable Constant<T>(string name, string description, Func<T> value) => new NEVariableConstant(name, description, () => value());
		public static NEVariable List<T>(string name, string description, Func<IEnumerable<T>> list, NEVariableListInitializer initializer = null) => new NEVariableList(name, description, () => list().Cast<object>().ToList(), initializer);
		public static NEVariable Series<T>(string name, string description, Func<int, T> series) => new NEVariableSeries(name, description, index => series(index));
	}
}
