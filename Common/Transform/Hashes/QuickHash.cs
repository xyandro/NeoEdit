using System;
using System.IO;
using System.Security.Cryptography;

namespace NeoEdit.Common.Transform.Hashes
{
	public static class QuickHash
	{
		const int QuickHashBlockSize = 2048;

		public static byte[] ComputeHash(byte[] data)
		{
			var hash = MD5.Create();
			hash.Initialize();

			var blockSize = (int)Math.Min(data.LongLength, QuickHashBlockSize);

			var length = BitConverter.GetBytes(data.LongLength);
			hash.TransformBlock(length, 0, length.Length, null, 0);

			hash.TransformBlock(data, 0, blockSize, null, 0); // First block
			hash.TransformFinalBlock(data, data.Length - blockSize, blockSize); // Last block

			return hash.Hash;
		}

		public static byte[] ComputeHash(Stream stream)
		{
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
