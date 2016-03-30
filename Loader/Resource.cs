using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Loader
{
	class Resource
	{
		public string Name { get; set; }
		public FileTypes FileType { get; set; }
		public DateTime WriteTime { get; set; }
		public BitDepths BitDepth { get; set; }
		public int CompressedSize { get; set; }
		public int ResourceID { get; set; }
		public int UncompressedSize { get; set; }
		byte[] compressedData;
		public byte[] CompressedData
		{
			get
			{
				return compressedData ?? ResourceReader.GetBinary(ResourceID);
			}
			set
			{
				CompressedSize = value?.Length ?? CompressedSize;
				compressedData = value;
			}
		}

		public byte[] UncompressedData
		{
			get
			{
				using (var ms = new MemoryStream())
				{
					using (var gz = new GZipStream(new MemoryStream(CompressedData), CompressionMode.Decompress))
						gz.CopyTo(ms);
					return ms.ToArray();
				}
			}
			set
			{
				using (var output = new MemoryStream())
				{
					using (var gz = new GZipStream(output, CompressionLevel.Optimal, true))
					using (var input = new MemoryStream(value))
						input.CopyTo(gz);

					CompressedData = output.ToArray();
					UncompressedSize = value.Length;
				}
			}
		}

		public byte[] SerializedHeader
		{
			get
			{
				using (var ms = new MemoryStream())
				using (var msWriter = new BinaryWriter(ms, Encoding.UTF8, true))
				{
					msWriter.Write(Name);
					msWriter.Write((int)FileType);
					msWriter.Write(WriteTime.ToBinary());
					msWriter.Write((int)BitDepth);
					msWriter.Write(CompressedSize);
					msWriter.Write(UncompressedSize);
					msWriter.Write(ResourceID);
					return ms.ToArray();
				}
			}
			set
			{
				using (var ms = new MemoryStream(value))
				using (var reader = new BinaryReader(ms, Encoding.UTF8, true))
				{
					Name = reader.ReadString();
					FileType = (FileTypes)reader.ReadInt32();
					WriteTime = DateTime.FromBinary(reader.ReadInt64());
					BitDepth = (BitDepths)reader.ReadInt32();
					CompressedSize = reader.ReadInt32();
					UncompressedSize = reader.ReadInt32();
					ResourceID = reader.ReadInt32();
				}
			}
		}

		Resource() { }

		public bool NameMatch(string name) => (Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.exe", StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.dll", StringComparison.OrdinalIgnoreCase));

		public static Resource CreateFromFile(string name, string fullPath, BitDepths bitDepth)
		{
			if ((string.IsNullOrWhiteSpace(fullPath)) || (!File.Exists(fullPath)))
				return null;

			var data = File.ReadAllBytes(fullPath);
			var peInfo = new PEInfo(data);
			return new Resource
			{
				Name = name,
				FileType = peInfo.FileType,
				WriteTime = File.GetLastWriteTimeUtc(fullPath),
				BitDepth = bitDepth,
				UncompressedData = data,
			};
		}

		public static Resource CreateFromSerializedHeader(byte[] data) => new Resource { SerializedHeader = data };

		public void WriteToPath(string path)
		{
			var outputFile = Path.Combine(path, Name);
			File.WriteAllBytes(outputFile, UncompressedData);
			File.SetLastWriteTimeUtc(outputFile, WriteTime);
		}

		static public bool DataMatch(Resource x32Res, Resource x64Res)
		{
			if ((x32Res == null) || (x64Res == null))
				return false;
			if (x32Res == x64Res)
				return true;
			if (x32Res.FileType != x64Res.FileType)
				return false;
			if (x32Res.CompressedSize != x64Res.CompressedSize)
				return false;
			if (x32Res.UncompressedSize != x64Res.UncompressedSize)
				return false;
			if ((x32Res.CompressedData == null) || (x64Res.CompressedData == null))
				return false;
			if (x32Res.CompressedData.Length != x64Res.CompressedData.Length)
				return false;
			for (var ctr = 0; ctr < x32Res.CompressedData.Length; ++ctr)
				if (x32Res.CompressedData[ctr] != x64Res.CompressedData[ctr])
					return false;
			return true;
		}

		public override string ToString() => Name;
	}
}
