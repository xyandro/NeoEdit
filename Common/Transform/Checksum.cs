using System;
using System.Security.Cryptography;

namespace NeoEdit.Common.Transform
{
	public static class Checksum
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
	}
}
