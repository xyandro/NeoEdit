#pragma once

#include "Types.h"
#include "Handle.h"
#include "Protect.h"

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
		};
	}
}
