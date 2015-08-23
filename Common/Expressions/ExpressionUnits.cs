using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnits
	{
		readonly ReadOnlyDictionary<string, int> units;

		public ExpressionUnits(string unit = null)
		{
			var units = new Dictionary<string, int>();
			if (unit != null)
				units[unit] = 1;
			this.units = new ReadOnlyDictionary<string, int>(units);
		}

		ExpressionUnits(ReadOnlyDictionary<string, int> units)
		{
			this.units = units;
		}

		public bool HasUnits { get { return units.Any(); } }
		public bool IsSI { get { return (units.Count == 1) && (units.First().Key.ToLowerInvariant() == "si") && (units.First().Value == 1); } }
		public bool IsSimple { get { return (units.Count == 1) && (units.First().Key.ToLowerInvariant() == "simple") && (units.First().Value == 1); } }

		public static ExpressionUnits operator *(ExpressionUnits factor1, ExpressionUnits factor2)
		{
			var units = factor1.units.Concat(factor2.units).GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value)).Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);
			return new ExpressionUnits(new ReadOnlyDictionary<string, int>(units));
		}

		public static ExpressionUnits operator /(ExpressionUnits dividend, ExpressionUnits divisor)
		{
			var units = dividend.units.Concat(divisor.units.ToDictionary(pair => pair.Key, pair => -pair.Value)).GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value)).Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);
			return new ExpressionUnits(new ReadOnlyDictionary<string, int>(units));
		}

		public static ExpressionUnits operator ^(ExpressionUnits units1, int power)
		{
			var units = units1.units.ToDictionary(pair => pair.Key, pair => pair.Value * power);
			return new ExpressionUnits(new ReadOnlyDictionary<string, int>(units));
		}

		public static bool operator ==(ExpressionUnits units1, ExpressionUnits units2)
		{
			if ((Object.ReferenceEquals(units1, null)) != (Object.ReferenceEquals(units2, null)))
				return false;
			if (Object.ReferenceEquals(units1, null))
				return true;
			if (units1.units.Count != units2.units.Count)
				return false;
			return !(units1 / units2).HasUnits;
		}

		public static bool operator !=(ExpressionUnits units1, ExpressionUnits units2)
		{
			return !(units1 == units2);
		}

		public bool Equals(ExpressionUnits expressionUnits)
		{
			return this == expressionUnits;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ExpressionUnits))
				return false;
			return Equals(obj as ExpressionUnits);
		}

		public override int GetHashCode()
		{
			return units.GetHashCode();
		}

		public override string ToString()
		{
			if (!HasUnits)
				return null;

			var posUnits = units.Where(pair => pair.Value > 0).Select(pair => pair.Key + (pair.Value > 1 ? "^" + pair.Value.ToString() : "")).ToList();
			var negUnits = units.Where(pair => pair.Value < 0).Select(pair => pair.Key + (pair.Value < -1 ? "^" + (-pair.Value).ToString() : "")).ToList();
			var posStr = String.Join("*", posUnits);
			var negStr = String.Join("*", negUnits);
			if (negUnits.Count > 1)
				negStr = "(" + negStr + ")";
			if ((String.IsNullOrWhiteSpace(posStr)) && (!String.IsNullOrWhiteSpace(negStr)))
				posUnits.Add("1");
			if (!String.IsNullOrWhiteSpace(negStr))
				posStr += "/" + negStr;
			return posStr;
		}

		public void GetConversion(out double conversion, out ExpressionUnits units)
		{
			conversion = 1;
			units = this;

			while (true)
			{
				var done = true;
				var unitList = units.units.ToList();
				units = new ExpressionUnits();
				foreach (var unit in unitList)
				{
					double unitConversion;
					ExpressionUnits unitUnits;
					if (ExpressionUnitConstants.GetConversion(unit.Key, out unitConversion, out unitUnits))
						done = false;

					conversion *= Math.Pow(unitConversion, unit.Value);
					units *= unitUnits ^ unit.Value;
				}
				if (done)
					break;
			}
		}

		public static ExpressionResult GetConversion(ExpressionUnits units1, ExpressionUnits units2)
		{
			double conversion1, conversion2;
			ExpressionUnits newUnits1, newUnits2;
			units1.GetConversion(out conversion1, out newUnits1);
			if (units2.IsSI)
				units2 = newUnits1;
			else if (units2.IsSimple)
				units2 = ExpressionUnitConstants.GetSimple(newUnits1);

			units2.GetConversion(out conversion2, out newUnits2);
			if (newUnits1 != newUnits2)
				throw new Exception("Cannot convert types");
			return new ExpressionResult(conversion1 / conversion2, units2 / units1);
		}

		public Func<ExpressionResult, ExpressionResult> GetToBase()
		{
			if ((!HasUnits) || (units.Count > 1) || (units.First().Value != 1))
				return null;
			return ExpressionUnitConstants.GetToBase(units.First().Key);
		}

		public Func<ExpressionResult, ExpressionResult> GetFromBase()
		{
			if ((!HasUnits) || (units.Count > 1) || (units.First().Value != 1))
				return null;
			return ExpressionUnitConstants.GetFromBase(units.First().Key);
		}
	}
}
