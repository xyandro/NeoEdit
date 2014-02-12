﻿using System;
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
			RSA,
			DSA,
		}

		static SymmetricAlgorithm GetSymmetricAlgorithm(CryptoType type)
		{
			switch (type)
			{
				case CryptoType.AES: return new AesCryptoServiceProvider();
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
					using (var alg = GetSymmetricAlgorithm(type))
					{
						alg.Key = Convert.FromBase64String(key);

						using (var encryptor = alg.CreateEncryptor())
						using (var ms = new MemoryStream())
						{
							ms.WriteByte((byte)alg.IV.Length);
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
						using (var alg = GetSymmetricAlgorithm(type))
						{
							alg.Key = Convert.FromBase64String(key);
							alg.IV = new byte[data[0]];
							Array.Copy(data, 1, alg.IV, 0, alg.IV.Length);

							using (var decryptor = alg.CreateDecryptor())
								return decryptor.TransformFinalBlock(data, alg.IV.Length + 1, data.Length - alg.IV.Length - 1);
						}
					case CryptoType.RSA: return DecryptRSA(data, key);
				}
				throw new Exception("Failed to decrypt");
			}
			catch (Exception ex) { throw new Exception(String.Format("Decryption failed: {0}", ex.Message), ex); }
		}

		public static string GeneratePrivateKey(CryptoType type, int keySize)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.KeySize = keySize;
			return alg.ToXmlString(true);
		}

		public static string GetPublicKey(CryptoType type, string privKey)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);
			return alg.ToXmlString(false);
		}

		public static void GetKeySizeInfo(CryptoType type, out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			var alg = GetAsymmetricAlgorithm(type);
			var keySet = new HashSet<int>();
			defaultKeySize = alg.KeySize;
			foreach (var keySize in alg.LegalKeySizes)
			{
				for (var size = 1; size <= keySize.MaxSize; size <<= 1)
					if (size >= keySize.MinSize)
						keySet.Add(size);
			}
			keySizes = keySet.OrderBy(size => size).ToList();
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
