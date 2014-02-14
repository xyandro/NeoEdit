﻿using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NeoEdit.Data
{
	public static class Crypto
	{
		public enum Type
		{
			None,
			AES,
			DES,
			DES3,
			RSA,
			DSA,
			RSAAES,
		}

		static Type BaseType(Type type)
		{
			if (type == Type.RSAAES)
				return Type.RSA;
			return type;
		}

		static bool IsSymmetric(Type type)
		{
			switch (BaseType(type))
			{
				case Type.AES:
				case Type.DES:
				case Type.DES3:
					return true;
				case Type.RSA:
				case Type.DSA:
					return false;
			}
			throw new Exception("Invalid query");
		}

		static SymmetricAlgorithm GetSymmetricAlgorithm(Type type)
		{
			switch (type)
			{
				case Type.AES: return new AesCryptoServiceProvider();
				case Type.DES: return new DESCryptoServiceProvider();
				case Type.DES3: return new TripleDESCryptoServiceProvider();
				default: throw new Exception("Not a symmetric type");
			}
		}

		static AsymmetricAlgorithm GetAsymmetricAlgorithm(Type type)
		{
			switch (type)
			{
				case Type.RSA: return new RSACryptoServiceProvider();
				case Type.DSA: return new DSACryptoServiceProvider();
				default: throw new Exception("Not an asymmetric type");
			}
		}

		public static string GetRfc2898Key(string password, string salt, int keySize)
		{
			using (var byteGenerator = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt)))
				return Convert.ToBase64String(byteGenerator.GetBytes(keySize / 8));
		}

		public static byte[] Encrypt(Type type, byte[] data, string key)
		{
			switch (type)
			{
				case Type.AES:
				case Type.DES:
				case Type.DES3:
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
				case Type.RSA: return EncryptRSA(data, key);
				case Type.RSAAES: return EncryptRSAAES(data, key);
			}
			throw new Exception("Failed to encrypt");
		}

		public static byte[] Decrypt(Type type, byte[] data, string key)
		{
			try
			{
				switch (type)
				{
					case Type.AES:
					case Type.DES:
					case Type.DES3:
						using (var alg = GetSymmetricAlgorithm(type))
						{
							alg.Key = Convert.FromBase64String(key);

							var iv = new byte[BitConverter.ToInt32(data, 0)];
							Array.Copy(data, sizeof(int), iv, 0, iv.Length);
							alg.IV = iv;

							using (var decryptor = alg.CreateDecryptor())
								return decryptor.TransformFinalBlock(data, sizeof(int) + iv.Length, data.Length - sizeof(int) - iv.Length);
						}
					case Type.RSA: return DecryptRSA(data, key);
					case Type.RSAAES: return DecryptRSAAES(data, key);
				}
				throw new Exception("Failed to decrypt");
			}
			catch (Exception ex) { throw new Exception(String.Format("Decryption failed: {0}", ex.Message), ex); }
		}

		public static string GenerateKey(Type type, int keySize)
		{
			type = BaseType(type);

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

		public static string GetPublicKey(Type type, string privKey)
		{
			type = BaseType(type);
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);
			return alg.ToXmlString(false);
		}

		public static void GetKeySizeInfo(Type type, out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			type = BaseType(type);
			KeySizes[] legalKeySizes;
			if (IsSymmetric(type))
			{
				type = BaseType(type);
				var alg = GetSymmetricAlgorithm(type);
				defaultKeySize = alg.KeySize;
				legalKeySizes = alg.LegalKeySizes;
			}
			else
			{
				type = BaseType(type);
				var alg = GetAsymmetricAlgorithm(type);
				defaultKeySize = alg.KeySize;
				legalKeySizes = alg.LegalKeySizes;
			}

			var keySet = new HashSet<int>();
			foreach (var keySize in legalKeySizes)
			{
				var skip = Math.Max(keySize.SkipSize, 1);
				for (var size = keySize.MinSize; size <= keySize.MaxSize; size += skip)
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

		static byte[] EncryptRSAAES(byte[] data, string pubKey)
		{
			var aesKey = GenerateKey(Type.AES, 0);
			using (var ms = new MemoryStream())
			{
				var encryptedAesKey = Encrypt(Type.RSA, Encoding.UTF8.GetBytes(aesKey), pubKey);
				var encryptedData = Encrypt(Type.AES, data, aesKey);

				ms.Write(BitConverter.GetBytes(encryptedAesKey.Length), 0, sizeof(int));
				ms.Write(encryptedAesKey, 0, encryptedAesKey.Length);
				ms.Write(encryptedData, 0, encryptedData.Length);

				return ms.ToArray();
			}
		}

		static byte[] DecryptRSAAES(byte[] data, string privKey)
		{
			var encryptedAesKey = new byte[BitConverter.ToInt32(data, 0)];
			Array.Copy(data, sizeof(int), encryptedAesKey, 0, encryptedAesKey.Length);
			var aesKey = Encoding.UTF8.GetString(Decrypt(Type.RSA, encryptedAesKey, privKey));
			var encryptedData = data.Skip(sizeof(int) + encryptedAesKey.Length).ToArray();
			return Decrypt(Type.AES, encryptedData, aesKey);
		}

		public static bool UseSigningHash(Type type)
		{
			switch (type)
			{
				case Type.RSA: return true;
				case Type.DSA: return false;
			}

			throw new Exception("Invalid UseHash query");
		}

		public static string Sign(Type type, byte[] data, string privKey, string hash)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);

			switch (type)
			{
				case Type.RSA: return Convert.ToBase64String((alg as RSACryptoServiceProvider).SignData(data, hash));
				case Type.DSA: return Convert.ToBase64String((alg as DSACryptoServiceProvider).SignData(data));
			}

			throw new Exception("Unable to sign");
		}

		public static bool Verify(Type type, byte[] data, string pubKey, string hash, string signature)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(pubKey);

			switch (type)
			{
				case Type.RSA: return (alg as RSACryptoServiceProvider).VerifyData(data, hash, Convert.FromBase64String(signature));
				case Type.DSA: return (alg as DSACryptoServiceProvider).VerifyData(data, Convert.FromBase64String(signature));
			}

			throw new Exception("Unable to verify");
		}
	}
}
