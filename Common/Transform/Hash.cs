using System;
using System.IO;
using System.Security.Cryptography;

namespace NeoEdit.Common.Transform
{
	public static class Hash
	{
		public enum Type
		{
			None,
			MD5,
			SHA1,
			SHA256,
		}

		static HashAlgorithm GetHashAlgorithm(Type type)
		{
			switch (type)
			{
				case Type.MD5: return MD5.Create();
				case Type.SHA1: return SHA1.Create();
				case Type.SHA256: return SHA256.Create();
				default: throw new InvalidOperationException();
			}
		}

		public static string Get(Type type, byte[] data)
		{
			return BitConverter.ToString(GetHashAlgorithm(type).ComputeHash(data)).Replace("-", "").ToLower();
		}

		public static string Get(Type type, Stream stream)
		{
			return BitConverter.ToString(GetHashAlgorithm(type).ComputeHash(stream)).Replace("-", "").ToLower();
		}

		public static string Get(Type type, string fileName)
		{
			using (var stream = File.OpenRead(fileName))
				return Get(type, stream);
		}
	}
}
