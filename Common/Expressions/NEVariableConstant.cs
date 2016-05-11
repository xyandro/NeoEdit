namespace NeoEdit.Common.Expressions
{
	class NEVariableConstant : NEVariable
	{
		object value;
		public NEVariableConstant(string name, string description, object value) : base(name, description)
		{
			this.value = value;
		}

		public override object GetValue(int index) => value;
	}
}
