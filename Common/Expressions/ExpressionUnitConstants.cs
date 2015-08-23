using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class ExpressionUnitConstants
	{
		[Flags]
		enum TypeAttrs
		{
			None = 0,
			Primary = 1,
			SIPrefix = 2,
			SILongPrefix = 4,
			CPrefix = 8,
			CLongPrefix = 16,
		}

		class ConstantData
		{
			public readonly string singular, plural;
			public readonly TypeAttrs attrs;

			public ConstantData(string name, TypeAttrs attrs = TypeAttrs.None) : this(name, name, attrs) { }
			public ConstantData(string singular, string plural, TypeAttrs attrs = TypeAttrs.None)
			{
				this.singular = singular;
				this.plural = plural;
				this.attrs = attrs;
			}
		}

		class UnitData
		{
			public readonly double conversion;
			public readonly ExpressionUnits units;
			public readonly bool isBase;
			public ExpressionUnits baseUnits;
			public readonly Func<ExpressionResult, ExpressionResult> toBase, fromBase;

			public UnitData(double conversion, ExpressionUnits units, bool isBase = false)
			{
				this.conversion = conversion;
				this.units = units;
				this.isBase = isBase;
			}

			public UnitData(Func<ExpressionResult, ExpressionResult> toBase, Func<ExpressionResult, ExpressionResult> fromBase)
			{
				this.toBase = toBase;
				this.fromBase = fromBase;
				this.isBase = true;
			}
		}

		static Dictionary<ConstantData, UnitData> data = new Dictionary<ConstantData, UnitData>();

		static ExpressionUnitConstants()
		{
			data[new ConstantData("tick", "ticks", TypeAttrs.Primary)] = new UnitData(1E-07, new ExpressionUnits("seconds"));
			data[new ConstantData("second", "seconds", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("sec", "secs")] =
			data[new ConstantData("s", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("s"), true);
			data[new ConstantData("minute", "minutes")] =
			data[new ConstantData("min", "mins", TypeAttrs.Primary)] = new UnitData(60, new ExpressionUnits("seconds"));
			data[new ConstantData("hour", "hours")] =
			data[new ConstantData("hr", "hrs")] =
			data[new ConstantData("h", TypeAttrs.Primary)] = new UnitData(60, new ExpressionUnits("minutes"));
			data[new ConstantData("day", "days", TypeAttrs.Primary)] = new UnitData(24, new ExpressionUnits("hours"));
			data[new ConstantData("week", "weeks")] =
			data[new ConstantData("wk", "wks", TypeAttrs.Primary)] = new UnitData(7, new ExpressionUnits("days"));
			data[new ConstantData("fortnight", "fortnights", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("weeks"));
			data[new ConstantData("month", "months")] =
			data[new ConstantData("mon", "mons", TypeAttrs.Primary)] = new UnitData(30.4368333333333, new ExpressionUnits("days"));
			data[new ConstantData("year", "years")] =
			data[new ConstantData("yr", "yrs")] =
			data[new ConstantData("y", TypeAttrs.Primary)] = new UnitData(365.242, new ExpressionUnits("days"));
			data[new ConstantData("olympiad", "olympiads", TypeAttrs.Primary)] = new UnitData(4, new ExpressionUnits("years"));
			data[new ConstantData("lustrum", "lustrums", TypeAttrs.Primary)] = new UnitData(5, new ExpressionUnits("years"));
			data[new ConstantData("indiction", "indictions", TypeAttrs.Primary)] = new UnitData(15, new ExpressionUnits("years"));
			data[new ConstantData("decade", "decades", TypeAttrs.Primary)] = new UnitData(10, new ExpressionUnits("years"));
			data[new ConstantData("century", "centuries", TypeAttrs.Primary)] = new UnitData(100, new ExpressionUnits("years"));
			data[new ConstantData("millennium", "millenniums", TypeAttrs.Primary)] = new UnitData(1000, new ExpressionUnits("years"));
			data[new ConstantData("point", "points")] =
			data[new ConstantData("p", TypeAttrs.Primary)] = new UnitData(0.000352777777777778, new ExpressionUnits("m"));
			data[new ConstantData("pica", "picas", TypeAttrs.Primary)] = new UnitData(12, new ExpressionUnits("points"));
			data[new ConstantData("inch", "inches")] =
			data[new ConstantData("in", TypeAttrs.Primary)] = new UnitData(6, new ExpressionUnits("picas"));
			data[new ConstantData("foot", "feet")] =
			data[new ConstantData("ft", TypeAttrs.Primary)] = new UnitData(12, new ExpressionUnits("inches"));
			data[new ConstantData("yard", "yards")] =
			data[new ConstantData("yd", "yds", TypeAttrs.Primary)] = new UnitData(3, new ExpressionUnits("feet"));
			data[new ConstantData("link", "links")] =
			data[new ConstantData("li", TypeAttrs.Primary)] = new UnitData(7.92, new ExpressionUnits("inches"));
			data[new ConstantData("rod", "rods")] =
			data[new ConstantData("rd", TypeAttrs.Primary)] = new UnitData(25, new ExpressionUnits("links"));
			data[new ConstantData("chain", "chains")] =
			data[new ConstantData("ch", TypeAttrs.Primary)] = new UnitData(4, new ExpressionUnits("rods"));
			data[new ConstantData("hand", "hands", TypeAttrs.Primary)] = new UnitData(0.1016, new ExpressionUnits("m"));
			data[new ConstantData("furlong", "furlongs")] =
			data[new ConstantData("fur", TypeAttrs.Primary)] = new UnitData(10, new ExpressionUnits("chains"));
			data[new ConstantData("mile", "miles", TypeAttrs.Primary)] = new UnitData(5280, new ExpressionUnits("feet"));
			data[new ConstantData("league", "leagues", TypeAttrs.Primary)] = new UnitData(3, new ExpressionUnits("miles"));
			data[new ConstantData("fathom", "fathoms")] =
			data[new ConstantData("ftm", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("yards"));
			data[new ConstantData("cable", "cables")] =
			data[new ConstantData("cb", TypeAttrs.Primary)] = new UnitData(120, new ExpressionUnits("fathoms"));
			data[new ConstantData("nmile", "nmiles")] =
			data[new ConstantData("nauticalmile", "nauticalmiles")] =
			data[new ConstantData("NM")] =
			data[new ConstantData("nmi", TypeAttrs.Primary)] = new UnitData(1852, new ExpressionUnits("m"));
			data[new ConstantData("meter", "meters", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("metre", "metres", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("m", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("m"), true);
			data[new ConstantData("AU")] =
			data[new ConstantData("au")] =
			data[new ConstantData("ua", TypeAttrs.Primary)] = new UnitData(149597870700, new ExpressionUnits("m"));
			data[new ConstantData("lightsecond", "lightseconds", TypeAttrs.Primary)] = new UnitData(299792458, new ExpressionUnits("m"));
			data[new ConstantData("lightminute", "lightminutes", TypeAttrs.Primary)] = new UnitData(60, new ExpressionUnits("lightseconds"));
			data[new ConstantData("lighthour", "lighthours", TypeAttrs.Primary)] = new UnitData(60, new ExpressionUnits("lightminutes"));
			data[new ConstantData("lightday", "lightdays", TypeAttrs.Primary)] = new UnitData(24, new ExpressionUnits("lighthours"));
			data[new ConstantData("lightweek", "lightweeks", TypeAttrs.Primary)] = new UnitData(7, new ExpressionUnits("lightdays"));
			data[new ConstantData("lightyear", "lightyears")] =
			data[new ConstantData("lightyr", "lightyrs")] =
			data[new ConstantData("ly", TypeAttrs.Primary)] = new UnitData(365.242, new ExpressionUnits("lightdays"));
			data[new ConstantData("lightdecade", "lightdecades", TypeAttrs.Primary)] = new UnitData(10, new ExpressionUnits("lightyears"));
			data[new ConstantData("lightcentury", "lightcenturies", TypeAttrs.Primary)] = new UnitData(100, new ExpressionUnits("lightyears"));
			data[new ConstantData("lightmillennium", "lightmillenniums", TypeAttrs.Primary)] = new UnitData(1000, new ExpressionUnits("lightyears"));
			data[new ConstantData("parsec", "parsecs")] =
			data[new ConstantData("pc", TypeAttrs.Primary)] = new UnitData(3.26156, new ExpressionUnits("lightyears"));
			data[new ConstantData("are", "ares", TypeAttrs.Primary)] = new UnitData(100, new ExpressionUnits("m") ^ 2);
			data[new ConstantData("hectare", "hectares", TypeAttrs.Primary)] = new UnitData(100, new ExpressionUnits("ares"));
			data[new ConstantData("acre", "acres")] =
			data[new ConstantData("ac", TypeAttrs.Primary)] = new UnitData(4840, new ExpressionUnits("yards") ^ 2);
			data[new ConstantData("section", "sections", TypeAttrs.Primary)] = new UnitData(640, new ExpressionUnits("acres"));
			data[new ConstantData("township", "townships")] =
			data[new ConstantData("twp", TypeAttrs.Primary)] = new UnitData(36, new ExpressionUnits("sections"));
			data[new ConstantData("liter", "liters", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("litre", "litres", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("l", TypeAttrs.SIPrefix)] =
			data[new ConstantData("L", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(0.001, new ExpressionUnits("m") ^ 3);
			data[new ConstantData("minim", "minims", TypeAttrs.Primary)] = new UnitData(6.1611519921875E-05, new ExpressionUnits("L"));
			data[new ConstantData("fluiddram", "fluiddrams")] =
			data[new ConstantData("fldr", TypeAttrs.Primary)] = new UnitData(60, new ExpressionUnits("minims"));
			data[new ConstantData("teaspoon", "teaspoons")] =
			data[new ConstantData("tsp", "tsps", TypeAttrs.Primary)] = new UnitData(80, new ExpressionUnits("minims"));
			data[new ConstantData("tablespoon", "tablespoons")] =
			data[new ConstantData("tbsp", "tbsps")] =
			data[new ConstantData("Tbsp", "Tbsps", TypeAttrs.Primary)] = new UnitData(3, new ExpressionUnits("tsp"));
			data[new ConstantData("fluidounce", "fluidounces")] =
			data[new ConstantData("floz", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("Tbsp"));
			data[new ConstantData("shot", "shots")] =
			data[new ConstantData("jib", TypeAttrs.Primary)] = new UnitData(3, new ExpressionUnits("Tbsp"));
			data[new ConstantData("gill", "gills")] =
			data[new ConstantData("gi", TypeAttrs.Primary)] = new UnitData(4, new ExpressionUnits("floz"));
			data[new ConstantData("cup", "cups")] =
			data[new ConstantData("cp", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("gills"));
			data[new ConstantData("pint", "pints")] =
			data[new ConstantData("pt", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("cups"));
			data[new ConstantData("quart", "quarts")] =
			data[new ConstantData("qt", "qts", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("pints"));
			data[new ConstantData("gallon", "gallons")] =
			data[new ConstantData("gal", "gals", TypeAttrs.Primary)] = new UnitData(4, new ExpressionUnits("quarts"));
			data[new ConstantData("barrel", "barrels")] =
			data[new ConstantData("bbl", "bbls", TypeAttrs.Primary)] = new UnitData(31.5, new ExpressionUnits("gal"));
			data[new ConstantData("oilbarrel", "oilbarrels", TypeAttrs.Primary)] = new UnitData(42, new ExpressionUnits("gal"));
			data[new ConstantData("hogshead", "hogsheads", TypeAttrs.Primary)] = new UnitData(63, new ExpressionUnits("gal"));
			data[new ConstantData("peck", "pecks")] =
			data[new ConstantData("pk", TypeAttrs.Primary)] = new UnitData(2, new ExpressionUnits("gallons"));
			data[new ConstantData("bushel", "bushels")] =
			data[new ConstantData("bu", TypeAttrs.Primary)] = new UnitData(4, new ExpressionUnits("pecks"));
			data[new ConstantData("gram", "grams", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("g", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("g"), true);
			data[new ConstantData("grain", "grains")] =
			data[new ConstantData("gr", TypeAttrs.Primary)] = new UnitData(0.06479891, new ExpressionUnits("g"));
			data[new ConstantData("dram", "drams")] =
			data[new ConstantData("dr", TypeAttrs.Primary)] = new UnitData(27.34375, new ExpressionUnits("grains"));
			data[new ConstantData("ounce", "ounces")] =
			data[new ConstantData("oz", TypeAttrs.Primary)] = new UnitData(16, new ExpressionUnits("drams"));
			data[new ConstantData("pound", "pounds")] =
			data[new ConstantData("lb", TypeAttrs.Primary)] = new UnitData(16, new ExpressionUnits("ounces"));
			data[new ConstantData("hundredweight", "hundredweights")] =
			data[new ConstantData("cwt", TypeAttrs.Primary)] = new UnitData(100, new ExpressionUnits("pounds"));
			data[new ConstantData("longhundredweight", "longhundredweights", TypeAttrs.Primary)] = new UnitData(112, new ExpressionUnits("pounds"));
			data[new ConstantData("ton", "tons")] =
			data[new ConstantData("shortton", "shorttons", TypeAttrs.Primary)] = new UnitData(2000, new ExpressionUnits("pounds"));
			data[new ConstantData("longton", "longtons", TypeAttrs.Primary)] = new UnitData(2240, new ExpressionUnits("pounds"));
			data[new ConstantData("pennyweight", "pennyweights")] =
			data[new ConstantData("dwt", TypeAttrs.Primary)] = new UnitData(24, new ExpressionUnits("grains"));
			data[new ConstantData("troyounce", "troyounces")] =
			data[new ConstantData("ozt", TypeAttrs.Primary)] = new UnitData(20, new ExpressionUnits("pennyweights"));
			data[new ConstantData("troypound", "troypounds")] =
			data[new ConstantData("lbt", TypeAttrs.Primary)] = new UnitData(12, new ExpressionUnits("troyounces"));
			data[new ConstantData("celsius")] =
			data[new ConstantData("centigrade")] =
			data[new ConstantData("degc", TypeAttrs.Primary)] = new UnitData(
				a => (a + new ExpressionResult(273.15, a)) * new ExpressionResult(1, new ExpressionResult(1, new ExpressionUnits("K")) / new ExpressionResult(1, a)),
				a => (a - new ExpressionResult(273.15, a)) * new ExpressionResult(1, new ExpressionResult(1, new ExpressionUnits("degc")) / new ExpressionResult(1, a))
			);
			data[new ConstantData("fahrenheit")] =
			data[new ConstantData("degf", TypeAttrs.Primary)] = new UnitData(
				a => (a + new ExpressionResult(459.67, a)) * new ExpressionResult(5d / 9, new ExpressionResult(1, new ExpressionUnits("K")) / new ExpressionResult(1, a)),
				a => a * new ExpressionResult(9d / 5, new ExpressionResult(1, new ExpressionUnits("degf")) / new ExpressionResult(1, a)) - new ExpressionResult(459.67, new ExpressionUnits("degf"))
			);
			data[new ConstantData("kelvin")] =
			data[new ConstantData("degk")] =
			data[new ConstantData("K", TypeAttrs.Primary)] = new UnitData(a => a, a => a);
			data[new ConstantData("ampere", "amperes", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("A", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("A"), true);
			data[new ConstantData("mole", "moles", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("mol", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("mol"), true);
			data[new ConstantData("candela", "candelas", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("cd", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("cd"), true);
			data[new ConstantData("britishthermalunit", "britishthermalunits")] =
			data[new ConstantData("btu", "btus", TypeAttrs.Primary)] = new UnitData(1055, new ExpressionUnits("J"));
			data[new ConstantData("calorie", "calories")] =
			data[new ConstantData("cal", "cals", TypeAttrs.Primary)] = new UnitData(4.184, new ExpressionUnits("J"));
			data[new ConstantData("foodcalorie", "foodcalories")] =
			data[new ConstantData("kcal", "kcals")] =
			data[new ConstantData("Cal", "Cals", TypeAttrs.Primary)] = new UnitData(4184, new ExpressionUnits("J"));
			data[new ConstantData("footpound", "footpounds", TypeAttrs.Primary)] = new UnitData(1.356, new ExpressionUnits("J"));
			data[new ConstantData("horsepower")] =
			data[new ConstantData("hp", TypeAttrs.Primary)] = new UnitData(745.7, new ExpressionUnits("W"));
			data[new ConstantData("rackunit", "rackunits")] =
			data[new ConstantData("U", TypeAttrs.Primary)] = new UnitData(1.75, new ExpressionUnits("inches"));
			data[new ConstantData("hertz", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("hz", TypeAttrs.SIPrefix)] =
			data[new ConstantData("Hz", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits() / new ExpressionUnits("s"));
			data[new ConstantData("newton", "newtons", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("N", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("m") * new ExpressionUnits("kg") / (new ExpressionUnits("s") ^ 2));
			data[new ConstantData("pascal", "pascals", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("Pa", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("N") / (new ExpressionUnits("m") ^ 2));
			data[new ConstantData("joule", "joules", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("J", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("N") * new ExpressionUnits("m"));
			data[new ConstantData("watt", "watts", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("W", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("J") / new ExpressionUnits("s"));
			data[new ConstantData("coulomb", "coulombs", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("C", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("s") * new ExpressionUnits("A"));
			data[new ConstantData("volt", "volts", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("V", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("W") / new ExpressionUnits("A"));
			data[new ConstantData("farad", "farads", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("F", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("C") / new ExpressionUnits("V"));
			data[new ConstantData("ohm", "ohms", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("O", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("V") / new ExpressionUnits("A"));
			data[new ConstantData("siemens", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("S", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("A") / new ExpressionUnits("V"));
			data[new ConstantData("weber", "webers", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("Wb", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("V") * new ExpressionUnits("s"));
			data[new ConstantData("tesla", "teslas", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("T", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("Wb") / (new ExpressionUnits("m") ^ 2));
			data[new ConstantData("henry", "henries", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("H", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("Wb") / new ExpressionUnits("A"));
			data[new ConstantData("lumen", "lumens", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("lm", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("cd"));
			data[new ConstantData("lux", TypeAttrs.SILongPrefix)] =
			data[new ConstantData("lx", TypeAttrs.Primary | TypeAttrs.SIPrefix)] = new UnitData(1, new ExpressionUnits("lm") / (new ExpressionUnits("m") ^ 2));
			data[new ConstantData("radian", "radians")] =
			data[new ConstantData("rad", TypeAttrs.Primary)] = new UnitData(1, new ExpressionUnits("rad"), true);
			data[new ConstantData("degree", "degrees")] =
			data[new ConstantData("deg", TypeAttrs.Primary)] = new UnitData(0.0174532925199433, new ExpressionUnits("radians"));
			data[new ConstantData("byte", "bytes", TypeAttrs.CLongPrefix)] =
			data[new ConstantData("b", TypeAttrs.Primary | TypeAttrs.CPrefix)] =
			data[new ConstantData("B", TypeAttrs.Primary | TypeAttrs.CPrefix)] = new UnitData(1, new ExpressionUnits("B"), true);
			data[new ConstantData("MPH")] =
			data[new ConstantData("mph", TypeAttrs.Primary)] = new UnitData(1, new ExpressionUnits("miles") / new ExpressionUnits("hour"));
			data[new ConstantData("KPH")] =
			data[new ConstantData("kph", TypeAttrs.Primary)] = new UnitData(1, new ExpressionUnits("km") / new ExpressionUnits("hour"));

			AddPrefixes();

			SetKGAsSI();

			SetBaseUnits();

			ValidateData();
		}

		static void AddPrefixes()
		{
			var siPrefixes = new List<Tuple<string, double>>
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

			var siLongPrefixes = new List<Tuple<string, double>>
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

			var cPrefixes = new List<Tuple<string, double>>
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

			var cLongPrefixes = new List<Tuple<string, double>>
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

			foreach (var item in data.ToList())
			{
				List<Tuple<string, double>> addList = null;
				if (item.Key.attrs.HasFlag(TypeAttrs.SIPrefix))
					addList = siPrefixes;
				else if (item.Key.attrs.HasFlag(TypeAttrs.SILongPrefix))
					addList = siLongPrefixes;
				else if (item.Key.attrs.HasFlag(TypeAttrs.CPrefix))
					addList = cPrefixes;
				else if (item.Key.attrs.HasFlag(TypeAttrs.CLongPrefix))
					addList = cLongPrefixes;
				else
					continue;

				foreach (var tuple in addList)
					data.Add(new ConstantData(tuple.Item1 + item.Key.singular, tuple.Item1 + item.Key.plural, item.Key.attrs), new UnitData(tuple.Item2, new ExpressionUnits(item.Key.plural)));
			}
		}

		static void SetKGAsSI()
		{
			data[data.Single(pair => pair.Key.singular == "g").Key] =
			data[data.Single(pair => pair.Key.singular == "gram").Key] = new UnitData(.001, new ExpressionUnits("kg"));
			data[data.Single(pair => pair.Key.singular == "kg").Key] =
			data[data.Single(pair => pair.Key.singular == "kilogram").Key] = new UnitData(1, new ExpressionUnits("kg"), true);
		}

		static void SetBaseUnits()
		{
			foreach (var value in data.Values.Distinct().Where(value => value.units != null))
			{
				double conversion;
				value.units.GetConversion(out conversion, out value.baseUnits);
			}
		}

		static void ValidateData()
		{
			var units = data.SelectMany(item => new List<string> { item.Key.singular, item.Key.singular == item.Key.plural ? null : item.Key.plural }).Where(unit => unit != null).ToList();
			var repeats = units.GroupBy(unit => unit).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (repeats.Any())
				throw new Exception("Units repeated: " + String.Join(", ", repeats));
		}

		public static bool GetConversion(string unit, out double conversion, out ExpressionUnits units)
		{
			var found = data.Where(pair => (pair.Key.singular == unit) || (pair.Key.plural == unit)).Select(pair => pair.Value).FirstOrDefault();
			if (found == null)
			{
				conversion = 1;
				units = new ExpressionUnits(unit);
				return false;
			}
			conversion = found.conversion;
			units = found.units;
			return !found.isBase;
		}

		public static ExpressionUnits GetSimple(ExpressionUnits units)
		{
			var matches = data.Where(pair => pair.Value.baseUnits == units).OrderBy(pair => !pair.Key.attrs.HasFlag(TypeAttrs.SIPrefix)).ToList();
			if (!matches.Any())
				return units;
			return new ExpressionUnits(matches.First().Key.plural);
		}

		public static Func<ExpressionResult, ExpressionResult> GetToBase(string unit)
		{
			var found = data.Where(pair => (pair.Key.singular == unit) || (pair.Key.plural == unit)).Select(pair => pair.Value).FirstOrDefault();
			if (found == null)
				return null;
			return found.toBase;
		}

		public static Func<ExpressionResult, ExpressionResult> GetFromBase(string unit)
		{
			var found = data.Where(pair => (pair.Key.singular == unit) || (pair.Key.plural == unit)).Select(pair => pair.Value).FirstOrDefault();
			if (found == null)
				return null;
			return found.fromBase;
		}
	}
}
