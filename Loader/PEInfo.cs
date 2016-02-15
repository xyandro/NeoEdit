using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Loader
{
	class PEInfo
	{
		public bool IsPE { get; }
		public BitDepths BitDepth { get; }
		public BitDepths PreferBitDepth { get; }
		public FileTypes FileType { get; }
		public byte[] Bytes { get; }
		public IEnumerable<string> ResourceNames => resources.Select(res => res.Item1);

		List<Tuple<string, int, int>> resources = new List<Tuple<string, int, int>>();
		int? corHeaderFlagsOffset;
		int pos = 0;
		public PEInfo(string fileName) : this(File.ReadAllBytes(fileName)) { }

		public PEInfo(byte[] bytes)
		{
			Bytes = bytes;

			var dosHeader = GetStruct<IMAGE_DOS_HEADER>();
			IsPE = dosHeader.isValid;
			if (!IsPE)
				return;

			IMAGE_NT_HEADERS ntHeaders;
			pos = dosHeader.e_lfanew;
			var ntHeaders32 = GetStruct<IMAGE_NT_HEADERS32>();
			pos = dosHeader.e_lfanew;
			var ntHeaders64 = GetStruct<IMAGE_NT_HEADERS64>();
			if (ntHeaders32.isValid)
			{
				BitDepth = PreferBitDepth = BitDepths.x32;
				ntHeaders = ntHeaders32;
			}
			else if (ntHeaders64.isValid)
			{
				BitDepth = PreferBitDepth = BitDepths.x64;
				ntHeaders = ntHeaders64;
			}
			else
				throw new Exception("Invalid file");

			pos = dosHeader.e_lfanew + Marshal.SizeOf(ntHeaders);

			FileType = FileTypes.Native;

			var sections = Enumerable.Range(0, ntHeaders.FileHeader.NumberOfSections).Select(offset => GetStruct<IMAGE_SECTION_HEADER>()).ToList();

			const int highBit = 1 << 31;
			if ((ntHeaders.OptionalHeader.ResourceTable.HasValue) && (ntHeaders.OptionalHeader.ResourceTable.Value.VirtualAddress != 0) && (ntHeaders.OptionalHeader.ResourceTable.Value.Size != 0))
			{
				var virtualAddress = ntHeaders.OptionalHeader.ResourceTable.Value.VirtualAddress;
				var section = sections.Single(sec => (virtualAddress >= sec.VirtualAddress) && (virtualAddress < sec.VirtualAddress + sec.SizeOfRawData));
				var resourceStart = virtualAddress - section.VirtualAddress + section.PointerToRawData;
				var directoryLocations = new List<Tuple<string, int>> { Tuple.Create("", resourceStart) };
				for (var directoryLocationIndex = 0; directoryLocationIndex < directoryLocations.Count; ++directoryLocationIndex)
				{
					var directoryLocation = directoryLocations[directoryLocationIndex];
					pos = directoryLocation.Item2;
					var resDir = GetStruct<IMAGE_RESOURCE_DIRECTORY>();
					var count = resDir.NumberOfNamedEntries + resDir.NumberOfIdEntries;
					var entries = Enumerable.Range(0, count).Select(num => GetStruct<IMAGE_RESOURCE_DIRECTORY_ENTRY>());
					foreach (var entry in entries)
					{
						string name;
						if ((entry.NameId & highBit) != 0)
						{
							var offset = (int)(resourceStart + entry.NameId & ~highBit);
							var len = BitConverter.ToInt16(bytes, offset);
							name = Encoding.Unicode.GetString(bytes, offset + sizeof(short), len * 2);
						}
						else
							name = entry.NameId.ToString();

						if ((entry.Data & highBit) != 0)
							directoryLocations.Add(Tuple.Create(directoryLocation.Item1 + @"\" + name, (int)(resourceStart + entry.Data & ~highBit)));
						else
						{
							pos = (int)(resourceStart + entry.Data);
							var dataEntry = GetStruct<IMAGE_RESOURCE_DATA_ENTRY>();
							resources.Add(Tuple.Create(directoryLocation.Item1, dataEntry.Data - section.VirtualAddress + section.PointerToRawData, dataEntry.Size));
						}
					}
				}
			}

			if ((ntHeaders.OptionalHeader.CLRRuntimeHeader.HasValue) && (ntHeaders.OptionalHeader.CLRRuntimeHeader.Value.VirtualAddress != 0) && (ntHeaders.OptionalHeader.CLRRuntimeHeader.Value.Size != 0))
			{
				var virtualAddress = ntHeaders.OptionalHeader.CLRRuntimeHeader.Value.VirtualAddress;
				var section = sections.Single(sec => (virtualAddress >= sec.VirtualAddress) && (virtualAddress < sec.VirtualAddress + sec.SizeOfRawData));
				pos = virtualAddress - section.VirtualAddress + section.PointerToRawData;

				corHeaderFlagsOffset = pos + Marshal.OffsetOf<IMAGE_COR20_HEADER>(nameof(IMAGE_COR20_HEADER.Flags)).ToInt32();
				var corHeader = GetStruct<IMAGE_COR20_HEADER>();
				if (!corHeader.Flags.HasFlag(IMAGE_COR20_HEADER_FLAGS.ILOnly))
					FileType = FileTypes.Mixed;
				else
				{
					FileType = FileTypes.Managed;

					if ((BitDepth == BitDepths.x32) && (corHeader.Flags.HasFlag(IMAGE_COR20_HEADER_FLAGS.x32BitRequired) == corHeader.Flags.HasFlag(IMAGE_COR20_HEADER_FLAGS.x32BitPreferred)))
					{
						BitDepth = BitDepths.Any;
						if (!corHeader.Flags.HasFlag(IMAGE_COR20_HEADER_FLAGS.x32BitPreferred))
							PreferBitDepth = BitDepths.x64;
					}
				}
			}
		}

		public byte[] GetResource(string name)
		{
			var resource = resources.Single(res => res.Item1 == name);
			var result = new byte[resource.Item3];
			Array.Copy(Bytes, resource.Item2, result, 0, result.Length);
			return result;
		}

		public IMAGE_COR20_HEADER_FLAGS CorFlags
		{
			get
			{
				if (!corHeaderFlagsOffset.HasValue)
					return default(IMAGE_COR20_HEADER_FLAGS);
				return (IMAGE_COR20_HEADER_FLAGS)BitConverter.ToInt32(Bytes, corHeaderFlagsOffset.Value);
			}
			set
			{
				if (!corHeaderFlagsOffset.HasValue)
					throw new Exception($"Only managed assemblies have {nameof(CorFlags)}");
				var data = BitConverter.GetBytes((int)value);
				Array.Copy(data, 0, Bytes, corHeaderFlagsOffset.Value, sizeof(int));
			}
		}

		T GetStruct<T>() where T : struct
		{
			var handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
			var data = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + pos, typeof(T));
			handle.Free();
			pos += Marshal.SizeOf<T>();
			return data;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_DOS_HEADER
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public char[] e_magic;
			public short e_cblp;
			public short e_cp;
			public short e_crlc;
			public short e_cparhdr;
			public short e_minalloc;
			public short e_maxalloc;
			public short e_ss;
			public short e_sp;
			public short e_csum;
			public short e_ip;
			public short e_cs;
			public short e_lfarlc;
			public short e_ovno;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public short[] e_res1;
			public short e_oemid;
			public short e_oeminfo;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
			public short[] e_res2;
			public int e_lfanew;

			private string _e_magic => new string(e_magic);
			public bool isValid => _e_magic == "MZ";
		}

		interface IMAGE_NT_HEADERS
		{
			IMAGE_FILE_HEADER FileHeader { get; }
			IMAGE_OPTIONAL_HEADER OptionalHeader { get; }
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_NT_HEADERS32 : IMAGE_NT_HEADERS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER32 OptionalHeader;

			private string _Signature => new string(Signature);
			public bool isValid => (_Signature == "PE\0\0") && (OptionalHeader.Magic == MagicType.IMAGE_NT_OPTIONAL_HDR32_MAGIC);

			IMAGE_FILE_HEADER IMAGE_NT_HEADERS.FileHeader => FileHeader;
			IMAGE_OPTIONAL_HEADER IMAGE_NT_HEADERS.OptionalHeader => OptionalHeader;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_NT_HEADERS64 : IMAGE_NT_HEADERS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER64 OptionalHeader;

			private string _Signature => new string(Signature);
			public bool isValid => (_Signature == "PE\0\0") && (OptionalHeader.Magic == MagicType.IMAGE_NT_OPTIONAL_HDR64_MAGIC);

			IMAGE_FILE_HEADER IMAGE_NT_HEADERS.FileHeader => FileHeader;
			IMAGE_OPTIONAL_HEADER IMAGE_NT_HEADERS.OptionalHeader => OptionalHeader;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_FILE_HEADER
		{
			public short Machine;
			public short NumberOfSections;
			public int TimeDateStamp;
			public int PointerToSymbolTable;
			public int NumberOfSymbols;
			public short SizeOfOptionalHeader;
			public short Characteristics;
		}

		interface IMAGE_OPTIONAL_HEADER
		{
			IMAGE_DATA_DIRECTORY? ResourceTable { get; }
			IMAGE_DATA_DIRECTORY? CLRRuntimeHeader { get; }
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_OPTIONAL_HEADER32 : IMAGE_OPTIONAL_HEADER
		{
			public MagicType Magic;
			public byte MajorLinkerVersion;
			public byte MinorLinkerVersion;
			public uint SizeOfCode;
			public uint SizeOfInitializedData;
			public uint SizeOfUninitializedData;
			public uint AddressOfEntryPoint;
			public uint BaseOfCode;
			public uint BaseOfData;
			public uint ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort MajorOperatingSystemVersion;
			public ushort MinorOperatingSystemVersion;
			public ushort MajorImageVersion;
			public ushort MinorImageVersion;
			public ushort MajorSubsystemVersion;
			public ushort MinorSubsystemVersion;
			public uint Win32VersionValue;
			public uint SizeOfImage;
			public uint SizeOfHeaders;
			public uint CheckSum;
			public SubSystemType Subsystem;
			public DllCharacteristicsType DllCharacteristics;
			public uint SizeOfStackReserve;
			public uint SizeOfStackCommit;
			public uint SizeOfHeapReserve;
			public uint SizeOfHeapCommit;
			public uint LoaderFlags;
			public uint NumberOfRvaAndSizes;
			public IMAGE_DATA_DIRECTORY ExportTable;
			public IMAGE_DATA_DIRECTORY ImportTable;
			public IMAGE_DATA_DIRECTORY ResourceTable;
			public IMAGE_DATA_DIRECTORY ExceptionTable;
			public IMAGE_DATA_DIRECTORY CertificateTable;
			public IMAGE_DATA_DIRECTORY BaseRelocationTable;
			public IMAGE_DATA_DIRECTORY Debug;
			public IMAGE_DATA_DIRECTORY Architecture;
			public IMAGE_DATA_DIRECTORY GlobalPtr;
			public IMAGE_DATA_DIRECTORY TLSTable;
			public IMAGE_DATA_DIRECTORY LoadConfigTable;
			public IMAGE_DATA_DIRECTORY BoundImport;
			public IMAGE_DATA_DIRECTORY IAT;
			public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
			public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
			public IMAGE_DATA_DIRECTORY Reserved;

			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.ResourceTable { get { return NumberOfRvaAndSizes > 14 ? ResourceTable : default(IMAGE_DATA_DIRECTORY?); } }
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.CLRRuntimeHeader { get { return NumberOfRvaAndSizes > 14 ? CLRRuntimeHeader : default(IMAGE_DATA_DIRECTORY?); } }
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_OPTIONAL_HEADER64 : IMAGE_OPTIONAL_HEADER
		{
			public MagicType Magic;
			public byte MajorLinkerVersion;
			public byte MinorLinkerVersion;
			public uint SizeOfCode;
			public uint SizeOfInitializedData;
			public uint SizeOfUninitializedData;
			public uint AddressOfEntryPoint;
			public uint BaseOfCode;
			public ulong ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort MajorOperatingSystemVersion;
			public ushort MinorOperatingSystemVersion;
			public ushort MajorImageVersion;
			public ushort MinorImageVersion;
			public ushort MajorSubsystemVersion;
			public ushort MinorSubsystemVersion;
			public uint Win32VersionValue;
			public uint SizeOfImage;
			public uint SizeOfHeaders;
			public uint CheckSum;
			public SubSystemType Subsystem;
			public DllCharacteristicsType DllCharacteristics;
			public ulong SizeOfStackReserve;
			public ulong SizeOfStackCommit;
			public ulong SizeOfHeapReserve;
			public ulong SizeOfHeapCommit;
			public uint LoaderFlags;
			public uint NumberOfRvaAndSizes;
			public IMAGE_DATA_DIRECTORY ExportTable;
			public IMAGE_DATA_DIRECTORY ImportTable;
			public IMAGE_DATA_DIRECTORY ResourceTable;
			public IMAGE_DATA_DIRECTORY ExceptionTable;
			public IMAGE_DATA_DIRECTORY CertificateTable;
			public IMAGE_DATA_DIRECTORY BaseRelocationTable;
			public IMAGE_DATA_DIRECTORY Debug;
			public IMAGE_DATA_DIRECTORY Architecture;
			public IMAGE_DATA_DIRECTORY GlobalPtr;
			public IMAGE_DATA_DIRECTORY TLSTable;
			public IMAGE_DATA_DIRECTORY LoadConfigTable;
			public IMAGE_DATA_DIRECTORY BoundImport;
			public IMAGE_DATA_DIRECTORY IAT;
			public IMAGE_DATA_DIRECTORY DelayImportDescriptor;
			public IMAGE_DATA_DIRECTORY CLRRuntimeHeader;
			public IMAGE_DATA_DIRECTORY Reserved;

			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.ResourceTable { get { return NumberOfRvaAndSizes > 14 ? ResourceTable : default(IMAGE_DATA_DIRECTORY?); } }
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.CLRRuntimeHeader { get { return NumberOfRvaAndSizes > 14 ? CLRRuntimeHeader : default(IMAGE_DATA_DIRECTORY?); } }
		}

		enum MagicType : ushort
		{
			IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b,
			IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b
		}

		enum SubSystemType : ushort
		{
			IMAGE_SUBSYSTEM_UNKNOWN = 0,
			IMAGE_SUBSYSTEM_NATIVE = 1,
			IMAGE_SUBSYSTEM_WINDOWS_GUI = 2,
			IMAGE_SUBSYSTEM_WINDOWS_CUI = 3,
			IMAGE_SUBSYSTEM_POSIX_CUI = 7,
			IMAGE_SUBSYSTEM_WINDOWS_CE_GUI = 9,
			IMAGE_SUBSYSTEM_EFI_APPLICATION = 10,
			IMAGE_SUBSYSTEM_EFI_BOOT_SERVICE_DRIVER = 11,
			IMAGE_SUBSYSTEM_EFI_RUNTIME_DRIVER = 12,
			IMAGE_SUBSYSTEM_EFI_ROM = 13,
			IMAGE_SUBSYSTEM_XBOX = 14
		}

		enum DllCharacteristicsType : ushort
		{
			RES_0 = 0x0001,
			RES_1 = 0x0002,
			RES_2 = 0x0004,
			RES_3 = 0x0008,
			IMAGE_DLL_CHARACTERISTICS_DYNAMIC_BASE = 0x0040,
			IMAGE_DLL_CHARACTERISTICS_FORCE_INTEGRITY = 0x0080,
			IMAGE_DLL_CHARACTERISTICS_NX_COMPAT = 0x0100,
			IMAGE_DLLCHARACTERISTICS_NO_ISOLATION = 0x0200,
			IMAGE_DLLCHARACTERISTICS_NO_SEH = 0x0400,
			IMAGE_DLLCHARACTERISTICS_NO_BIND = 0x0800,
			RES_4 = 0x1000,
			IMAGE_DLLCHARACTERISTICS_WDM_DRIVER = 0x2000,
			IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = 0x8000
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_DATA_DIRECTORY
		{
			public int VirtualAddress;
			public int Size;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_SECTION_HEADER
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public char[] Name;
			public int VirtualSize;
			public int VirtualAddress;
			public int SizeOfRawData;
			public int PointerToRawData;
			public int PointerToRelocations;
			public int PointerToLinenumbers;
			public short NumberOfRelocations;
			public short NumberOfLinenumbers;
			public DataSectionFlags Characteristics;

			public string Section => new string(Name).TrimEnd('\0');
		}

		[Flags]
		enum DataSectionFlags : uint
		{
			TypeReg = 0x00000000,
			TypeDsect = 0x00000001,
			TypeNoLoad = 0x00000002,
			TypeGroup = 0x00000004,
			TypeNoPadded = 0x00000008,
			TypeCopy = 0x00000010,
			ContentCode = 0x00000020,
			ContentInitializedData = 0x00000040,
			ContentUninitializedData = 0x00000080,
			LinkOther = 0x00000100,
			LinkInfo = 0x00000200,
			TypeOver = 0x00000400,
			LinkRemove = 0x00000800,
			LinkComDat = 0x00001000,
			NoDeferSpecExceptions = 0x00004000,
			RelativeGP = 0x00008000,
			MemPurgeable = 0x00020000,
			Memory16Bit = 0x00020000,
			MemoryLocked = 0x00040000,
			MemoryPreload = 0x00080000,
			Align1Bytes = 0x00100000,
			Align2Bytes = 0x00200000,
			Align4Bytes = 0x00300000,
			Align8Bytes = 0x00400000,
			Align16Bytes = 0x00500000,
			Align32Bytes = 0x00600000,
			Align64Bytes = 0x00700000,
			Align128Bytes = 0x00800000,
			Align256Bytes = 0x00900000,
			Align512Bytes = 0x00A00000,
			Align1024Bytes = 0x00B00000,
			Align2048Bytes = 0x00C00000,
			Align4096Bytes = 0x00D00000,
			Align8192Bytes = 0x00E00000,
			LinkExtendedRelocationOverflow = 0x01000000,
			MemoryDiscardable = 0x02000000,
			MemoryNotCached = 0x04000000,
			MemoryNotPaged = 0x08000000,
			MemoryShared = 0x10000000,
			MemoryExecute = 0x20000000,
			MemoryRead = 0x40000000,
			MemoryWrite = 0x80000000
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_COR20_HEADER
		{
			public int cb;
			public short MajorRuntimeVersion;
			public short MinorRuntimeVersion;
			public IMAGE_DATA_DIRECTORY MetaData;
			public IMAGE_COR20_HEADER_FLAGS Flags;
			public int EntryPointTokenOrRVA;
			public IMAGE_DATA_DIRECTORY Resources;
			public IMAGE_DATA_DIRECTORY StrongNameSignature;
			public IMAGE_DATA_DIRECTORY CodeManagerTable;
			public IMAGE_DATA_DIRECTORY VTableFixups;
			public IMAGE_DATA_DIRECTORY ExportAddressTableJumps;
			public IMAGE_DATA_DIRECTORY ManagedNativeHeader;
		}

		[Flags]
		public enum IMAGE_COR20_HEADER_FLAGS : int
		{
			ILOnly = 0x00000001,
			x32BitRequired = 0x00000002,
			ILLibrary = 0x00000004,
			StrongNameSigned = 0x00000008,
			NativeEntryPoint = 0x00000010,
			TrackDebugData = 0x00010000,
			x32BitPreferred = 0x00020000,
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_RESOURCE_DIRECTORY
		{
			public int Characteristics;
			public int TimeDateStamp;
			public short MajorVersion;
			public short MinorVersion;
			public short NumberOfNamedEntries;
			public short NumberOfIdEntries;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_RESOURCE_DIRECTORY_ENTRY
		{
			public uint NameId;
			public uint Data;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct IMAGE_RESOURCE_DATA_ENTRY
		{
			public int Data;
			public int Size;
			public int CodePage;
			public int Reserved;
		}
	}
}
