using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.TextEdit.Expressions
{
	public class ExpressionUnits : IEnumerable<ExpressionUnit>
	{
		readonly ReadOnlyCollection<ExpressionUnit> units;

		public ExpressionUnits() { units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit>()); }
		public ExpressionUnits(ExpressionUnit unit) { units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit> { unit }); }
		public ExpressionUnits(string unit) { units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit> { new ExpressionUnit(unit) }); }
		ExpressionUnits(IEnumerable<ExpressionUnit> units) { this.units = new ReadOnlyCollection<ExpressionUnit>(units.GroupBy(unit => unit.Unit).Select(group => new ExpressionUnit(group.Key, group.Sum(unit => unit.Exp))).Where(unit => unit.Exp != 0).OrderBy(unit => unit.Exp < 0).ThenBy(unit => unit.Unit).ToList()); }
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<ExpressionUnit> GetEnumerator() => units.GetEnumerator();

		public bool HasUnits => units.Any();
		public bool IsSI => (this.Count() == 1) && (units[0].Exp == 1) && (units[0].Unit.ToLowerInvariant()) == "si";
		public bool IsSimple => (this.Count() == 1) && (units[0].Exp == 1) && (units[0].Unit.ToLowerInvariant()) == "simple";

		public static ExpressionUnits operator *(ExpressionUnits factor1, ExpressionUnits factor2) => new ExpressionUnits(factor1.units.Concat(factor2.units));
		public static ExpressionUnits operator /(ExpressionUnits dividend, ExpressionUnits divisor) => new ExpressionUnits(dividend.units.Concat(divisor.units.Select(unit => new ExpressionUnit(unit.Unit, -unit.Exp))));
		public static ExpressionUnits operator ^(ExpressionUnits units1, int power) => new ExpressionUnits(units1.units.Select(unit => new ExpressionUnit(unit.Unit, unit.Exp * power)));

		public bool Equals(ExpressionUnits testObj)
		{
			if (units.Count != testObj.units.Count)
				return false;
			return !(this / testObj).HasUnits;
		}

		public override string ToString()
		{
			if (!HasUnits)
				return null;

			var numeratorUnits = units.Where(unit => unit.Exp > 0).ToList();
			var denominatorUnits = units.Where(unit => unit.Exp < 0).ToList();
			if (numeratorUnits.Count == 0)
			{
				numeratorUnits = denominatorUnits;
				denominatorUnits = new List<ExpressionUnit>();
			}
			else
				denominatorUnits = denominatorUnits.Select(unit => new ExpressionUnit(unit.Unit, -unit.Exp)).ToList();

			var numeratorStr = string.Join("*", numeratorUnits.Select(unit => unit.ToString()));
			var denominatorStr = string.Join("*", denominatorUnits.Select(unit => unit.ToString()));
			if (denominatorUnits.Count > 1)
				denominatorStr = $"({denominatorStr})";
			if (!string.IsNullOrWhiteSpace(denominatorStr))
				numeratorStr += $"/{denominatorStr}";
			return numeratorStr;
		}
	}
}
