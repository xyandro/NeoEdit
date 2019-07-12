using System;
using System.IO;
using System.IO.Compression;

namespace NeoEdit.Program.Transform
{
	public static class Compressor
	{
		public enum Type
		{
			None,
			GZip,
			Deflate,
		}

		public static byte[] Compress(byte[] data, Type type)
		{
			switch (type)
			{
				case Type.GZip:
					using (var ms = new MemoryStream())
					{
						using (var gz = new GZipStream(ms, CompressionLevel.Optimal, true))
							gz.Write(data, 0, data.Length);
						return ms.ToArray();
					}
				case Type.Deflate:
					using (var ms = new MemoryStream())
					{
						using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
							deflate.Write(data, 0, data.Length);
						return ms.ToArray();
					}
			}
			throw new InvalidOperationException();
		}

		public static byte[] Decompress(byte[] data, Type type)
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
