using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace NeoEdit.Loader
{
	class Resource
	{
		public string Name { get; set; }
		public bool Managed { get; set; }
		public DateTime WriteTime { get; set; }
		public byte[] CompressedData { get; set; }
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
				}
			}
		}
		public Assembly Assembly => Assembly.Load(UncompressedData);
		public byte[] SerializedData
		{
			get
			{
				using (var ms = new MemoryStream())
				using (var msWriter = new BinaryWriter(ms, Encoding.UTF8, true))
				{
					msWriter.Write(Name);
					msWriter.Write(Managed);
					msWriter.Write(WriteTime.ToBinary());
					msWriter.Write(CompressedData);
					return ms.ToArray();
				}
			}
			set
			{
				using (var ms = new MemoryStream(value))
				using (var reader = new BinaryReader(ms, Encoding.UTF8, true))
				{
					Name = reader.ReadString();
					Managed = reader.ReadBoolean();
					WriteTime = DateTime.FromBinary(reader.ReadInt64());
					CompressedData = reader.ReadBytes((int)(ms.Length - ms.Position));
				}
			}
		}

		Resource() { }

		public static bool IsAssembly(string fileName) => (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) || (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

		public bool NameMatch(string name) => (Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.exe", StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.dll", StringComparison.OrdinalIgnoreCase));

		public static Resource CreateFromFile(string fileName)
		{
			var name = Path.GetFileName(fileName);
			var managed = false;
			try
			{
				AssemblyName.GetAssemblyName(fileName);
				managed = true;
			}
			catch { }

			return new Resource { Name = name, Managed = managed, WriteTime = File.GetLastWriteTimeUtc(fileName), UncompressedData = File.ReadAllBytes(fileName) };
		}

		public static Resource CreateFromSerialized(byte[] data) => new Resource { SerializedData = data };

		public void WriteToPath(string path)
		{
			var outputFile = Path.Combine(path, Name);
			File.WriteAllBytes(outputFile, UncompressedData);
			File.SetLastWriteTimeUtc(outputFile, WriteTime);
		}

		public override string ToString() => Name;
	}
}
