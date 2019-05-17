using System;
using System.Runtime.InteropServices;

namespace NeoEdit.Loader
{
	static class Native
	{
		public static readonly IntPtr RT_ICON = (IntPtr)3;
		public static readonly IntPtr RT_GROUP_ICON = (IntPtr)14;
		public static readonly IntPtr RT_VERSION = (IntPtr)16;
		public static readonly IntPtr RT_RCDATA = (IntPtr)10;

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr GetConsoleWindow();
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LockResource(IntPtr hResData);
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern void SetDllDirectory(string lpPathName);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, int cbData);

		[Flags]
		public enum DataSectionFlags : uint
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

		public enum DllCharacteristicsType : ushort
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

		public enum MagicType : ushort
		{
			IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b,
			IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b
		}

		public enum SubSystemType : ushort
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

		public interface IMAGE_NT_HEADERS
		{
			IMAGE_FILE_HEADER FileHeader { get; }
			IMAGE_OPTIONAL_HEADER OptionalHeader { get; }
			int OptionalHeaderOffset { get; }
		}

		public interface IMAGE_OPTIONAL_HEADER
		{
			int SubsystemOffset { get; }
			IMAGE_DATA_DIRECTORY? ResourceTable { get; }
			IMAGE_DATA_DIRECTORY? CLRRuntimeHeader { get; }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_COR20_HEADER
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

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_DATA_DIRECTORY
		{
			public int VirtualAddress;
			public int Size;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_DOS_HEADER
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

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_FILE_HEADER
		{
			public short Machine;
			public short NumberOfSections;
			public int TimeDateStamp;
			public int PointerToSymbolTable;
			public int NumberOfSymbols;
			public short SizeOfOptionalHeader;
			public short Characteristics;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_NT_HEADERS32 : IMAGE_NT_HEADERS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER32 OptionalHeader;

			private string _Signature => new string(Signature);
			public bool isValid => (_Signature == "PE\0\0") && (OptionalHeader.Magic == MagicType.IMAGE_NT_OPTIONAL_HDR32_MAGIC);

			IMAGE_FILE_HEADER IMAGE_NT_HEADERS.FileHeader => FileHeader;
			IMAGE_OPTIONAL_HEADER IMAGE_NT_HEADERS.OptionalHeader => OptionalHeader;
			public int OptionalHeaderOffset => Marshal.OffsetOf<IMAGE_NT_HEADERS32>(nameof(OptionalHeader)).ToInt32();
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_NT_HEADERS64 : IMAGE_NT_HEADERS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] Signature;
			public IMAGE_FILE_HEADER FileHeader;
			public IMAGE_OPTIONAL_HEADER64 OptionalHeader;

			private string _Signature => new string(Signature);
			public bool isValid => (_Signature == "PE\0\0") && (OptionalHeader.Magic == MagicType.IMAGE_NT_OPTIONAL_HDR64_MAGIC);

			IMAGE_FILE_HEADER IMAGE_NT_HEADERS.FileHeader => FileHeader;
			IMAGE_OPTIONAL_HEADER IMAGE_NT_HEADERS.OptionalHeader => OptionalHeader;
			public int OptionalHeaderOffset => Marshal.OffsetOf<IMAGE_NT_HEADERS64>(nameof(OptionalHeader)).ToInt32();
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_OPTIONAL_HEADER32 : IMAGE_OPTIONAL_HEADER
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

			public int SubsystemOffset => Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER32>(nameof(Subsystem)).ToInt32();
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.ResourceTable => NumberOfRvaAndSizes > 2 ? ResourceTable : default(IMAGE_DATA_DIRECTORY?);
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.CLRRuntimeHeader => NumberOfRvaAndSizes > 14 ? CLRRuntimeHeader : default(IMAGE_DATA_DIRECTORY?);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_OPTIONAL_HEADER64 : IMAGE_OPTIONAL_HEADER
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

			public int SubsystemOffset => Marshal.OffsetOf<IMAGE_OPTIONAL_HEADER64>(nameof(Subsystem)).ToInt32();
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.ResourceTable => NumberOfRvaAndSizes > 2 ? ResourceTable : default(IMAGE_DATA_DIRECTORY?);
			IMAGE_DATA_DIRECTORY? IMAGE_OPTIONAL_HEADER.CLRRuntimeHeader => NumberOfRvaAndSizes > 14 ? CLRRuntimeHeader : default(IMAGE_DATA_DIRECTORY?);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_RESOURCE_DATA_ENTRY
		{
			public int Data;
			public int Size;
			public int CodePage;
			public int Reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_RESOURCE_DIRECTORY
		{
			public int Characteristics;
			public int TimeDateStamp;
			public short MajorVersion;
			public short MinorVersion;
			public short NumberOfNamedEntries;
			public short NumberOfIdEntries;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_RESOURCE_DIRECTORY_ENTRY
		{
			public uint NameId;
			public uint Data;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct IMAGE_SECTION_HEADER
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
	}
}
