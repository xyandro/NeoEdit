#pragma once

#include "HandleInfo.h"
#include "Handle.h"
#include "Protect.h"
#include "VirtualQueryInfo.h"

namespace NeoEdit
{
	namespace Win32
	{
		public ref class Interop
		{
		public:
			static void SuspendProcess(int pid);
			static void ResumeProcess(int pid);
			static Handle ^OpenReadMemoryProcess(int pid);
			static int64_t GetProcessMemoryLength(Handle ^handle);
			static VirtualQueryInfo ^VirtualQuery(Handle ^handle, int64_t index);
			static Protect ^SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write);
			static void ReadProcessMemory(Handle ^handle, int64_t index, array<uint8_t> ^bytes, int bytesIndex, int numBytes);
			static void WriteProcessMemory(Handle ^handle, int64_t index, array<uint8_t> ^bytes, int numBytes);
			static System::Collections::Generic::List<int> ^GetPIDsWithFileLock(System::String ^fileName);
			static System::Collections::Generic::List<HandleInfo^> ^GetHandles();
			static System::Collections::Generic::List<HandleInfo^> ^GetProcessHandles(int pid);
			static System::Collections::Generic::List<System::String^> ^GetHandleTypes();
			static System::Collections::Generic::List<HandleInfo^> ^GetTypeHandles(System::String ^type);
			static int64_t GetSharedMemorySize(int pid, System::IntPtr handle);
			static void ReadSharedMemory(int pid, System::IntPtr handle, int64_t index, array<uint8_t> ^bytes, int bytesIndex, int numBytes);
			static void WriteSharedMemory(int pid, System::IntPtr handle, int64_t index, array<uint8_t> ^bytes);
		private:
			static System::Collections::Generic::List<HandleInfo^> ^GetHandleInfo(std::shared_ptr<const std::vector<std::shared_ptr<const Win32Lib::HandleInfo>>> handles);
		};
	}
}
