using System;

namespace NeoEdit.Expressions
{
	internal class NEVariableSeries : NEVariable
	{
		readonly Func<int, object> series;

		public NEVariableSeries(string name, string description, Func<int, object> series, NEVariableInitializer initializer = null) : base(name, description, initializer) => this.series = series;

		protected override object VirtValue(int index) => series(index);
	}
}
