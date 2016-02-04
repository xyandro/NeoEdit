using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Loader
{
	static class ResourceReader
	{
		public static Config Config { get; }

		public static bool HasData => Config != null;

		static ResourceReader()
		{
			Config = null;
			var data = GetBinary(1, false);
			if (data != null)
				Config = new Config { SerializedData = data };
		}

		public static byte[] GetBinary(int id, bool throwOnFailure = true)
		{
			var resource = Native.FindResource(IntPtr.Zero, (IntPtr)id, Native.RT_RCDATA);
			if (resource == IntPtr.Zero)
				if (throwOnFailure)
					throw new Exception("Failed to get resource");
				else
					return null;
			var loaded = Native.LoadResource(IntPtr.Zero, resource);
			var size = Native.SizeofResource(IntPtr.Zero, resource);
			var locked = Native.LockResource(loaded);
			var result = new byte[size];
			Marshal.Copy(locked, result, 0, size);
			return result;
		}

		public static IEnumerable<Resource> AllResources => Config.Resources;

		public static IEnumerable<Resource> Resources
		{
			get
			{
				var type = Environment.Is64BitProcess ? BitDepths.x64 : BitDepths.x32;
				foreach (var resource in AllResources)
					if ((resource.BitDepth == type) || (resource.BitDepth == BitDepths.Any))
						yield return resource;
			}
		}
	}
}
