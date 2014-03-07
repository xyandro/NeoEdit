#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace NeoEdit
{
	namespace Interop
	{
		void NEInterop::SuspendProcess(int pid)
		{
			return Win32Lib::SuspendProcess(pid);
		}

		void NEInterop::ResumeProcess(int pid)
		{
			return Win32Lib::ResumeProcess(pid);
		}

		Handle ^NEInterop::OpenReadMemoryProcess(int pid)
		{
			return gcnew Handle(Win32Lib::OpenReadMemoryProcess(pid));
		}

		VirtualQueryInfo ^NEInterop::VirtualQuery(Handle ^handle, IntPtr index)
		{
			return gcnew VirtualQueryInfo(Win32Lib::VirtualQuery(handle->Get(), (byte*)(intptr_t)index));
		}

		Protect ^NEInterop::SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write)
		{
			return gcnew Protect(Win32Lib::SetProtect(handle->Get(), info->Get(), write));
		}

		void NEInterop::ReadProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			return Win32Lib::ReadProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr + bytesIndex, numBytes);
		}

		void NEInterop::WriteProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			return Win32Lib::WriteProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr, numBytes);
		}

		List<int> ^NEInterop::GetPIDsWithFileLock(String ^fileName)
		{
			auto handles = Win32Lib::GetAllHandles();
			handles = Win32Lib::GetTypeHandles(handles, L"File");
			auto handleInfo = Win32Lib::GetHandleInfo(handles);
			auto result = gcnew List<int>();
			for each (auto handle in *handleInfo)
				if (fileName->Equals(gcnew String(handle->Name.c_str()), StringComparison::OrdinalIgnoreCase))
					result->Add(handle->PID);
			return result;
		}

		List<HandleInfo^> ^NEInterop::GetProcessHandles(int pid)
		{
			auto handles = Win32Lib::GetAllHandles();
			handles = Win32Lib::GetProcessHandles(handles, (DWORD)pid);
			return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
		}

		List<String^> ^NEInterop::GetHandleTypes()
		{
			auto types = Win32Lib::GetHandleTypes();
			auto result = gcnew List<String^>;
			for each (auto name in *types)
				result->Add(gcnew String(name.c_str()));
			return result;
		}

		List<HandleInfo^> ^NEInterop::GetTypeHandles(String ^type)
		{
			auto handles = Win32Lib::GetAllHandles();
			handles = Win32Lib::GetTypeHandles(handles, marshal_as<wstring>(type));
			return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
		}

		IntPtr NEInterop::GetSharedMemorySize(int pid, IntPtr handle)
		{
			return (IntPtr)(intptr_t)Win32Lib::GetSharedMemorySize(pid, (HANDLE)handle);
		}

		void NEInterop::ReadSharedMemory(int pid, IntPtr handle, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			pin_ptr<byte> bytesPtr = &bytes[0];
			return Win32Lib::ReadSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr + bytesIndex, numBytes);
		}

		void NEInterop::WriteSharedMemory(int pid, IntPtr handle, IntPtr index, array<byte> ^bytes)
		{
			pin_ptr<byte> bytesPtr = &bytes[0];
			return Win32Lib::WriteSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr, bytes->Length);
		}

		List<HandleInfo^> ^NEInterop::GetHandleInfo(shared_ptr<vector<shared_ptr<Win32Lib::HandleInfo>>> handles)
		{
			auto result = gcnew List<HandleInfo^>();
			for each (auto handle in *handles)
				result->Add(gcnew HandleInfo(handle));
			return result;
		}
	}
}
