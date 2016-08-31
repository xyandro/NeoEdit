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

#pragma warning( push )
#pragma warning( disable : 4305)
#pragma warning( disable : 4309)

		template <typename type> System::Collections::Generic::List<int64_t> ^GetLinesTemplate(Interop::GetLinesEncoding encoding, cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
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

			auto lineStart = gcnew System::Collections::Generic::List<int64_t>();
			auto size = data->Length / sizeof(type);
			if (size == 0)
				return lineStart;

			pin_ptr<const unsigned char> pin = &data[0];
			auto start = (const type*)pin;
			auto end = start + size;

			for (auto ptr = start; ptr < end; ++ptr)
			{
				auto endLen = 0;
				if ((ptr + 1 < end) && (((ptr[0] == cr) && (ptr[1] == lf)) || ((ptr[0] == lf) && (ptr[1] == cr)))) endLen = 2;
				else if ((*ptr == cr) || (*ptr == lf)) endLen = 1;
				else if (*ptr == tab)
					lineLength = ((lineLength >> 2) + 1) << 2;
				else if ((mask == 0) || ((*ptr & mask) != value))
					++lineLength;

				if (endLen != 0)
				{
					if (lineLength > maxLine)
						maxLine = lineLength;
					lineLength = 0;
					lineStart->Add((ptr - start + endLen) * sizeof(type));
					ptr += endLen - 1;
				}
			}
			return lineStart;
		}
#pragma warning( pop ) 

		System::Collections::Generic::List<int64_t> ^Interop::GetLines(GetLinesEncoding encoding, cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			try
			{
				switch (encoding)
				{
				case GetLinesEncoding::Default: return GetLinesTemplate<uint8_t>(encoding, data, lineLength, maxLine);
				case GetLinesEncoding::UTF8: return GetLinesTemplate<uint8_t>(encoding, data, lineLength, maxLine);
				case GetLinesEncoding::UTF16LE: return GetLinesTemplate<uint16_t>(encoding, data, lineLength, maxLine);
				case GetLinesEncoding::UTF16BE: return GetLinesTemplate<uint16_t>(encoding, data, lineLength, maxLine);
				case GetLinesEncoding::UTF32LE: return GetLinesTemplate<uint32_t>(encoding, data, lineLength, maxLine);
				case GetLinesEncoding::UTF32BE: return GetLinesTemplate<uint32_t>(encoding, data, lineLength, maxLine);
				default: throw gcnew System::Exception("Invalid argument type");
				}
			}
			catch (Win32Lib::Win32Exception &ex) { throw gcnew Win32Exception(gcnew String(ex.Message().c_str())); }
			return nullptr;
		}
	}
}
