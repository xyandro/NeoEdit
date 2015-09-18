using System;
using System.IO;
using System.Security.Cryptography;

namespace NeoEdit.Common.Transform
{
	abstract class Hasher
	{
		public abstract byte[] ComputeHash(byte[] data);
		public abstract byte[] ComputeHash(Stream stream);
	}

	class MD5Hasher : Hasher
	{
		public override byte[] ComputeHash(byte[] data) { return MD5.Create().ComputeHash(data); }
		public override byte[] ComputeHash(Stream stream) { return MD5.Create().ComputeHash(stream); }
	}

	class SHA1Hasher : Hasher
	{
		public override byte[] ComputeHash(byte[] data) { return SHA1.Create().ComputeHash(data); }
		public override byte[] ComputeHash(Stream stream) { return SHA1.Create().ComputeHash(stream); }
	}

	class SHA256Hasher : Hasher
	{
		public override byte[] ComputeHash(byte[] data) { return SHA256.Create().ComputeHash(data); }
		public override byte[] ComputeHash(Stream stream) { return SHA256.Create().ComputeHash(stream); }
	}

	class QuickHasher : Hasher
	{
		const int BlockSize = 2048;

		public override byte[] ComputeHash(byte[] data)
		{
			var hash = SHA256.Create();
			hash.Initialize();

			var blockSize = (int)Math.Min(data.LongLength, BlockSize);

			var length = BitConverter.GetBytes(data.LongLength);
			hash.TransformBlock(length, 0, length.Length, null, 0);

			hash.TransformBlock(data, 0, blockSize, null, 0); // First block
			hash.TransformFinalBlock(data, data.Length - blockSize, blockSize); // Last block

			return hash.Hash;
		}

		public override byte[] ComputeHash(Stream stream)
		{
			var hash = SHA256.Create();
			hash.Initialize();

			var blockSize = (int)Math.Min(stream.Length, BlockSize);
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

	public static class Hash
	{
		public enum Type
		{
			None,
			MD5,
			SHA1,
			SHA256,
			QuickHash,
		}

		static Hasher GetHasher(Type type)
		{
			switch (type)
			{
				case Type.MD5: return new MD5Hasher();
				case Type.SHA1: return new SHA1Hasher();
				case Type.SHA256: return new SHA256Hasher();
				case Type.QuickHash: return new QuickHasher();
				default: throw new InvalidOperationException();
			}
		}

		public static string Get(Type type, byte[] data)
		{
			return Coder.BytesToString(GetHasher(type).ComputeHash(data), Coder.CodePage.Hex);
		}

		public static string Get(Type type, Stream stream)
		{
			return Coder.BytesToString(GetHasher(type).ComputeHash(stream), Coder.CodePage.Hex);
		}

		public static string Get(Type type, string fileName)
		{
			using (var stream = File.OpenRead(fileName))
				return Get(type, stream);
		}
	}
}
