using System;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnit
	{
		public string Unit { get; }
		public int Exp { get; }

		public ExpressionUnit(string unit, int exp = 1)
		{
			Unit = unit;
			Exp = exp;
		}

		public override string ToString() => Exp == 1 ? Unit : $"{Unit}^{Exp}";
	}
}
