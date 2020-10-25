using System;
using System.IO;
using System.Text;

namespace NeoEdit.Loader
{
	class ResourceHeader
	{
		public int ResourceID { get; set; }
		public string Name { get; set; }
		public FileTypes FileType { get; set; }
		public DateTime WriteTime { get; set; }
		public BitDepths BitDepth { get; set; }
		public string SHA1 { get; set; }

		public byte[] ToBytes()
		{
			using (var ms = new MemoryStream())
			using (var msWriter = new BinaryWriter(ms, Encoding.UTF8, true))
			{
				msWriter.Write(ResourceID);
				msWriter.Write(Name);
				msWriter.Write((int)FileType);
				msWriter.Write(WriteTime.ToBinary());
				msWriter.Write((int)BitDepth);
				msWriter.Write(SHA1);
				return ms.ToArray();
			}
		}

		public static ResourceHeader FromBytes(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			using (var reader = new BinaryReader(ms, Encoding.UTF8, true))
			{
				return new ResourceHeader
				{
					ResourceID = reader.ReadInt32(),
					Name = reader.ReadString(),
					FileType = (FileTypes)reader.ReadInt32(),
					WriteTime = DateTime.FromBinary(reader.ReadInt64()),
					BitDepth = (BitDepths)reader.ReadInt32(),
					SHA1 = reader.ReadString(),
				};
			}
		}

		public bool NameMatch(string name) => (Name.Equals(name, StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.exe", StringComparison.OrdinalIgnoreCase)) || (Name.Equals($"{name}.dll", StringComparison.OrdinalIgnoreCase));

		public static bool DataMatch(ResourceHeader x32Res, ResourceHeader x64Res)
		{
			if ((x32Res == null) || (x64Res == null))
				return false;
			if (x32Res == x64Res)
				return true;
			if (x32Res.FileType != x64Res.FileType)
				return false;
			if (x32Res.SHA1 != x64Res.SHA1)
				return false;
			return true;
		}

		public void SetDate(string path) => File.SetLastWriteTimeUtc(Path.Combine(path, Name), WriteTime);

		public override string ToString() => Name;
	}
}
