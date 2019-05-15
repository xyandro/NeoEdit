using System;
using NeoEdit.Expressions;

namespace NeoEdit
{
	public class InlineVariable
	{
		public string Name { get; set; }
		public string Expression { get => NEExpression.ToString(); set => NEExpression = new NEExpression(value); }
		public Range ExpressionRange { get; set; }
		public NEExpression NEExpression { get; set; }
		double value;
		public double Value
		{
			get => value; set
			{
				if (value == this.value)
					return;
				this.value = value;
			}
		}
		public Range ValueRange { get; set; }
		public Exception Exception { get; set; }

		public InlineVariable(string name, string expression, Range expressionRange, string value, Range valueRange)
		{
			Name = name;
			Expression = expression;
			ExpressionRange = expressionRange;
			double.TryParse(value, out this.value);
			ValueRange = valueRange;
		}
	}
}
