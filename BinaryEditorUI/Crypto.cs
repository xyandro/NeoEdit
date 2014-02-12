using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NeoEdit.BinaryEditorUI
{
	public static class Crypto
	{
		public enum CryptoType
		{
			AES,
			DES,
			DES3,
			RSA,
			DSA,
		}

		static bool IsSymmetric(CryptoType type)
		{
			switch (type)
			{
				case CryptoType.AES:
				case CryptoType.DES:
				case CryptoType.DES3:
					return true;
				case CryptoType.RSA:
				case CryptoType.DSA:
					return false;
			}
			throw new Exception("Invalid query");
		}

		static SymmetricAlgorithm GetSymmetricAlgorithm(CryptoType type)
		{
			switch (type)
			{
				case CryptoType.AES: return new AesCryptoServiceProvider();
				case CryptoType.DES: return new DESCryptoServiceProvider();
				case CryptoType.DES3: return new TripleDESCryptoServiceProvider();
				default: throw new Exception("Not a symmetric type");
			}
		}

		static AsymmetricAlgorithm GetAsymmetricAlgorithm(CryptoType type)
		{
			switch (type)
			{
				case CryptoType.RSA: return new RSACryptoServiceProvider();
				case CryptoType.DSA: return new DSACryptoServiceProvider();
				default: throw new Exception("Not a symmetric type");
			}
		}

		public static string GetRfc2898Key(string password, string salt, int keySize)
		{
			using (var byteGenerator = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt)))
				return Convert.ToBase64String(byteGenerator.GetBytes(keySize / 8));
		}

		public static byte[] Encrypt(CryptoType type, byte[] data, string key)
		{
			switch (type)
			{
				case CryptoType.AES:
				case CryptoType.DES:
				case CryptoType.DES3:
					using (var alg = GetSymmetricAlgorithm(type))
					{
						alg.Key = Convert.FromBase64String(key);

						using (var encryptor = alg.CreateEncryptor())
						using (var ms = new MemoryStream())
						{
							ms.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
							ms.Write(alg.IV, 0, alg.IV.Length);
							var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
							ms.Write(encrypted, 0, encrypted.Length);
							return ms.ToArray();
						}
					}
				case CryptoType.RSA: return EncryptRSA(data, key);
			}
			throw new Exception("Failed to encrypt");
		}

		public static byte[] Decrypt(CryptoType type, byte[] data, string key)
		{
			try
			{
				switch (type)
				{
					case CryptoType.AES:
					case CryptoType.DES:
					case CryptoType.DES3:
						using (var alg = GetSymmetricAlgorithm(type))
						{
							alg.Key = Convert.FromBase64String(key);
							var ivLen = BitConverter.ToInt32(data, 0);
							alg.IV = new byte[ivLen];
							Array.Copy(data, sizeof(int), alg.IV, 0, alg.IV.Length);

							using (var decryptor = alg.CreateDecryptor())
								return decryptor.TransformFinalBlock(data, sizeof(int) + alg.IV.Length, data.Length - sizeof(int) - alg.IV.Length);
						}
					case CryptoType.RSA: return DecryptRSA(data, key);
				}
				throw new Exception("Failed to decrypt");
			}
			catch (Exception ex) { throw new Exception(String.Format("Decryption failed: {0}", ex.Message), ex); }
		}

		public static string GenerateKey(CryptoType type, int keySize)
		{
			if (IsSymmetric(type))
			{
				var alg = GetSymmetricAlgorithm(type);
				if (keySize != 0)
					alg.KeySize = keySize;
				return Convert.ToBase64String(alg.Key);
			}
			else
			{
				var alg = GetAsymmetricAlgorithm(type);
				if (keySize != 0)
					alg.KeySize = keySize;
				return alg.ToXmlString(true);
			}
		}

		public static string GetPublicKey(CryptoType type, string privKey)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);
			return alg.ToXmlString(false);
		}

		public static void GetKeySizeInfo(KeySizes[] legalKeySizes, out IEnumerable<int> keySizes)
		{
			var keySet = new HashSet<int>();
			foreach (var keySize in legalKeySizes)
			{
				var skip = Math.Max(keySize.SkipSize, 1);
				for (var size = keySize.MinSize; size <= keySize.MaxSize; size += skip)
					keySet.Add(size);
			}
			keySizes = keySet.OrderBy(size => size).ToList();
		}

		public static void GetSymmetricKeySizeInfo(CryptoType type, out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			var alg = GetSymmetricAlgorithm(type);
			GetKeySizeInfo(alg.LegalKeySizes, out keySizes);
			defaultKeySize = alg.KeySize;
		}

		public static void GetAsymmetricKeySizeInfo(CryptoType type, out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			var alg = GetAsymmetricAlgorithm(type);
			GetKeySizeInfo(alg.LegalKeySizes, out keySizes);
			defaultKeySize = alg.KeySize;
		}

		static byte[] EncryptRSA(byte[] data, string pubKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(pubKey);
			return rsa.Encrypt(data, false);
		}

		static byte[] DecryptRSA(byte[] data, string privKey)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(privKey);
			return rsa.Decrypt(data, false);
		}

		public static bool UseSigningHash(CryptoType type)
		{
			switch (type)
			{
				case CryptoType.RSA: return true;
				case CryptoType.DSA: return false;
			}

			throw new Exception("Invalid UseHash query");
		}

		public static string Sign(CryptoType type, byte[] data, string privKey, string hash)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);

			switch (type)
			{
				case CryptoType.RSA: return Convert.ToBase64String((alg as RSACryptoServiceProvider).SignData(data, hash));
				case CryptoType.DSA: return Convert.ToBase64String((alg as DSACryptoServiceProvider).SignData(data));
			}

			throw new Exception("Unable to sign");
		}

		public static bool Verify(CryptoType type, byte[] data, string pubKey, string hash, string signature)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(pubKey);

			switch (type)
			{
				case CryptoType.RSA: return (alg as RSACryptoServiceProvider).VerifyData(data, hash, Convert.FromBase64String(signature));
				case CryptoType.DSA: return (alg as DSACryptoServiceProvider).VerifyData(data, Convert.FromBase64String(signature));
			}

			throw new Exception("Unable to verify");
		}
	}
}
