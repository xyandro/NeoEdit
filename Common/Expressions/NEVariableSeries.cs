using System;

namespace NeoEdit.Common.Expressions
{
	internal class NEVariableSeries : NEVariable
	{
		Func<int, object> series;

		public NEVariableSeries(string name, string description, Func<int, object> series) : base(name, description)
		{
			this.series = series;
		}

		public override object GetValue(int index) => series(index);
	}
}
