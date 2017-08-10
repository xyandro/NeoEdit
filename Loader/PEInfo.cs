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
		public bool IsConsole
		{
			get { return (Native.SubSystemType)BitConverter.ToUInt16(Bytes, subsystemOffset) == Native.SubSystemType.IMAGE_SUBSYSTEM_WINDOWS_CUI; }
			set
			{
				if (!value)
					return;
				var bytes = BitConverter.GetBytes((ushort)Native.SubSystemType.IMAGE_SUBSYSTEM_WINDOWS_CUI);
				Array.Copy(bytes, 0, Bytes, subsystemOffset, bytes.Length);
			}
		}
		public byte[] Bytes { get; }
		public IEnumerable<string> ResourceNames => resources.Select(res => res.Item1);

		List<Tuple<string, int, int>> resources = new List<Tuple<string, int, int>>();
		int? corHeaderFlagsOffset;
		int subsystemOffset;
		int pos = 0;
		public PEInfo(string fileName) : this(File.ReadAllBytes(fileName)) { }

		public PEInfo(byte[] bytes)
		{
			Bytes = bytes;

			var dosHeader = GetStruct<Native.IMAGE_DOS_HEADER>();
			IsPE = dosHeader.isValid;
			if (!IsPE)
				return;

			Native.IMAGE_NT_HEADERS ntHeaders;
			pos = dosHeader.e_lfanew;
			var ntHeaders32 = GetStruct<Native.IMAGE_NT_HEADERS32>();
			pos = dosHeader.e_lfanew;
			var ntHeaders64 = GetStruct<Native.IMAGE_NT_HEADERS64>();
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

			subsystemOffset = dosHeader.e_lfanew + ntHeaders.OptionalHeaderOffset + ntHeaders.OptionalHeader.SubsystemOffset;

			var sections = Enumerable.Range(0, ntHeaders.FileHeader.NumberOfSections).Select(offset => GetStruct<Native.IMAGE_SECTION_HEADER>()).ToList();

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
					var resDir = GetStruct<Native.IMAGE_RESOURCE_DIRECTORY>();
					var count = resDir.NumberOfNamedEntries + resDir.NumberOfIdEntries;
					var entries = Enumerable.Range(0, count).Select(num => GetStruct<Native.IMAGE_RESOURCE_DIRECTORY_ENTRY>());
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
							var dataEntry = GetStruct<Native.IMAGE_RESOURCE_DATA_ENTRY>();
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

				corHeaderFlagsOffset = pos + Marshal.OffsetOf<Native.IMAGE_COR20_HEADER>(nameof(Native.IMAGE_COR20_HEADER.Flags)).ToInt32();
				var corHeader = GetStruct<Native.IMAGE_COR20_HEADER>();
				if (!corHeader.Flags.HasFlag(Native.IMAGE_COR20_HEADER_FLAGS.ILOnly))
					FileType = FileTypes.Mixed;
				else
				{
					FileType = FileTypes.Managed;

					if ((BitDepth == BitDepths.x32) && (corHeader.Flags.HasFlag(Native.IMAGE_COR20_HEADER_FLAGS.x32BitRequired) == corHeader.Flags.HasFlag(Native.IMAGE_COR20_HEADER_FLAGS.x32BitPreferred)))
					{
						BitDepth = BitDepths.Any;
						if (!corHeader.Flags.HasFlag(Native.IMAGE_COR20_HEADER_FLAGS.x32BitPreferred))
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

		public Native.IMAGE_COR20_HEADER_FLAGS CorFlags
		{
			get
			{
				if (!corHeaderFlagsOffset.HasValue)
					return default(Native.IMAGE_COR20_HEADER_FLAGS);
				return (Native.IMAGE_COR20_HEADER_FLAGS)BitConverter.ToInt32(Bytes, corHeaderFlagsOffset.Value);
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
	}
}
