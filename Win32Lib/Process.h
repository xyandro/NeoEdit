#pragma once

#include "Protect.h"
#include "VirtualQueryInfo.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			class Process
			{
			public:
				static void SuspendProcess(DWORD pid);
				static void ResumeProcess(DWORD pid);
				static std::shared_ptr<void> OpenReadMemoryProcess(DWORD pid);
				static SIZE_T GetProcessMemoryLength(std::shared_ptr<void> handle);
				static std::shared_ptr<VirtualQueryInfo> VirtualQuery(std::shared_ptr<void> handle, byte *index);
				static std::shared_ptr<Protect> Process::SetProtect(std::shared_ptr<void> handle, std::shared_ptr<VirtualQueryInfo> info, bool write);
				static void ReadProcessMemory(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes);
				static void WriteProcessMemory(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes);
			private:
				static std::shared_ptr<std::hash_set<DWORD>> GetThreadIDs(DWORD pid);
			};
		}
	}
}
