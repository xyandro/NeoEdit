#include "stdafx.h"
#include "Win32Lib.h"

#include "Handle.h"

namespace NeoEdit
{
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetAllHandles)() = Win32LibNS::Handles::Handle::GetAllHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetTypeHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, std::wstring type) = Win32LibNS::Handles::Handle::GetTypeHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetProcessHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, int32_t pid) = Win32LibNS::Handles::Handle::GetProcessHandles;
	std::shared_ptr<const std::vector<std::shared_ptr<const Win32Lib::HandleInfo>>> (*Win32Lib::GetHandleInfo)(std::shared_ptr<const Win32Lib::HandleList> handles) = Win32LibNS::Handles::Handle::GetHandleInfo;
	std::shared_ptr<const std::vector<std::wstring>> (*Win32Lib::GetHandleTypes)() = Win32LibNS::Handles::Handle::GetHandleTypes;
	uintptr_t (*Win32Lib::GetSharedMemorySize)(int32_t pid, void *intHandle) = Win32LibNS::Handles::Handle::GetSharedMemorySize;
	void (*Win32Lib::ReadSharedMemory)(int32_t pid, void *handle, uintptr_t index, uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Handles::Handle::ReadSharedMemory;
	void (*Win32Lib::WriteSharedMemory)(int32_t pid, void *handle, uintptr_t index, const uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Handles::Handle::WriteSharedMemory;
}
