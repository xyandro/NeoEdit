using System;

namespace NeoEdit.Common.Expressions
{
	class NEVariableConstant : NEVariable
	{
		Func<object> valueFunc;
		object value;
		bool isSet = false;
		object lockObj = new object();

		public NEVariableConstant(string name, string description, Func<object> valueFunc) : base(name, description)
		{
			this.valueFunc = valueFunc;
		}

		public override object GetValue(int index)
		{
			if (!isSet)
			{
				lock (lockObj)
				{
					if (!isSet)
					{
						value = valueFunc();
						isSet = true;
					}
				}
			}
			return value;
		}
	}
}
