using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Expressions
{
	internal class NEVariableList : NEVariable
	{
		Func<List<object>> listFunc;
		List<object> list;
		NEVariableListInitializer initializer;

		public NEVariableList(string name, string description, Func<List<object>> listFunc, NEVariableListInitializer initializer = null) : base(name, description)
		{
			this.listFunc = listFunc;
			this.initializer = initializer;
		}

		void Setup()
		{
			if (list != null)
				return;
			initializer?.Initialize();
			list = listFunc();
		}

		public override object GetValue(int index)
		{
			Setup();
			if (index >= list.Count)
				throw new ArgumentException($"Not enough values for variable {Name}");
			return list[index];
		}

		public override int? Count()
		{
			Setup();
			return list.Count;
		}
	}
}
