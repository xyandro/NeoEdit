using System;

namespace NeoEdit.Common.Expressions
{
	class NEVariableConstant : NEVariable
	{
		readonly Func<object> valueFunc;
		object value;

		public NEVariableConstant(string name, string description, Func<object> valueFunc, NEVariableInitializer initializer = null) : base(name, description, initializer) => this.valueFunc = valueFunc;

		protected override void VirtSetup() => value = valueFunc();

		protected override object VirtValue(int index) => value;
	}
}
