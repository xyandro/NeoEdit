using System;
using System.IO;
using System.IO.Compression;

namespace NeoEdit.Common.Transform
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
			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream(data.Length))
			{
				Compress(input, output, type, null);
				return output.ToArray();
			}
		}

		public static void Compress(string fileName, Type type, Action<long> progress)
		{
			string tempFile;
			using (var input = File.OpenRead(fileName))
			{
				tempFile = Path.Combine(Path.GetDirectoryName(fileName), Guid.NewGuid().ToString() + Path.GetExtension(fileName));
				using (var output = File.Create(tempFile))
					try { Compress(input, output, type, progress); }
					catch { File.Delete(tempFile); throw; }
			}
			File.Delete(fileName);
			File.Move(tempFile, fileName);
		}

		public static byte[] Decompress(byte[] data, Type type)
		{
			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream(data.Length))
			{
				Decompress(input, output, type, null);
				return output.ToArray();
			}
		}

		public static void Decompress(string fileName, Type type, Action<long> progress)
		{
			string tempFile;
			using (var input = File.OpenRead(fileName))
			{
				tempFile = Path.Combine(Path.GetDirectoryName(fileName), Guid.NewGuid().ToString() + Path.GetExtension(fileName));
				using (var output = File.Create(tempFile))
					try { Decompress(input, output, type, progress); }
					catch { File.Delete(tempFile); throw; }
			}
			File.Delete(fileName);
			File.Move(tempFile, fileName);
		}

		static void Compress(Stream input, Stream output, Type type, Action<long> progress)
		{
			Stream stream;
			switch (type)
			{
				case Type.GZip: stream = new GZipStream(output, CompressionLevel.Optimal, true); break;
				case Type.Deflate: stream = new DeflateStream(output, CompressionLevel.Optimal, true); break;
				default: throw new InvalidOperationException();
			}
			using (stream)
			{
				var block = new byte[65536];
				while (true)
				{
					progress?.Invoke(input.Position);
					var count = input.Read(block, 0, block.Length);
					if (count == 0)
						break;
					stream.Write(block, 0, count);
				}
			}
		}

		static void Decompress(Stream input, Stream output, Type type, Action<long> progress)
		{
			Stream stream;
			switch (type)
			{
				case Type.GZip: stream = new GZipStream(input, CompressionMode.Decompress); break;
				case Type.Deflate: stream = new DeflateStream(input, CompressionMode.Decompress); break;
				default: throw new InvalidOperationException();
			}
			using (stream)
			{
				var block = new byte[65536];
				while (true)
				{
					progress?.Invoke(input.Position);
					var count = stream.Read(block, 0, block.Length);
					if (count == 0)
						break;
					output.Write(block, 0, count);
				}
			}
		}
	}
}
