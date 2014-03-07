#include "stdafx.h"
#include "Handle.h"

#include "Win32Exception.h"

using namespace std;
using namespace NeoEdit::Win32LibNS;

namespace
{
	enum SYSTEM_INFORMATION_CLASS
	{
		SystemExtendedHandleInformation = 64,
	};

	enum OBJECT_INFORMATION_CLASS
	{
		ObjectBasicInformation = 0,
		ObjectNameInformation = 1,
		ObjectTypeInformation = 2,
		ObjectAllTypesInformation = 3,
	};

	enum SEMAPHORE_INFORMATION_CLASS
	{
		SemaphoreBasicInformation
	};

	enum NTSTATUS_RESULT
	{
		STATUS_INFO_LENGTH_MISMATCH = 0xc0000004
	};

	struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
	{
		PVOID Object;
		HANDLE UniqueProcessId;
		HANDLE HandleValue;
		ULONG GrantedAccess;
		USHORT CreatorBackTraceIndex;
		USHORT ObjectTypeIndex;
		ULONG HandleAttributes;
		ULONG Reserved;
	};

	struct SYSTEM_HANDLE_INFORMATION_EX
	{
		ULONG_PTR NumberOfHandles;
		ULONG_PTR Reserved;
		SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX Handles[1];
	};

	struct SEMAPHORE_BASIC_INFORMATION
	{
		ULONG CurrentCount;
		ULONG MaximumCount;
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

	typedef NTSTATUS_RESULT (WINAPI *_NtQuerySystemInformation)(SYSTEM_INFORMATION_CLASS SystemInformationClass, PVOID SystemInformation, ULONG SystemInformationLength, PULONG ReturnLength OPTIONAL);
	typedef NTSTATUS_RESULT (WINAPI *_NtQueryObject)(HANDLE ObjectHandle, OBJECT_INFORMATION_CLASS ObjectInformationClass, PVOID ObjectInformation, ULONG Length, PULONG ResultLength);
	typedef NTSTATUS_RESULT (WINAPI *_NtQuerySemaphore)(HANDLE SemaphoreHandle, SEMAPHORE_INFORMATION_CLASS SemaphoreInformationClass, PVOID SemaphoreInformation, ULONG SemaphoreInformationLength, PULONG ReturnLength);

	HMODULE ntdll;
	_NtQuerySystemInformation NtQuerySystemInformation;
	_NtQueryObject NtQueryObject;
	_NtQuerySemaphore NtQuerySemaphore;

	vector<wstring> typeNames;
	map<wstring, wstring> dosToLogical;

	SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX *GetHandle(shared_ptr<void> ptr)
	{
		return (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX*)ptr.get();
	}

	wstring GetLogicalName(shared_ptr<void> handle)
	{
		auto size = 2048;
		shared_ptr<UNICODE_STRING> str((UNICODE_STRING*)malloc(size), free);
		auto result = NtQueryObject(handle.get(), ObjectNameInformation, str.get(), size, NULL);
		if (!NT_SUCCESS(result))
			Win32Exception::Throw();

		wstring name(str->Buffer, str->Length / 2);

		if ((_wcsnicmp(name.c_str(), L"\\Device\\Serial", 14)) == 0 || (_wcsnicmp(name.c_str(), L"\\Device\\UsbSer", 14) == 0))
		{
			HKEY key;
			LONG err;
			if ((err = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"Hardware\\DeviceMap\\SerialComm", 0, KEY_QUERY_VALUE, &key)) != ERROR_SUCCESS)
				Win32Exception::Throw();
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

	SIZE_T GetSizeOfMap(shared_ptr<void> handle)
	{
		auto ptr = MapViewOfFile(handle.get(), FILE_MAP_READ, 0, 0, 0);
		if (ptr == NULL)
			Win32Exception::Throw();
		shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

		MEMORY_BASIC_INFORMATION mbi;
		VirtualQuery(ptr, &mbi, sizeof(mbi));
		return mbi.RegionSize;
	}

	wstring GetData(wstring type, shared_ptr<void> handle)
	{
		if (type == L"Semaphore")
		{
			SEMAPHORE_BASIC_INFORMATION info;
			if (!NT_SUCCESS(NtQuerySemaphore(handle.get(), SemaphoreBasicInformation, &info, sizeof(info), NULL)))
				Win32Exception::Throw();

			return to_wstring(info.CurrentCount) + L" (Max " + to_wstring(info.MaximumCount) + L")";
		}
		if (type == L"Mutant")
		{
			auto result = WaitForSingleObject(handle.get(), 0);
			if ((result == WAIT_OBJECT_0) || (result == WAIT_ABANDONED))
			{
				ReleaseMutex(handle.get());
				return L"Unlocked";
			}
			return L"Locked";
		}
		if (type == L"Section")
			return to_wstring(GetSizeOfMap(handle)) + L" bytes";
		if (type == L"Thread")
			return L"ThreadID: " + to_wstring(GetThreadId(handle.get()));

		return L"";
	}

	shared_ptr<void> OpenProcess(DWORD pid, bool throwOnFail = true)
	{
		auto handle = ::OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
		if (handle == NULL)
			if (throwOnFail)
				Win32Exception::Throw();
			else
				return shared_ptr<void>();
		return shared_ptr<void>(handle, CloseHandle);
	}

	shared_ptr<void> DuplicateHandle(shared_ptr<void> process, HANDLE handle, bool throwOnFail = true)
	{
		HANDLE dupHandle;
		if (!::DuplicateHandle(process.get(), handle, GetCurrentProcess(), &dupHandle, 0, false, DUPLICATE_SAME_ACCESS))
			if (throwOnFail)
				Win32Exception::Throw();
			else
				return shared_ptr<void>();
		return shared_ptr<void>(dupHandle, CloseHandle);
	}

	static void SetDebug()
	{
		HANDLE token;
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &token))
			Win32Exception::Throw();
		shared_ptr<void> tokenDeleter(token, CloseHandle);

