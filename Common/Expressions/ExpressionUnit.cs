using System;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnit
	{
		public string Unit { get; private set; }
		public int Exp { get; private set; }

		public ExpressionUnit(string unit, int exp = 1)
		{
			Unit = unit;
			Exp = exp;
		}

		public override string ToString()
		{
			return Exp == 1 ? Unit : String.Format("{0}^{1}", Unit, Exp);
		}
	}
}
