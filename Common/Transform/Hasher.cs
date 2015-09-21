using System;
using System.IO;
using System.Security.Cryptography;
using NeoEdit.Common.Transform.Hashes;

namespace NeoEdit.Common.Transform
{
	public static class Hasher
	{
		public enum Type
		{
			None,
			MD2,
			MD4,
			MD5,
			SHA1,
			SHA256,
			SHA384,
			SHA512,
			HMACMD5,
			HMACRIPEMD160,
			HMACSHA1,
			HMACSHA256,
			HMACSHA384,
			HMACSHA512,
			MACTripleDES,
			RIPEMD160,
			QuickHash,
		}

		static HashAlgorithm GetHashAlgorithm(Type type, byte[] key)
		{
			switch (type)
			{
				case Type.MD2: return new MD2();
				case Type.MD4: return new MD4();
				case Type.MD5: return MD5.Create();
				case Type.SHA1: return SHA1.Create();
				case Type.SHA256: return SHA256.Create();
				case Type.SHA384: return SHA384.Create();
				case Type.SHA512: return SHA512.Create();
				case Type.HMACMD5: return new HMACMD5(key);
				case Type.HMACRIPEMD160: return new HMACRIPEMD160(key);
				case Type.HMACSHA1: return new HMACSHA1(key);
				case Type.HMACSHA256: return new HMACSHA256(key);
				case Type.HMACSHA384: return new HMACSHA384(key);
				case Type.HMACSHA512: return new HMACSHA512(key);
				case Type.MACTripleDES: return new MACTripleDES(key);
				case Type.RIPEMD160: return RIPEMD160.Create();
				default: return null;
			}
		}

		public static string Get(byte[] data, Type type, byte[] key = null)
		{
			switch (type)
			{
				case Type.QuickHash: return Coder.BytesToString(QuickHash.ComputeHash(data), Coder.CodePage.Hex);
			}

			var hashAlg = GetHashAlgorithm(type, key);
			if (hashAlg != null)
				return Coder.BytesToString(hashAlg.ComputeHash(data), Coder.CodePage.Hex);

			throw new NotImplementedException();
		}

		public static string Get(Stream stream, Type type, byte[] key = null)
		{
			switch (type)
			{
				case Type.QuickHash: return Coder.BytesToString(QuickHash.ComputeHash(stream), Coder.CodePage.Hex);
			}

			var hashAlg = GetHashAlgorithm(type, key);
			if (hashAlg != null)
				return Coder.BytesToString(hashAlg.ComputeHash(stream), Coder.CodePage.Hex);

			throw new NotImplementedException();
		}

		public static string Get(string fileName, Type type, byte[] key = null)
		{
			using (var stream = File.OpenRead(fileName))
				return Get(stream, type, key);
		}
	}
}
