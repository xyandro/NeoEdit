using System;
using System.IO;
using System.Security.Cryptography;

namespace NeoEdit.Common.Transform
{
	public static class Hasher
	{
		public enum Type
		{
			None,
			QuickHash,
			MD5,
			SHA1,
			SHA256,
			SHA384,
			SHA512,
		}

		static HashAlgorithm GetHashAlgorithm(Type type)
		{
			switch (type)
			{
				case Type.MD5: return MD5.Create();
				case Type.SHA1: return SHA1.Create();
				case Type.SHA256: return SHA256.Create();
				case Type.SHA384: return SHA384.Create();
				case Type.SHA512: return SHA512.Create();
				default: throw new NotImplementedException();
			}
		}

		static string Get(Stream input, Type type, Action<long> progress)
		{
			if (type == Type.QuickHash)
				return Coder.BytesToString(ComputeQuickHash(input), Coder.CodePage.Hex);

			var hashAlg = GetHashAlgorithm(type);
			hashAlg.Initialize();
			var buffer = new byte[65536];
			while (true)
			{
				progress?.Invoke(input.Position);

				var block = input.Read(buffer, 0, buffer.Length);
				if (block == 0)
					break;
				hashAlg.TransformBlock(buffer, 0, block, null, 0);
			}
			hashAlg.TransformFinalBlock(buffer, 0, 0);
			return Coder.BytesToString(hashAlg.Hash, Coder.CodePage.Hex);
		}

		public static string Get(string fileName, Type type, Action<long> progress)
		{
			using (var stream = File.OpenRead(fileName))
				return Get(stream, type, progress);
		}

		public static string Get(byte[] data, Type type)
		{
			using (var stream = new MemoryStream(data))
				return Get(stream, type, null);
		}

		public static byte[] ComputeQuickHash(Stream stream)
		{
			const int QuickHashBlockSize = 2048;

			var hash = MD5.Create();
			hash.Initialize();

			var blockSize = (int)Math.Min(stream.Length, QuickHashBlockSize);
			var buffer = new byte[blockSize];

			var length = BitConverter.GetBytes(stream.Length);
			hash.TransformBlock(length, 0, length.Length, null, 0);

			// First block
			stream.Position = 0;
			stream.Read(buffer, 0, blockSize);
			hash.TransformBlock(buffer, 0, buffer.Length, null, 0);

			// Last block
			stream.Position = stream.Length - blockSize;
			stream.Read(buffer, 0, blockSize);
			hash.TransformFinalBlock(buffer, 0, buffer.Length);

			return hash.Hash;
		}
	}
}
