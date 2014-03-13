#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections::Generic;
using namespace msclr::interop;

namespace NeoEdit
{
	namespace Win32
	{
		void Interop::SuspendProcess(int pid)
		{
			try
			{
				return Win32Lib::SuspendProcess(pid);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::ResumeProcess(int pid)
		{
			try
			{
				return Win32Lib::ResumeProcess(pid);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		Handle ^Interop::OpenReadMemoryProcess(int pid)
		{
			try
			{
				return gcnew Handle(Win32Lib::OpenReadMemoryProcess(pid));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		int64_t Interop::GetProcessMemoryLength(Handle ^handle)
		{
			try
			{
				return Win32Lib::GetProcessMemoryLength(handle->Get());
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		VirtualQueryInfo ^Interop::VirtualQuery(Handle ^handle, int64_t index)
		{
			try
			{
				auto result = Win32Lib::VirtualQuery(handle->Get(), (byte*)(intptr_t)index);
				if (result == nullptr)
					return nullptr;
				return gcnew VirtualQueryInfo(result);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		Protect ^Interop::SetProtect(Handle ^handle, VirtualQueryInfo ^info, bool write)
		{
			try
			{
				return gcnew Protect(Win32Lib::SetProtect(handle->Get(), info->Get(), write));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::ReadProcessMemory(Handle ^handle, int64_t index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			try
			{
				pin_ptr<byte> ptr = &bytes[0];
				return Win32Lib::ReadProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr + bytesIndex, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::WriteProcessMemory(Handle ^handle, int64_t index, array<byte> ^bytes, int numBytes)
		{
			try
			{
				pin_ptr<byte> ptr = &bytes[0];
				return Win32Lib::WriteProcessMemory(handle->Get(), (byte*)(intptr_t)index, ptr, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<int> ^Interop::GetPIDsWithFileLock(String ^fileName)
		{
			try
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
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<HandleInfo^> ^Interop::GetHandles()
		{
			try
			{
				auto handles = Win32Lib::GetAllHandles();
				return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<HandleInfo^> ^Interop::GetProcessHandles(int pid)
		{
			try
			{
				auto handles = Win32Lib::GetAllHandles();
				handles = Win32Lib::GetProcessHandles(handles, (DWORD)pid);
				return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<String^> ^Interop::GetHandleTypes()
		{
			try
			{
				auto types = Win32Lib::GetHandleTypes();
				auto result = gcnew List<String^>;
				for each (auto name in *types)
					result->Add(gcnew String(name.c_str()));
				return result;
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<HandleInfo^> ^Interop::GetTypeHandles(String ^type)
		{
			try
			{
				auto handles = Win32Lib::GetAllHandles();
				handles = Win32Lib::GetTypeHandles(handles, marshal_as<wstring>(type));
				return GetHandleInfo(Win32Lib::GetHandleInfo(handles));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		int64_t Interop::GetSharedMemorySize(int pid, IntPtr handle)
		{
			try
			{
				return Win32Lib::GetSharedMemorySize(pid, (HANDLE)handle);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::ReadSharedMemory(int pid, IntPtr handle, int64_t index, array<byte> ^bytes, int bytesIndex, int numBytes)
		{
			try
			{
				pin_ptr<byte> bytesPtr = &bytes[0];
				return Win32Lib::ReadSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr + bytesIndex, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::WriteSharedMemory(int pid, IntPtr handle, int64_t index, array<byte> ^bytes)
		{
			try
			{
				pin_ptr<byte> bytesPtr = &bytes[0];
				return Win32Lib::WriteSharedMemory(pid, (HANDLE)handle, (intptr_t)index, (byte*)bytesPtr, bytes->Length);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<HandleInfo^> ^Interop::GetHandleInfo(shared_ptr<const vector<shared_ptr<const Win32Lib::HandleInfo>>> handles)
		{
			try
			{
				auto result = gcnew List<HandleInfo^>();
				for each (auto handle in *handles)
					result->Add(gcnew HandleInfo(handle));
				return result;
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}
	}
}
