using System;
using NeoEdit.UI.BinaryEditorUI;

namespace NeoEdit.Test
{
	class ConverterTest
	{
		void RunTest(Converter.ConverterType type, string value, byte[] expected)
		{
			var result = Converter.Convert(type, value);
			if ((result == null) != (expected == null))
				throw new Exception("Didn't receive expected success status");
			if (expected == null)
				return;

			var str = Converter.Convert(type, result, 0, 0);
			if (str != value)
				throw new Exception("Reverse conversion failed");

			if (expected.Length != result.Length)
				throw new Exception("Expected result not returned");
			for (var ctr = 0; ctr < result.Length; ctr++)
				if (expected[ctr] != result[ctr])
					throw new Exception("Expected result not returned");
		}

		public void Run()
		{
			// UInt8LE
			RunTest(Converter.ConverterType.UInt8LE, "0", new byte[] { 0 });
			RunTest(Converter.ConverterType.UInt8LE, "100", new byte[] { 100 });
			RunTest(Converter.ConverterType.UInt8LE, "200", new byte[] { 200 });
			RunTest(Converter.ConverterType.UInt8LE, "255", new byte[] { 255 });
			RunTest(Converter.ConverterType.UInt8LE, "", null);
			RunTest(Converter.ConverterType.UInt8LE, "-1", null);
			RunTest(Converter.ConverterType.UInt8LE, "256", null);
			RunTest(Converter.ConverterType.UInt8LE, "Whee", null);

			// UInt16LE
			RunTest(Converter.ConverterType.UInt16LE, "0", new byte[] { 0, 0 });
			RunTest(Converter.ConverterType.UInt16LE, "20000", new byte[] { 32, 78 });
			RunTest(Converter.ConverterType.UInt16LE, "40000", new byte[] { 64, 156 });
			RunTest(Converter.ConverterType.UInt16LE, "65535", new byte[] { 255, 255 });
			RunTest(Converter.ConverterType.UInt16LE, "", null);
			RunTest(Converter.ConverterType.UInt16LE, "-1", null);
			RunTest(Converter.ConverterType.UInt16LE, "65536", null);
			RunTest(Converter.ConverterType.UInt16LE, "Whee", null);

			// UInt32LE
			RunTest(Converter.ConverterType.UInt32LE, "0", new byte[] { 0, 0, 0, 0 });
			RunTest(Converter.ConverterType.UInt32LE, "2000000000", new byte[] { 0, 148, 53, 119 });
			RunTest(Converter.ConverterType.UInt32LE, "3000000000", new byte[] { 0, 94, 208, 178 });
			RunTest(Converter.ConverterType.UInt32LE, "4294967295", new byte[] { 255, 255, 255, 255 });
			RunTest(Converter.ConverterType.UInt32LE, "", null);
			RunTest(Converter.ConverterType.UInt32LE, "-1", null);
			RunTest(Converter.ConverterType.UInt32LE, "4294967296", null);
			RunTest(Converter.ConverterType.UInt32LE, "Whee", null);

			// UInt64LE
			RunTest(Converter.ConverterType.UInt64LE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
			RunTest(Converter.ConverterType.UInt64LE, "6000000000000000000", new byte[] { 0, 0, 88, 236, 53, 72, 68, 83 });
			RunTest(Converter.ConverterType.UInt64LE, "12000000000000000000", new byte[] { 0, 0, 176, 216, 107, 144, 136, 166 });
			RunTest(Converter.ConverterType.UInt64LE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
			RunTest(Converter.ConverterType.UInt64LE, "", null);
			RunTest(Converter.ConverterType.UInt64LE, "-1", null);
			RunTest(Converter.ConverterType.UInt64LE, "18446744073709551616", null);
			RunTest(Converter.ConverterType.UInt64LE, "Whee", null);

			// Int8LE
			RunTest(Converter.ConverterType.Int8LE, "-128", new byte[] { 128 });
			RunTest(Converter.ConverterType.Int8LE, "-28", new byte[] { 228 });
			RunTest(Converter.ConverterType.Int8LE, "72", new byte[] { 72 });
			RunTest(Converter.ConverterType.Int8LE, "127", new byte[] { 127 });
			RunTest(Converter.ConverterType.Int8LE, "", null);
			RunTest(Converter.ConverterType.Int8LE, "-129", null);
			RunTest(Converter.ConverterType.Int8LE, "128", null);
			RunTest(Converter.ConverterType.Int8LE, "Whee", null);

			// Int16LE
			RunTest(Converter.ConverterType.Int16LE, "-32768", new byte[] { 0, 128 });
			RunTest(Converter.ConverterType.Int16LE, "-12768", new byte[] { 32, 206 });
			RunTest(Converter.ConverterType.Int16LE, "7232", new byte[] { 64, 28 });
			RunTest(Converter.ConverterType.Int16LE, "32767", new byte[] { 255, 127 });
			RunTest(Converter.ConverterType.Int16LE, "", null);
			RunTest(Converter.ConverterType.Int16LE, "-32769", null);
			RunTest(Converter.ConverterType.Int16LE, "32768", null);
			RunTest(Converter.ConverterType.Int16LE, "Whee", null);

			// Int32LE
			RunTest(Converter.ConverterType.Int32LE, "-2147483648", new byte[] { 0, 0, 0, 128 });
			RunTest(Converter.ConverterType.Int32LE, "-147483648", new byte[] { 0, 148, 53, 247 });
			RunTest(Converter.ConverterType.Int32LE, "852516352", new byte[] { 0, 94, 208, 50 });
			RunTest(Converter.ConverterType.Int32LE, "2147483647", new byte[] { 255, 255, 255, 127 });
			RunTest(Converter.ConverterType.Int32LE, "", null);
			RunTest(Converter.ConverterType.Int32LE, "-2147483649", null);
			RunTest(Converter.ConverterType.Int32LE, "2147483648", null);
			RunTest(Converter.ConverterType.Int32LE, "Whee", null);

			// Int64LE
			RunTest(Converter.ConverterType.Int64LE, "-9223372036854775808", new byte[] { 0, 0, 0, 0, 0, 0, 0, 128 });
			RunTest(Converter.ConverterType.Int64LE, "-3223372036854775808", new byte[] { 0, 0, 88, 236, 53, 72, 68, 211 });
			RunTest(Converter.ConverterType.Int64LE, "2776627963145224192", new byte[] { 0, 0, 176, 216, 107, 144, 136, 38 });
			RunTest(Converter.ConverterType.Int64LE, "9223372036854775807", new byte[] { 255, 255, 255, 255, 255, 255, 255, 127 });
			RunTest(Converter.ConverterType.Int64LE, "", null);
			RunTest(Converter.ConverterType.Int64LE, "-9223372036854775809", null);
			RunTest(Converter.ConverterType.Int64LE, "9223372036854775808", null);
			RunTest(Converter.ConverterType.Int64LE, "Whee", null);

			// UInt8BE
			RunTest(Converter.ConverterType.UInt8BE, "0", new byte[] { 0 });
			RunTest(Converter.ConverterType.UInt8BE, "100", new byte[] { 100 });
			RunTest(Converter.ConverterType.UInt8BE, "200", new byte[] { 200 });
			RunTest(Converter.ConverterType.UInt8BE, "255", new byte[] { 255 });
			RunTest(Converter.ConverterType.UInt8BE, "", null);
			RunTest(Converter.ConverterType.UInt8BE, "-1", null);
			RunTest(Converter.ConverterType.UInt8BE, "256", null);
			RunTest(Converter.ConverterType.UInt8BE, "Whee", null);

			// UInt16BE
			RunTest(Converter.ConverterType.UInt16BE, "0", new byte[] { 0, 0 });
			RunTest(Converter.ConverterType.UInt16BE, "20000", new byte[] { 78, 32 });
			RunTest(Converter.ConverterType.UInt16BE, "40000", new byte[] { 156, 64 });
			RunTest(Converter.ConverterType.UInt16BE, "65535", new byte[] { 255, 255 });
			RunTest(Converter.ConverterType.UInt16BE, "", null);
			RunTest(Converter.ConverterType.UInt16BE, "-1", null);
			RunTest(Converter.ConverterType.UInt16BE, "65536", null);
			RunTest(Converter.ConverterType.UInt16BE, "Whee", null);

			// UInt32BE
			RunTest(Converter.ConverterType.UInt32BE, "0", new byte[] { 0, 0, 0, 0 });
			RunTest(Converter.ConverterType.UInt32BE, "2000000000", new byte[] { 119, 53, 148, 0 });
			RunTest(Converter.ConverterType.UInt32BE, "3000000000", new byte[] { 178, 208, 94, 0 });
			RunTest(Converter.ConverterType.UInt32BE, "4294967295", new byte[] { 255, 255, 255, 255 });
			RunTest(Converter.ConverterType.UInt32BE, "", null);
			RunTest(Converter.ConverterType.UInt32BE, "-1", null);
			RunTest(Converter.ConverterType.UInt32BE, "4294967296", null);
			RunTest(Converter.ConverterType.UInt32BE, "Whee", null);

			// UInt64BE
			RunTest(Converter.ConverterType.UInt64BE, "0", new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
			RunTest(Converter.ConverterType.UInt64BE, "6000000000000000000", new byte[] { 83, 68, 72, 53, 236, 88, 0, 0 });
			RunTest(Converter.ConverterType.UInt64BE, "12000000000000000000", new byte[] { 166, 136, 144, 107, 216, 176, 0, 0 });
			RunTest(Converter.ConverterType.UInt64BE, "18446744073709551615", new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
			RunTest(Converter.ConverterType.UInt64BE, "", null);
			RunTest(Converter.ConverterType.UInt64BE, "-1", null);
			RunTest(Converter.ConverterType.UInt64BE, "18446744073709551616", null);
			RunTest(Converter.ConverterType.UInt64BE, "Whee", null);

			// Int8BE
			RunTest(Converter.ConverterType.Int8BE, "-128", new byte[] { 128 });
			RunTest(Converter.ConverterType.Int8BE, "-28", new byte[] { 228 });
			RunTest(Converter.ConverterType.Int8BE, "72", new byte[] { 72 });
			RunTest(Converter.ConverterType.Int8BE, "127", new byte[] { 127 });
			RunTest(Converter.ConverterType.Int8BE, "", null);
			RunTest(Converter.ConverterType.Int8BE, "-129", null);
			RunTest(Converter.ConverterType.Int8BE, "128", null);
			RunTest(Converter.ConverterType.Int8BE, "Whee", null);

			// Int16BE
			RunTest(Converter.ConverterType.Int16BE, "-32768", new byte[] { 128, 0 });
			RunTest(Converter.ConverterType.Int16BE, "-12768", new byte[] { 206, 32 });
			RunTest(Converter.ConverterType.Int16BE, "7232", new byte[] { 28, 64 });
			RunTest(Converter.ConverterType.Int16BE, "32767", new byte[] { 127, 255 });
			RunTest(Converter.ConverterType.Int16BE, "", null);
			RunTest(Converter.ConverterType.Int16BE, "-32769", null);
			RunTest(Converter.ConverterType.Int16BE, "32768", null);
			RunTest(Converter.ConverterType.Int16BE, "Whee", null);

			// Int32BE
			RunTest(Converter.ConverterType.Int32BE, "-2147483648", new byte[] { 128, 0, 0, 0 });
			RunTest(Converter.ConverterType.Int32BE, "-147483648", new byte[] { 247, 53, 148, 0 });
			RunTest(Converter.ConverterType.Int32BE, "852516352", new byte[] { 50, 208, 94, 0 });
			RunTest(Converter.ConverterType.Int32BE, "2147483647", new byte[] { 127, 255, 255, 255 });
			RunTest(Converter.ConverterType.Int32BE, "", null);
			RunTest(Converter.ConverterType.Int32BE, "-2147483649", null);
			RunTest(Converter.ConverterType.Int32BE, "2147483648", null);
			RunTest(Converter.ConverterType.Int32BE, "Whee", null);

			// Int64BE
			RunTest(Converter.ConverterType.Int64BE, "-9223372036854775808", new byte[] { 128, 0, 0, 0, 0, 0, 0, 0 });
			RunTest(Converter.ConverterType.Int64BE, "-3223372036854775808", new byte[] { 211, 68, 72, 53, 236, 88, 0, 0 });
			RunTest(Converter.ConverterType.Int64BE, "2776627963145224192", new byte[] { 38, 136, 144, 107, 216, 176, 0, 0 });
			RunTest(Converter.ConverterType.Int64BE, "9223372036854775807", new byte[] { 127, 255, 255, 255, 255, 255, 255, 255 });
			RunTest(Converter.ConverterType.Int64BE, "", null);
			RunTest(Converter.ConverterType.Int64BE, "-9223372036854775809", null);
			RunTest(Converter.ConverterType.Int64BE, "9223372036854775808", null);
			RunTest(Converter.ConverterType.Int64BE, "Whee", null);

			// Single
			RunTest(Converter.ConverterType.Single, "-3.402823E+38", new byte[] { 253, 255, 127, 255 });
			RunTest(Converter.ConverterType.Single, "-1.134274E+38", new byte[] { 165, 170, 170, 254 });
			RunTest(Converter.ConverterType.Single, "1.134274E+38", new byte[] { 165, 170, 170, 126 });
			RunTest(Converter.ConverterType.Single, "3.402823E+38", new byte[] { 253, 255, 127, 127 });
			RunTest(Converter.ConverterType.Single, "", null);
			RunTest(Converter.ConverterType.Single, "-3.402824E+38", null);
			RunTest(Converter.ConverterType.Single, "3.402824E+38", null);
			RunTest(Converter.ConverterType.Single, "Whee", null);

			// Double
			RunTest(Converter.ConverterType.Double, "-1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 255 });
			RunTest(Converter.ConverterType.Double, "-5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 255 });
			RunTest(Converter.ConverterType.Double, "5.99231044954107E+307", new byte[] { 102, 85, 85, 85, 85, 85, 213, 127 });
			RunTest(Converter.ConverterType.Double, "1.79769313486231E+308", new byte[] { 226, 255, 255, 255, 255, 255, 239, 127 });
			RunTest(Converter.ConverterType.Double, "", null);
			RunTest(Converter.ConverterType.Double, "-1.79769313486232E+308", null);
			RunTest(Converter.ConverterType.Double, "1.79769313486232E+308", null);
			RunTest(Converter.ConverterType.Double, "Whee", null);

			// Strings
			RunTest(Converter.ConverterType.UTF7, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF7, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 });
			RunTest(Converter.ConverterType.UTF8, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF8, "This is my string", new byte[] { 84, 104, 105, 115, 32, 105, 115, 32, 109, 121, 32, 115, 116, 114, 105, 110, 103 });
			RunTest(Converter.ConverterType.UTF16LE, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF16LE, "This is my string", new byte[] { 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 });
			RunTest(Converter.ConverterType.UTF16BE, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF16BE, "This is my string", new byte[] { 0, 84, 0, 104, 0, 105, 0, 115, 0, 32, 0, 105, 0, 115, 0, 32, 0, 109, 0, 121, 0, 32, 0, 115, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 });
			RunTest(Converter.ConverterType.UTF32LE, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF32LE, "This is my string", new byte[] { 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103, 0, 0, 0 });
			RunTest(Converter.ConverterType.UTF32BE, "", new byte[] { });
			RunTest(Converter.ConverterType.UTF32BE, "This is my string", new byte[] { 0, 0, 0, 84, 0, 0, 0, 104, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 105, 0, 0, 0, 115, 0, 0, 0, 32, 0, 0, 0, 109, 0, 0, 0, 121, 0, 0, 0, 32, 0, 0, 0, 115, 0, 0, 0, 116, 0, 0, 0, 114, 0, 0, 0, 105, 0, 0, 0, 110, 0, 0, 0, 103 });

			// Hex
			RunTest(Converter.ConverterType.Hex, "DEADBEEF", new byte[] { 222, 173, 190, 239 });
			RunTest(Converter.ConverterType.Hex, "", new byte[] { });
			RunTest(Converter.ConverterType.Hex, "0123456789ABCDEF", new byte[] { 1, 35, 69, 103, 137, 171, 205, 239 });
			RunTest(Converter.ConverterType.Hex, "0000", new byte[] { 0, 0 });
			RunTest(Converter.ConverterType.Hex, "GEADBEEF", null);

			// HexRev
			RunTest(Converter.ConverterType.HexRev, "DEADBEEF", new byte[] { 239, 190, 173, 222 });
			RunTest(Converter.ConverterType.HexRev, "", new byte[] { });
			RunTest(Converter.ConverterType.HexRev, "0123456789ABCDEF", new byte[] { 239, 205, 171, 137, 103, 69, 35, 1 });
			RunTest(Converter.ConverterType.HexRev, "0000", new byte[] { 0, 0 });
			RunTest(Converter.ConverterType.HexRev, "GEADBEEF", null);
		}
	}
}
