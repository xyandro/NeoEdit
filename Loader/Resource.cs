using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Loader
{
	class Resource
	{
		public static string Password
		{
			set
			{
				encryptionKey = null;

				if (string.IsNullOrWhiteSpace(value))
					return;

				using (var byteGenerator = new Rfc2898DeriveBytes(value, Convert.FromBase64String("nMrXc9KbRi9a1sc67ASTcIY+IFydLdFh")))
					encryptionKey = byteGenerator.GetBytes(32);
			}
		}
		static byte[] encryptionKey;

		public ResourceHeader Header { get; private set; }
		public byte[] Data { get; private set; }
		public byte[] RawData { get; private set; }

		Resource() { }

		public static Resource CreateFromFile(string name, string fullPath, BitDepths bitDepth)
		{
			if ((string.IsNullOrWhiteSpace(fullPath)) || (!File.Exists(fullPath)))
				return null;

			var rawData = File.ReadAllBytes(fullPath);
			var data = rawData;
			using (var output = new MemoryStream())
			{
				using (var gz = new GZipStream(output, CompressionLevel.Optimal, true))
				using (var input = new MemoryStream(data))
					input.CopyTo(gz);

				data = output.ToArray();
			}

			if (encryptionKey != null)
			{
				using (var alg = new AesCryptoServiceProvider { Key = encryptionKey })
				using (var encryptor = alg.CreateEncryptor())
				using (var ms = new MemoryStream())
				{
					ms.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
					ms.Write(alg.IV, 0, alg.IV.Length);
					var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
					ms.Write(encrypted, 0, encrypted.Length);
					data = ms.ToArray();
				}
			}

			var peInfo = new PEInfo(rawData);
			var sha1 = BitConverter.ToString(new SHA1Managed().ComputeHash(rawData)).Replace("-", "").ToLower();

			return new Resource
			{
				Header = new ResourceHeader
				{
					Name = name,
					FileType = peInfo.FileType,
					WriteTime = File.GetLastWriteTimeUtc(fullPath),
					BitDepth = bitDepth,
					SHA1 = sha1,
				},
				RawData = rawData,
				Data = data,
			};
		}

		public static Resource CreateFromHeader(ResourceHeader header)
		{
			var data = ResourceReader.GetBinary(header.ResourceID);

			var rawData = data;

			if (encryptionKey != null)
			{
				try
				{
					using (var alg = new AesCryptoServiceProvider { Key = encryptionKey })
					{
						var iv = new byte[BitConverter.ToInt32(rawData, 0)];
						Array.Copy(rawData, sizeof(int), iv, 0, iv.Length);
						alg.IV = iv;

						using (var decryptor = alg.CreateDecryptor())
							rawData = decryptor.TransformFinalBlock(rawData, sizeof(int) + iv.Length, rawData.Length - sizeof(int) - iv.Length);
					}
				}
				catch
				{
					throw new Exception("Failed to decrypt; password invalid.");
				}
			}

			using (var ms = new MemoryStream())
			{
				using (var gz = new GZipStream(new MemoryStream(rawData), CompressionMode.Decompress))
					gz.CopyTo(ms);
				rawData = ms.ToArray();
			}

			return new Resource
			{
				Header = header,
				Data = data,
				RawData = rawData,
			};
		}

		public void WriteToPath(string path) => File.WriteAllBytes(Path.Combine(path, Header.Name), RawData);

		static public bool DataMatch(Resource x32Res, Resource x64Res)
		{
			if ((x32Res == null) || (x64Res == null))
				return false;
			if (x32Res == x64Res)
				return true;
			if (!ResourceHeader.DataMatch(x32Res.Header, x64Res.Header))
				return false;
			if ((x32Res.RawData == null) || (x64Res.RawData == null))
				return false;
			if (x32Res.RawData.Length != x64Res.RawData.Length)
				return false;
			for (var ctr = 0; ctr < x32Res.RawData.Length; ++ctr)
				if (x32Res.RawData[ctr] != x64Res.RawData[ctr])
					return false;
			return true;
		}

		public override string ToString() => Header.Name;
	}
}
