#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace
{
	// Helper stuff

	using namespace NeoEdit::Interop;

	enum OBJECT_INFORMATION_CLASS
	{
		ObjectBasicInformation = 0,
		ObjectNameInformation = 1,
		ObjectTypeInformation = 2,
		ObjectAllTypesInformation = 3,
	};

	struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
	{
		PVOID Object;
		HANDLE UniqueProcessId;
		HANDLE HandleValue;
		ACCESS_MASK GrantedAccess;
		USHORT CreatorBackTraceIndex;
		USHORT ObjectTypeIndex;
		ULONG HandleAttributes;
		PVOID Reserved;
	};

	struct SYSTEM_HANDLE_INFORMATION_EX
	{
		ULONG_PTR NumberOfHandles;
		ULONG_PTR Reserved;
		SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX Handles[1];
	};

	enum VALUES
	{
		SystemExtendedHandleInformation = 64,

		STATUS_INFO_LENGTH_MISMATCH = 0xc0000004
	};

	struct OBJECT_TYPE_INFORMATION
	{
		UNICODE_STRING TypeName;
		ULONG TotalNumberOfObjects;
		ULONG TotalNumberOfHandles;
		ULONG TotalPagedPoolUsage;
		ULONG TotalNonPagedPoolUsage;
		ULONG TotalNamePoolUsage;
		ULONG TotalHandleTableUsage;
		ULONG HighWaterNumberOfObjects;
		ULONG HighWaterNumberOfHandles;
		ULONG HighWaterPagedPoolUsage;
		ULONG HighWaterNonPagedPoolUsage;
		ULONG HighWaterNamePoolUsage;
		ULONG HighWaterHandleTableUsage;
		ULONG InvalidAttributes;
		GENERIC_MAPPING GenericMapping;
		ULONG ValidAccessMask;
		BOOLEAN SecurityRequired;
		BOOLEAN MaintainHandleCount;
		ULONG PoolType;
		ULONG DefaultPagedPoolCharge;
		ULONG DefaultNonPagedPoolCharge;
	};

	struct OBJECT_ALL_TYPES_INFORMATION
	{
		ULONG NumberOfTypes;
		OBJECT_TYPE_INFORMATION TypeInformation[1];
	};

	HMODULE ntdll;
	typedef NTSTATUS (WINAPI *_NtQuerySystemInformation)(ULONG SystemInformationClass, PVOID SystemInformation, ULONG SystemInformationLength, PULONG ReturnLength OPTIONAL);
	_NtQuerySystemInformation NtQuerySystemInformation;
	typedef NTSTATUS (*_NtQueryObject)(HANDLE ObjectHandle, OBJECT_INFORMATION_CLASS ObjectInformationClass, PVOID ObjectInformation, ULONG Length, PULONG ResultLength);
	_NtQueryObject NtQueryObject;

	vector<wstring> typeNames;
	map<wstring, wstring> dosToLogical;

	ref class HandleHelper
	{
	private:
		static HandleHelper()
		{
			ntdll = GetModuleHandle(L"ntdll.dll");
			NtQuerySystemInformation = (_NtQuerySystemInformation)GetProcAddress(ntdll, "NtQuerySystemInformation");
			NtQueryObject= (_NtQueryObject)GetProcAddress(ntdll, "NtQueryObject");

			SetDebug();

			GetTypeNames();
			GetDosToLogicalMap();
		}

		static void SetDebug()
		{
			HANDLE token;
			if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &token))
				throw gcnew Win32Exception();
			shared_ptr<void> tokenDeleter(token, CloseHandle);

			LUID luid;
			if (!LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid))
				throw gcnew Win32Exception();

			TOKEN_PRIVILEGES tp;
			tp.PrivilegeCount = 1;
			tp.Privileges[0].Luid = luid;
			tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
			if (!AdjustTokenPrivileges(token, false, &tp, sizeof(tp), NULL, NULL))
				throw gcnew Win32Exception();
		}

		static void GetTypeNames()
		{
			shared_ptr<OBJECT_ALL_TYPES_INFORMATION> types;
			ULONG size = 0;
			while (true)
			{
				types = shared_ptr<OBJECT_ALL_TYPES_INFORMATION>((OBJECT_ALL_TYPES_INFORMATION*)malloc(size), free);
				auto result = NtQueryObject(NULL, ObjectAllTypesInformation, types.get(), size, &size);
				if (NT_SUCCESS(result))
					break;
				if (result == STATUS_INFO_LENGTH_MISMATCH)
					continue;
				throw gcnew Win32Exception();
			}

			typeNames.push_back(L"Unknown");
			typeNames.push_back(L"Unknown");

			auto typeInfo = (OBJECT_TYPE_INFORMATION*)&types->TypeInformation;
			for (ULONG ctr = 0; ctr < types->NumberOfTypes; ctr++)
			{
				typeNames.push_back(wstring((wchar_t*)typeInfo->TypeName.Buffer, typeInfo->TypeName.Length / 2));
				auto pos = (intptr_t)typeInfo + sizeof(OBJECT_TYPE_INFORMATION) + typeInfo->TypeName.MaximumLength;
				// DWORD align
				pos += 7 - (pos - 1) % 8;
				typeInfo = (OBJECT_TYPE_INFORMATION*)pos;
			}
		}

		static void GetDosToLogicalMap()
		{
			wchar_t drivesBuf[2048];
			if (!GetLogicalDriveStrings(sizeof(drivesBuf) / sizeof(*drivesBuf), drivesBuf))
				throw gcnew Win32Exception();

			for (wchar_t *ptr = drivesBuf; *ptr != 0; ptr += wcslen(ptr) + 1)
			{
				wstring drive(ptr, 2);
				wchar_t device[8192];
				if (!QueryDosDevice(drive.c_str(), device, sizeof(device) / sizeof(*device)))
					throw gcnew Win32Exception();
				dosToLogical[wstring(device) + L"\\"] = drive + L"\\";
			}
		}

	public:
		typedef vector<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handleVector;
		typedef shared_ptr<handleVector> handleVectorPtr;

		static bool sortPred(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle1, SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle2)
		{
			if (handle1.UniqueProcessId < handle2.UniqueProcessId)
				return true;
			if (handle1.UniqueProcessId > handle2.UniqueProcessId)
				return false;

			if (handle1.ObjectTypeIndex < handle2.ObjectTypeIndex)
				return true;
			if (handle1.ObjectTypeIndex > handle2.ObjectTypeIndex)
				return false;

			return false;
		}

		static handleVectorPtr GetAllHandles()
		{
			shared_ptr<SYSTEM_HANDLE_INFORMATION_EX> handleInfo;
			ULONG size = 4096;
			while (true)
			{
				handleInfo = shared_ptr<SYSTEM_HANDLE_INFORMATION_EX>((SYSTEM_HANDLE_INFORMATION_EX*)malloc(size), free);
				auto ret = NtQuerySystemInformation(SystemExtendedHandleInformation, handleInfo.get(), size, NULL);

				auto sizeRequired = sizeof(SYSTEM_HANDLE_INFORMATION_EX) + sizeof(SYSTEM_HANDLE_INFORMATION_EX) * (handleInfo->NumberOfHandles - 1); // The -1 is because SYSTEM_HANDLE_INFORMATION_EX has one.
				if (size < sizeRequired)
				{
					size = (ULONG)(sizeRequired + sizeof(SYSTEM_HANDLE_INFORMATION_EX) * 10);
					continue;
				}

				if (NT_SUCCESS(ret))
					break;

				throw gcnew Win32Exception();
			}

			handleVectorPtr result(new handleVector);
			for (auto ctr = 0; ctr < handleInfo->NumberOfHandles; ++ctr)
				result->push_back(handleInfo->Handles[ctr]);

			sort(result->begin(), result->end(), sortPred);
			return result;
		}

		static handleVectorPtr GetProcessHandles(handleVectorPtr handles, DWORD pid)
		{
			handleVectorPtr result(new handleVector);
			for each (auto handle in *handles)
				if ((DWORD)handle.UniqueProcessId == pid)
					result->push_back(handle);
			return result;
		}

		static handleVectorPtr GetTypeHandles(handleVectorPtr handles, wstring type)
		{
			handleVectorPtr result(new handleVector);
			auto itr = find(typeNames.begin(), typeNames.end(), type);
			if (itr == typeNames.end())
				return result;

			auto index = itr - typeNames.begin();
			for each (auto handle in *handles)
				if (handle.ObjectTypeIndex == index)
					result->push_back(handle);
			return result;
		}

		static wstring GetLogicalName(HANDLE handle)
		{
			auto size = 2048;
			shared_ptr<UNICODE_STRING> str((UNICODE_STRING*)malloc(size), free);
			auto result = NtQueryObject(handle, ObjectNameInformation, str.get(), size, NULL);
			if (!NT_SUCCESS(result))
				throw gcnew Win32Exception();

			wstring name(str->Buffer, str->Length / 2);

			if ((_wcsnicmp(name.c_str(), L"\\Device\\Serial", 14)) == 0 || (_wcsnicmp(name.c_str(), L"\\Device\\UsbSer", 14) == 0))
			{
				HKEY key;
				LONG err;
				if ((err = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"Hardware\\DeviceMap\\SerialComm", 0, KEY_QUERY_VALUE, &key)) != ERROR_SUCCESS)
					throw gcnew Win32Exception(err);
				shared_ptr<void> keyDeleter(key, RegCloseKey);

				WCHAR port[50];
				DWORD size = sizeof(port); 
				if ((err = RegQueryValueEx(key, name.c_str(), NULL, NULL, (BYTE*)port, &size)) == ERROR_SUCCESS)
					name = port;
				else
					name += L" UNKNOWN PORT";
			}
			else
			{
				for each (auto entry in dosToLogical)
					if (name.compare(0, entry.first.size(), entry.first) == 0)
						name = entry.second + name.substr(entry.first.size());
			}

			return name;
		}

		static List<HandleInfo^> ^GetHandleInfo(handleVectorPtr handles)
		{
			map<DWORD, handleVector> handlesByProcess;
			for each (auto handle in *handles)
				handlesByProcess[(DWORD)handle.UniqueProcessId].push_back(handle);

			auto result = gcnew List<HandleInfo^>();

			auto currentProcess = GetCurrentProcess();
			for each (auto entry in handlesByProcess)
			{
				auto processHandle = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, entry.first);
				if (processHandle == NULL)
					continue;
				shared_ptr<void> processHandleDeleter(processHandle, CloseHandle);

				for each (auto handle in entry.second)
				{
					HANDLE dupHandle;
					if (!DuplicateHandle(processHandle, handle.HandleValue, currentProcess, &dupHandle, 0, false, DUPLICATE_SAME_ACCESS))
						continue;
					shared_ptr<void> dupHandleDeleter(dupHandle, CloseHandle);

					auto type = typeNames[handle.ObjectTypeIndex];
					auto name = (type == L"File") && (GetFileType(dupHandle) == FILE_TYPE_PIPE) ? L"Pipe" : GetLogicalName(dupHandle);

					result->Add(gcnew HandleInfo((int)handle.UniqueProcessId, gcnew String(type.c_str()), gcnew String(name.c_str())));
				}
			}

			return result;
		}
	};

	shared_ptr<hash_set<int>> GetThreadIDs(int pid)
	{
		shared_ptr<hash_set<int>> threadSet(new hash_set<int>);

		HANDLE toolHelp = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
		if (toolHelp == INVALID_HANDLE_VALUE)
			throw gcnew Win32Exception();
		shared_ptr<void> toolHelpDeleter(toolHelp, CloseHandle);

		THREADENTRY32 te;
		te.dwSize = sizeof(te);
		if (!Thread32First(toolHelp, &te))
			return threadSet;

		do
		{
			if (te.th32OwnerProcessID != pid)
				continue;

			threadSet->insert(te.th32ThreadID);
		}
		while (Thread32Next(toolHelp, &te));

		return threadSet;
	}
}

