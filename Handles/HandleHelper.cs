using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace NeoEdit.Handles
{
	static class HandleHelper
	{
		internal static List<string> HandleTypes { get; } = GetHandleTypes();
		static Dictionary<string, string> TranslateMap { get; } = GetTranslateMap();

		static unsafe List<string> GetHandleTypes()
		{
			var data = default(byte[]);
			int size = 1024;
			while (true)
			{
				Array.Resize(ref data, size);
				var result = NtQueryObject(IntPtr.Zero, OBJECT_INFORMATION_CLASS.ObjectAllTypesInformation, data, size, ref size);
				if (result >= NtStatus.Success)
					break;
				if (result == NtStatus.InfoLengthMismatch)
					continue;
				throw new Win32Exception();
			}

			var results = new List<string>();
			results.Add("");
			results.Add("");

			fixed (byte* dataPtr = data)
			{
				var types = (OBJECT_ALL_TYPES_INFORMATION*)dataPtr;
				var offset = Marshal.OffsetOf<OBJECT_ALL_TYPES_INFORMATION>(nameof(OBJECT_ALL_TYPES_INFORMATION.TypeInformation)).ToInt32();
				for (var ctr = 0; ctr < types->NumberOfTypes; ++ctr)
				{
					var typeInfo = (OBJECT_TYPE_INFORMATION*)(dataPtr + offset);
					results.Add(typeInfo->TypeName.ToString());
					offset = (offset + sizeof(OBJECT_TYPE_INFORMATION) + typeInfo->TypeName.MaximumLength + sizeof(void*) - 1) & ~(sizeof(void*) - 1);
				}
			}

			return results;
		}

		static Dictionary<string, string> GetTranslateMap()
		{
			var result = new Dictionary<string, string>();

			// Translate drive device names
			foreach (var drive in DriveInfo.GetDrives())
			{
				var device = new char[8192];
				var len = QueryDosDevice(drive.Name.Substring(0, 2), device, device.Length);
				if (len == 0)
					throw new Win32Exception();
				result[$"{new string(device, 0, len - 2)}\\"] = drive.Name;
			}

			// Translate serial/usb ports device names
			using (var serialComm = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
			{
				if (serialComm != null)
				{
					foreach (var name in serialComm.GetValueNames())
					{
						var value = serialComm.GetValue(name) as string;
						if (value != null)
							result[name] = value;
					}
				}
			}

			return result;
		}

		internal static unsafe List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> GetAllHandles()
		{
			var data = default(byte[]);
			int size = 4096;
			while (true)
			{
				Array.Resize(ref data, size);
				var ret = NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemExtendedHandleInformation, data, size, IntPtr.Zero);

				fixed (byte* dataPtr = data)
				{
					var handleInfo = (SYSTEM_HANDLE_INFORMATION_EX*)dataPtr;
					var sizeRequired = sizeof(SYSTEM_HANDLE_INFORMATION_EX) + sizeof(SYSTEM_HANDLE_INFORMATION_EX) * (handleInfo->NumberOfHandles.ToInt32() - 1); // The -1 is because SYSTEM_HANDLE_INFORMATION_EX has one.
					if (size < sizeRequired)
					{
						size = sizeRequired + sizeof(SYSTEM_HANDLE_INFORMATION_EX) * 10;
						continue;
					}
				}

				if (ret >= NtStatus.Success)
					break;

				throw new Win32Exception();
			}

			var results = new List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
			fixed (byte* dataPtr = data)
			{
				var handleInfo = (SYSTEM_HANDLE_INFORMATION_EX*)dataPtr;
				for (var ctr = 0; ctr < handleInfo->NumberOfHandles.ToInt32(); ++ctr)
					results.Add((&handleInfo->Handle)[ctr]);
			}

			results.Sort(Comparer<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>.Create(Compare));
			return results;
		}

		internal static List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> GetProcessHandles(List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handles, int pid) => handles.Where(handle => handle.UniqueProcessId.ToInt32() == pid).ToList();

		internal static List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> GetTypeHandles(List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handles, string type)
		{
			var index = HandleTypes.IndexOf(type);
			if (index == -1)
				throw new Win32Exception("Invalid type");

			return handles.Where(handle => handle.ObjectTypeIndex == index).ToList();
		}

		static SafeProcessHandle DuplicateHandle(SafeProcessHandle process, IntPtr handle)
		{
			SafeProcessHandle dupHandle;
			if (!DuplicateHandle(process, handle, GetCurrentProcess(), out dupHandle, 0, false, DuplicateOptions.DUPLICATE_SAME_ACCESS))
				dupHandle.SetHandleAsInvalid();
			return dupHandle;
		}

		static unsafe string GetLogicalName(SafeProcessHandle handle)
		{
			var data = new byte[2048];
			int size = data.Length;
			var result = NtQueryObject(handle.DangerousGetHandle(), OBJECT_INFORMATION_CLASS.ObjectNameInformation, data, size, ref size);
			if (result < NtStatus.Success)
				throw new Win32Exception();

			string name;
			fixed (byte* dataPtr = data)
				name = ((UNICODE_STRING*)dataPtr)->ToString();

			foreach (var pair in TranslateMap)
				if (name.StartsWith(pair.Key))
					name = pair.Value + name.Substring(pair.Key.Length);

			return name;
		}

		internal static List<HandleItem> GetHandleItem(List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handles)
		{
			var handlesByProcess = handles.GroupBy(handle => handle.UniqueProcessId.ToInt32()).ToDictionary(group => group.Key, group => group.ToList());

			var result = new List<HandleItem>();

			foreach (var entry in handlesByProcess)
			{
				using (var processHandle = OpenProcess(ProcessAccessFlags.DuplicateHandle | ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead, false, entry.Key))
				{
					if (processHandle.IsInvalid)
						continue;

					foreach (var handle in entry.Value)
					{
						if ((handle.ObjectTypeIndex < 0) || (handle.ObjectTypeIndex >= HandleTypes.Count))
							continue;

						using (var dupHandle = DuplicateHandle(processHandle, handle.HandleValue))
						{
							if (dupHandle.IsInvalid)
								continue;

							var type = HandleTypes[handle.ObjectTypeIndex];
							string name = "", data = "";
							try { name = (type == "File") && (GetFileType(dupHandle) == FileType.FileTypePipe) ? "Pipe" : GetLogicalName(dupHandle); }
							catch { }
							try { data = GetData(type, dupHandle); }
							catch { }

							var handleInfo = new HandleItem
							{
								PID = handle.UniqueProcessId.ToInt32(),
								Handle = handle.HandleValue,
								Type = type,
								Name = name,
								Data = data,
							};
							result.Add(handleInfo);
						}
					}
				}
			}

			return result;
		}

		static string GetData(string type, SafeProcessHandle handle)
		{
			switch (type)
			{
				case "Mutant": return GetMutantData(handle);
				case "Section": return GetSectionData(handle);
				case "Semaphore": return GetSemaphoreData(handle);
				case "Thread": return $"ThreadID: {GetThreadId(handle)}";
				default: return "";
			}
		}

		static string GetMutantData(SafeProcessHandle handle)
		{
			var result = WaitForSingleObject(handle, 0);
			if ((result == WaitResult.WAIT_OBJECT_0) || (result == WaitResult.WAIT_ABANDONED))
			{
				ReleaseMutex(handle);
				return "Unlocked";
			}
			return "Locked";
		}

		static string GetSectionData(SafeProcessHandle handle)
		{
			using (var ptr = MapViewOfFile(handle, FileMapAccess.FileMapRead, 0, 0, IntPtr.Zero))
			{
				if (ptr.IsInvalid)
					throw new Win32Exception();

				MEMORY_BASIC_INFORMATION mbi;
				VirtualQuery(ptr, out mbi, (IntPtr)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());
				return $"{mbi.RegionSize.ToInt64():n0} bytes";
			}
		}

		static string GetSemaphoreData(SafeProcessHandle handle)
		{
			SEMAPHORE_BASIC_INFORMATION info;
			if (NtQuerySemaphore(handle, SEMAPHORE_INFORMATION_CLASS.SemaphoreBasicInformation, out info, Marshal.SizeOf<SEMAPHORE_BASIC_INFORMATION>(), IntPtr.Zero) < NtStatus.Success)
				throw new Win32Exception();

			return $"{info.CurrentCount} (Max {info.MaximumCount})";
		}

		static int Compare(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle1, SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle2)
		{
			if (handle1.UniqueProcessId.ToInt64() < handle2.UniqueProcessId.ToInt64())
				return 1;
			if (handle1.UniqueProcessId.ToInt64() > handle2.UniqueProcessId.ToInt64())
				return -1;

			if (handle1.ObjectTypeIndex < handle2.ObjectTypeIndex)
				return 1;
			if (handle1.ObjectTypeIndex > handle2.ObjectTypeIndex)
				return -1;

			if (handle1.HandleValue.ToInt64() < handle2.HandleValue.ToInt64())
				return 1;
			if (handle1.HandleValue.ToInt64() > handle2.HandleValue.ToInt64())
				return -1;

			return 0;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DuplicateHandle(SafeProcessHandle hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out SafeProcessHandle lpTargetHandle, int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, DuplicateOptions dwOptions);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern FileType GetFileType(SafeProcessHandle hFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int GetThreadId(SafeProcessHandle Thread);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern SafeMapHandle MapViewOfFile(SafeProcessHandle hFileMappingObject, FileMapAccess dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, IntPtr dwNumberOfBytesToMap);

		[DllImport("ntdll.dll", SetLastError = true)]
		static extern NtStatus NtQueryObject(IntPtr objectHandle, OBJECT_INFORMATION_CLASS informationClass, byte[] informationPtr, int informationLength, ref int returnLength);

		[DllImport("ntdll.dll", SetLastError = true)]
		static extern NtStatus NtQuerySemaphore(SafeProcessHandle SemaphoreHandle, SEMAPHORE_INFORMATION_CLASS SemaphoreInformationClass, out SEMAPHORE_BASIC_INFORMATION SemaphoreInformation, int SemaphoreInformationLength, IntPtr ReturnLength);

		[DllImport("ntdll.dll", SetLastError = true)]
		static extern NtStatus NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS InfoClass, byte[] Info, int Size, IntPtr Length);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern SafeProcessHandle OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern int QueryDosDevice(string lpDeviceName, [Out] char[] lpTargetPath, int ucchMax);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ReleaseMutex(SafeProcessHandle hMutex);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr VirtualQuery(SafeMapHandle lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern WaitResult WaitForSingleObject(SafeProcessHandle hHandle, int dwMilliseconds);

		enum DuplicateOptions : int
		{
			DUPLICATE_SAME_ACCESS = 2,
		}

		[Flags]
		enum FileMapAccess : int
		{
			FileMapRead = 4,
		}

		enum FileType : int
		{
			FileTypePipe = 3,
		}

		enum NtStatus : int
		{
			Success = 0,
			InfoLengthMismatch = -1073741820,

		}

		enum OBJECT_INFORMATION_CLASS : int
		{
			ObjectNameInformation = 1,
			ObjectAllTypesInformation = 3,
		}

		[Flags]
		enum ProcessAccessFlags : int
		{
			VirtualMemoryRead = 16,
			DuplicateHandle = 64,
			QueryInformation = 1024,
		}

		enum SEMAPHORE_INFORMATION_CLASS : int
		{
			SemaphoreBasicInformation = 0,
		};

		enum SYSTEM_INFORMATION_CLASS : int
		{
			SystemExtendedHandleInformation = 64,
		}

		enum WaitResult : int
		{
			WAIT_OBJECT_0 = 0,
			WAIT_ABANDONED = 128,
		}

		class SafeMapHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			public SafeMapHandle() : base(true) { }
			protected override bool ReleaseHandle()
			{
				UnmapViewOfFile(handle);
				return true;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct GENERIC_MAPPING
		{
			public int GenericRead;
			public int GenericWrite;
			public int GenericExecute;
			public int GenericAll;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MEMORY_BASIC_INFORMATION
		{
			public IntPtr BaseAddress;
			public IntPtr AllocationBase;
			public int AllocationProtect;
			public IntPtr RegionSize;
			public int State;
			public int Protect;
			public int Type;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct OBJECT_ALL_TYPES_INFORMATION
		{
			public int NumberOfTypes;
			public OBJECT_TYPE_INFORMATION TypeInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct OBJECT_TYPE_INFORMATION
		{
			public UNICODE_STRING TypeName;
			public int TotalNumberOfObjects;
			public int TotalNumberOfHandles;
			public int TotalPagedPoolUsage;
			public int TotalNonPagedPoolUsage;
			public int TotalNamePoolUsage;
			public int TotalHandleTableUsage;
			public int HighWaterNumberOfObjects;
			public int HighWaterNumberOfHandles;
			public int HighWaterPagedPoolUsage;
			public int HighWaterNonPagedPoolUsage;
			public int HighWaterNamePoolUsage;
			public int HighWaterHandleTableUsage;
			public int InvalidAttributes;
			public GENERIC_MAPPING GenericMapping;
			public int ValidAccessMask;
			[MarshalAs(UnmanagedType.I1)]
			public bool SecurityRequired;
			[MarshalAs(UnmanagedType.I1)]
			public bool MaintainHandleCount;
			public int PoolType;
			public int DefaultPagedPoolCharge;
			public int DefaultNonPagedPoolCharge;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct SEMAPHORE_BASIC_INFORMATION
		{
			public int CurrentCount;
			public int MaximumCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct SYSTEM_HANDLE_INFORMATION_EX
		{
			public IntPtr NumberOfHandles;
			public IntPtr Reserved;
			public SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX Handle;
		};

		[StructLayout(LayoutKind.Sequential)]
		internal struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
		{
			public IntPtr Object;
			public IntPtr UniqueProcessId;
			public IntPtr HandleValue;
			public int GrantedAccess;
			public short CreatorBackTraceIndex;
			public short ObjectTypeIndex;
			public int HandleAttributes;
			public int Reserved;

			public override string ToString() => $"{Object} {UniqueProcessId} {HandleValue} {GrantedAccess} {CreatorBackTraceIndex} {ObjectTypeIndex} {HandleAttributes} {Reserved}";
		}

		[StructLayout(LayoutKind.Sequential)]
		struct UNICODE_STRING
		{
			public short Length;
			public short MaximumLength;
			IntPtr buffer;

			public override string ToString() => (Length == 0) || (buffer == IntPtr.Zero) ? "" : Marshal.PtrToStringUni(buffer, Length / 2);
		}
	}
}
