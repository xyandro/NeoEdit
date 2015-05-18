#include "stdafx.h"
#include "Win32Lib.h"

#include "Process.h"
#include "Handle.h"
#include "Window.h"

namespace NeoEdit
{
	void (*Win32Lib::SuspendProcess)(int32_t pid) = Win32LibNS::Processes::Process::SuspendProcess;
	void (*Win32Lib::ResumeProcess)(int32_t pid) = Win32LibNS::Processes::Process::ResumeProcess;
	std::shared_ptr<void> (*Win32Lib::OpenReadMemoryProcess)(int32_t pid) = Win32LibNS::Processes::Process::OpenReadMemoryProcess;
	uintptr_t (*Win32Lib::GetProcessMemoryLength)(std::shared_ptr<void>) = Win32LibNS::Processes::Process::GetProcessMemoryLength;
	std::shared_ptr<const Win32Lib::VirtualQueryInfo> (*Win32Lib::VirtualQuery)(std::shared_ptr<void> handle, const uint8_t *index) = Win32LibNS::Processes::Process::VirtualQuery;
	std::shared_ptr<const Win32Lib::Protect> (*Win32Lib::SetProtect)(std::shared_ptr<void> handle, std::shared_ptr<const VirtualQueryInfo> info, bool write) = Win32LibNS::Processes::Process::SetProtect;
	void (*Win32Lib::ReadProcessMemory)(std::shared_ptr<void> handle, const uint8_t *index, uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Processes::Process::ReadProcessMemory;
	void (*Win32Lib::WriteProcessMemory)(std::shared_ptr<void> handle, uint8_t *index, const uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Processes::Process::WriteProcessMemory;

	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetAllHandles)() = Win32LibNS::Handles::Handle::GetAllHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetTypeHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, std::wstring type) = Win32LibNS::Handles::Handle::GetTypeHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetProcessHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, int32_t pid) = Win32LibNS::Handles::Handle::GetProcessHandles;
	std::shared_ptr<const std::vector<std::shared_ptr<const Win32Lib::HandleInfo>>> (*Win32Lib::GetHandleInfo)(std::shared_ptr<const Win32Lib::HandleList> handles) = Win32LibNS::Handles::Handle::GetHandleInfo;
	std::shared_ptr<const std::vector<std::wstring>> (*Win32Lib::GetHandleTypes)() = Win32LibNS::Handles::Handle::GetHandleTypes;
	uintptr_t (*Win32Lib::GetSharedMemorySize)(int32_t pid, void *intHandle) = Win32LibNS::Handles::Handle::GetSharedMemorySize;
	void (*Win32Lib::ReadSharedMemory)(int32_t pid, void *handle, uintptr_t index, uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Handles::Handle::ReadSharedMemory;
	void (*Win32Lib::WriteSharedMemory)(int32_t pid, void *handle, uintptr_t index, const uint8_t *bytes, uint32_t numBytes) = Win32LibNS::Handles::Handle::WriteSharedMemory;

	intptr_t (*Win32Lib::AllocConsole)() = Win32LibNS::Window::AllocConsole;
	void (*Win32Lib::SendChar)(intptr_t handle, unsigned char ch) = Win32LibNS::Window::SendChar;
}