namespace NeoEdit
{
	namespace Interop
	{
		void NEInterop::SuspendProcess(int pid)
		{
			hash_set<int> suspended;

			// Keep going until we get them all; more might pop up as we're working
			while (true)
			{
				auto found = false;

				auto threadSet = GetThreadIDs(pid);
				for each (auto threadId in *threadSet)
				{
					if (suspended.find(threadId) == suspended.end())
					{
						found = true;
						suspended.insert(threadId);

						auto threadHandle = OpenThread(THREAD_SUSPEND_RESUME, FALSE, threadId);
						if (threadHandle == NULL)
							throw gcnew Win32Exception();
						shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

						if (SuspendThread(threadHandle) == -1)
							throw gcnew Win32Exception();
					}
				}

				if (!found)
					break;
			}
		}

		void NEInterop::ResumeProcess(int pid)
		{
			auto threadSet = GetThreadIDs(pid);
			for each (auto threadId in *threadSet)
			{
				auto threadHandle = OpenThread(THREAD_SUSPEND_RESUME, FALSE, threadId);
				if (threadHandle == NULL)
					throw gcnew Win32Exception();
				shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

				if (ResumeThread(threadHandle) == -1)
					throw gcnew Win32Exception();
			}
		}

		Handle ^NEInterop::OpenReadMemoryProcess(int pid)
		{
			auto handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, pid);
			if (handle == NULL)
				throw gcnew Win32Exception();

