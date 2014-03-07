#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace NeoEdit
{
	namespace Win32Interop
	{
		void Interop::SuspendProcess(int pid)
		{
			return Win32Lib::SuspendProcess(pid);
		}

		void Interop::ResumeProcess(int pid)
		{
			return Win32Lib::ResumeProcess(pid);
		}

		Handle ^Interop::OpenReadMemoryProcess(int pid)
		{
			return gcnew Handle(Win32Lib::OpenReadMemoryProcess(pid));
		}

		VirtualQueryInfo ^Interop::VirtualQuery(Handle ^handle, IntPtr index)
		{
			return gcnew VirtualQueryInfo(Win32Lib::VirtualQuery(handle->Get(), (byte*)(intptr_t)index));
		}

		Protect ^Interop::SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write)
		{
			return gcnew Protect(Win32Lib::SetProtect(handle->Get(), info->Get(), write));
		}

		void Interop::ReadProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			return Win32Lib::ReadProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr + bytesIndex, numBytes);
		}

		void Interop::WriteProcessMemory(Handle ^handle, IntPtr index, array<byte> ^bytes, int numBytes)
		{
			pin_ptr<byte> ptr = &bytes[0];
			return Win32Lib::WriteProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr, numBytes);
		}

		List<int> ^Interop::GetPIDsWithFileLock(String ^fileName)
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

		List<HandleInfo^> ^Interop::GetProcessHandles(int pid)
		{
			auto handles = Win32Lib::GetAllHandles();
			handles = Win32Lib::GetProcessHandles(handles, (DWORD)pid);
			return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
		}

		List<String^> ^Interop::GetHandleTypes()
		{
			auto types = Win32Lib::GetHandleTypes();
			auto result = gcnew List<String^>;
			for each (auto name in *types)
				result->Add(gcnew String(name.c_str()));
			return result;
		}

		List<HandleInfo^> ^Interop::GetTypeHandles(String ^type)
		{
			auto handles = Win32Lib::GetAllHandles();
			handles = Win32Lib::GetTypeHandles(handles, marshal_as<wstring>(type));
			return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
		}

		IntPtr Interop::GetSharedMemorySize(int pid, IntPtr handle)
		{
			return (IntPtr)(intptr_t)Win32Lib::GetSharedMemorySize(pid, (HANDLE)handle);
		}

		void Interop::ReadSharedMemory(int pid, IntPtr handle, IntPtr index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			pin_ptr<byte> bytesPtr = &bytes[0];
			return Win32Lib::ReadSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr + bytesIndex, numBytes);
		}

		void Interop::WriteSharedMemory(int pid, IntPtr handle, IntPtr index, array<byte> ^bytes)
		{
			pin_ptr<byte> bytesPtr = &bytes[0];
			return Win32Lib::WriteSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr, bytes->Length);
		}

		List<HandleInfo^> ^Interop::GetHandleInfo(shared_ptr<vector<shared_ptr<Win32Lib::HandleInfo>>> handles)
		{
			auto result = gcnew List<HandleInfo^>();
			for each (auto handle in *handles)
				result->Add(gcnew HandleInfo(handle));
			return result;
		}
	}
}
