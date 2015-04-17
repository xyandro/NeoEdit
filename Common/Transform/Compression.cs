using System;
using System.IO;
using System.IO.Compression;

namespace NeoEdit.Common.Transform
{
	public static class Compression
	{
		public enum Type
		{
			None,
			GZip,
			Deflate,
		}

		public static byte[] Compress(Type type, byte[] data)
		{
			switch (type)
			{
				case Type.GZip:
					using (var ms = new MemoryStream())
					{
						using (var gz = new GZipStream(ms, CompressionMode.Compress, true))
							gz.Write(data, 0, data.Length);
						return ms.ToArray();
					}
				case Type.Deflate:
					using (var ms = new MemoryStream())
					{
						using (var deflate = new DeflateStream(ms, CompressionMode.Decompress, true))
							deflate.Write(data, 0, data.Length);
						return ms.ToArray();
					}
			}
			throw new InvalidOperationException();
		}

		public static byte[] Decompress(Type type, byte[] data)
		{
			switch (type)
			{
				case Type.GZip:
					using (var gz = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
					using (var ms = new MemoryStream())
					{
						gz.CopyTo(ms);
						return ms.ToArray();
					}
				case Type.Deflate:
					using (var inflate = new DeflateStream(new MemoryStream(data), CompressionMode.Decompress))
					using (var ms = new MemoryStream())
					{
						inflate.CopyTo(ms);
						return ms.ToArray();
					}
			}
			throw new InvalidOperationException();
		}
	}
}
