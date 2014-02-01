using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NeoEdit.Records.Disk
{
	class Identifier
	{
		[DllImport("magic1.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr magic_open(int type);
		[DllImport("magic1.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static int magic_load(IntPtr handle, string file);
		[DllImport("magic1.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr magic_file(IntPtr handle, string file);

		static IntPtr handle;
		static Identifier()
		{
			handle = magic_open(0);
			var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "magic.mgc");
			magic_load(handle, location);
		}

		public static string Identify(string fileName)
		{
			return Marshal.PtrToStringAnsi(magic_file(handle, fileName));
		}
	}
}
