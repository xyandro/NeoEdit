using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common.UnitTest
{
	public partial class UnitTest
	{
		void VerifyCoder(Coder.CodePage codePage, string value, byte[] expected, bool bom, string reverse = null)
		{
			if (reverse == null)
				reverse = value;

			var result = Coder.TryStringToBytes(value, codePage, bom);
			Assert.AreEqual(result != null, expected != null);
			if (expected == null)
				return;

			var str = Coder.BytesToString(result, codePage, bom);
			Assert.AreEqual(str, reverse);

			Assert.AreEqual(expected.Length, result.Length);
			for (var ctr = 0; ctr < result.Length; ctr++)
				Assert.AreEqual(expected[ctr], result[ctr]);
		}

		[TestMethod]
		public void CoderIntTest()
		{
			// UInt8LE
			VerifyCoder(Coder.CodePage.UInt8LE, "0", new byte[] { 0 }, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "100", new byte[] { 100 }, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "200", new byte[] { 200 }, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "255", new byte[] { 255 }, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "256", null, false);
			VerifyCoder(Coder.CodePage.UInt8LE, "Whee", null, false);

			// UInt16LE
			VerifyCoder(Coder.CodePage.UInt16LE, "0", new byte[] { 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "20000", new byte[] { 32, 78 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "40000", new byte[] { 64, 156 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "65535", new byte[] { 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "65536", null, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "Whee", null, false);

			// UInt32LE
			VerifyCoder(Coder.CodePage.UInt32LE, "0", new byte[] { 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "2000000000", new byte[] { 0, 148, 53, 119 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "3000000000", new byte[] { 0, 94, 208, 178 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "4294967295", new byte[] { 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "4294967296", null, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "Whee", null, false);

			// UInt64LE
			VerifyCoder(Coder.CodePage.UInt64LE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "6000000000000000000", new byte[] { 0, 0, 88, 236, 53, 72, 68, 83 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "12000000000000000000", new byte[] { 0, 0, 176, 216, 107, 144, 136, 166 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "18446744073709551616", null, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "Whee", null, false);

			// Int8LE
			VerifyCoder(Coder.CodePage.Int8LE, "-128", new byte[] { 128 }, false);
			VerifyCoder(Coder.CodePage.Int8LE, "-28", new byte[] { 228 }, false);
			VerifyCoder(Coder.CodePage.Int8LE, "72", new byte[] { 72 }, false);
			VerifyCoder(Coder.CodePage.Int8LE, "127", new byte[] { 127 }, false);
			VerifyCoder(Coder.CodePage.Int8LE, "", null, false);
			VerifyCoder(Coder.CodePage.Int8LE, "-129", null, false);
			VerifyCoder(Coder.CodePage.Int8LE, "128", null, false);
			VerifyCoder(Coder.CodePage.Int8LE, "Whee", null, false);

			// Int16LE
			VerifyCoder(Coder.CodePage.Int16LE, "-32768", new byte[] { 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "-12768", new byte[] { 32, 206 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "7232", new byte[] { 64, 28 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "32767", new byte[] { 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "", null, false);
			VerifyCoder(Coder.CodePage.Int16LE, "-32769", null, false);
			VerifyCoder(Coder.CodePage.Int16LE, "32768", null, false);
			VerifyCoder(Coder.CodePage.Int16LE, "Whee", null, false);

			// Int32LE
			VerifyCoder(Coder.CodePage.Int32LE, "-2147483648", new byte[] { 0, 0, 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "-147483648", new byte[] { 0, 148, 53, 247 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "852516352", new byte[] { 0, 94, 208, 50 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "2147483647", new byte[] { 255, 255, 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "", null, false);
			VerifyCoder(Coder.CodePage.Int32LE, "-2147483649", null, false);
			VerifyCoder(Coder.CodePage.Int32LE, "2147483648", null, false);
			VerifyCoder(Coder.CodePage.Int32LE, "Whee", null, false);

			// Int64LE
			VerifyCoder(Coder.CodePage.Int64LE, "-9223372036854775808", new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "-3223372036854775808", new byte[] { 0, 0, 88, 236, 53, 72, 68, 211 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "2776627963145224192", new byte[] { 0, 0, 176, 216, 107, 144, 136, 38 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "9223372036854775807", new byte[] { 255, 255, 255, 255, 255, 255, 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "", null, false);
			VerifyCoder(Coder.CodePage.Int64LE, "-9223372036854775809", null, false);
			VerifyCoder(Coder.CodePage.Int64LE, "9223372036854775808", null, false);
			VerifyCoder(Coder.CodePage.Int64LE, "Whee", null, false);

			// UInt8BE
			VerifyCoder(Coder.CodePage.UInt8BE, "0", new byte[] { 0 }, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "100", new byte[] { 100 }, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "200", new byte[] { 200 }, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "255", new byte[] { 255 }, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "256", null, false);
			VerifyCoder(Coder.CodePage.UInt8BE, "Whee", null, false);

			// UInt16BE
			VerifyCoder(Coder.CodePage.UInt16BE, "0", new byte[] { 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "20000", new byte[] { 78, 32 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "40000", new byte[] { 156, 64 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "65535", new byte[] { 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "65536", null, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "Whee", null, false);

			// UInt32BE
			VerifyCoder(Coder.CodePage.UInt32BE, "0", new byte[] { 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "2000000000", new byte[] { 119, 53, 148, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "3000000000", new byte[] { 178, 208, 94, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "4294967295", new byte[] { 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "4294967296", null, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "Whee", null, false);

			// UInt64BE
			VerifyCoder(Coder.CodePage.UInt64BE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "6000000000000000000", new byte[] { 83, 68, 72, 53, 236, 88, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "12000000000000000000", new byte[] { 166, 136, 144, 107, 216, 176, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "", null, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "18446744073709551616", null, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "Whee", null, false);

			// Int8BE
			VerifyCoder(Coder.CodePage.Int8BE, "-128", new byte[] { 128 }, false);
			VerifyCoder(Coder.CodePage.Int8BE, "-28", new byte[] { 228 }, false);
			VerifyCoder(Coder.CodePage.Int8BE, "72", new byte[] { 72 }, false);
			VerifyCoder(Coder.CodePage.Int8BE, "127", new byte[] { 127 }, false);
			VerifyCoder(Coder.CodePage.Int8BE, "", null, false);
			VerifyCoder(Coder.CodePage.Int8BE, "-129", null, false);
			VerifyCoder(Coder.CodePage.Int8BE, "128", null, false);
			VerifyCoder(Coder.CodePage.Int8BE, "Whee", null, false);

			// Int16BE
			VerifyCoder(Coder.CodePage.Int16BE, "-32768", new byte[] { 128, 0 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "-12768", new byte[] { 206, 32 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "7232", new byte[] { 28, 64 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "32767", new byte[] { 127, 255 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "", null, false);
			VerifyCoder(Coder.CodePage.Int16BE, "-32769", null, false);
			VerifyCoder(Coder.CodePage.Int16BE, "32768", null, false);
			VerifyCoder(Coder.CodePage.Int16BE, "Whee", null, false);

			// Int32BE
			VerifyCoder(Coder.CodePage.Int32BE, "-2147483648", new byte[] { 128, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "-147483648", new byte[] { 247, 53, 148, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "852516352", new byte[] { 50, 208, 94, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "2147483647", new byte[] { 127, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "", null, false);
			VerifyCoder(Coder.CodePage.Int32BE, "-2147483649", null, false);
			VerifyCoder(Coder.CodePage.Int32BE, "2147483648", null, false);
			VerifyCoder(Coder.CodePage.Int32BE, "Whee", null, false);

			// Int64BE
			VerifyCoder(Coder.CodePage.Int64BE, "-9223372036854775808", new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "-3223372036854775808", new byte[] { 211, 68, 72, 53, 236, 88, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "2776627963145224192", new byte[] { 38, 136, 144, 107, 216, 176, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "9223372036854775807", new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "", null, false);
			VerifyCoder(Coder.CodePage.Int64BE, "-9223372036854775809", null, false);
			VerifyCoder(Coder.CodePage.Int64BE, "9223372036854775808", null, false);
			VerifyCoder(Coder.CodePage.Int64BE, "Whee", null, false);
		}

		[TestMethod]
		public void CoderFloatTest()
		{
			// Single
			VerifyCoder(Coder.CodePage.Single, "-3.402823E+38", new byte[] { 253, 255, 127, 255 }, false);
			VerifyCoder(Coder.CodePage.Single, "-1.134274E+38", new byte[] { 165, 170, 170, 254 }, false);
			VerifyCoder(Coder.CodePage.Single, "1.134274E+38", new byte[] { 165, 170, 170, 126 }, false);
			VerifyCoder(Coder.CodePage.Single, "3.402823E+38", new byte[] { 253, 255, 127, 127 }, false);
			VerifyCoder(Coder.CodePage.Single, "", null, false);
			VerifyCoder(Coder.CodePage.Single, "-3.402824E+38", null, false);
			VerifyCoder(Coder.CodePage.Single, "3.402824E+38", null, false);
			VerifyCoder(Coder.CodePage.Single, "Whee", null, false);

			// Double
			VerifyCoder(Coder.CodePage.Double, "-1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 255 }, false);
			VerifyCoder(Coder.CodePage.Double, "-5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 255 }, false);
			VerifyCoder(Coder.CodePage.Double, "5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 127 }, false);
			VerifyCoder(Coder.CodePage.Double, "1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 127 }, false);
			VerifyCoder(Coder.CodePage.Double, "", null, false);
			VerifyCoder(Coder.CodePage.Double, "-1.79769313486232E+308", null, false);
			VerifyCoder(Coder.CodePage.Double, "1.79769313486232E+308", null, false);
			VerifyCoder(Coder.CodePage.Double, "Whee", null, false);
		}

		[TestMethod]
		public void CoderStringTest()
		{
			// Strings
			VerifyCoder(Coder.CodePage.UTF8, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UTF8, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, false);
			VerifyCoder(Coder.CodePage.UTF8, "This is my string", new byte[] { 239, 187, 191, 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, true);
			VerifyCoder(Coder.CodePage.UTF16LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UTF16LE, "This is my string", new byte[] { 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, false);
			VerifyCoder(Coder.CodePage.UTF16LE, "This is my string", new byte[] { 255, 254, 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, true);
			VerifyCoder(Coder.CodePage.UTF16BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UTF16BE, "This is my string", new byte[] { 0, 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, false);
			VerifyCoder(Coder.CodePage.UTF16BE, "This is my string", new byte[] { 254, 255, 0, 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, true);
			VerifyCoder(Coder.CodePage.UTF32LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UTF32LE, "This is my string", new byte[] { 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UTF32LE, "This is my string", new byte[] { 255, 254, 0, 0, 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103, 0, 0, 0 }, true);
			VerifyCoder(Coder.CodePage.UTF32BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UTF32BE, "This is my string", new byte[] { 0, 0, 0, 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103 }, false);
			VerifyCoder(Coder.CodePage.UTF32BE, "This is my string", new byte[] { 0, 0, 254, 255, 0, 0, 0, 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103 }, true);
			VerifyCoder(Coder.CodePage.Default, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Default, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, false);
			VerifyCoder(Coder.CodePage.Default, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, true);

			VerifyCoder(Coder.CodePage.UTF8, "\ufeffThis is my string", new byte[] { 239, 187, 191, 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, false);
			VerifyCoder(Coder.CodePage.UTF8, "\ufeffThis is my string", new byte[] { 239, 187, 191, 239, 187, 191, 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, true);
		}

		[TestMethod]
		public void CoderOtherTest()
		{
			// Base64
			VerifyCoder(Coder.CodePage.Base64, "", Encoding.UTF8.GetBytes(""), false);
			VerifyCoder(Coder.CodePage.Base64, " V G h p c y B p c y \r B t e S B z \n d H J p b m c = ", Encoding.UTF8.GetBytes("This is my string"), false, "VGhpcyBpcyBteSBzdHJpbmc=");
			VerifyCoder(Coder.CodePage.Base64, "\ufeffVGhpcyBpcyBteSBzdHJpbmc=", Encoding.UTF8.GetBytes("This is my string"), false, "VGhpcyBpcyBteSBzdHJpbmc="); // BOM at start is ignored
			VerifyCoder(Coder.CodePage.Base64, " \ufeffVGhpcyBpcyBteSBzdHJpbmc=", null, false); // BOM not at start is error
			VerifyCoder(Coder.CodePage.Base64, "(INVALID STRING)", null, false);
			VerifyCoder(Coder.CodePage.Base64, "VGhpcyBpcyBteSBzdHJpbmc", Encoding.UTF8.GetBytes("This is my string"), false, "VGhpcyBpcyBteSBzdHJpbmc="); // Missing ending padding
			VerifyCoder(Coder.CodePage.Base64, "V=GhpcyBpcyBteSBzdHJpbmc=", null, false); // Padding in middle

			// Hex
			VerifyCoder(Coder.CodePage.Hex, "deadbeef", new byte[] { 222, 173, 190, 239 }, false);
			VerifyCoder(Coder.CodePage.Hex, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Hex, "0123456789abcdef", new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 }, false);
			VerifyCoder(Coder.CodePage.Hex, "0000", new byte[] { 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Hex, "geadbeef", null, false);

			// Binary
			VerifyCoder(Coder.CodePage.Binary, "1010101001010101", new byte[] { 0xaa, 0x55 }, false);
			VerifyCoder(Coder.CodePage.Binary, "10 11011101 11011111", new byte[] { 0x02, 0xdd, 0xdf }, false, "000000101101110111011111");
			VerifyCoder(Coder.CodePage.Binary, "	 11011110101011011011 111011101111 \n ", new byte[] { 0xde, 0xad, 0xbe, 0xef }, false, "11011110101011011011111011101111");
			VerifyCoder(Coder.CodePage.Binary, "000000000", new byte[] { 0, 0 }, false, "0000000000000000");
			VerifyCoder(Coder.CodePage.Binary, "0101012", null, false);
		}
	}
}
