using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Loader
{
	class IconInfo
	{
		public byte[] IconBytes { get; }
		public byte Width { get; }
		public byte Height { get; }
		public short Planes { get; }
		public short BitsPerPixel { get; }

		public IconInfo(Icon icon)
		{
			byte[] bytes;
			using (var ms = new MemoryStream())
			{
				icon.Save(ms);
				bytes = ms.ToArray();
			}

			int pos = 0;
			var dir = GetStruct<ICONDIR>(bytes, ref pos);
			if (dir.idCount < 1)
				return;

			var dirEntry = GetStruct<ICONDIRENTRY>(bytes, ref pos);
			IconBytes = new byte[dirEntry.dwbytesInRes];
			Array.Copy(bytes, dirEntry.dwImageOffset, IconBytes, 0, IconBytes.Length);
			Width = dirEntry.bWidth;
			Height = dirEntry.bHeight;
			Planes = dirEntry.wPlanes;
			BitsPerPixel = dirEntry.wBitCount;
		}

		static T GetStruct<T>(byte[] bytes, ref int pos) where T : struct
		{
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var data = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + pos, typeof(T));
			handle.Free();
			pos += Marshal.SizeOf<T>();
			return data;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct ICONDIR
		{
			public short idReserved;
			public short idType;
			public short idCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct ICONDIRENTRY
		{
			public byte bWidth;
			public byte bHeight;
			public byte bColorCount;
			public byte bReserved;
			public short wPlanes;
			public short wBitCount;
			public int dwbytesInRes;
			public int dwImageOffset;
		}

	}
}
