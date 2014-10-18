using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		void VerifyCoder(Coder.Type type, string value, byte[] expected, string reverse = null)
		{
			if (reverse == null)
				reverse = value;

			var result = Coder.TryStringToBytes(value, type);
			Assert.AreEqual(result != null, expected != null);
			if (expected == null)
				return;

			var str = Coder.BytesToString(result, type);
			Assert.AreEqual(str, reverse);

			Assert.AreEqual(expected.Length, result.Length);
			for (var ctr = 0; ctr < result.Length; ctr++)
				Assert.AreEqual(expected[ctr], result[ctr]);
		}

		[TestMethod]
		public void CoderIntTest()
		{
			// UInt8LE
			VerifyCoder(Coder.Type.UInt8LE, "0", new byte[] { 0 });
			VerifyCoder(Coder.Type.UInt8LE, "100", new byte[] { 100 });
			VerifyCoder(Coder.Type.UInt8LE, "200", new byte[] { 200 });
			VerifyCoder(Coder.Type.UInt8LE, "255", new byte[] { 255 });
			VerifyCoder(Coder.Type.UInt8LE, "", null);
			VerifyCoder(Coder.Type.UInt8LE, "-1", null);
			VerifyCoder(Coder.Type.UInt8LE, "256", null);
			VerifyCoder(Coder.Type.UInt8LE, "Whee", null);

			// UInt16LE
			VerifyCoder(Coder.Type.UInt16LE, "0", new byte[] { 0, 0 });
			VerifyCoder(Coder.Type.UInt16LE, "20000", new byte[] { 32, 78 });
			VerifyCoder(Coder.Type.UInt16LE, "40000", new byte[] { 64, 156 });
			VerifyCoder(Coder.Type.UInt16LE, "65535", new byte[] { 255, 255 });
			VerifyCoder(Coder.Type.UInt16LE, "", null);
			VerifyCoder(Coder.Type.UInt16LE, "-1", null);
			VerifyCoder(Coder.Type.UInt16LE, "65536", null);
			VerifyCoder(Coder.Type.UInt16LE, "Whee", null);

			// UInt32LE
			VerifyCoder(Coder.Type.UInt32LE, "0", new byte[] { 0, 0, 0, 0 });
			VerifyCoder(Coder.Type.UInt32LE, "2000000000", new byte[] { 0, 148, 53, 119 });
			VerifyCoder(Coder.Type.UInt32LE, "3000000000", new byte[] { 0, 94, 208, 178 });
			VerifyCoder(Coder.Type.UInt32LE, "4294967295", new byte[] { 255, 255, 255, 255 });
			VerifyCoder(Coder.Type.UInt32LE, "", null);
			VerifyCoder(Coder.Type.UInt32LE, "-1", null);
			VerifyCoder(Coder.Type.UInt32LE, "4294967296", null);
			VerifyCoder(Coder.Type.UInt32LE, "Whee", null);

			// UInt64LE
			VerifyCoder(Coder.Type.UInt64LE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
			VerifyCoder(Coder.Type.UInt64LE, "6000000000000000000", new byte[] { 0, 0, 88, 236, 53, 72, 68, 83 });
			VerifyCoder(Coder.Type.UInt64LE, "12000000000000000000", new byte[] { 0, 0, 176, 216, 107, 144, 136, 166 });
			VerifyCoder(Coder.Type.UInt64LE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
			VerifyCoder(Coder.Type.UInt64LE, "", null);
			VerifyCoder(Coder.Type.UInt64LE, "-1", null);
			VerifyCoder(Coder.Type.UInt64LE, "18446744073709551616", null);
			VerifyCoder(Coder.Type.UInt64LE, "Whee", null);

			// Int8LE
			VerifyCoder(Coder.Type.Int8LE, "-128", new byte[] { 128 });
			VerifyCoder(Coder.Type.Int8LE, "-28", new byte[] { 228 });
			VerifyCoder(Coder.Type.Int8LE, "72", new byte[] { 72 });
			VerifyCoder(Coder.Type.Int8LE, "127", new byte[] { 127 });
			VerifyCoder(Coder.Type.Int8LE, "", null);
			VerifyCoder(Coder.Type.Int8LE, "-129", null);
			VerifyCoder(Coder.Type.Int8LE, "128", null);
			VerifyCoder(Coder.Type.Int8LE, "Whee", null);

			// Int16LE
			VerifyCoder(Coder.Type.Int16LE, "-32768", new byte[] { 0, 128 });
			VerifyCoder(Coder.Type.Int16LE, "-12768", new byte[] { 32, 206 });
			VerifyCoder(Coder.Type.Int16LE, "7232", new byte[] { 64, 28 });
			VerifyCoder(Coder.Type.Int16LE, "32767", new byte[] { 255, 127 });
			VerifyCoder(Coder.Type.Int16LE, "", null);
			VerifyCoder(Coder.Type.Int16LE, "-32769", null);
			VerifyCoder(Coder.Type.Int16LE, "32768", null);
			VerifyCoder(Coder.Type.Int16LE, "Whee", null);

			// Int32LE
			VerifyCoder(Coder.Type.Int32LE, "-2147483648", new byte[] { 0, 0, 0, 128 });
			VerifyCoder(Coder.Type.Int32LE, "-147483648", new byte[] { 0, 148, 53, 247 });
			VerifyCoder(Coder.Type.Int32LE, "852516352", new byte[] { 0, 94, 208, 50 });
			VerifyCoder(Coder.Type.Int32LE, "2147483647", new byte[] { 255, 255, 255, 127 });
			VerifyCoder(Coder.Type.Int32LE, "", null);
			VerifyCoder(Coder.Type.Int32LE, "-2147483649", null);
			VerifyCoder(Coder.Type.Int32LE, "2147483648", null);
			VerifyCoder(Coder.Type.Int32LE, "Whee", null);

			// Int64LE
			VerifyCoder(Coder.Type.Int64LE, "-9223372036854775808", new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 });
			VerifyCoder(Coder.Type.Int64LE, "-3223372036854775808", new byte[] { 0, 0, 88, 236, 53, 72, 68, 211 });
			VerifyCoder(Coder.Type.Int64LE, "2776627963145224192", new byte[] { 0, 0, 176, 216, 107, 144, 136, 38 });
			VerifyCoder(Coder.Type.Int64LE, "9223372036854775807", new byte[] { 255, 255, 255, 255, 255, 255, 255, 127 });
			VerifyCoder(Coder.Type.Int64LE, "", null);
			VerifyCoder(Coder.Type.Int64LE, "-9223372036854775809", null);
			VerifyCoder(Coder.Type.Int64LE, "9223372036854775808", null);
			VerifyCoder(Coder.Type.Int64LE, "Whee", null);

			// UInt8BE
			VerifyCoder(Coder.Type.UInt8BE, "0", new byte[] { 0 });
			VerifyCoder(Coder.Type.UInt8BE, "100", new byte[] { 100 });
			VerifyCoder(Coder.Type.UInt8BE, "200", new byte[] { 200 });
			VerifyCoder(Coder.Type.UInt8BE, "255", new byte[] { 255 });
			VerifyCoder(Coder.Type.UInt8BE, "", null);
			VerifyCoder(Coder.Type.UInt8BE, "-1", null);
			VerifyCoder(Coder.Type.UInt8BE, "256", null);
			VerifyCoder(Coder.Type.UInt8BE, "Whee", null);

			// UInt16BE
			VerifyCoder(Coder.Type.UInt16BE, "0", new byte[] { 0, 0 });
			VerifyCoder(Coder.Type.UInt16BE, "20000", new byte[] { 78, 32 });
			VerifyCoder(Coder.Type.UInt16BE, "40000", new byte[] { 156, 64 });
			VerifyCoder(Coder.Type.UInt16BE, "65535", new byte[] { 255, 255 });
			VerifyCoder(Coder.Type.UInt16BE, "", null);
			VerifyCoder(Coder.Type.UInt16BE, "-1", null);
			VerifyCoder(Coder.Type.UInt16BE, "65536", null);
			VerifyCoder(Coder.Type.UInt16BE, "Whee", null);

			// UInt32BE
			VerifyCoder(Coder.Type.UInt32BE, "0", new byte[] { 0, 0, 0, 0 });
			VerifyCoder(Coder.Type.UInt32BE, "2000000000", new byte[] { 119, 53, 148, 0 });
			VerifyCoder(Coder.Type.UInt32BE, "3000000000", new byte[] { 178, 208, 94, 0 });
			VerifyCoder(Coder.Type.UInt32BE, "4294967295", new byte[] { 255, 255, 255, 255 });
			VerifyCoder(Coder.Type.UInt32BE, "", null);
			VerifyCoder(Coder.Type.UInt32BE, "-1", null);
			VerifyCoder(Coder.Type.UInt32BE, "4294967296", null);
			VerifyCoder(Coder.Type.UInt32BE, "Whee", null);

			// UInt64BE
			VerifyCoder(Coder.Type.UInt64BE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
			VerifyCoder(Coder.Type.UInt64BE, "6000000000000000000", new byte[] { 83, 68, 72, 53, 236, 88, 0, 0 });
			VerifyCoder(Coder.Type.UInt64BE, "12000000000000000000", new byte[] { 166, 136, 144, 107, 216, 176, 0, 0 });
			VerifyCoder(Coder.Type.UInt64BE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
			VerifyCoder(Coder.Type.UInt64BE, "", null);
			VerifyCoder(Coder.Type.UInt64BE, "-1", null);
			VerifyCoder(Coder.Type.UInt64BE, "18446744073709551616", null);
			VerifyCoder(Coder.Type.UInt64BE, "Whee", null);

			// Int8BE
			VerifyCoder(Coder.Type.Int8BE, "-128", new byte[] { 128 });
			VerifyCoder(Coder.Type.Int8BE, "-28", new byte[] { 228 });
			VerifyCoder(Coder.Type.Int8BE, "72", new byte[] { 72 });
			VerifyCoder(Coder.Type.Int8BE, "127", new byte[] { 127 });
			VerifyCoder(Coder.Type.Int8BE, "", null);
			VerifyCoder(Coder.Type.Int8BE, "-129", null);
			VerifyCoder(Coder.Type.Int8BE, "128", null);
			VerifyCoder(Coder.Type.Int8BE, "Whee", null);

			// Int16BE
			VerifyCoder(Coder.Type.Int16BE, "-32768", new byte[] { 128, 0 });
			VerifyCoder(Coder.Type.Int16BE, "-12768", new byte[] { 206, 32 });
			VerifyCoder(Coder.Type.Int16BE, "7232", new byte[] { 28, 64 });
			VerifyCoder(Coder.Type.Int16BE, "32767", new byte[] { 127, 255 });
			VerifyCoder(Coder.Type.Int16BE, "", null);
			VerifyCoder(Coder.Type.Int16BE, "-32769", null);
			VerifyCoder(Coder.Type.Int16BE, "32768", null);
			VerifyCoder(Coder.Type.Int16BE, "Whee", null);

			// Int32BE
			VerifyCoder(Coder.Type.Int32BE, "-2147483648", new byte[] { 128, 0, 0, 0 });
			VerifyCoder(Coder.Type.Int32BE, "-147483648", new byte[] { 247, 53, 148, 0 });
			VerifyCoder(Coder.Type.Int32BE, "852516352", new byte[] { 50, 208, 94, 0 });
			VerifyCoder(Coder.Type.Int32BE, "2147483647", new byte[] { 127, 255, 255, 255 });
			VerifyCoder(Coder.Type.Int32BE, "", null);
			VerifyCoder(Coder.Type.Int32BE, "-2147483649", null);
			VerifyCoder(Coder.Type.Int32BE, "2147483648", null);
			VerifyCoder(Coder.Type.Int32BE, "Whee", null);

			// Int64BE
			VerifyCoder(Coder.Type.Int64BE, "-9223372036854775808", new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 });
			VerifyCoder(Coder.Type.Int64BE, "-3223372036854775808", new byte[] { 211, 68, 72, 53, 236, 88, 0, 0 });
			VerifyCoder(Coder.Type.Int64BE, "2776627963145224192", new byte[] { 38, 136, 144, 107, 216, 176, 0, 0 });
			VerifyCoder(Coder.Type.Int64BE, "9223372036854775807", new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 });
			VerifyCoder(Coder.Type.Int64BE, "", null);
			VerifyCoder(Coder.Type.Int64BE, "-9223372036854775809", null);
			VerifyCoder(Coder.Type.Int64BE, "9223372036854775808", null);
			VerifyCoder(Coder.Type.Int64BE, "Whee", null);
		}

		[TestMethod]
		public void CoderFloatTest()
		{
			// Single
			VerifyCoder(Coder.Type.Single, "-3.402823E+38", new byte[] { 253, 255, 127, 255 });
			VerifyCoder(Coder.Type.Single, "-1.134274E+38", new byte[] { 165, 170, 170, 254 });
			VerifyCoder(Coder.Type.Single, "1.134274E+38", new byte[] { 165, 170, 170, 126 });
			VerifyCoder(Coder.Type.Single, "3.402823E+38", new byte[] { 253, 255, 127, 127 });
			VerifyCoder(Coder.Type.Single, "", null);
			VerifyCoder(Coder.Type.Single, "-3.402824E+38", null);
			VerifyCoder(Coder.Type.Single, "3.402824E+38", null);
			VerifyCoder(Coder.Type.Single, "Whee", null);

			// Double
			VerifyCoder(Coder.Type.Double, "-1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 255 });
			VerifyCoder(Coder.Type.Double, "-5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 255 });
			VerifyCoder(Coder.Type.Double, "5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 127 });
			VerifyCoder(Coder.Type.Double, "1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 127 });
			VerifyCoder(Coder.Type.Double, "", null);
			VerifyCoder(Coder.Type.Double, "-1.79769313486232E+308", null);
			VerifyCoder(Coder.Type.Double, "1.79769313486232E+308", null);
			VerifyCoder(Coder.Type.Double, "Whee", null);
		}

		[TestMethod]
		public void CoderStringTest()
		{
			// Strings
			VerifyCoder(Coder.Type.UTF7, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF7, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 });
			VerifyCoder(Coder.Type.UTF8, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF8, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 });
			VerifyCoder(Coder.Type.UTF16LE, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF16LE, "This is my string", new byte[] { 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 });
			VerifyCoder(Coder.Type.UTF16BE, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF16BE, "This is my string", new byte[] { 0, 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 });
			VerifyCoder(Coder.Type.UTF32LE, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF32LE, "This is my string", new byte[] { 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103, 0, 0, 0 });
			VerifyCoder(Coder.Type.UTF32BE, "", new byte[] { });
			VerifyCoder(Coder.Type.UTF32BE, "This is my string", new byte[] { 0, 0, 0, 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103 });
		}

		[TestMethod]
		public void CoderOtherTest()
		{
			// Base64
			VerifyCoder(Coder.Type.Base64, "", Encoding.UTF8.GetBytes(""));
			VerifyCoder(Coder.Type.Base64, " V G h p c y B p c y \r B t e S B z \n d H J p b m c = ", Encoding.UTF8.GetBytes("This is my string"), "VGhpcyBpcyBteSBzdHJpbmc");
			VerifyCoder(Coder.Type.Base64, "\ufeffVGhpcyBpcyBteSBzdHJpbmc=", Encoding.UTF8.GetBytes("This is my string"), "VGhpcyBpcyBteSBzdHJpbmc"); // BOM at start is ignored
			VerifyCoder(Coder.Type.Base64, " \ufeffVGhpcyBpcyBteSBzdHJpbmc=", null); // BOM not at start is error
			VerifyCoder(Coder.Type.Base64, "(INVALID STRING)", null);
			VerifyCoder(Coder.Type.Base64, "VGhpcyBpcyBteSBzdHJpbmc", Encoding.UTF8.GetBytes("This is my string")); // Missing ending padding
			VerifyCoder(Coder.Type.Base64, "V=GhpcyBpcyBteSBzdHJpbmc=", null); // Padding in middle

			// Hex
			VerifyCoder(Coder.Type.Hex, "deadbeef", new byte[] { 222, 173, 190, 239 });
			VerifyCoder(Coder.Type.Hex, "", new byte[] { });
			VerifyCoder(Coder.Type.Hex, "0123456789abcdef", new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 });
			VerifyCoder(Coder.Type.Hex, "0000", new byte[] { 0, 0 });
			VerifyCoder(Coder.Type.Hex, "geadbeef", null);
		}
	}
}
