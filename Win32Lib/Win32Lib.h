#pragma once

#include "HandleInfo.h"
#include "Protect.h"
#include "VirtualQueryInfo.h"
#include "Win32Exception.h"

namespace NeoEdit
{
	class Win32Lib
	{
	public:
		typedef Win32LibNS::Handles::HandleInfo HandleInfo;
		typedef Win32LibNS::Processes::Protect Protect;
		typedef Win32LibNS::Processes::VirtualQueryInfo VirtualQueryInfo;
		typedef Win32LibNS::Win32Exception Win32Exception;

		static void (*SuspendProcess)(DWORD pid);
		static void (*ResumeProcess)(DWORD pid);
		static std::shared_ptr<void> (*OpenReadMemoryProcess)(DWORD pid);
		static SIZE_T (*GetProcessMemoryLength)(std::shared_ptr<void>);
		static std::shared_ptr<VirtualQueryInfo> (*VirtualQuery)(std::shared_ptr<void> handle, byte *index);
		static std::shared_ptr<Protect> (*SetProtect)(std::shared_ptr<void> handle, std::shared_ptr<VirtualQueryInfo> info, bool write);
		static void (*ReadProcessMemory)(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes);
		static void (*WriteProcessMemory)(std::shared_ptr<void> handle, byte *index, byte *bytes, int numBytes);

		static std::shared_ptr<std::vector<std::shared_ptr<void>>> (__cdecl*GetAllHandles)();
		static std::shared_ptr<std::vector<std::shared_ptr<void>>> (*GetTypeHandles)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, std::wstring type);
		static std::shared_ptr<std::vector<std::shared_ptr<void>>> (*GetProcessHandles)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles, DWORD pid);
		static std::shared_ptr<std::vector<std::shared_ptr<HandleInfo>>> (*GetHandleInfo)(std::shared_ptr<std::vector<std::shared_ptr<void>>> handles);
		static std::shared_ptr<std::vector<std::wstring>> (*GetHandleTypes)();
		static SIZE_T (*GetSharedMemorySize)(DWORD pid, HANDLE handle);
		static void (*ReadSharedMemory)(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes);
		static void (*WriteSharedMemory)(DWORD pid, HANDLE handle, intptr_t index, byte *bytes, int numBytes);
	};
}
