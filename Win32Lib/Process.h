#pragma once

#include "Protect.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			class Process
			{
			public:
				static void SuspendProcess(int32_t pid);
				static void ResumeProcess(int32_t pid);
				static std::shared_ptr<void> OpenReadMemoryProcess(int32_t pid);
				static uintptr_t GetProcessMemoryLength(std::shared_ptr<void> handle);
				static std::shared_ptr<const VirtualQueryInfo> VirtualQuery(std::shared_ptr<void> handle, const uint8_t *index);
				static std::shared_ptr<const Protect> Process::SetProtect(std::shared_ptr<void> handle, std::shared_ptr<const VirtualQueryInfo> info, bool write);
				static void ReadProcessMemory(std::shared_ptr<void> handle, const uint8_t *index, uint8_t *bytes, uint32_t numBytes);
				static void WriteProcessMemory(std::shared_ptr<void> handle, uint8_t *index, const uint8_t *bytes, uint32_t numBytes);
			};
		}
	}
}
