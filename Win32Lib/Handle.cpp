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

	SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX *GetHandle(shared_ptr<const void> ptr)
	{
		return (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX*)ptr.get();
	}

	wstring GetLogicalName(shared_ptr<void> handle)
	{
		auto size = 2048;
		shared_ptr<UNICODE_STRING> str((UNICODE_STRING*)malloc(size), free);
		auto result = NtQueryObject(handle.get(), ObjectNameInformation, str.get(), size, nullptr);
		if (!NT_SUCCESS(result))
			Win32Exception::Throw();

		wstring name(str->Buffer, str->Length / 2);

		if ((_wcsnicmp(name.c_str(), L"\\Device\\Serial", 14)) == 0 || (_wcsnicmp(name.c_str(), L"\\Device\\UsbSer", 14) == 0))
		{
			HKEY key;
			LONG err;
			if ((err = RegOpenKeyEx(HKEY_LOCAL_MACHINE, L"Hardware\\DeviceMap\\SerialComm", 0, KEY_QUERY_VALUE, &key)) != ERROR_SUCCESS)
				Win32Exception::Throw();
			shared_ptr<const void> keyDeleter(key, RegCloseKey);

			WCHAR port[50];
			DWORD size = sizeof(port); 
			if ((err = RegQueryValueEx(key, name.c_str(), nullptr, nullptr, (BYTE*)port, &size)) == ERROR_SUCCESS)
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
		if (ptr == nullptr)
			Win32Exception::Throw();
		shared_ptr<const void> ptrDeleter(ptr, UnmapViewOfFile);

		MEMORY_BASIC_INFORMATION mbi;
		VirtualQuery(ptr, &mbi, sizeof(mbi));
		return mbi.RegionSize;
	}

	wstring GetData(wstring type, shared_ptr<void> handle)
	{
		if (type == L"Semaphore")
		{
			SEMAPHORE_BASIC_INFORMATION info;
			if (!NT_SUCCESS(NtQuerySemaphore(handle.get(), SemaphoreBasicInformation, &info, sizeof(info), nullptr)))
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

	shared_ptr<void> OpenProcess(int32_t pid, bool throwOnFail = true)
	{
		auto handle = ::OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
		if (handle == nullptr)
			if (throwOnFail)
				Win32Exception::Throw();
			else
				return nullptr;
		return shared_ptr<void>(handle, CloseHandle);
	}

	shared_ptr<void> DuplicateHandle(shared_ptr<void> process, HANDLE handle, bool throwOnFail = true)
	{
		HANDLE dupHandle;
		if (!::DuplicateHandle(process.get(), handle, GetCurrentProcess(), &dupHandle, 0, false, DUPLICATE_SAME_ACCESS))
			if (throwOnFail)
				Win32Exception::Throw();
			else
				return nullptr;
		return shared_ptr<void>(dupHandle, CloseHandle);
	}

	void SetDebug()
	{
		HANDLE token;
		if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &token))
			Win32Exception::Throw();
		shared_ptr<const void> tokenDeleter(token, CloseHandle);

		LUID luid;
		if (!LookupPrivilegeValue(nullptr, SE_DEBUG_NAME, &luid))
			Win32Exception::Throw();

		TOKEN_PRIVILEGES tp;
		tp.PrivilegeCount = 1;
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
		if (!AdjustTokenPrivileges(token, false, &tp, sizeof(tp), nullptr, nullptr))
			Win32Exception::Throw();
	}

	void GetTypeNames()
	{
		shared_ptr<OBJECT_ALL_TYPES_INFORMATION> types;
		ULONG size = 1048576;
		while (true)
		{
			types.reset((OBJECT_ALL_TYPES_INFORMATION*)malloc(size), free);
			auto result = NtQueryObject(nullptr, ObjectAllTypesInformation, types.get(), size, &size);
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

	void GetDosToLogicalMap()
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

	bool sortPred(shared_ptr<const void> ptr1, shared_ptr<const void> ptr2)
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

	class HandleHelper
	{
	private:
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
		static HandleHelper handleHelper;
	};

	HandleHelper HandleHelper::handleHelper;
}

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			shared_ptr<const vector<shared_ptr<const void>>> Handle::GetAllHandles()
			{
				shared_ptr<SYSTEM_HANDLE_INFORMATION_EX> handleInfo;
				ULONG size = 4096;
				while (true)
				{
					handleInfo.reset((SYSTEM_HANDLE_INFORMATION_EX*)malloc(size), free);
					auto ret = NtQuerySystemInformation(SystemExtendedHandleInformation, handleInfo.get(), size, nullptr);

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

				shared_ptr<vector<shared_ptr<const void>>> result(new vector<shared_ptr<const void>>);
				for (ULONG_PTR ctr = 0; ctr < handleInfo->NumberOfHandles; ++ctr)
					result->push_back(shared_ptr<const void>(new SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX(handleInfo->Handles[ctr])));

				sort(result->begin(), result->end(), sortPred);
				return result;
			}

			shared_ptr<const vector<shared_ptr<const void>>> Handle::GetProcessHandles(shared_ptr<const vector<shared_ptr<const void>>> handles, int32_t pid)
			{
				shared_ptr<vector<shared_ptr<const void>>> result(new vector<shared_ptr<const void>>);
				for each (auto handle in *handles)
					if ((int32_t)GetHandle(handle)->UniqueProcessId == pid)
						result->push_back(handle);
				return result;
			}

			shared_ptr<const vector<shared_ptr<const void>>> Handle::GetTypeHandles(shared_ptr<const vector<shared_ptr<const void>>> handles, wstring type)
			{
				shared_ptr<vector<shared_ptr<const void>>> result(new vector<shared_ptr<const void>>);
				auto itr = find(typeNames.begin(), typeNames.end(), type);
				if (itr == typeNames.end())
					throw Win32Exception(L"Invalid type");

				auto index = itr - typeNames.begin();
				for each (auto handle in *handles)
					if (GetHandle(handle)->ObjectTypeIndex == index)
						result->push_back(handle);
				return result;
			}

			shared_ptr<const vector<shared_ptr<const HandleInfo>>> Handle::GetHandleInfo(shared_ptr<const vector<shared_ptr<const void>>> handles)
			{
				map<DWORD, vector<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX*>> handlesByProcess;
				for each (auto handle in *handles)
					handlesByProcess[(DWORD)GetHandle(handle)->UniqueProcessId].push_back(GetHandle(handle));

				shared_ptr<vector<shared_ptr<const HandleInfo>>> result(new vector<shared_ptr<const HandleInfo>>());

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

						result->push_back(shared_ptr<const HandleInfo>(new HandleInfo((DWORD)handle->UniqueProcessId, handle->HandleValue, type, name, data)));
					}
				}

				return result;
			}

			shared_ptr<const vector<wstring>> Handle::GetHandleTypes()
			{
				shared_ptr<vector<wstring>> result(new vector<wstring>);
				for each (auto name in typeNames)
					result->push_back(name);
				return result;
			}

			uintptr_t Handle::GetSharedMemorySize(int32_t pid, void *handle)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);
				return GetSizeOfMap(dupHandle);
			}

			void Handle::ReadSharedMemory(int32_t pid, void *handle, uintptr_t index, uint8_t *bytes, uint32_t numBytes)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);

				auto ptr = (uint8_t*)MapViewOfFile(dupHandle.get(), FILE_MAP_READ, 0, 0, 0);
				if (ptr == nullptr)
					Win32Exception::Throw();
				shared_ptr<const void> ptrDeleter(ptr, UnmapViewOfFile);

				memcpy(bytes, ptr + index, numBytes);
			}

			void Handle::WriteSharedMemory(int32_t pid, void *handle, uintptr_t index, const uint8_t *bytes, uint32_t numBytes)
			{
				auto process = OpenProcess(pid);
				auto dupHandle = DuplicateHandle(process, handle);

				auto ptr = (uint8_t*)MapViewOfFile(dupHandle.get(), FILE_MAP_WRITE, 0, 0, 0);
				if (ptr == nullptr)
					Win32Exception::Throw();
				shared_ptr<const void> ptrDeleter(ptr, UnmapViewOfFile);

				memcpy(ptr + index, bytes, numBytes);
			}
		}
	}
}