		LUID luid;
		if (!LookupPrivilegeValue(NULL, SE_DEBUG_NAME, &luid))
			Win32Exception::Throw();

		TOKEN_PRIVILEGES tp;
		tp.PrivilegeCount = 1;
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
		if (!AdjustTokenPrivileges(token, false, &tp, sizeof(tp), NULL, NULL))
			Win32Exception::Throw();
	}

	static void GetTypeNames()
	{
		shared_ptr<OBJECT_ALL_TYPES_INFORMATION> types;
		ULONG size = 1048576;
		while (true)
		{
			types = shared_ptr<OBJECT_ALL_TYPES_INFORMATION>((OBJECT_ALL_TYPES_INFORMATION*)malloc(size), free);
			auto result = NtQueryObject(NULL, ObjectAllTypesInformation, types.get(), size, &size);
			if (NT_SUCCESS(result))
				break;
			if (result == STATUS_INFO_LENGTH_MISMATCH)
				continue;
			Win32Exception::Throw();
		}

		typeNames.push_back(L"Unknown");
		typeNames.push_back(L"Unknown");

		auto typeInfo = (OBJECT_TYPE_INFORMATION*)&types->TypeInformation;
		for (ULONG ctr = 0; ctr < types->NumberOfTypes; ctr++)
		{
			typeNames.push_back(wstring((wchar_t*)typeInfo->TypeName.Buffer, typeInfo->TypeName.Length / 2));
			typeInfo = (OBJECT_TYPE_INFORMATION*)(((intptr_t)typeInfo + sizeof(OBJECT_TYPE_INFORMATION) + typeInfo->TypeName.MaximumLength + sizeof(void*) - 1) & ~(sizeof(void*) - 1));
		}
	}

	static void GetDosToLogicalMap()
	{
		wchar_t drivesBuf[2048];
		if (!GetLogicalDriveStrings(sizeof(drivesBuf) / sizeof(*drivesBuf), drivesBuf))
			Win32Exception::Throw();

		for (wchar_t *ptr = drivesBuf; *ptr != 0; ptr += wcslen(ptr) + 1)
		{
			wstring drive(ptr, 2);
			wchar_t device[8192];
			if (!QueryDosDevice(drive.c_str(), device, sizeof(device) / sizeof(*device)))
				Win32Exception::Throw();
			dosToLogical[wstring(device) + L"\\"] = drive + L"\\";
		}
	}

	class HandleHelper
	{
		static HandleHelper handleHelper;
	public:
		HandleHelper()
		{
			ntdll = GetModuleHandle(L"ntdll.dll");
			NtQuerySystemInformation = (_NtQuerySystemInformation)GetProcAddress(ntdll, "NtQuerySystemInformation");
			NtQueryObject = (_NtQueryObject)GetProcAddress(ntdll, "NtQueryObject");
			NtQuerySemaphore = (_NtQuerySemaphore)GetProcAddress(ntdll, "NtQuerySemaphore");

			SetDebug();
			GetTypeNames();
			GetDosToLogicalMap();
		}
	};

