using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoEdit.SevenZip
{
	enum Formats
	{
		None,
		APM,
		Arj,
		BZip2,
		Cab,
		Chm,
		Compound,
		Cpio,
		CramFS,
		Deb,
		Dmg,
		ELF,
		FAT,
		FLV,
		GZip,
		HFS,
		Iso,
		Lzh,
		Lzma,
		Lzma86,
		MachO,
		MBR,
		MsLZ,
		Mub,
		Nsis,
		NTFS,
		PE,
		Ppmd,
		Rar,
		Rpm,
		SevenZip,
		Split,
		SquashFS,
		SWF,
		SWFc,
		Tar,
		TE,
		Udf,
		UEFIc,
		UEFIs,
		VHD,
		Wim,
		Xar,
		XZ,
		Z,
		Zip,
	}

	static class Format
	{
		readonly static Dictionary<Formats, Guid> formatGuids = new Dictionary<Formats, Guid>
		{
			[Formats.APM] = new Guid("23170f69-40c1-278a-1000-000110d40000"),
			[Formats.Arj] = new Guid("23170f69-40c1-278a-1000-000110040000"),
			[Formats.BZip2] = new Guid("23170f69-40c1-278a-1000-000110020000"),
			[Formats.Cab] = new Guid("23170f69-40c1-278a-1000-000110080000"),
			[Formats.Chm] = new Guid("23170f69-40c1-278a-1000-000110e90000"),
			[Formats.Compound] = new Guid("23170f69-40c1-278a-1000-000110e50000"),
			[Formats.Cpio] = new Guid("23170f69-40c1-278a-1000-000110ed0000"),
			[Formats.CramFS] = new Guid("23170f69-40c1-278a-1000-000110d30000"),
			[Formats.Deb] = new Guid("23170f69-40c1-278a-1000-000110ec0000"),
			[Formats.Dmg] = new Guid("23170f69-40c1-278a-1000-000110e40000"),
			[Formats.ELF] = new Guid("23170f69-40c1-278a-1000-000110de0000"),
			[Formats.FAT] = new Guid("23170f69-40c1-278a-1000-000110da0000"),
			[Formats.FLV] = new Guid("23170f69-40c1-278a-1000-000110d60000"),
			[Formats.GZip] = new Guid("23170f69-40c1-278a-1000-000110ef0000"),
			[Formats.HFS] = new Guid("23170f69-40c1-278a-1000-000110e30000"),
			[Formats.Iso] = new Guid("23170f69-40c1-278a-1000-000110e70000"),
			[Formats.Lzh] = new Guid("23170f69-40c1-278a-1000-000110060000"),
			[Formats.Lzma] = new Guid("23170f69-40c1-278a-1000-0001100a0000"),
			[Formats.Lzma86] = new Guid("23170f69-40c1-278a-1000-0001100b0000"),
			[Formats.MachO] = new Guid("23170f69-40c1-278a-1000-000110df0000"),
			[Formats.MBR] = new Guid("23170f69-40c1-278a-1000-000110db0000"),
			[Formats.MsLZ] = new Guid("23170f69-40c1-278a-1000-000110d50000"),
			[Formats.Mub] = new Guid("23170f69-40c1-278a-1000-000110e20000"),
			[Formats.Nsis] = new Guid("23170f69-40c1-278a-1000-000110090000"),
			[Formats.NTFS] = new Guid("23170f69-40c1-278a-1000-000110d90000"),
			[Formats.PE] = new Guid("23170f69-40c1-278a-1000-000110dd0000"),
			[Formats.Ppmd] = new Guid("23170f69-40c1-278a-1000-0001100d0000"),
			[Formats.Rar] = new Guid("23170f69-40c1-278a-1000-000110030000"),
			[Formats.Rpm] = new Guid("23170f69-40c1-278a-1000-000110eb0000"),
			[Formats.SevenZip] = new Guid("23170f69-40c1-278a-1000-000110070000"),
			[Formats.Split] = new Guid("23170f69-40c1-278a-1000-000110ea0000"),
			[Formats.SquashFS] = new Guid("23170f69-40c1-278a-1000-000110d20000"),
			[Formats.SWF] = new Guid("23170f69-40c1-278a-1000-000110d70000"),
			[Formats.SWFc] = new Guid("23170f69-40c1-278a-1000-000110d80000"),
			[Formats.Tar] = new Guid("23170f69-40c1-278a-1000-000110ee0000"),
			[Formats.TE] = new Guid("23170f69-40c1-278a-1000-000110cf0000"),
			[Formats.Udf] = new Guid("23170f69-40c1-278a-1000-000110e00000"),
			[Formats.UEFIc] = new Guid("23170f69-40c1-278a-1000-000110d00000"),
			[Formats.UEFIs] = new Guid("23170f69-40c1-278a-1000-000110d10000"),
			[Formats.VHD] = new Guid("23170f69-40c1-278a-1000-000110dc0000"),
			[Formats.Wim] = new Guid("23170f69-40c1-278a-1000-000110e60000"),
			[Formats.Xar] = new Guid("23170f69-40c1-278a-1000-000110e10000"),
			[Formats.XZ] = new Guid("23170f69-40c1-278a-1000-0001100c0000"),
			[Formats.Z] = new Guid("23170f69-40c1-278a-1000-000110050000"),
			[Formats.Zip] = new Guid("23170f69-40c1-278a-1000-000110010000"),
		};

		internal static Guid Guid(this Formats format) => formatGuids[format];

		readonly static Dictionary<string, Formats> extensionFormats = new Dictionary<string, Formats>
		{
			[".001"] = Formats.Split,
			[".7z"] = Formats.SevenZip,
			[".a"] = Formats.Deb,
			[".ar"] = Formats.Deb,
			[".arj"] = Formats.Arj,
			[".bak2"] = Formats.PE,
			[".bz2"] = Formats.BZip2,
			[".bzip2"] = Formats.BZip2,
			[".cab"] = Formats.Cab,
			[".chi"] = Formats.Chm,
			[".chm"] = Formats.Chm,
			[".chq"] = Formats.Chm,
			[".chw"] = Formats.Chm,
			[".cpio"] = Formats.Cpio,
			[".cramfs"] = Formats.CramFS,
			[".db"] = Formats.Compound,
			[".deb"] = Formats.Deb,
			[".dll"] = Formats.PE,
			[".dmg"] = Formats.Dmg,
			[".doc"] = Formats.Compound,
			[".docx"] = Formats.Zip,
			[".dylib"] = Formats.MachO,
			[".epub"] = Formats.Zip,
			[".esd"] = Formats.Wim,
			[".exe"] = Formats.PE,
			[".fat"] = Formats.FAT,
			[".flv"] = Formats.FLV,
			[".gz"] = Formats.GZip,
			[".gzip"] = Formats.GZip,
			[".hfs"] = Formats.HFS,
			[".hxi"] = Formats.Chm,
			[".hxq"] = Formats.Chm,
			[".hxr"] = Formats.Chm,
			[".hxs"] = Formats.Chm,
			[".hxw"] = Formats.Chm,
			[".img"] = Formats.Iso,
			[".iso"] = Formats.Iso,
			[".jar"] = Formats.Zip,
			[".lha"] = Formats.Lzh,
			[".lib"] = Formats.Deb,
			[".lit"] = Formats.Chm,
			[".lzh"] = Formats.Lzh,
			[".lzma"] = Formats.Lzma,
			[".lzma86"] = Formats.Lzma86,
			[".mbr"] = Formats.MBR,
			[".msi"] = Formats.Compound,
			[".msp"] = Formats.Compound,
			[".nsis"] = Formats.Nsis,
			[".ntfs"] = Formats.NTFS,
			[".ods"] = Formats.Zip,
			[".odt"] = Formats.Zip,
			[".ova"] = Formats.Tar,
			[".pmd"] = Formats.Ppmd,
			[".ppt"] = Formats.Compound,
			[".r00"] = Formats.Rar,
			[".rar"] = Formats.Rar,
			[".rpm"] = Formats.Rpm,
			[".scap"] = Formats.UEFIc,
			[".squashfs"] = Formats.SquashFS,
			[".swf"] = Formats.SWF,
			[".swm"] = Formats.Wim,
			[".sys"] = Formats.PE,
			[".tar"] = Formats.Tar,
			[".taz"] = Formats.Z,
			[".tbz"] = Formats.BZip2,
			[".tbz2"] = Formats.BZip2,
			[".te"] = Formats.TE,
			[".tgz"] = Formats.GZip,
			[".tpz"] = Formats.GZip,
			[".txz"] = Formats.XZ,
			[".udf"] = Formats.Iso,
			[".vhd"] = Formats.VHD,
			[".wcx"] = Formats.PE,
			[".wim"] = Formats.Wim,
			[".xar"] = Formats.Xar,
			[".xls"] = Formats.Compound,
			[".xlsx"] = Formats.Zip,
			[".xpi"] = Formats.Zip,
			[".xz"] = Formats.XZ,
			[".z"] = Formats.Z,
			[".zip"] = Formats.Zip,
			[".zipx"] = Formats.Zip,
		};

		internal static Formats GetExtensionFormat(string fileName)
		{
			var ext = Path.GetExtension(fileName).ToLowerInvariant();
			if (extensionFormats.ContainsKey(ext))
				return extensionFormats[ext];
			return Formats.None;
		}

		internal static Formats GetStreamFormat(Stream stream)
		{
			try
			{
				var fileSize = stream.Length;
				var formats = new List<Tuple<long, string, Formats>>
				{
					Tuple.Create(0L, "N3q8rycc", Formats.SevenZip),
					Tuple.Create(0L, "H4sI", Formats.GZip),
					Tuple.Create(0L, "UmFyIRoHAA==", Formats.Rar),
					Tuple.Create(0L, "UEsDBA==", Formats.Zip),
					Tuple.Create(0L, "XQAAQAA=", Formats.Lzma),
					Tuple.Create(0L, "H52Q", Formats.Z),
					Tuple.Create(0L, "YOo=", Formats.Arj),
					Tuple.Create(0L, "Qlpo", Formats.BZip2),
					Tuple.Create(0L, "TVNDRg==", Formats.Cab),
					Tuple.Create(0L, "SVRTRg==", Formats.Chm),
					Tuple.Create(0L, "ITxhcmNoPgpkZWJpYW4tYmluYXJ5", Formats.Deb),
					Tuple.Create(0L, "7avu2w==", Formats.Rpm),
					Tuple.Create(0L, "TVNXSU0AAAA=", Formats.Wim),
					Tuple.Create(0L, "eGFyIQ==", Formats.Xar),
					Tuple.Create(0L, "/Td6WFo=", Formats.XZ),
					Tuple.Create(0L, "RldT", Formats.SWF),
					Tuple.Create(0L, "TVo=", Formats.PE),
					Tuple.Create(0L, "f0VMRg==", Formats.ELF),
					Tuple.Create(0L, "eA==", Formats.Dmg),
					Tuple.Create(0L, "0M8R4KGxGuE=", Formats.Cab),
					Tuple.Create(2L, "LWxo", Formats.Lzh),
					Tuple.Create(257L, "dXN0YXI=", Formats.Tar),
					Tuple.Create(0x400L, "SCs=", Formats.HFS),
					Tuple.Create(0x8001L, "Q0QwMDE=", Formats.Iso),
					Tuple.Create(0x8801L, "Q0QwMDE=", Formats.Iso),
					Tuple.Create(0x9001L, "Q0QwMDE=", Formats.Iso),
					Tuple.Create(fileSize - 1024, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA==", Formats.Tar),
				};

				var groups = formats.Where(format => (format.Item1 >= 0) && (format.Item1 + format.Item2.Length / 2 <= fileSize)).GroupBy(format => format.Item1).Select(group => new { offset = group.Key, items = group.Select(format => new { bytes = Convert.FromBase64String(format.Item2), format = format.Item3 }) }).OrderBy(group => group.offset).ToList();
				foreach (var group in groups)
				{
					var bytes = new byte[group.items.Max(item => item.bytes.Length)];
					stream.Position = group.offset;
					var read = 0;
					while (read < bytes.Length)
						read += stream.Read(bytes, read, bytes.Length - read);
					var found = group.items.FirstOrDefault(item => item.bytes.Zip(bytes.Take(item.bytes.Length), (itemByte, fileByte) => itemByte == fileByte).All(x => x));
					if (found != null)
						return found.format;
				}

				return Formats.None;
			}
			finally
			{
				stream.Position = 0;
			}
		}
	}
}
