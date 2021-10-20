using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NeoEdit.Common.Transform
{
	public static class Cryptor
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

		public static string GetFormatDescription(Type type)
		{
			switch (type)
			{
				case Type.AES:
				case Type.DES:
				case Type.DES3: return "IV Length (4 bytes) + IV + Encrypted data";
				case Type.RSA:
				case Type.DSA:
				case Type.RSAAES: return "Encrypted data";
				default: return "Unknown";
			}
		}

		public static bool IsSymmetric(this Type type)
		{
			switch (type)
			{
				case Type.AES:
				case Type.DES:
				case Type.DES3:
					return true;
				case Type.RSA:
				case Type.DSA:
				case Type.RSAAES:
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

		static AsymmetricAlgorithm GetAsymmetricAlgorithm(Type type, int keySize = 0)
		{
			switch (type)
			{
				case Type.RSA: case Type.RSAAES: return new RSACryptoServiceProvider(keySize);
				case Type.DSA: return new DSACryptoServiceProvider(keySize);
				default: throw new Exception("Not an asymmetric type");
			}
		}

		public static string GetRfc2898Key(string password, string salt, int keySize)
		{
			using (var byteGenerator = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt)))
				return Convert.ToBase64String(byteGenerator.GetBytes(keySize / 8));
		}

		static void Encrypt(Stream input, Stream output, Type type, string key, Action<long> progress)
		{
			switch (type)
			{
				case Type.AES:
				case Type.DES:
				case Type.DES3: EncryptSymmetric(input, output, type, key, progress); break;
				case Type.RSAAES: EncryptRSAAES(input, output, key, progress); break;
				default: throw new Exception("Failed to encrypt");
			}
		}

		public static byte[] Encrypt(byte[] data, Type type, string key)
		{
			if (type == Type.RSA)
				return EncryptRSA(data, key);

			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream(data.Length))
			{
				Encrypt(input, output, type, key, null);
				return output.ToArray();
			}
		}

		public static void Encrypt(string fileName, Type type, string key, Action<long> progress)
		{
			string tempFile;
			using (var input = File.OpenRead(fileName))
			{
				tempFile = Path.Combine(Path.GetDirectoryName(fileName), Guid.NewGuid().ToString() + Path.GetExtension(fileName));
				using (var output = File.Create(tempFile))
					try { Encrypt(input, output, type, key, progress); }
					catch { File.Delete(tempFile); throw; }

			}
			File.Delete(fileName);
			File.Move(tempFile, fileName);
		}

		static void Decrypt(Stream input, Stream output, Type type, string key, Action<long> progress)
		{
			try
			{
				switch (type)
				{
					case Type.AES:
					case Type.DES:
					case Type.DES3: DecryptSymmetric(input, output, type, key, progress); break;
					case Type.RSAAES: DecryptRSAAES(input, output, key, progress); break;
					default: throw new Exception("Failed to decrypt");
				}
			}
			catch (Exception ex) { throw new Exception($"Decryption failed: {ex.Message}", ex); }
		}

		public static byte[] Decrypt(byte[] data, Type type, string key)
		{
			if (type == Type.RSA)
				return DecryptRSA(data, key);

			using (var input = new MemoryStream(data))
			using (var output = new MemoryStream(data.Length))
			{
				Decrypt(input, output, type, key, null);
				return output.ToArray();
			}
		}

		public static void Decrypt(string fileName, Type type, string key, Action<long> progress)
		{
			string tempFile;
			using (var input = File.OpenRead(fileName))
			{
				tempFile = Path.Combine(Path.GetDirectoryName(fileName), Guid.NewGuid().ToString() + Path.GetExtension(fileName));
				using (var output = File.Create(tempFile))
					try { Decrypt(input, output, type, key, progress); }
					catch { File.Delete(tempFile); throw; }
			}
			File.Delete(fileName);
			File.Move(tempFile, fileName);
		}

		public static string GenerateKey(Type type, int keySize)
		{
			if (IsSymmetric(type))
			{
				var alg = GetSymmetricAlgorithm(type);
				if (keySize != 0)
					alg.KeySize = keySize;
				return Convert.ToBase64String(alg.Key);
			}
			else
				return GetAsymmetricAlgorithm(type, keySize).ToXmlString(true);
		}

		public static string GetPublicKey(Type type, string privKey)
		{
			if (type.IsSymmetric())
				return privKey;

			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);
			return alg.ToXmlString(false);
		}

		public static void GetKeySizeInfo(Type type, out IEnumerable<int> keySizes, out int defaultKeySize)
		{
			KeySizes[] legalKeySizes;
			if (IsSymmetric(type))
			{
				var alg = GetSymmetricAlgorithm(type);
				defaultKeySize = alg.KeySize;
				legalKeySizes = alg.LegalKeySizes;
			}
			else
			{
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
			keySizes = keySet.OrderBy().ToList();
		}

		static void EncryptSymmetric(Stream input, Stream output, Type type, string key, Action<long> progress)
		{
			using (var alg = GetSymmetricAlgorithm(type))
			{
				alg.Key = Convert.FromBase64String(key);

				using (var encryptor = alg.CreateEncryptor())
				{
					output.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
					output.Write(alg.IV, 0, alg.IV.Length);

					var inputBuffer = new byte[65536];
					var inputSize = 0;
					var outputBuffer = new byte[inputBuffer.Length];
					while (true)
					{
						progress?.Invoke(input.Position);
						var inputCount = input.Read(inputBuffer, inputSize, inputBuffer.Length - inputSize);
						if (inputCount == 0)
							break;
						inputSize += inputCount;

						var inputUse = inputSize / alg.BlockSize * alg.BlockSize;
						if (inputUse == 0)
							continue;

						var outputCount = encryptor.TransformBlock(inputBuffer, 0, inputUse, outputBuffer, 0);
						output.Write(outputBuffer, 0, outputCount);

						Array.Copy(inputBuffer, inputUse, inputBuffer, 0, inputSize - inputUse);
						inputSize -= inputUse;
					}

					var final = encryptor.TransformFinalBlock(inputBuffer, 0, inputSize);
					output.Write(final, 0, final.Length);
				}
			}
		}

		static void DecryptSymmetric(Stream input, Stream output, Type type, string key, Action<long> progress)
		{
			using (var alg = GetSymmetricAlgorithm(type))
			{
				alg.Key = Convert.FromBase64String(key);

				var inputBuffer = new byte[65536];
				var outputBuffer = new byte[inputBuffer.Length];

				// Read IV
				Helpers.ReadFully(input, inputBuffer, 0, sizeof(int));
				var iv = new byte[BitConverter.ToInt32(inputBuffer, 0)];
				Helpers.ReadFully(input, iv, 0, iv.Length);
				alg.IV = iv;

				using (var decryptor = alg.CreateDecryptor())
				{
					while (true)
					{
						progress?.Invoke(input.Position);
						var inputCount = input.Read(inputBuffer, 0, inputBuffer.Length);
						if (inputCount == 0)
							break;

						var outputCount = decryptor.TransformBlock(inputBuffer, 0, inputCount, outputBuffer, 0);
						output.Write(outputBuffer, 0, outputCount);
					}

					var final = decryptor.TransformFinalBlock(inputBuffer, 0, 0);
					output.Write(final, 0, final.Length);
				}
			}
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

		static void EncryptRSAAES(Stream input, Stream output, string pubKey, Action<long> progress)
		{
			var aesKey = GenerateKey(Type.AES, 0);
			var encryptedAesKey = EncryptRSA(Encoding.UTF8.GetBytes(aesKey), pubKey);
			output.Write(BitConverter.GetBytes(encryptedAesKey.Length), 0, sizeof(int));
			output.Write(encryptedAesKey, 0, encryptedAesKey.Length);

			EncryptSymmetric(input, output, Type.AES, aesKey, progress);
		}

		static void DecryptRSAAES(Stream input, Stream output, string privKey, Action<long> progress)
		{
			var buffer = new byte[sizeof(int)];
			Helpers.ReadFully(input, buffer, 0, buffer.Length);

			buffer = new byte[BitConverter.ToInt32(buffer, 0)];
			Helpers.ReadFully(input, buffer, 0, buffer.Length);
			var aesKey = Encoding.UTF8.GetString(DecryptRSA(buffer, privKey));

			DecryptSymmetric(input, output, Type.AES, aesKey, progress);
		}

		public static IEnumerable<string> SigningHashes(this Type type)
		{
			switch (type)
			{
				case Type.RSA: return new List<string> { "SHA1", "SHA256", "SHA512", "MD5" };
				case Type.DSA: return new List<string> { "None" };
				default: return new List<string>();
			}
		}

		public static string Sign(byte[] data, Type type, string privKey, string hash)
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

		public static string Sign(string fileName, Type type, string privKey, string hash)
		{
			var alg = GetAsymmetricAlgorithm(type);
			alg.FromXmlString(privKey);

			using (var stream = File.OpenRead(fileName))
			{
				switch (type)
				{
					case Type.RSA: return Convert.ToBase64String((alg as RSACryptoServiceProvider).SignData(stream, hash));
					case Type.DSA: return Convert.ToBase64String((alg as DSACryptoServiceProvider).SignData(stream));
				}
			}

			throw new Exception("Unable to sign");
		}

		public static bool Verify(byte[] data, Type type, string pubKey, string hash, string signature)
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
