#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;

namespace NeoEdit
{
	namespace Interop
	{
		shared_ptr<hash_set<int>> GetThreadIDs(int pid)
		{
			shared_ptr<hash_set<int>> threadSet(new hash_set<int>);

			HANDLE toolHelp = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
			if (toolHelp == INVALID_HANDLE_VALUE)
				throw gcnew System::ComponentModel::Win32Exception();
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
							throw gcnew System::ComponentModel::Win32Exception();
						shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

						if (SuspendThread(threadHandle) == -1)
							throw gcnew System::ComponentModel::Win32Exception();
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
					throw gcnew System::ComponentModel::Win32Exception();
				shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

				if (ResumeThread(threadHandle) == -1)
					throw gcnew System::ComponentModel::Win32Exception();
			}
		}

		Handle ^NEInterop::OpenReadMemoryProcess(int pid)
		{
			auto handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, pid);
			if (handle == NULL)
				throw gcnew System::ComponentModel::Win32Exception();

			return gcnew Handle(shared_ptr<void>(handle, CloseHandle));
		}

		VirtualQueryInfo ^NEInterop::VirtualQuery(Handle ^handle, IntPtr index)
		{
			MEMORY_BASIC_INFORMATION memInfo;
			if (VirtualQueryEx(handle->Get(), (void*)index, &memInfo, sizeof(memInfo)) == 0)
			{
				if (GetLastError() != ERROR_INVALID_PARAMETER)
					throw gcnew System::ComponentModel::Win32Exception();
				return nullptr;
			}

			auto info = gcnew VirtualQueryInfo();
			info->Committed = (memInfo.State & MEM_COMMIT) != 0;
			info->Mapped = (memInfo.Type & MEM_MAPPED) != 0;
			info->NoAccess = (memInfo.Protect & PAGE_NOACCESS) != 0;
			info->StartAddress = (IntPtr)memInfo.BaseAddress;
			info->RegionSize = (IntPtr)(void*)memInfo.RegionSize;
			info->EndAddress = (IntPtr)((char*)memInfo.BaseAddress + memInfo.RegionSize);
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
				throw gcnew System::ComponentModel::Win32Exception();
		}

		void NEInterop::WriteProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			SIZE_T written;
			if (!::WriteProcessMemory(handle->Get(), (void*)index, (byte*)ptr, numBytes, &written))
				throw gcnew System::ComponentModel::Win32Exception();
		}
	}
}
