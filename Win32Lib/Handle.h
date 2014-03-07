#pragma once

#include "HandleInfo.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			class Handle
			{
			public:
				static std::shared_ptr<std::vector<std::shared_ptr<void>>> GetAllHandles();
				static std::shared_ptr<std::vector<std::shared_ptr<void>>> GetTypeHandles(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, std::wstring type);
				static std::shared_ptr<std::vector<std::shared_ptr<void>>> GetProcessHandles(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, DWORD pid);
				static std::shared_ptr<std::vector<std::shared_ptr<HandleInfo>>> GetHandleInfo(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles);
				static std::shared_ptr<std::vector<std::wstring>> GetHandleTypes();
				static SIZE_T GetSharedMemorySize(DWORD pid, HANDLE handle);
				static void ReadSharedMemory(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes);
				static void WriteSharedMemory(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes);
			};
		}
	}
}
