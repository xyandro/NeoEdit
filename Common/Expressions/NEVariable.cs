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
		bool setupDone = false;

		readonly NEVariableInitializer initializer;

		public NEVariable(string name, string description, NEVariableInitializer initializer = null)
		{
			Name = name;
			Description = description;
			this.initializer = initializer;
		}

		virtual protected void VirtSetup() { }
		virtual protected int? VirtCount() => null;
		abstract protected object VirtValue(int index);

		void Setup()
		{
			if (setupDone)
				return;

			lock (this)
			{
				if (setupDone)
					return;

				initializer?.Initialize();
				VirtSetup();
				setupDone = true;
			}
		}

		public int? Count()
		{
			Setup();
			return VirtCount();
		}

		public object GetValue(int index)
		{
			Setup();
			return VirtValue(index);
		}

		public static NEVariable Constant<T>(string name, string description, T value) => new NEVariableConstant(name, description, () => value);
		public static NEVariable Constant<T>(string name, string description, Func<T> value, NEVariableInitializer initializer = null) => new NEVariableConstant(name, description, () => value(), initializer);
		public static NEVariable List<T>(string name, string description, Func<IEnumerable<T>> list, NEVariableInitializer initializer = null) => new NEVariableList(name, description, () => list().Cast<object>().ToList(), initializer);
		public static NEVariable Series<T>(string name, string description, Func<int, T> series, NEVariableInitializer initializer = null) => new NEVariableSeries(name, description, index => series(index), initializer);
	}
}
