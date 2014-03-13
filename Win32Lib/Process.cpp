#include "stdafx.h"
#include "Process.h"

#include "Win32Exception.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			void Process::SuspendProcess(DWORD pid)
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
							if (threadHandle == nullptr)
								Win32Exception::Throw();
							shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

							if (SuspendThread(threadHandle) == -1)
								Win32Exception::Throw();
						}
					}

					if (!found)
						break;
				}
			}

			void Process::ResumeProcess(DWORD pid)
			{
				auto threadSet = GetThreadIDs(pid);
				for each (auto threadId in *threadSet)
				{
					auto threadHandle = OpenThread(THREAD_SUSPEND_RESUME, FALSE, threadId);
					if (threadHandle == nullptr)
						Win32Exception::Throw();
					shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

					if (ResumeThread(threadHandle) == -1)
						Win32Exception::Throw();
				}
			}

			shared_ptr<hash_set<DWORD>> Process::GetThreadIDs(DWORD pid)
			{
				shared_ptr<hash_set<DWORD>> threadSet(new hash_set<DWORD>);

				HANDLE toolHelp = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
				if (toolHelp == INVALID_HANDLE_VALUE)
					Win32Exception::Throw();
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

			shared_ptr<void> Process::OpenReadMemoryProcess(DWORD pid)
			{
				auto handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, pid);
				if (handle == nullptr)
					Win32Exception::Throw();

				return shared_ptr<void>(handle, CloseHandle);
			}

			SIZE_T Process::GetProcessMemoryLength(shared_ptr<void> handle)
			{
				byte *index = nullptr;
				while (true)
				{
					MEMORY_BASIC_INFORMATION memInfo;
					if (VirtualQueryEx(handle.get(), index, &memInfo, sizeof(memInfo)) == 0)
					{
						if (GetLastError() != ERROR_INVALID_PARAMETER)
							Win32Exception::Throw();
						return (SIZE_T)index;
					}
					auto size = min(memInfo.RegionSize - (index - (byte*)memInfo.BaseAddress), SIZE_T((byte*)-1 - index));
					if (size == 0)
						return (SIZE_T)index;
					index += size;
				}
				return 0;
			}

			shared_ptr<VirtualQueryInfo> Process::VirtualQuery(shared_ptr<void> handle, byte *index)
			{
				MEMORY_BASIC_INFORMATION memInfo;
				if (VirtualQueryEx(handle.get(), index, &memInfo, sizeof(memInfo)) == 0)
				{
					if (GetLastError() != ERROR_INVALID_PARAMETER)
						Win32Exception::Throw();
					return nullptr;
				}

				auto info = shared_ptr<VirtualQueryInfo>(new VirtualQueryInfo);
				info->Committed = (memInfo.State & MEM_COMMIT) != 0;
				info->Mapped = (memInfo.Type & MEM_MAPPED) != 0;
				info->NoAccess = (memInfo.Protect & PAGE_NOACCESS) != 0;
				info->StartAddress = memInfo.BaseAddress;
				info->RegionSize = memInfo.RegionSize;
				info->EndAddress = ((byte*)memInfo.BaseAddress + memInfo.RegionSize);
				info->Protect = memInfo.Protect;
				return info;
			}

			shared_ptr<Protect> Process::SetProtect(shared_ptr<void> handle, shared_ptr<VirtualQueryInfo> info, bool write)
			{
				auto protect = info->Protect;

				if (!info->Mapped) // Can't change protection on mapped memory
				{
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
				}

				return shared_ptr<Protect>(new Protect(handle, info, protect));
			}

			void Process::ReadProcessMemory(shared_ptr<void> handle, byte *index, byte *bytes, int numBytes)
			{
				SIZE_T read;
				if (!::ReadProcessMemory(handle.get(), index, bytes, numBytes, &read))
					Win32Exception::Throw();
			}

			void Process::WriteProcessMemory(shared_ptr<void> handle, byte *index, byte *bytes, int numBytes)
			{
				SIZE_T written;
				if (!::WriteProcessMemory(handle.get(), index, bytes, numBytes, &written))
					Win32Exception::Throw();
			}
		}
	}
}
