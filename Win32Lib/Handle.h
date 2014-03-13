#pragma once

#include <memory>
#include <vector>

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
				static std::shared_ptr<const std::vector<std::shared_ptr<const void>>> GetAllHandles();
				static std::shared_ptr<const std::vector<std::shared_ptr<const void>>> GetTypeHandles(std::shared_ptr<const std::vector<std::shared_ptr<const void>>> handles, std::wstring type);
				static std::shared_ptr<const std::vector<std::shared_ptr<const void>>> GetProcessHandles(std::shared_ptr<const std::vector<std::shared_ptr<const void>>> handles, int32_t pid);
				static std::shared_ptr<const std::vector<std::shared_ptr<const HandleInfo>>> GetHandleInfo(std::shared_ptr<const std::vector<std::shared_ptr<const void>>> handles);
				static std::shared_ptr<const std::vector<std::wstring>> GetHandleTypes();
				static uintptr_t GetSharedMemorySize(int32_t pid, void *handle);
				static void ReadSharedMemory(int32_t pid, void *handle, uintptr_t index, uint8_t *bytes, uint32_t numBytes);
				static void WriteSharedMemory(int32_t pid, void *handle, uintptr_t index, const uint8_t *bytes, uint32_t numBytes);
			};
		}
	}
}
