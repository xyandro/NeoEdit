#include "stdafx.h"
#include "Win32Lib.h"

#include "Process.h"
#include "Handle.h"

namespace NeoEdit
{
	void (*Win32Lib::SuspendProcess)(DWORD pid) = Win32LibNS::Processes::Process::SuspendProcess;
	void (*Win32Lib::ResumeProcess)(DWORD pid) = Win32LibNS::Processes::Process::ResumeProcess;
	std::shared_ptr<void> (*Win32Lib::OpenReadMemoryProcess)(DWORD pid) = Win32LibNS::Processes::Process::OpenReadMemoryProcess;
	std::shared_ptr<Win32Lib::VirtualQueryInfo> (*Win32Lib::VirtualQuery)(std::shared_ptr<void> handle, byte *index) = Win32LibNS::Processes::Process::VirtualQuery;
	std::shared_ptr<Win32Lib::Protect> (*Win32Lib::SetProtect)(std::shared_ptr<void> handle, std::shared_ptr<VirtualQueryInfo> info, bool write) = Win32LibNS::Processes::Process::SetProtect;
	void (*Win32Lib::ReadProcessMemory)(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes) = Win32LibNS::Processes::Process::ReadProcessMemory;
	void (*Win32Lib::WriteProcessMemory)(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes) = Win32LibNS::Processes::Process::WriteProcessMemory;

	std::shared_ptr<std::vector<std::shared_ptr<void>>> (*Win32Lib::GetAllHandles)() = Win32LibNS::Handles::Handle::GetAllHandles;
	std::shared_ptr<std::vector<std::shared_ptr<void>>> (*Win32Lib::GetTypeHandles)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, std::wstring type) = Win32LibNS::Handles::Handle::GetTypeHandles;
	std::shared_ptr<std::vector<std::shared_ptr<void>>> (*Win32Lib::GetProcessHandles)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, DWORD pid) = Win32LibNS::Handles::Handle::GetProcessHandles;
	std::shared_ptr<std::vector<std::shared_ptr<Win32Lib::HandleInfo>>> (*Win32Lib::GetHandleInfo)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles) = Win32LibNS::Handles::Handle::GetHandleInfo;
	std::shared_ptr<std::vector<std::wstring>> (*Win32Lib::GetHandleTypes)() = Win32LibNS::Handles::Handle::GetHandleTypes;
	SIZE_T (*Win32Lib::GetSharedMemorySize)(DWORD pid, HANDLE intHandle) = Win32LibNS::Handles::Handle::GetSharedMemorySize;
	void (*Win32Lib::ReadSharedMemory)(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes) = Win32LibNS::Handles::Handle::ReadSharedMemory;
	void (*Win32Lib::WriteSharedMemory)(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes) = Win32LibNS::Handles::Handle::WriteSharedMemory;
}
