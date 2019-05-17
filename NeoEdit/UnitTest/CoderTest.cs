using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoEdit.Common.Transform;

namespace NeoEdit.UnitTest
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
			// UInt8
			VerifyCoder(Coder.CodePage.UInt8, "0", new byte[] { 0 }, false);
			VerifyCoder(Coder.CodePage.UInt8, "100", new byte[] { 100 }, false);
			VerifyCoder(Coder.CodePage.UInt8, "200", new byte[] { 200 }, false);
			VerifyCoder(Coder.CodePage.UInt8, "255", new byte[] { 255 }, false);
			VerifyCoder(Coder.CodePage.UInt8, "180 230 134", new byte[] { 180, 230, 134 }, false);
			VerifyCoder(Coder.CodePage.UInt8, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt8, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt8, "256", null, false);
			VerifyCoder(Coder.CodePage.UInt8, "Whee", null, false);

			// UInt16LE
			VerifyCoder(Coder.CodePage.UInt16LE, "0", new byte[] { 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "20000", new byte[] { 32, 78 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "40000", new byte[] { 64, 156 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "65535", new byte[] { 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "58938 29268 48883", new byte[] { 58, 230, 84, 114, 243, 190 }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "65536", null, false);
			VerifyCoder(Coder.CodePage.UInt16LE, "Whee", null, false);

			// UInt32LE
			VerifyCoder(Coder.CodePage.UInt32LE, "0", new byte[] { 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "2000000000", new byte[] { 0, 148, 53, 119 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "3000000000", new byte[] { 0, 94, 208, 178 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "4294967295", new byte[] { 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "502326096 349128254 87642076", new byte[] { 80, 227, 240, 29, 62, 70, 207, 20, 220, 79, 57, 5 }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "4294967296", null, false);
			VerifyCoder(Coder.CodePage.UInt32LE, "Whee", null, false);

			// UInt64LE
			VerifyCoder(Coder.CodePage.UInt64LE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "6000000000000000000", new byte[] { 0, 0, 88, 236, 53, 72, 68, 83 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "12000000000000000000", new byte[] { 0, 0, 176, 216, 107, 144, 136, 166 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "5529324512211775585 7759772867682036333 6572006540597709296", new byte[] { 97, 48, 62, 38, 89, 27, 188, 76, 109, 234, 230, 99, 79, 64, 176, 107, 240, 201, 222, 241, 32, 117, 52, 91 }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "18446744073709551616", null, false);
			VerifyCoder(Coder.CodePage.UInt64LE, "Whee", null, false);

			// Int8
			VerifyCoder(Coder.CodePage.Int8, "-128", new byte[] { 128 }, false);
			VerifyCoder(Coder.CodePage.Int8, "-28", new byte[] { 228 }, false);
			VerifyCoder(Coder.CodePage.Int8, "72", new byte[] { 72 }, false);
			VerifyCoder(Coder.CodePage.Int8, "127", new byte[] { 127 }, false);
			VerifyCoder(Coder.CodePage.Int8, "23\t-33", new byte[] { 23, 223 }, false, "23 -33");
			VerifyCoder(Coder.CodePage.Int8, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int8, "-129", null, false);
			VerifyCoder(Coder.CodePage.Int8, "128", null, false);
			VerifyCoder(Coder.CodePage.Int8, "Whee", null, false);

			// Int16LE
			VerifyCoder(Coder.CodePage.Int16LE, "-32768", new byte[] { 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "-12768", new byte[] { 32, 206 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "7232", new byte[] { 64, 28 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "32767", new byte[] { 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "5282 -26865 11608", new byte[] { 162, 20, 15, 151, 88, 45 }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int16LE, "-32769", null, false);
			VerifyCoder(Coder.CodePage.Int16LE, "32768", null, false);
			VerifyCoder(Coder.CodePage.Int16LE, "Whee", null, false);

			// Int32LE
			VerifyCoder(Coder.CodePage.Int32LE, "-2147483648", new byte[] { 0, 0, 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "-147483648", new byte[] { 0, 148, 53, 247 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "852516352", new byte[] { 0, 94, 208, 50 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "2147483647", new byte[] { 255, 255, 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "1387477915 -2064890447 2136415377", new byte[] { 155, 59, 179, 82, 177, 69, 236, 132, 145, 28, 87, 127 }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int32LE, "-2147483649", null, false);
			VerifyCoder(Coder.CodePage.Int32LE, "2147483648", null, false);
			VerifyCoder(Coder.CodePage.Int32LE, "Whee", null, false);

			// Int64LE
			VerifyCoder(Coder.CodePage.Int64LE, "-9223372036854775808", new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "-3223372036854775808", new byte[] { 0, 0, 88, 236, 53, 72, 68, 211 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "2776627963145224192", new byte[] { 0, 0, 176, 216, 107, 144, 136, 38 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "9223372036854775807", new byte[] { 255, 255, 255, 255, 255, 255, 255, 127 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "5603948564873056495 -3225847061438761581 1084590168216077244", new byte[] { 239, 144, 209, 92, 135, 57, 197, 77, 147, 117, 52, 91, 48, 125, 59, 211, 188, 131, 102, 247, 2, 61, 13, 15 }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int64LE, "-9223372036854775809", null, false);
			VerifyCoder(Coder.CodePage.Int64LE, "9223372036854775808", null, false);
			VerifyCoder(Coder.CodePage.Int64LE, "Whee", null, false);

			// UInt16BE
			VerifyCoder(Coder.CodePage.UInt16BE, "0", new byte[] { 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "20000", new byte[] { 78, 32 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "40000", new byte[] { 156, 64 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "65535", new byte[] { 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "45679 33057 54528", new byte[] { 178, 111, 129, 33, 213, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "65536", null, false);
			VerifyCoder(Coder.CodePage.UInt16BE, "Whee", null, false);

			// UInt32BE
			VerifyCoder(Coder.CodePage.UInt32BE, "0", new byte[] { 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "2000000000", new byte[] { 119, 53, 148, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "3000000000", new byte[] { 178, 208, 94, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "4294967295", new byte[] { 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "3780308447 2635936151 3530580886", new byte[] { 225, 82, 237, 223, 157, 29, 49, 151, 210, 112, 99, 150 }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "4294967296", null, false);
			VerifyCoder(Coder.CodePage.UInt32BE, "Whee", null, false);

			// UInt64BE
			VerifyCoder(Coder.CodePage.UInt64BE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "6000000000000000000", new byte[] { 83, 68, 72, 53, 236, 88, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "12000000000000000000", new byte[] { 166, 136, 144, 107, 216, 176, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "17824946424400120411 6514206364314995226 12728849806597405327", new byte[] { 247, 94, 238, 85, 22, 189, 10, 91, 90, 103, 28, 44, 178, 149, 138, 26, 176, 165, 245, 117, 113, 137, 186, 143 }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "-1", null, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "18446744073709551616", null, false);
			VerifyCoder(Coder.CodePage.UInt64BE, "Whee", null, false);

			// Int16BE
			VerifyCoder(Coder.CodePage.Int16BE, "-32768", new byte[] { 128, 0 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "-12768", new byte[] { 206, 32 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "7232", new byte[] { 28, 64 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "32767", new byte[] { 127, 255 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "11714 -29454 12412", new byte[] { 45, 194, 140, 242, 48, 124 }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int16BE, "-32769", null, false);
			VerifyCoder(Coder.CodePage.Int16BE, "32768", null, false);
			VerifyCoder(Coder.CodePage.Int16BE, "Whee", null, false);

			// Int32BE
			VerifyCoder(Coder.CodePage.Int32BE, "-2147483648", new byte[] { 128, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "-147483648", new byte[] { 247, 53, 148, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "852516352", new byte[] { 50, 208, 94, 0 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "2147483647", new byte[] { 127, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "2083528577 -2139949104 575535353", new byte[] { 124, 48, 31, 129, 128, 114, 247, 208, 34, 77, 248, 249 }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Int32BE, "-2147483649", null, false);
			VerifyCoder(Coder.CodePage.Int32BE, "2147483648", null, false);
			VerifyCoder(Coder.CodePage.Int32BE, "Whee", null, false);

			// Int64BE
			VerifyCoder(Coder.CodePage.Int64BE, "-9223372036854775808", new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "-3223372036854775808", new byte[] { 211, 68, 72, 53, 236, 88, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "2776627963145224192", new byte[] { 38, 136, 144, 107, 216, 176, 0, 0 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "9223372036854775807", new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "4661815790766985193 -4928550129601778329 2627481701813862033", new byte[] { 64, 178, 24, 195, 40, 249, 15, 233, 187, 154, 69, 197, 11, 97, 109, 103, 36, 118, 176, 175, 210, 116, 62, 145 }, false);
			VerifyCoder(Coder.CodePage.Int64BE, "", new byte[] { }, false);
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
			VerifyCoder(Coder.CodePage.Single, "6.343272E+24 -4.067692E-14 3.170289E+12", new byte[] { 175, 231, 167, 104, 76, 49, 55, 169, 253, 136, 56, 84 }, false);
			VerifyCoder(Coder.CodePage.Single, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Single, "-3.402824E+38", null, false);
			VerifyCoder(Coder.CodePage.Single, "3.402824E+38", null, false);
			VerifyCoder(Coder.CodePage.Single, "Whee", null, false);

			// Double
			VerifyCoder(Coder.CodePage.Double, "-1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 255 }, false);
			VerifyCoder(Coder.CodePage.Double, "-5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 255 }, false);
			VerifyCoder(Coder.CodePage.Double, "5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 127 }, false);
			VerifyCoder(Coder.CodePage.Double, "1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 127 }, false);
			VerifyCoder(Coder.CodePage.Double, "8.89584492479304E-157 -9.57253643279842E+88 2.55292594410904E+209", new byte[] { 173, 6, 238, 147, 97, 109, 136, 31, 126, 201, 146, 215, 101, 15, 104, 210, 4, 118, 90, 173, 96, 217, 104, 107 }, false);
			VerifyCoder(Coder.CodePage.Double, "", new byte[] { }, false);
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
			VerifyCoder(Coder.DefaultCodePage, "", Encoding.Default.GetBytes(""), false);
			VerifyCoder(Coder.DefaultCodePage, "This is my string", Encoding.Default.GetBytes("This is my string"), false);
			VerifyCoder(Coder.DefaultCodePage, "This is my string", Encoding.Default.GetBytes("This is my string"), true);
			VerifyCoder(Coder.CodePage.ASCII, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.ASCII, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, false);
			VerifyCoder(Coder.CodePage.ASCII, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 }, true);

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

		[TestMethod]
		public void CoderImageTest()
		{
			const string image1 = "ffffffff ffff0000\r\nff00ff00 ff0000ff\r\n";
			const string image2 = "fff ff00\n00ff00  ff0000ff";
			const string revImageJpeg = "ffebf1ef ff545a58\r\nffa4aaa8 ff181e1c\r\n";

			// Bitmap
			const string bmpBytes = "Qk1GAAAAAAAAADYAAAAoAAAAAgAAAAIAAAABACAAAAAAAAAAAADEDgAAxA4AAAAAAAAAAAAAAP8A//8AAP//////AAD//w==";
			VerifyCoder(Coder.CodePage.Bitmap, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.Bitmap, image1, Convert.FromBase64String(bmpBytes), false);
			VerifyCoder(Coder.CodePage.Bitmap, image2, Convert.FromBase64String(bmpBytes), false, image1);

			// GIF
			const string gifBytes = "R0lGODlhAgACAPcAAAAAAAAAMwAAZgAAmQAAzAAA/wArAAArMwArZgArmQArzAAr/wBVAABVMwBVZgBVmQBVzABV/wCAAACAMwCAZgCAmQCAzACA/wCqAACqMwCqZgCqmQCqzACq/wDVAADVMwDVZgDVmQDVzADV/wD/AAD/MwD/ZgD/mQD/zAD//zMAADMAMzMAZjMAmTMAzDMA/zMrADMrMzMrZjMrmTMrzDMr/zNVADNVMzNVZjNVmTNVzDNV/zOAADOAMzOAZjOAmTOAzDOA/zOqADOqMzOqZjOqmTOqzDOq/zPVADPVMzPVZjPVmTPVzDPV/zP/ADP/MzP/ZjP/mTP/zDP//2YAAGYAM2YAZmYAmWYAzGYA/2YrAGYrM2YrZmYrmWYrzGYr/2ZVAGZVM2ZVZmZVmWZVzGZV/2aAAGaAM2aAZmaAmWaAzGaA/2aqAGaqM2aqZmaqmWaqzGaq/2bVAGbVM2bVZmbVmWbVzGbV/2b/AGb/M2b/Zmb/mWb/zGb//5kAAJkAM5kAZpkAmZkAzJkA/5krAJkrM5krZpkrmZkrzJkr/5lVAJlVM5lVZplVmZlVzJlV/5mAAJmAM5mAZpmAmZmAzJmA/5mqAJmqM5mqZpmqmZmqzJmq/5nVAJnVM5nVZpnVmZnVzJnV/5n/AJn/M5n/Zpn/mZn/zJn//8wAAMwAM8wAZswAmcwAzMwA/8wrAMwrM8wrZswrmcwrzMwr/8xVAMxVM8xVZsxVmcxVzMxV/8yAAMyAM8yAZsyAmcyAzMyA/8yqAMyqM8yqZsyqmcyqzMyq/8zVAMzVM8zVZszVmczVzMzV/8z/AMz/M8z/Zsz/mcz/zMz///8AAP8AM/8AZv8Amf8AzP8A//8rAP8rM/8rZv8rmf8rzP8r//9VAP9VM/9VZv9Vmf9VzP9V//+AAP+AM/+AZv+Amf+AzP+A//+qAP+qM/+qZv+qmf+qzP+q///VAP/VM//VZv/Vmf/VzP/V////AP//M///Zv//mf//zP///wAAAAAAAAAAAAAAACH5BAEAAPwALAAAAAACAAIAAAgHAPdJI1EgIAA7";
			VerifyCoder(Coder.CodePage.GIF, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.GIF, image1, Convert.FromBase64String(gifBytes), false);
			VerifyCoder(Coder.CodePage.GIF, image2, Convert.FromBase64String(gifBytes), false, image1);

			// JPEG
			const string jpegBytes = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAACAAIDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDzLXry6bxFqbNczFjdykkyHn5zRRRX0NH+FH0R9Th/4MPRfkf/2Q==";
			VerifyCoder(Coder.CodePage.JPEG, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.JPEG, image2, Convert.FromBase64String(jpegBytes), false, revImageJpeg);

			// PNG
			const string pngBytes = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAATSURBVBhXY/gPAgwMYAQk/v8HAGOuCff0e1nZAAAAAElFTkSuQmCC";
			VerifyCoder(Coder.CodePage.PNG, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.PNG, image1, Convert.FromBase64String(pngBytes), false);
			VerifyCoder(Coder.CodePage.PNG, image2, Convert.FromBase64String(pngBytes), false, image1);

			// TIFF
			const string tiffBytes = "SUkqABgAAACAP+BP8AAEAgAAQSCAGCQEEAD+AAQAAQAAAAAAAAAAAQQAAQAAAAIAAAABAQQAAQAAAAIAAAACAQMABAAAAN4AAAADAQMAAQAAAAUAAAAGAQMAAQAAAAIAAAARAQQAAQAAAAgAAAAVAQMAAQAAAAQAAAAWAQQAAQAAAAIAAAAXAQQAAQAAABAAAAAaAQUAAQAAAOYAAAAbAQUAAQAAAO4AAAAcAQMAAQAAAAEAAAAoAQMAAQAAAAIAAAA9AQMAAQAAAAIAAABSAQMAAQAAAAIAAAAAAAAACAAIAAgACAAAdwEA6AMAAAB3AQDoAwAA";
			VerifyCoder(Coder.CodePage.TIFF, "", new byte[] { }, false);
			VerifyCoder(Coder.CodePage.TIFF, image1, Convert.FromBase64String(tiffBytes), false);
			VerifyCoder(Coder.CodePage.TIFF, image2, Convert.FromBase64String(tiffBytes), false, image1);
		}
	}
}
