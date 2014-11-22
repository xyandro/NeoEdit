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
				auto result = gcnew List < String^ > ;
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

#pragma warning( push )
#pragma warning( disable : 4305)
#pragma warning( disable : 4309)

		template <typename type> System::Collections::Generic::List<int64_t> ^GetLinesTemplate(Interop::GetLinesEncoding encoding, array<uint8_t>^ data, int64_t use, int64_t %position, int %lineLength, int %maxLine)
		{
			bool bigEndian = (encoding == Interop::GetLinesEncoding::UTF16BE) || (encoding == Interop::GetLinesEncoding::UTF32BE);
			type cr = !bigEndian ? 0x0d : sizeof(type) == 2 ? 0x0d00 : 0x0d000000;
			type lf = !bigEndian ? 0x0a : sizeof(type) == 2 ? 0x0a00 : 0x0a000000;
			type tab = !bigEndian ? 0x09 : sizeof(type) == 2 ? 0x0900 : 0x09000000;
			type mask = 0, value = 0;
			switch (encoding)
			{
			case NeoEdit::Win32::Interop::GetLinesEncoding::UTF8: mask = 0xc0; value = 0x80; break;
			case NeoEdit::Win32::Interop::GetLinesEncoding::UTF16LE: mask = 0xfc00; value = 0xdc00; break;
			case NeoEdit::Win32::Interop::GetLinesEncoding::UTF16BE: mask = 0x00fc; value = 0x00dc; break;
			}

			use /= sizeof(type);
			auto lineStart = gcnew System::Collections::Generic::List<int64_t>();
			pin_ptr<const unsigned char> pin = &data[0];
			auto block = (const type*)pin;

			int ctr, endLen = 0;
			for (ctr = 0; ctr < use; ctr += 1)
			{
				if (((block[ctr + 0] == cr) && (block[ctr + 1] == lf)) || ((block[ctr + 0] == lf) && (block[ctr + 1] == cr))) endLen = 2;
				else if ((block[ctr + 0] == cr) || (block[ctr + 0] == lf)) endLen = 1;
				else if (block[ctr] == tab)
					lineLength = ((lineLength >> 2) + 1) << 2;
				else if ((mask == 0) || ((block[ctr] & mask) != value))
					++lineLength;

				if (endLen != 0)
				{
					if (lineLength > maxLine)
						maxLine = lineLength;
					lineLength = 0;
					lineStart->Add(position + (ctr + endLen) * sizeof(type));
					ctr += endLen - 1;
					endLen = 0;
				}
			}
			position += ctr * sizeof(type);
			return lineStart;
		}
#pragma warning( pop ) 

		System::Collections::Generic::List<int64_t> ^Interop::GetLines(GetLinesEncoding encoding, array<uint8_t>^ data, int64_t use, int64_t %position, int %lineLength, int %maxLine)
		{
			try
			{
				switch (encoding)
				{
				case GetLinesEncoding::Default: return GetLinesTemplate<uint8_t>(encoding, data, use, position, lineLength, maxLine);
				case GetLinesEncoding::UTF8: return GetLinesTemplate<uint8_t>(encoding, data, use, position, lineLength, maxLine);
				case GetLinesEncoding::UTF16LE: return GetLinesTemplate<uint16_t>(encoding, data, use, position, lineLength, maxLine);
				case GetLinesEncoding::UTF16BE: return GetLinesTemplate<uint16_t>(encoding, data, use, position, lineLength, maxLine);
				case GetLinesEncoding::UTF32LE: return GetLinesTemplate<uint32_t>(encoding, data, use, position, lineLength, maxLine);
				case GetLinesEncoding::UTF32BE: return GetLinesTemplate<uint32_t>(encoding, data, use, position, lineLength, maxLine);
				default: throw gcnew System::Exception("Invalid argument type");
				}
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
			return nullptr;
		}
	}
}
