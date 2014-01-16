using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NeoEdit.Records.Disk
{
	internal static class Helpers
	{
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern uint GetLongPathName(string ShortPath, StringBuilder sb, int buffer);

		[DllImport("kernel32.dll")]
		static extern uint GetShortPathName(string longpath, StringBuilder sb, int buffer);

		internal static string GetWindowsPhysicalPath(string path)
		{
			StringBuilder builder = new StringBuilder(13);

			for (int pass = 0; pass < 2; pass++)
			{
				var GetPathName = (pass == 0) ? (Func<string, StringBuilder, int, uint>)GetShortPathName : GetLongPathName;

				var result = GetPathName(path, builder, builder.Capacity);
				if (result <= 0)
					throw new Exception("Invalid path");

				if (result >= builder.Capacity)
				{
					builder.Capacity = (int)result;
					result = GetPathName(path, builder, builder.Capacity);
				}

				builder[0] = Char.ToUpper(builder[0]);
				path = builder.ToString();
			}

			return path;
		}
	}
}
