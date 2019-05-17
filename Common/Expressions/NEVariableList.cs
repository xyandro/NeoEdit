using System;
using System.Collections.Generic;

namespace NeoEdit.Common.Expressions
{
	internal class NEVariableList : NEVariable
	{
		readonly Func<List<object>> listFunc;
		List<object> list;

		public NEVariableList(string name, string description, Func<List<object>> listFunc, NEVariableInitializer initializer = null) : base(name, description, initializer) => this.listFunc = listFunc;

		protected override void VirtSetup() => list = listFunc();

		protected override int? VirtCount() => list.Count;

		protected override object VirtValue(int index)
		{
			if (index >= list.Count)
				throw new ArgumentException($"Not enough values for variable {Name}");
			return list[index];
		}
	}
}
