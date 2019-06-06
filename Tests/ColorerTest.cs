using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Transform;

namespace NeoEdit.Tests
{
	public partial class UnitTest
	{
		void ValueToARGB(uint value, byte alpha, byte red, byte green, byte blue)
		{
			Colorer.ValueToARGB(value, out var alphaOut, out var redOut, out var greenOut, out var blueOut);
			Assert.AreEqual(alpha, alphaOut);
			Assert.AreEqual(red, redOut);
			Assert.AreEqual(green, greenOut);
			Assert.AreEqual(blue, blueOut);
		}

		void StringToARGB(string value, byte alpha, byte red, byte green, byte blue)
		{
			Colorer.StringToARGB(value, out var alphaOut, out var redOut, out var greenOut, out var blueOut);
			Assert.AreEqual(alpha, alphaOut);
			Assert.AreEqual(red, redOut);
			Assert.AreEqual(green, greenOut);
			Assert.AreEqual(blue, blueOut);
		}

		[TestMethod]
		public void ColorerTest()
		{
			// StringToString
			Assert.AreEqual(Colorer.StringToString("a"), "ffaaaaaa");
			Assert.AreEqual(Colorer.StringToString("AB"), "ffababab");
			Assert.AreEqual(Colorer.StringToString("cD0"), "ffccdd00");
			Assert.AreEqual(Colorer.StringToString("C0DE"), "cc00ddee");
			try { Colorer.StringToString("ad0be"); Assert.Fail(); } catch { }
			Assert.AreEqual(Colorer.StringToString("dEc0dE"), "ffdec0de");
			try { Colorer.StringToString("dec0ded"); Assert.Fail(); } catch { }
			Assert.AreEqual(Colorer.StringToString("F00dFaCe"), "f00dface");
			try { Colorer.StringToString("coder"); Assert.Fail(); } catch { }
			try { Colorer.StringToString("deadbeeff00d"); Assert.Fail(); } catch { }

			// StringToValue
			Assert.AreEqual(Colorer.StringToValue("a"), 0xffaaaaaa);
			Assert.AreEqual(Colorer.StringToValue("AB"), 0xffababab);
			Assert.AreEqual(Colorer.StringToValue("cD0"), 0xffccdd00);
			Assert.AreEqual(Colorer.StringToValue("C0DE"), 0xcc00ddee);
			try { Colorer.StringToValue("ad0be"); Assert.Fail(); } catch { }
			Assert.AreEqual(Colorer.StringToValue("dEc0dE"), 0xffdec0de);
			try { Colorer.StringToValue("dec0ded"); Assert.Fail(); } catch { }
			Assert.AreEqual(Colorer.StringToValue("F00dFaCe"), 0xf00dface);
			try { Colorer.StringToValue("coder"); Assert.Fail(); } catch { }
			try { Colorer.StringToValue("deadbeeff00d"); Assert.Fail(); } catch { }

			// ValueToString
			Assert.AreEqual(Colorer.ValueToString(0xffaaaaaa), "ffaaaaaa");
			Assert.AreEqual(Colorer.ValueToString(0xffababab), "ffababab");
			Assert.AreEqual(Colorer.ValueToString(0xffccdd00), "ffccdd00");
			Assert.AreEqual(Colorer.ValueToString(0xcc00ddee), "cc00ddee");
			Assert.AreEqual(Colorer.ValueToString(0xffdec0de), "ffdec0de");
			Assert.AreEqual(Colorer.ValueToString(0xf00dface), "f00dface");

			// ValueToARGB
			ValueToARGB(0xffaaaaaa, 0xff, 0xaa, 0xaa, 0xaa);
			ValueToARGB(0xffababab, 0xff, 0xab, 0xab, 0xab);
			ValueToARGB(0xffccdd00, 0xff, 0xcc, 0xdd, 0x00);
			ValueToARGB(0xcc00ddee, 0xcc, 0x00, 0xdd, 0xee);
			ValueToARGB(0xffdec0de, 0xff, 0xde, 0xc0, 0xde);
			ValueToARGB(0xf00dface, 0xf0, 0x0d, 0xfa, 0xce);

			// StringToARGB
			StringToARGB("a", 0xff, 0xaa, 0xaa, 0xaa);
			StringToARGB("AB", 0xff, 0xab, 0xab, 0xab);
			StringToARGB("cD0", 0xff, 0xcc, 0xdd, 0x00);
			StringToARGB("C0DE", 0xcc, 0x00, 0xdd, 0xee);
			try { StringToARGB("ad0be", 0, 0, 0, 0); Assert.Fail(); } catch { }
			StringToARGB("dEc0dE", 0xff, 0xde, 0xc0, 0xde);
			try { StringToARGB("dec0ded", 0, 0, 0, 0); Assert.Fail(); } catch { }
			StringToARGB("F00dFaCe", 0xf0, 0x0d, 0xfa, 0xce);
			try { StringToARGB("coder", 0, 0, 0, 0); Assert.Fail(); } catch { }
			try { StringToARGB("deadbeeff00d", 0, 0, 0, 0); Assert.Fail(); } catch { }

			// ARGBToString
			Assert.AreEqual(Colorer.ARGBToString(0xff, 0xaa, 0xaa, 0xaa), "ffaaaaaa");
			Assert.AreEqual(Colorer.ARGBToString(0xff, 0xab, 0xab, 0xab), "ffababab");
			Assert.AreEqual(Colorer.ARGBToString(0xff, 0xcc, 0xdd, 0x00), "ffccdd00");
			Assert.AreEqual(Colorer.ARGBToString(0xcc, 0x00, 0xdd, 0xee), "cc00ddee");
			Assert.AreEqual(Colorer.ARGBToString(0xff, 0xde, 0xc0, 0xde), "ffdec0de");
			Assert.AreEqual(Colorer.ARGBToString(0xf0, 0x0d, 0xfa, 0xce), "f00dface");

			// ARGBToValue
			Assert.AreEqual(Colorer.ARGBToValue(0xff, 0xaa, 0xaa, 0xaa), 0xffaaaaaa);
			Assert.AreEqual(Colorer.ARGBToValue(0xff, 0xab, 0xab, 0xab), 0xffababab);
			Assert.AreEqual(Colorer.ARGBToValue(0xff, 0xcc, 0xdd, 0x00), 0xffccdd00);
			Assert.AreEqual(Colorer.ARGBToValue(0xcc, 0x00, 0xdd, 0xee), 0xcc00ddee);
			Assert.AreEqual(Colorer.ARGBToValue(0xff, 0xde, 0xc0, 0xde), 0xffdec0de);
			Assert.AreEqual(Colorer.ARGBToValue(0xf0, 0x0d, 0xfa, 0xce), 0xf00dface);
		}
	}
}
