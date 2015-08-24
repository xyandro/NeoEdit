using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnits : IEnumerable<ExpressionUnit>
	{
		readonly ReadOnlyCollection<ExpressionUnit> units;

		public ExpressionUnits()
		{
			units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit>());
		}

		public ExpressionUnits(ExpressionUnit unit)
		{
			units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit> { unit });
		}

		public ExpressionUnits(string unit, int exp = 1)
		{
			units = new ReadOnlyCollection<ExpressionUnit>(new List<ExpressionUnit> { new ExpressionUnit(unit, exp) });
		}

		ExpressionUnits(IEnumerable<ExpressionUnit> units)
		{
			this.units = new ReadOnlyCollection<ExpressionUnit>(units.GroupBy(unit => unit.Unit).Select(group => new ExpressionUnit(group.Key, group.Sum(unit => unit.Exp))).Where(unit => unit.Exp != 0).OrderBy(unit => unit.Exp < 0).ThenBy(unit => unit.Unit).ToList());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<ExpressionUnit> GetEnumerator()
		{
			return units.GetEnumerator();
		}

		public bool HasUnits { get { return units.Any(); } }
		public bool IsSI { get { return (this.Count() == 1) && (units[0].Exp == 1) && (units[0].Unit.ToLowerInvariant()) == "si"; } }
		public bool IsSimple { get { return (this.Count() == 1) && (units[0].Exp == 1) && (units[0].Unit.ToLowerInvariant()) == "simple"; } }

		public static ExpressionUnits operator *(ExpressionUnits factor1, ExpressionUnits factor2)
		{
			return new ExpressionUnits(factor1.units.Concat(factor2.units));
		}

		public static ExpressionUnits operator /(ExpressionUnits dividend, ExpressionUnits divisor)
		{
			return new ExpressionUnits(dividend.units.Concat(divisor.units.Select(unit => new ExpressionUnit(unit.Unit, -unit.Exp))));
		}

		public static ExpressionUnits operator ^(ExpressionUnits units1, int power)
		{
			return new ExpressionUnits(units1.units.Select(unit => new ExpressionUnit(unit.Unit, unit.Exp * power)));
		}

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

			var numeratorStr = String.Join("*", numeratorUnits.Select(unit => unit.ToString()));
			var denominatorStr = String.Join("*", denominatorUnits.Select(unit => unit.ToString()));
			if (denominatorUnits.Count > 1)
				denominatorStr = "(" + denominatorStr + ")";
			if (!String.IsNullOrWhiteSpace(denominatorStr))
				numeratorStr += "/" + denominatorStr;
			return numeratorStr;
		}
	}
}
