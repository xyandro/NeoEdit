#pragma once

#include "HandleInfo.h"
#include "Handle.h"
#include "Protect.h"
#include "VirtualQueryInfo.h"

namespace NeoEdit
{
	namespace Interop
	{
		public ref class NEInterop
		{
		public:
			static void SuspendProcess(int pid);
			static void ResumeProcess(int pid);
			static Handle ^OpenReadMemoryProcess(int pid);
			static VirtualQueryInfo ^VirtualQuery(Handle ^handle, System::IntPtr index);
			static Protect ^SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write);
			static void ReadProcessMemory(Handle ^handle, System::IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes);
			static void WriteProcessMemory(Handle ^handle, System::IntPtr index, array<byte> ^bytes, int numBytes);
			static System::Collections::Generic::List<int> ^GetPIDsWithFileLock(System::String ^fileName);
			static System::Collections::Generic::List<HandleInfo^> ^GetProcessHandles(int pid);
			static System::Collections::Generic::List<System::String^> ^GetHandleTypes();
			static System::Collections::Generic::List<HandleInfo^> ^GetTypeHandles(System::String ^type);
			static System::IntPtr GetSharedMemorySize(int pid, System::IntPtr handle);
			static void ReadSharedMemory(int pid, System::IntPtr handle, System::IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes);
			static void WriteSharedMemory(int pid, System::IntPtr handle, System::IntPtr index, array<byte> ^bytes);
		private:
			static System::Collections::Generic::List<HandleInfo^> ^GetHandleInfo(std::shared_ptr<std::vector<std::shared_ptr<Win32Lib::HandleInfo>>> handles);
		};
	}
}
