using System;

namespace NeoEdit.TextEdit.Transform
{
	public static class Colorer
	{
		public static string StringToString(string str) => ValueToString(StringToValue(str));

		public static uint StringToValue(string str)
		{
			var offset = 0;
			var value = ReadWhileIsColor(str, ref offset);
			if (offset != str.Length)
				throw new Exception("Invalid color");
			return value;
		}

		public static uint ReadWhileIsColor(string str, ref int offset)
		{
			var start = offset;
			uint value = 0;
			while (offset < str.Length)
			{
				if (offset - start > 8)
					throw new Exception("Invalid color");

				var c = str[offset];
				if ((c >= '0') && (c <= '9'))
					value = value * 16 + c - '0';
				else if ((c >= 'a') && (c <= 'f'))
					value = value * 16 + c - 'a' + 10;
				else if ((c >= 'A') && (c <= 'F'))
					value = value * 16 + c - 'A' + 10;
				else
					break;
				++offset;
			}

			switch (offset - start)
			{
				case 1: return 0xff000000 | (value << 20) | (value << 16) | (value << 12) | (value << 8) | (value << 4) | (value << 0);
				case 2: return 0xff000000 | (value << 16) | (value << 8) | (value << 0);
				case 3: return 0xff000000 | ((value & 0xf00) << 12) | ((value & 0xf00) << 8) | ((value & 0xf0) << 8) | ((value & 0xf0) << 4) | ((value & 0xf) << 4) | (value & 0xf);
				case 4: return ((value & 0xf000) << 16) | ((value & 0xf000) << 12) | ((value & 0xf00) << 12) | ((value & 0xf00) << 8) | ((value & 0xf0) << 8) | ((value & 0xf0) << 4) | ((value & 0xf) << 4) | (value & 0xf);
				case 6: return 0xff000000 | value;
				case 8: return value;
				default: throw new Exception("Invalid color");
			}
		}

		public static string ValueToString(uint value) => $"{value:x8}";

		public static void ValueToARGB(uint color, out byte alpha, out byte red, out byte green, out byte blue)
		{
			alpha = (byte)(color >> 24 & 255);
			red = (byte)(color >> 16 & 255);
			green = (byte)(color >> 8 & 255);
			blue = (byte)(color >> 0 & 255);
		}

		public static void StringToARGB(string color, out byte alpha, out byte red, out byte green, out byte blue) => ValueToARGB(StringToValue(color), out alpha, out red, out green, out blue);

		public static string ARGBToString(byte alpha, byte red, byte green, byte blue) => $"{alpha:x2}{red:x2}{green:x2}{blue:x2}";

		public static uint ARGBToValue(byte alpha, byte red, byte green, byte blue) => ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | ((uint)blue << 0);
	}
}
