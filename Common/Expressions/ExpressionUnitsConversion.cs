using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnitsConversion
	{
		public readonly ExpressionUnits fromUnits, toUnits;
		public readonly double mult = 1, add = 0;

		public ExpressionUnitsConversion(ExpressionUnits units) { fromUnits = toUnits = units; }

		public ExpressionUnitsConversion(ExpressionUnits fromUnits, ExpressionUnits toUnits, double mult, double add)
		{
			this.fromUnits = fromUnits;
			this.toUnits = toUnits;
			this.mult = mult;
			this.add = add;
		}

		public static ExpressionUnitsConversion operator ^(ExpressionUnitsConversion conv, int power)
		{
			if ((conv.add != 0) && (power != 1))
				throw new Exception("Cannot do conversion");
			return new ExpressionUnitsConversion(conv.fromUnits ^ power, conv.toUnits ^ power, Math.Pow(conv.mult, power), conv.add);
		}

		public static ExpressionUnitsConversion operator *(ExpressionUnitsConversion uc1, ExpressionUnitsConversion uc2)
		{
			var result = new ExpressionUnitsConversion(uc1.fromUnits, uc1.toUnits / uc2.fromUnits * uc2.toUnits, uc1.mult * uc2.mult, uc1.add + uc2.add);
			return result;
		}

		public static ExpressionUnitsConversion operator /(ExpressionUnitsConversion uc1, ExpressionUnitsConversion uc2)
		{
			var result = new ExpressionUnitsConversion(uc1.fromUnits, uc1.toUnits * uc2.fromUnits / uc2.toUnits, uc1.mult / uc2.mult, (uc1.add - uc2.add) / uc2.mult);
			return result;
		}

		public override string ToString() => $"From: {fromUnits} To: {toUnits}, Mult: {mult:R}, Add {add:R}";

		public override int GetHashCode() => base.GetHashCode();

		enum ConversionConstantAttr
		{
			None,
			SIPrefix,
			SILongPrefix,
			CPrefix,
			CLongPrefix,
		}

		static Dictionary<string, ExpressionUnitsConversion> conversionConstants = new Dictionary<string, ExpressionUnitsConversion>();

		static void AddConversionConstant(string fromUnitStr, ExpressionUnits toUnit, double mult, double add = 0, ConversionConstantAttr attr = ConversionConstantAttr.None)
		{
			var fromUnit = new ExpressionUnits(fromUnitStr);
			conversionConstants[fromUnitStr] = new ExpressionUnitsConversion(fromUnit, toUnit, mult, add);

			List<Tuple<string, double>> addList = null;
			switch (attr)
			{
				case ConversionConstantAttr.SIPrefix:
					addList = new List<Tuple<string, double>>
					{
						Tuple.Create("da", 1e1d),
						Tuple.Create("h", 1e2d),
						Tuple.Create("k", 1e3d),
						Tuple.Create("M", 1e6d),
						Tuple.Create("G", 1e9d),
						Tuple.Create("g", 1e9d),
						Tuple.Create("T", 1e12d),
						Tuple.Create("t", 1e12d),
						Tuple.Create("P", 1e15d),
						Tuple.Create("E", 1e18d),
						Tuple.Create("e", 1e18d),
						Tuple.Create("Z", 1e21d),
						Tuple.Create("Y", 1e24d),
						Tuple.Create("d", 1e-1d),
						Tuple.Create("c", 1e-2d),
						Tuple.Create("m", 1e-3d),
						Tuple.Create("µ", 1e-6d),
						Tuple.Create("n", 1e-9d),
						Tuple.Create("p", 1e-12d),
						Tuple.Create("f", 1e-15d),
						Tuple.Create("a", 1e-18d),
						Tuple.Create("z", 1e-21d),
						Tuple.Create("y", 1e-24d),
					};
					break;
				case ConversionConstantAttr.SILongPrefix:
					addList = new List<Tuple<string, double>>
					{
						Tuple.Create("deca", 1e1d),
						Tuple.Create("hecto", 1e2d),
						Tuple.Create("kilo", 1e3d),
						Tuple.Create("mega", 1e6d),
						Tuple.Create("giga", 1e9d),
						Tuple.Create("tera", 1e12d),
						Tuple.Create("peta", 1e15d),
						Tuple.Create("exa", 1e18d),
						Tuple.Create("zetta", 1e21d),
						Tuple.Create("yotta", 1e24d),
						Tuple.Create("deci", 1e-1d),
						Tuple.Create("centi", 1e-2d),
						Tuple.Create("milli", 1e-3d),
						Tuple.Create("micro", 1e-6d),
						Tuple.Create("nano", 1e-9d),
						Tuple.Create("pico", 1e-12d),
						Tuple.Create("femto", 1e-15d),
						Tuple.Create("atto", 1e-18d),
						Tuple.Create("zepto", 1e-21d),
						Tuple.Create("yocto", 1e-24d),
					};
					break;
				case ConversionConstantAttr.CPrefix:
					addList = new List<Tuple<string, double>>
					{
						Tuple.Create("K", 1024d),
						Tuple.Create("k", 1024d),
						Tuple.Create("M", 1048576d),
						Tuple.Create("m", 1048576d),
						Tuple.Create("G", 1073741824d),
						Tuple.Create("g", 1073741824d),
						Tuple.Create("T", 1099511627776d),
						Tuple.Create("t", 1099511627776d),
						Tuple.Create("P", 1125899906842624d),
						Tuple.Create("p", 1125899906842624d),
						Tuple.Create("E", 1152921504606846976d),
						Tuple.Create("e", 1152921504606846976d),
						Tuple.Create("Z", 1180591620717411303424d),
						Tuple.Create("z", 1180591620717411303424d),
						Tuple.Create("Y", 1208925819614629174706176d),
						Tuple.Create("y", 1208925819614629174706176d),
					};
					break;
				case ConversionConstantAttr.CLongPrefix:
					addList = new List<Tuple<string, double>>
					{
						Tuple.Create("kilo", 1024d),
						Tuple.Create("mega", 1048576d),
						Tuple.Create("giga", 1073741824d),
						Tuple.Create("tera", 1099511627776d),
						Tuple.Create("peta", 1125899906842624d),
						Tuple.Create("exa", 1152921504606846976d),
						Tuple.Create("zetta", 1180591620717411303424d),
						Tuple.Create("yotta", 1208925819614629174706176d),
					};
					break;
				default: return;
			}

			foreach (var tuple in addList)
			{
				var newFromUnit = new ExpressionUnits(tuple.Item1 + fromUnitStr);
				conversionConstants[newFromUnit.Single().Unit] = new ExpressionUnitsConversion(newFromUnit, fromUnit, tuple.Item2, add);
			}
		}

		static ExpressionUnitsConversion()
		{
			AddConversionConstant("tick", new ExpressionUnits("seconds"), 1E-07);
			AddConversionConstant("ticks", new ExpressionUnits("seconds"), 1E-07);
			AddConversionConstant("second", new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("seconds", new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("sec", new ExpressionUnits("s"), 1);
			AddConversionConstant("secs", new ExpressionUnits("s"), 1);
			AddConversionConstant("s", new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("minute", new ExpressionUnits("seconds"), 60);
			AddConversionConstant("minutes", new ExpressionUnits("seconds"), 60);
			AddConversionConstant("min", new ExpressionUnits("seconds"), 60);
			AddConversionConstant("mins", new ExpressionUnits("seconds"), 60);
			AddConversionConstant("hour", new ExpressionUnits("minutes"), 60);
			AddConversionConstant("hours", new ExpressionUnits("minutes"), 60);
			AddConversionConstant("hr", new ExpressionUnits("minutes"), 60);
			AddConversionConstant("hrs", new ExpressionUnits("minutes"), 60);
			AddConversionConstant("h", new ExpressionUnits("minutes"), 60);
			AddConversionConstant("day", new ExpressionUnits("hours"), 24);
			AddConversionConstant("days", new ExpressionUnits("hours"), 24);
			AddConversionConstant("week", new ExpressionUnits("days"), 7);
			AddConversionConstant("weeks", new ExpressionUnits("days"), 7);
			AddConversionConstant("wk", new ExpressionUnits("days"), 7);
			AddConversionConstant("wks", new ExpressionUnits("days"), 7);
			AddConversionConstant("fortnight", new ExpressionUnits("weeks"), 2);
			AddConversionConstant("fortnights", new ExpressionUnits("weeks"), 2);
			AddConversionConstant("month", new ExpressionUnits("days"), 30.4368333333333);
			AddConversionConstant("months", new ExpressionUnits("days"), 30.4368333333333);
			AddConversionConstant("mon", new ExpressionUnits("days"), 30.4368333333333);
			AddConversionConstant("mons", new ExpressionUnits("days"), 30.4368333333333);
			AddConversionConstant("year", new ExpressionUnits("days"), 365.2425);
			AddConversionConstant("years", new ExpressionUnits("days"), 365.2425);
			AddConversionConstant("yr", new ExpressionUnits("days"), 365.2425);
			AddConversionConstant("yrs", new ExpressionUnits("days"), 365.2425);
			AddConversionConstant("y", new ExpressionUnits("days"), 365.2425);
			AddConversionConstant("olympiad", new ExpressionUnits("years"), 4);
			AddConversionConstant("olympiads", new ExpressionUnits("years"), 4);
			AddConversionConstant("lustrum", new ExpressionUnits("years"), 5);
			AddConversionConstant("lustrums", new ExpressionUnits("years"), 5);
			AddConversionConstant("indiction", new ExpressionUnits("years"), 15);
			AddConversionConstant("indictions", new ExpressionUnits("years"), 15);
			AddConversionConstant("decade", new ExpressionUnits("years"), 10);
			AddConversionConstant("decades", new ExpressionUnits("years"), 10);
			AddConversionConstant("century", new ExpressionUnits("years"), 100);
			AddConversionConstant("centuries", new ExpressionUnits("years"), 100);
			AddConversionConstant("millennium", new ExpressionUnits("years"), 1000);
			AddConversionConstant("millenniums", new ExpressionUnits("years"), 1000);
			AddConversionConstant("point", new ExpressionUnits("m"), 0.000352777777777778);
			AddConversionConstant("points", new ExpressionUnits("m"), 0.000352777777777778);
			AddConversionConstant("p", new ExpressionUnits("m"), 0.000352777777777778);
			AddConversionConstant("pica", new ExpressionUnits("points"), 12);
			AddConversionConstant("picas", new ExpressionUnits("points"), 12);
			AddConversionConstant("inch", new ExpressionUnits("picas"), 6);
			AddConversionConstant("inches", new ExpressionUnits("picas"), 6);
			AddConversionConstant("in", new ExpressionUnits("picas"), 6);
			AddConversionConstant("foot", new ExpressionUnits("inches"), 12);
			AddConversionConstant("feet", new ExpressionUnits("inches"), 12);
			AddConversionConstant("ft", new ExpressionUnits("inches"), 12);
			AddConversionConstant("yard", new ExpressionUnits("feet"), 3);
			AddConversionConstant("yards", new ExpressionUnits("feet"), 3);
			AddConversionConstant("yd", new ExpressionUnits("feet"), 3);
			AddConversionConstant("yds", new ExpressionUnits("feet"), 3);
			AddConversionConstant("link", new ExpressionUnits("inches"), 7.92);
			AddConversionConstant("links", new ExpressionUnits("inches"), 7.92);
			AddConversionConstant("li", new ExpressionUnits("inches"), 7.92);
			AddConversionConstant("rod", new ExpressionUnits("links"), 25);
			AddConversionConstant("rods", new ExpressionUnits("links"), 25);
			AddConversionConstant("rd", new ExpressionUnits("links"), 25);
			AddConversionConstant("chain", new ExpressionUnits("rods"), 4);
			AddConversionConstant("chains", new ExpressionUnits("rods"), 4);
			AddConversionConstant("ch", new ExpressionUnits("rods"), 4);
			AddConversionConstant("hand", new ExpressionUnits("m"), 0.1016);
			AddConversionConstant("hands", new ExpressionUnits("m"), 0.1016);
			AddConversionConstant("furlong", new ExpressionUnits("chains"), 10);
			AddConversionConstant("furlongs", new ExpressionUnits("chains"), 10);
			AddConversionConstant("fur", new ExpressionUnits("chains"), 10);
			AddConversionConstant("mile", new ExpressionUnits("feet"), 5280);
			AddConversionConstant("miles", new ExpressionUnits("feet"), 5280);
			AddConversionConstant("league", new ExpressionUnits("miles"), 3);
			AddConversionConstant("leagues", new ExpressionUnits("miles"), 3);
			AddConversionConstant("fathom", new ExpressionUnits("yards"), 2);
			AddConversionConstant("fathoms", new ExpressionUnits("yards"), 2);
			AddConversionConstant("ftm", new ExpressionUnits("yards"), 2);
			AddConversionConstant("cable", new ExpressionUnits("fathoms"), 120);
			AddConversionConstant("cables", new ExpressionUnits("fathoms"), 120);
			AddConversionConstant("cb", new ExpressionUnits("fathoms"), 120);
			AddConversionConstant("nmile", new ExpressionUnits("m"), 1852);
			AddConversionConstant("nmiles", new ExpressionUnits("m"), 1852);
			AddConversionConstant("nauticalmile", new ExpressionUnits("m"), 1852);
			AddConversionConstant("nauticalmiles", new ExpressionUnits("m"), 1852);
			AddConversionConstant("NM", new ExpressionUnits("m"), 1852);
			AddConversionConstant("nmi", new ExpressionUnits("m"), 1852);
			AddConversionConstant("knot", new ExpressionUnits("nmiles") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("knots", new ExpressionUnits("nmiles") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("kn", new ExpressionUnits("nmiles") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("meter", new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("meters", new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("metre", new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("metres", new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("m", new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("AU", new ExpressionUnits("m"), 149597870700);
			AddConversionConstant("au", new ExpressionUnits("m"), 149597870700);
			AddConversionConstant("ua", new ExpressionUnits("m"), 149597870700);
			AddConversionConstant("lightsecond", new ExpressionUnits("m"), 299792458);
			AddConversionConstant("lightseconds", new ExpressionUnits("m"), 299792458);
			AddConversionConstant("lightminute", new ExpressionUnits("lightseconds"), 60);
			AddConversionConstant("lightminutes", new ExpressionUnits("lightseconds"), 60);
			AddConversionConstant("lighthour", new ExpressionUnits("lightminutes"), 60);
			AddConversionConstant("lighthours", new ExpressionUnits("lightminutes"), 60);
			AddConversionConstant("lightday", new ExpressionUnits("lighthours"), 24);
			AddConversionConstant("lightdays", new ExpressionUnits("lighthours"), 24);
			AddConversionConstant("lightweek", new ExpressionUnits("lightdays"), 7);
			AddConversionConstant("lightweeks", new ExpressionUnits("lightdays"), 7);
			AddConversionConstant("lightyear", new ExpressionUnits("lightdays"), 365.2425);
			AddConversionConstant("lightyears", new ExpressionUnits("lightdays"), 365.2425);
			AddConversionConstant("lightyr", new ExpressionUnits("lightdays"), 365.2425);
			AddConversionConstant("lightyrs", new ExpressionUnits("lightdays"), 365.2425);
			AddConversionConstant("ly", new ExpressionUnits("lightdays"), 365.2425);
			AddConversionConstant("lightdecade", new ExpressionUnits("lightyears"), 10);
			AddConversionConstant("lightdecades", new ExpressionUnits("lightyears"), 10);
			AddConversionConstant("lightcentury", new ExpressionUnits("lightyears"), 100);
			AddConversionConstant("lightcenturies", new ExpressionUnits("lightyears"), 100);
			AddConversionConstant("lightmillennium", new ExpressionUnits("lightyears"), 1000);
			AddConversionConstant("lightmillenniums", new ExpressionUnits("lightyears"), 1000);
			AddConversionConstant("parsec", new ExpressionUnits("lightyears"), 3.26156);
			AddConversionConstant("parsecs", new ExpressionUnits("lightyears"), 3.26156);
			AddConversionConstant("pc", new ExpressionUnits("lightyears"), 3.26156);
			AddConversionConstant("are", new ExpressionUnits("m") ^ 2, 100);
			AddConversionConstant("ares", new ExpressionUnits("m") ^ 2, 100);
			AddConversionConstant("hectare", new ExpressionUnits("ares"), 100);
			AddConversionConstant("hectares", new ExpressionUnits("ares"), 100);
			AddConversionConstant("acre", new ExpressionUnits("yards") ^ 2, 4840);
			AddConversionConstant("acres", new ExpressionUnits("yards") ^ 2, 4840);
			AddConversionConstant("ac", new ExpressionUnits("yards") ^ 2, 4840);
			AddConversionConstant("section", new ExpressionUnits("acres"), 640);
			AddConversionConstant("sections", new ExpressionUnits("acres"), 640);
			AddConversionConstant("township", new ExpressionUnits("sections"), 36);
			AddConversionConstant("townships", new ExpressionUnits("sections"), 36);
			AddConversionConstant("twp", new ExpressionUnits("sections"), 36);
			AddConversionConstant("liter", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("liters", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("litre", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("litres", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("l", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("L", new ExpressionUnits("m") ^ 3, 0.001, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("minim", new ExpressionUnits("L"), 6.1611519921875E-05);
			AddConversionConstant("minims", new ExpressionUnits("L"), 6.1611519921875E-05);
			AddConversionConstant("fluiddram", new ExpressionUnits("minims"), 60);
			AddConversionConstant("fluiddrams", new ExpressionUnits("minims"), 60);
			AddConversionConstant("fldr", new ExpressionUnits("minims"), 60);
			AddConversionConstant("teaspoon", new ExpressionUnits("minims"), 80);
			AddConversionConstant("teaspoons", new ExpressionUnits("minims"), 80);
			AddConversionConstant("tsp", new ExpressionUnits("minims"), 80);
			AddConversionConstant("tsps", new ExpressionUnits("minims"), 80);
			AddConversionConstant("tablespoon", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("tablespoons", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("tbsp", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("tbsps", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("Tbsp", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("Tbsps", new ExpressionUnits("tsp"), 3);
			AddConversionConstant("fluidounce", new ExpressionUnits("Tbsp"), 2);
			AddConversionConstant("fluidounces", new ExpressionUnits("Tbsp"), 2);
			AddConversionConstant("floz", new ExpressionUnits("Tbsp"), 2);
			AddConversionConstant("shot", new ExpressionUnits("Tbsp"), 3);
			AddConversionConstant("shots", new ExpressionUnits("Tbsp"), 3);
			AddConversionConstant("jib", new ExpressionUnits("Tbsp"), 3);
			AddConversionConstant("gill", new ExpressionUnits("floz"), 4);
			AddConversionConstant("gills", new ExpressionUnits("floz"), 4);
			AddConversionConstant("gi", new ExpressionUnits("floz"), 4);
			AddConversionConstant("cup", new ExpressionUnits("gills"), 2);
			AddConversionConstant("cups", new ExpressionUnits("gills"), 2);
			AddConversionConstant("cp", new ExpressionUnits("gills"), 2);
			AddConversionConstant("pint", new ExpressionUnits("cups"), 2);
			AddConversionConstant("pints", new ExpressionUnits("cups"), 2);
			AddConversionConstant("pt", new ExpressionUnits("cups"), 2);
			AddConversionConstant("quart", new ExpressionUnits("pints"), 2);
			AddConversionConstant("quarts", new ExpressionUnits("pints"), 2);
			AddConversionConstant("qt", new ExpressionUnits("pints"), 2);
			AddConversionConstant("qts", new ExpressionUnits("pints"), 2);
			AddConversionConstant("gallon", new ExpressionUnits("quarts"), 4);
			AddConversionConstant("gallons", new ExpressionUnits("quarts"), 4);
			AddConversionConstant("gal", new ExpressionUnits("quarts"), 4);
			AddConversionConstant("gals", new ExpressionUnits("quarts"), 4);
			AddConversionConstant("barrel", new ExpressionUnits("gal"), 31.5);
			AddConversionConstant("barrels", new ExpressionUnits("gal"), 31.5);
			AddConversionConstant("bbl", new ExpressionUnits("gal"), 31.5);
			AddConversionConstant("bbls", new ExpressionUnits("gal"), 31.5);
			AddConversionConstant("oilbarrel", new ExpressionUnits("gal"), 42);
			AddConversionConstant("oilbarrels", new ExpressionUnits("gal"), 42);
			AddConversionConstant("hogshead", new ExpressionUnits("gal"), 63);
			AddConversionConstant("hogsheads", new ExpressionUnits("gal"), 63);
			AddConversionConstant("peck", new ExpressionUnits("gallons"), 2);
			AddConversionConstant("pecks", new ExpressionUnits("gallons"), 2);
			AddConversionConstant("pk", new ExpressionUnits("gallons"), 2);
			AddConversionConstant("bushel", new ExpressionUnits("pecks"), 4);
			AddConversionConstant("bushels", new ExpressionUnits("pecks"), 4);
			AddConversionConstant("bu", new ExpressionUnits("pecks"), 4);
			AddConversionConstant("gram", new ExpressionUnits("g"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("grams", new ExpressionUnits("g"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("g", new ExpressionUnits("g"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("grain", new ExpressionUnits("g"), 0.06479891);
			AddConversionConstant("grains", new ExpressionUnits("g"), 0.06479891);
			AddConversionConstant("gr", new ExpressionUnits("g"), 0.06479891);
			AddConversionConstant("dram", new ExpressionUnits("grains"), 27.34375);
			AddConversionConstant("drams", new ExpressionUnits("grains"), 27.34375);
			AddConversionConstant("dr", new ExpressionUnits("grains"), 27.34375);
			AddConversionConstant("ounce", new ExpressionUnits("drams"), 16);
			AddConversionConstant("ounces", new ExpressionUnits("drams"), 16);
			AddConversionConstant("oz", new ExpressionUnits("drams"), 16);
			AddConversionConstant("pound", new ExpressionUnits("ounces"), 16);
			AddConversionConstant("pounds", new ExpressionUnits("ounces"), 16);
			AddConversionConstant("lb", new ExpressionUnits("ounces"), 16);
			AddConversionConstant("hundredweight", new ExpressionUnits("pounds"), 100);
			AddConversionConstant("hundredweights", new ExpressionUnits("pounds"), 100);
			AddConversionConstant("cwt", new ExpressionUnits("pounds"), 100);
			AddConversionConstant("longhundredweight", new ExpressionUnits("pounds"), 112);
			AddConversionConstant("longhundredweights", new ExpressionUnits("pounds"), 112);
			AddConversionConstant("ton", new ExpressionUnits("pounds"), 2000);
			AddConversionConstant("tons", new ExpressionUnits("pounds"), 2000);
			AddConversionConstant("shortton", new ExpressionUnits("pounds"), 2000);
			AddConversionConstant("shorttons", new ExpressionUnits("pounds"), 2000);
			AddConversionConstant("longton", new ExpressionUnits("pounds"), 2240);
			AddConversionConstant("longtons", new ExpressionUnits("pounds"), 2240);
			AddConversionConstant("pennyweight", new ExpressionUnits("grains"), 24);
			AddConversionConstant("pennyweights", new ExpressionUnits("grains"), 24);
			AddConversionConstant("dwt", new ExpressionUnits("grains"), 24);
			AddConversionConstant("troyounce", new ExpressionUnits("pennyweights"), 20);
			AddConversionConstant("troyounces", new ExpressionUnits("pennyweights"), 20);
			AddConversionConstant("ozt", new ExpressionUnits("pennyweights"), 20);
			AddConversionConstant("troypound", new ExpressionUnits("troyounces"), 12);
			AddConversionConstant("troypounds", new ExpressionUnits("troyounces"), 12);
			AddConversionConstant("lbt", new ExpressionUnits("troyounces"), 12);
			AddConversionConstant("celsius", new ExpressionUnits("K"), 1, 273.15);
			AddConversionConstant("centigrade", new ExpressionUnits("K"), 1, 273.15);
			AddConversionConstant("degc", new ExpressionUnits("K"), 1, 273.15);
			AddConversionConstant("fahrenheit", new ExpressionUnits("K"), 5d / 9, 273.15 - 32d * 5 / 9);
			AddConversionConstant("degf", new ExpressionUnits("K"), 5d / 9, 273.15 - 32d * 5 / 9);
			AddConversionConstant("kelvin", new ExpressionUnits("K"), 1);
			AddConversionConstant("degk", new ExpressionUnits("K"), 1);
			AddConversionConstant("K", new ExpressionUnits("K"), 1);
			AddConversionConstant("ampere", new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("amperes", new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("A", new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("mole", new ExpressionUnits("mol"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("moles", new ExpressionUnits("mol"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("mol", new ExpressionUnits("mol"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("candela", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("candelas", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("cd", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("britishthermalunit", new ExpressionUnits("J"), 1055);
			AddConversionConstant("britishthermalunits", new ExpressionUnits("J"), 1055);
			AddConversionConstant("btu", new ExpressionUnits("J"), 1055);
			AddConversionConstant("btus", new ExpressionUnits("J"), 1055);
			AddConversionConstant("calorie", new ExpressionUnits("J"), 4.184);
			AddConversionConstant("calories", new ExpressionUnits("J"), 4.184);
			AddConversionConstant("cal", new ExpressionUnits("J"), 4.184);
			AddConversionConstant("cals", new ExpressionUnits("J"), 4.184);
			AddConversionConstant("foodcalorie", new ExpressionUnits("J"), 4184);
			AddConversionConstant("foodcalories", new ExpressionUnits("J"), 4184);
			AddConversionConstant("kcal", new ExpressionUnits("J"), 4184);
			AddConversionConstant("kcals", new ExpressionUnits("J"), 4184);
			AddConversionConstant("Cal", new ExpressionUnits("J"), 4184);
			AddConversionConstant("Cals", new ExpressionUnits("J"), 4184);
			AddConversionConstant("footpound", new ExpressionUnits("J"), 1.356);
			AddConversionConstant("footpounds", new ExpressionUnits("J"), 1.356);
			AddConversionConstant("horsepower", new ExpressionUnits("W"), 745.7);
			AddConversionConstant("hp", new ExpressionUnits("W"), 745.7);
			AddConversionConstant("rackunit", new ExpressionUnits("inches"), 1.75);
			AddConversionConstant("rackunits", new ExpressionUnits("inches"), 1.75);
			AddConversionConstant("U", new ExpressionUnits("inches"), 1.75);
			AddConversionConstant("hertz", new ExpressionUnits() / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("hz", new ExpressionUnits() / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("Hz", new ExpressionUnits() / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("newton", new ExpressionUnits("m") * new ExpressionUnits("kg") / (new ExpressionUnits("s") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("newtons", new ExpressionUnits("m") * new ExpressionUnits("kg") / (new ExpressionUnits("s") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("N", new ExpressionUnits("m") * new ExpressionUnits("kg") / (new ExpressionUnits("s") ^ 2), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("pascal", new ExpressionUnits("N") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("pascals", new ExpressionUnits("N") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("Pa", new ExpressionUnits("N") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("joule", new ExpressionUnits("N") * new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("joules", new ExpressionUnits("N") * new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("J", new ExpressionUnits("N") * new ExpressionUnits("m"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("watt", new ExpressionUnits("J") / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("watts", new ExpressionUnits("J") / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("W", new ExpressionUnits("J") / new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("coulomb", new ExpressionUnits("s") * new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("coulombs", new ExpressionUnits("s") * new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("C", new ExpressionUnits("s") * new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("volt", new ExpressionUnits("W") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("volts", new ExpressionUnits("W") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("V", new ExpressionUnits("W") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("farad", new ExpressionUnits("C") / new ExpressionUnits("V"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("farads", new ExpressionUnits("C") / new ExpressionUnits("V"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("F", new ExpressionUnits("C") / new ExpressionUnits("V"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("ohm", new ExpressionUnits("V") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("ohms", new ExpressionUnits("V") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("O", new ExpressionUnits("V") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("siemens", new ExpressionUnits("A") / new ExpressionUnits("V"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("S", new ExpressionUnits("A") / new ExpressionUnits("V"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("weber", new ExpressionUnits("V") * new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("webers", new ExpressionUnits("V") * new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("Wb", new ExpressionUnits("V") * new ExpressionUnits("s"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("tesla", new ExpressionUnits("Wb") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("teslas", new ExpressionUnits("Wb") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("T", new ExpressionUnits("Wb") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("henry", new ExpressionUnits("Wb") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("henries", new ExpressionUnits("Wb") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("H", new ExpressionUnits("Wb") / new ExpressionUnits("A"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("lumen", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("lumens", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("lm", new ExpressionUnits("cd"), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("lux", new ExpressionUnits("lm") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SILongPrefix);
			AddConversionConstant("lx", new ExpressionUnits("lm") / (new ExpressionUnits("m") ^ 2), 1, attr: ConversionConstantAttr.SIPrefix);
			AddConversionConstant("radian", new ExpressionUnits("rad"), 1);
			AddConversionConstant("radians", new ExpressionUnits("rad"), 1);
			AddConversionConstant("rad", new ExpressionUnits("rad"), 1);
			AddConversionConstant("degree", new ExpressionUnits("radians"), 0.0174532925199433);
			AddConversionConstant("degrees", new ExpressionUnits("radians"), 0.0174532925199433);
			AddConversionConstant("deg", new ExpressionUnits("radians"), 0.0174532925199433);
			AddConversionConstant("byte", new ExpressionUnits("b"), 1, attr: ConversionConstantAttr.CLongPrefix);
			AddConversionConstant("bytes", new ExpressionUnits("b"), 1, attr: ConversionConstantAttr.CLongPrefix);
			AddConversionConstant("b", new ExpressionUnits("b"), 1, attr: ConversionConstantAttr.CPrefix);
			AddConversionConstant("B", new ExpressionUnits("b"), 1, attr: ConversionConstantAttr.CPrefix);
			AddConversionConstant("MPH", new ExpressionUnits("miles") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("mph", new ExpressionUnits("miles") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("KPH", new ExpressionUnits("km") / new ExpressionUnits("hour"), 1);
			AddConversionConstant("kph", new ExpressionUnits("km") / new ExpressionUnits("hour"), 1);

			// Set kg as base unit instead of g
			var gValue = conversionConstants["g"];
			var kgValue = conversionConstants["kg"];
			conversionConstants["g"] = new ExpressionUnitsConversion(gValue.fromUnits, kgValue.fromUnits, gValue.mult / kgValue.mult, 0);
			conversionConstants["kg"] = new ExpressionUnitsConversion(kgValue.fromUnits, kgValue.fromUnits, 1, 0);

			SetConstantBaseUnits();
			ValidateData();
		}

		static void SetConstantBaseUnits()
		{
			var done = new HashSet<string>();
			while (true)
			{
				var stop = true;
				foreach (var pair in conversionConstants.ToList())
				{
					var unitStr = pair.Key;
					if (done.Contains(unitStr))
						continue;

					var baseConv = GetBaseConversion(pair.Value.toUnits);
					if (pair.Value.toUnits.Equals(baseConv.toUnits))
					{
						done.Add(unitStr);
						continue;
					}

					conversionConstants[pair.Key] = pair.Value * baseConv;
					stop = false;
				}
				if (stop)
					break;
			}
		}

		static void ValidateData()
		{
			var repeats = conversionConstants.Values.GroupBy(conv => conv.fromUnits.ToString()).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (repeats.Any())
				throw new Exception($"Units repeated: {String.Join(", ", repeats)}");
			var invalidBase = conversionConstants.Values.Where(conv => conv.fromUnits.Equals(conv.toUnits)).Where(conv => (conv.mult != 1) || (conv.add != 0)).Select(conv => conv.fromUnits.Single().Unit).ToList();
			if (invalidBase.Any())
				throw new Exception($"Unit has no base: {String.Join(", ", invalidBase)}");
		}

		public static ExpressionUnitsConversion GetBaseConversion(ExpressionUnits units)
		{
			var conversion = new ExpressionUnitsConversion(units);
			foreach (var unit in units)
			{
				var unitConversion = conversionConstants.ContainsKey(unit.Unit) ? conversionConstants[unit.Unit] : new ExpressionUnitsConversion(new ExpressionUnits(unit.Unit));
				conversion *= unitConversion ^ unit.Exp;
			}
			return conversion;
		}

		public static ExpressionUnitsConversion GetConversion(ExpressionUnits units1, ExpressionUnits units2)
		{
			var conversion1 = ExpressionUnitsConversion.GetBaseConversion(units1);
			var conversion2 = ExpressionUnitsConversion.GetBaseConversion(units2.IsSI ? conversion1.toUnits : units2.IsSimple ? ExpressionUnitsConversion.GetSimple(conversion1.toUnits) : units2);

			if (!conversion1.toUnits.Equals(conversion2.toUnits))
				throw new Exception("Cannot convert units");

			return conversion1 / conversion2;
		}

		public static ExpressionUnits GetSimple(ExpressionUnits units)
		{
			var match = conversionConstants.Values.Where(pair => pair.toUnits.Equals(units)).OrderBy(pair => pair.fromUnits.ToString().Length).FirstOrDefault();
			if (match == null)
				return units;
			return match.fromUnits;
		}

		public bool Equals(ExpressionUnitsConversion obj) => (fromUnits.Equals(obj.fromUnits)) && (toUnits.Equals(obj.toUnits)) && (mult == obj.mult) && (add == obj.add);
	}
}
