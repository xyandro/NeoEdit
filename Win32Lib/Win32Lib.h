#pragma once

#include "HandleList.h"
#include "HandleInfo.h"
#include "Protect.h"
#include "Win32Exception.h"

namespace NeoEdit
{
	class Win32Lib
	{
	public:
		typedef Win32LibNS::Handles::HandleInfo HandleInfo;
		typedef Win32LibNS::Handles::HandleList HandleList;
		typedef Win32LibNS::Processes::Protect Protect;
		typedef Win32LibNS::Processes::VirtualQueryInfo VirtualQueryInfo;
		typedef Win32LibNS::Win32Exception Win32Exception;

		static std::shared_ptr<void> (*OpenReadMemoryProcess)(int32_t pid);
		static uintptr_t (*GetProcessMemoryLength)(std::shared_ptr<void>);
		static std::shared_ptr<const VirtualQueryInfo> (*VirtualQuery)(std::shared_ptr<void> handle, const uint8_t *index);
		static std::shared_ptr<const Protect> (*SetProtect)(std::shared_ptr<void> handle, std::shared_ptr<const VirtualQueryInfo> info, bool write);
		static void (*ReadProcessMemory)(std::shared_ptr<void> handle, const uint8_t *index, uint8_t *bytes, uint32_t numBytes);
		static void (*WriteProcessMemory)(std::shared_ptr<void> handle, uint8_t *index, const uint8_t *bytes, uint32_t numBytes);

		static std::shared_ptr<const HandleList> (*GetAllHandles)();
		static std::shared_ptr<const HandleList> (*GetTypeHandles)(std::shared_ptr<const HandleList> handles, std::wstring type);
		static std::shared_ptr<const HandleList> (*GetProcessHandles)(std::shared_ptr<const HandleList> handles, int32_t pid);
		static std::shared_ptr<const std::vector<std::shared_ptr<const HandleInfo>>> (*GetHandleInfo)(std::shared_ptr<const HandleList> handles);
		static std::shared_ptr<const std::vector<std::wstring>> (*GetHandleTypes)();
		static uintptr_t (*GetSharedMemorySize)(int32_t pid, void *handle);
		static void (*ReadSharedMemory)(int32_t pid, void *handle, uintptr_t index, uint8_t *bytes, uint32_t numBytes);
		static void (*WriteSharedMemory)(int32_t pid, void *handle, uintptr_t index, const uint8_t *bytes, uint32_t numBytes);
	};
}
