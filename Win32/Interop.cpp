#include "stdafx.h"
#include "Interop.h"

#include <msclr\marshal_cppstd.h>

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
				auto result = Win32Lib::VirtualQuery(handle->Get(), (uint8_t*)index);
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

		void Interop::ReadProcessMemory(Handle ^handle, int64_t index, array<uint8_t> ^bytes, int bytesIndex, int numBytes)
		{
			try
			{
				pin_ptr<uint8_t> ptr = &bytes[bytesIndex];
				return Win32Lib::ReadProcessMemory(handle->Get(), (uint8_t*)index, ptr, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::WriteProcessMemory(Handle ^handle, int64_t index, array<uint8_t> ^bytes, int numBytes)
		{
			try
			{
				pin_ptr<uint8_t> ptr = &bytes[0];
				return Win32Lib::WriteProcessMemory(handle->Get(), (uint8_t*)index, ptr, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		HandleList ^Interop::GetAllHandles()
		{
			try
			{
				return gcnew HandleList(Win32Lib::GetAllHandles());
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::GetTypeHandles(HandleList ^handles, System::String ^type)
		{
			try
			{
				handles->Set(Win32Lib::GetTypeHandles(handles->Get(), marshal_as<wstring>(type)));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::GetProcessHandles(HandleList ^handles, int pid)
		{
			try
			{
				handles->Set(Win32Lib::GetProcessHandles(handles->Get(), pid));
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		List<HandleInfo^> ^Interop::GetHandleInfo(HandleList ^handles)
		{
			try
			{
				auto handleInfo = Win32Lib::GetHandleInfo(handles->Get());
				handles->Set(nullptr);

				auto result = gcnew List<HandleInfo^>();
				for each (auto handle in *handleInfo)
					result->Add(gcnew HandleInfo(handle));
				return result;
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

		int64_t Interop::GetSharedMemorySize(int pid, IntPtr handle)
		{
			try
			{
				return Win32Lib::GetSharedMemorySize(pid, (HANDLE)handle);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::ReadSharedMemory(int pid, IntPtr handle, int64_t index, array<uint8_t> ^bytes, int bytesIndex, int numBytes)
		{
			try
			{
				pin_ptr<uint8_t> bytesPtr = &bytes[bytesIndex];
				return Win32Lib::ReadSharedMemory(pid, (HANDLE)handle, (uintptr_t)index, bytesPtr, numBytes);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::WriteSharedMemory(int pid, IntPtr handle, int64_t index, array<uint8_t> ^bytes)
		{
			try
			{
				pin_ptr<uint8_t> bytesPtr = &bytes[0];
				return Win32Lib::WriteSharedMemory(pid, (HANDLE)handle, (uintptr_t)index, bytesPtr, bytes->Length);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		System::IntPtr Interop::AllocConsole()
		{
			try
			{
				return System::IntPtr(Win32Lib::AllocConsole());
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}

		void Interop::SendChar(System::IntPtr handle, unsigned char ch)
		{
			try
			{
				return Win32Lib::SendChar((intptr_t)handle, ch);
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
		}
	}
}