	HandleHelper HandleHelper::handleHelper;
}

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			bool sortPred(shared_ptr<void> ptr1, shared_ptr<void> ptr2)
			{
				auto handle1 = GetHandle(ptr1);
				auto handle2 = GetHandle(ptr2);

				if (handle1->UniqueProcessId < handle2->UniqueProcessId)
					return true;
				if (handle1->UniqueProcessId > handle2->UniqueProcessId)
					return false;

				if (handle1->ObjectTypeIndex < handle2->ObjectTypeIndex)
					return true;
				if (handle1->ObjectTypeIndex > handle2->ObjectTypeIndex)
					return false;

				if (handle1->HandleValue < handle2->HandleValue)
					return true;
				if (handle1->HandleValue > handle2->HandleValue)
					return false;

				return false;
			}

			shared_ptr<vector<shared_ptr<void>>> Handle::GetAllHandles()
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

					Win32Exception::Throw();
				}

				auto result = shared_ptr<vector<shared_ptr<void>>>(new vector<shared_ptr<void>>);
				for (unsigned int ctr = 0; ctr < handleInfo->NumberOfHandles; ++ctr)
					result->push_back(shared_ptr<void>(new SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX(handleInfo->Handles[ctr])));

				sort(result->begin(), result->end(), sortPred);
				return result;
			}

			shared_ptr<vector<shared_ptr<void>>> Handle::GetProcessHandles(shared_ptr<vector<shared_ptr<void>>> handles, DWORD pid)
			{
				auto result = shared_ptr<vector<shared_ptr<void>>>(new vector<shared_ptr<void>>);
				for each (auto handle in *handles)
					if ((DWORD)GetHandle(handle)->UniqueProcessId == pid)
						result->push_back(handle);
				return result;
			}

			shared_ptr<vector<shared_ptr<void>>> Handle::GetTypeHandles(shared_ptr<vector<shared_ptr<void>>> handles, wstring type)
			{
				auto result = shared_ptr<vector<shared_ptr<void>>>(new vector<shared_ptr<void>>);
				auto itr = find(typeNames.begin(), typeNames.end(), type);
				if (itr == typeNames.end())
					return result;

				auto index = itr - typeNames.begin();
				for each (auto handle in *handles)
					if (GetHandle(handle)->ObjectTypeIndex == index)
						result->push_back(handle);
				return result;
			}

			shared_ptr<vector<shared_ptr<HandleInfo>>> Handle::GetHandleInfo(shared_ptr<vector<shared_ptr<void>>> handles)
			{
				map<DWORD, vector<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX*>> handlesByProcess;
				for each (auto handle in *handles)
					handlesByProcess[(DWORD)GetHandle(handle)->UniqueProcessId].push_back(GetHandle(handle));

				auto result = shared_ptr<vector<shared_ptr<HandleInfo>>>(new vector<shared_ptr<HandleInfo>>());

				auto currentProcess = GetCurrentProcess();
				for each (auto entry in handlesByProcess)
				{
					auto processHandle = OpenProcess(entry.first, false);
					if (!processHandle)
						continue;

					for each (auto handle in entry.second)
					{
						auto dupHandle = DuplicateHandle(processHandle, handle->HandleValue, false);
						if (!dupHandle)
							continue;

						auto type = typeNames[handle->ObjectTypeIndex];
						wstring name, data;
						try { name = (type == L"File") && (GetFileType(dupHandle.get()) == FILE_TYPE_PIPE) ? L"Pipe" : GetLogicalName(dupHandle); } catch (...) { }
						try { data = GetData(type, dupHandle); } catch (...) { }

						result->push_back(shared_ptr<HandleInfo>(new HandleInfo((DWORD)handle->UniqueProcessId, handle->HandleValue, type, name, data)));
					}
				}

				return result;
			}

			shared_ptr<vector<wstring>> Handle::GetHandleTypes()
			{
				auto result = shared_ptr<vector<wstring>>(new vector<wstring>);
				for each (auto name in typeNames)
					result->push_back(name);
				return result;
			}

			SIZE_T Handle::GetSharedMemorySize(DWORD pid, HANDLE handle)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);
				return GetSizeOfMap(dupHandle);
			}

			void Handle::ReadSharedMemory(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);

				auto ptr = (byte*)MapViewOfFile(dupHandle.get(), FILE_MAP_READ, 0, 0, 0);
				if (ptr == NULL)
					Win32Exception::Throw();
				shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

				memcpy(bytes, ptr + index, numBytes);
			}

			void Handle::WriteSharedMemory(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);

				auto ptr = (byte*)MapViewOfFile(dupHandle.get(), FILE_MAP_WRITE, 0, 0, 0);
				if (ptr == NULL)
					Win32Exception::Throw();
				shared_ptr<void> ptrDeleter(ptr, UnmapViewOfFile);

				memcpy(ptr + index, bytes, numBytes);
			}
		}
	}
}