			return gcnew Handle(shared_ptr<void>(handle, CloseHandle));
		}

		VirtualQueryInfo ^NEInterop::VirtualQuery(Handle ^handle, IntPtr index)
		{
			MEMORY_BASIC_INFORMATION memInfo;
			if (VirtualQueryEx(handle->Get(), (void*)index, &memInfo, sizeof(memInfo)) == 0)
			{
				if (GetLastError() != ERROR_INVALID_PARAMETER)
					throw gcnew Win32Exception();
				return nullptr;
			}

			auto info = gcnew VirtualQueryInfo();
			info->Committed = (memInfo.State & MEM_COMMIT) != 0;
			info->Mapped = (memInfo.Type & MEM_MAPPED) != 0;
			info->NoAccess = (memInfo.Protect & PAGE_NOACCESS) != 0;
			info->StartAddress = (IntPtr)memInfo.BaseAddress;
			info->RegionSize = (IntPtr)(void*)memInfo.RegionSize;
			info->EndAddress = (IntPtr)((byte*)memInfo.BaseAddress + memInfo.RegionSize);
			info->Protect = memInfo.Protect;
			return info;
		}

		Protect ^NEInterop::SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write)
		{
			auto protect = info->Protect;
			if ((protect & PAGE_GUARD) != 0)
				protect ^= PAGE_GUARD;
			auto extra = protect & ~(PAGE_GUARD - 1);
			if (write)
			{
				if ((protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_WRITECOPY)) != 0)
					protect = PAGE_EXECUTE_READWRITE;
				if ((protect & (PAGE_NOACCESS | PAGE_READONLY | PAGE_WRITECOPY)) != 0)
					protect = PAGE_READWRITE;
			}
			else
			{
				if ((protect & PAGE_NOACCESS) != 0)
					protect = PAGE_READONLY;
			}
			protect |= extra;

			// Can't change protection on mapped memory
			if (info->Mapped)
				protect = info->Protect;

			return gcnew Protect(handle, info, protect);
		}

		void NEInterop::ReadProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			SIZE_T read;
			if (!::ReadProcessMemory(handle->Get(), (void*)index, (byte*)ptr + bytesIndex, numBytes, &read))
				throw gcnew Win32Exception();
		}

		void NEInterop::WriteProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			SIZE_T written;
			if (!::WriteProcessMemory(handle->Get(), (void*)index, (byte*)ptr, numBytes, &written))
				throw gcnew Win32Exception();
		}

		List<int> ^NEInterop::GetPIDsWithFileLock(String ^fileName)
		{
			auto handles = HandleHelper::GetAllHandles();
			handles = HandleHelper::GetTypeHandles(handles, L"File");
			auto handleInfo = HandleHelper::GetHandleInfo(handles);
			auto result = gcnew List<int>();
			for each (HandleInfo ^handle in handleInfo)
				if (handle->Name->Equals(fileName, StringComparison::OrdinalIgnoreCase))
					result->Add(handle->PID);
			return result;
		}

		List<HandleInfo^> ^NEInterop::GetProcessHandleInfo(int pid)
		{
			auto handles = HandleHelper::GetAllHandles();
			handles = HandleHelper::GetProcessHandles(handles, (DWORD)pid);
			return HandleHelper::GetHandleInfo(handles);
		}

		List<String^> ^NEInterop::GetSharedMemoryNames()
		{
			auto handles = HandleHelper::GetAllHandles();
			handles = HandleHelper::GetTypeHandles(handles, L"Section");
			auto handleInfo = HandleHelper::GetHandleInfo(handles);
			auto result = gcnew List<String^>();
			for each (HandleInfo ^handle in handleInfo)
				if (!result->Contains(handle->Name))
					result->Add(handle->Name);
			return result;
		}

		Int64 NEInterop::GetSharedMemorySize(String ^name)
		{
			auto wname = marshal_as<std::wstring>(name);
			auto handle = OpenFileMapping(FILE_MAP_READ, FALSE, wname.c_str());
			if (handle == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> handleDeleter(handle, CloseHandle);

			auto ptr = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, 0);
			if (ptr == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

			MEMORY_BASIC_INFORMATION mbi;
			::VirtualQuery(ptr, &mbi, sizeof(mbi));
			return mbi.RegionSize;
		}

		void NEInterop::ReadSharedMemory(String ^name, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			auto wname = marshal_as<std::wstring>(name);
			auto handle = OpenFileMapping(FILE_MAP_READ, FALSE, wname.c_str());
			if (handle == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> handleDeleter(handle, CloseHandle);

			auto ptr = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, 0);
			if (ptr == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

			pin_ptr<byte> bytesPtr = &bytes[0];
			memcpy((byte*)bytesPtr + bytesIndex, (byte*)ptr + (intptr_t)index, numBytes);
		}

		void NEInterop::WriteSharedMemory(String ^name, IntPtr index, array<byte> ^bytes)
		{
			auto wname = marshal_as<std::wstring>(name);
			auto handle = OpenFileMapping(FILE_MAP_WRITE, FALSE, wname.c_str());
			if (handle == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> handleDeleter(handle, CloseHandle);

			auto ptr = MapViewOfFile(handle, FILE_MAP_WRITE, 0, 0, 0);
			if (ptr == NULL)
				throw gcnew Win32Exception();
			shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

			pin_ptr<byte> bytesPtr = &bytes[0];
			memcpy((byte*)ptr + (intptr_t)index, bytesPtr, bytes->Length);
		}
	}
}
