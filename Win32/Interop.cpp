#include "stdafx.h"
#include "Interop.h"

using namespace std;
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
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

		void Interop::ConvertEncoding(array<uint8_t>^ inputArray, int inputSize, GetLinesEncoding inputEncoding, array<uint8_t>^ outputArray, GetLinesEncoding outputEncoding, [Out]int %inputUsed, [Out]int %outputUsed)
		{
			inputUsed = outputUsed = 0;
			if (inputSize == 0)
				return;

			pin_ptr<uint8_t> inputPin = &inputArray[0];
			auto input = (uint8_t*)inputPin;
			auto inputEnd = input + inputSize;

			pin_ptr<uint8_t> outputPin = &outputArray[0];
			auto output = (uint8_t*)outputPin;

			while (input < inputEnd)
			{
				// Figure out which code point we have
				uint32_t codePoint;
				if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::Default)
				{
					codePoint = *(input++);
					switch (codePoint)
					{
					case 0x80: codePoint = 0x20ac; break;
					case 0x82: codePoint = 0x201a; break;
					case 0x83: codePoint = 0x0192; break;
					case 0x84: codePoint = 0x201e; break;
					case 0x85: codePoint = 0x2026; break;
					case 0x86: codePoint = 0x2020; break;
					case 0x87: codePoint = 0x2021; break;
					case 0x88: codePoint = 0x02c6; break;
					case 0x89: codePoint = 0x2030; break;
					case 0x8a: codePoint = 0x0160; break;
					case 0x8b: codePoint = 0x2039; break;
					case 0x8c: codePoint = 0x0152; break;
					case 0x8e: codePoint = 0x017d; break;
					case 0x91: codePoint = 0x2018; break;
					case 0x92: codePoint = 0x2019; break;
					case 0x93: codePoint = 0x201c; break;
					case 0x94: codePoint = 0x201d; break;
					case 0x95: codePoint = 0x2022; break;
					case 0x96: codePoint = 0x2013; break;
					case 0x97: codePoint = 0x2014; break;
					case 0x98: codePoint = 0x02dc; break;
					case 0x99: codePoint = 0x2122; break;
					case 0x9a: codePoint = 0x0161; break;
					case 0x9b: codePoint = 0x203a; break;
					case 0x9c: codePoint = 0x0153; break;
					case 0x9e: codePoint = 0x017e; break;
					case 0x9f: codePoint = 0x0178; break;
					}
				}
				else if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::UTF8)
				{
					uint8_t len;
					if ((*input & 0x80) == 0x00) len = 1;
					else if ((*input & 0xe0) == 0xc0) len = 2;
					else if ((*input & 0xf0) == 0xe0) len = 3;
					else if ((*input & 0xf8) == 0xf0) len = 4;
					else if ((*input & 0xfc) == 0xf8) len = 5;
					else if ((*input & 0xfe) == 0xfc) len = 6;
					else if ((*input & 0xff) == 0xfe) len = 7;
					if (input + len > inputEnd)
						break;

					switch (len)
					{
					case 1: codePoint = *input; break;
					case 2: codePoint = ((uint32_t)(input[0] & 0x1f) << 0x06) | ((uint32_t)(input[1] & 0x3f) << 0x00); break;
					case 3: codePoint = ((uint32_t)(input[0] & 0x0f) << 0x0c) | ((uint32_t)(input[1] & 0x3f) << 0x06) | ((uint32_t)(input[2] & 0x3f) << 0x00); break;
					case 4: codePoint = ((uint32_t)(input[0] & 0x07) << 0x12) | ((uint32_t)(input[1] & 0x3f) << 0x0c) | ((uint32_t)(input[2] & 0x3f) << 0x06) | ((uint32_t)(input[3] & 0x3f) << 0x00); break;
					case 5: codePoint = ((uint32_t)(input[0] & 0x03) << 0x18) | ((uint32_t)(input[1] & 0x3f) << 0x12) | ((uint32_t)(input[2] & 0x3f) << 0x0c) | ((uint32_t)(input[3] & 0x3f) << 0x06) | ((uint32_t)(input[4] & 0x3f) << 0x00); break;
					case 6: codePoint = ((uint32_t)(input[0] & 0x01) << 0x1e) | ((uint32_t)(input[1] & 0x3f) << 0x18) | ((uint32_t)(input[2] & 0x3f) << 0x12) | ((uint32_t)(input[3] & 0x3f) << 0x0c) | ((uint32_t)(input[4] & 0x3f) << 0x06) | ((uint32_t)(input[5] & 0x3f) << 0x00); break;
					case 7: codePoint = ((uint32_t)(input[1] & 0x3f) << 0x1e) | ((uint32_t)(input[2] & 0x3f) << 0x18) | ((uint32_t)(input[3] & 0x3f) << 0x12) | ((uint32_t)(input[4] & 0x3f) << 0x0c) | ((uint32_t)(input[5] & 0x3f) << 0x06) | ((uint32_t)(input[6] & 0x3f) << 0x00); break;
					}

					input += len;
				}
				else if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::UTF16LE)
				{
					codePoint = ((uint16_t)input[0] << 0) | ((uint16_t)input[1] << 8);
					if ((codePoint & 0xfc00) == 0xd800)
					{
						if (input + 4 > inputEnd)
							break;
						input += 2;
						codePoint = 0x10000 + (((codePoint & 0x03ff) << 10) | (((uint16_t)input[0] << 0) | ((uint16_t)input[1] << 8) & 0x03ff));
					}
					input += 2;
				}
				else if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::UTF16BE)
				{
					codePoint = ((uint16_t)input[0] << 8) | ((uint16_t)input[1] << 0);
					if ((codePoint & 0xfc00) == 0xd800)
					{
						if (input + 4 > inputEnd)
							break;
						input += 2;
						codePoint = 0x10000 + (((codePoint & 0x03ff) << 10) | ((((uint16_t)input[0] << 8) | ((uint16_t)input[1] << 0)) & 0x03ff));
					}
					input += 2;
				}
				else if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::UTF32LE)
				{
					codePoint = ((uint32_t)input[0] << 0x00) | ((uint32_t)input[1] << 0x08) | ((uint32_t)input[2] << 0x10) | ((uint32_t)input[3] << 0x18);
					input += 4;
				}
				else if (inputEncoding == NeoEdit::Win32::Interop::GetLinesEncoding::UTF32BE)
				{
					codePoint = ((uint32_t)input[0] << 0x18) | ((uint32_t)input[1] << 0x10) | ((uint32_t)input[2] << 0x08) | ((uint32_t)input[3] << 0x00);
					input += 4;
				}

				switch (outputEncoding)
				{
				case NeoEdit::Win32::Interop::GetLinesEncoding::Default:
					switch (codePoint)
					{
					case 0x20ac: *(output++) = 0x80; break;
					case 0x201a: *(output++) = 0x82; break;
					case 0x0192: *(output++) = 0x83; break;
					case 0x201e: *(output++) = 0x84; break;
					case 0x2026: *(output++) = 0x85; break;
					case 0x2020: *(output++) = 0x86; break;
					case 0x2021: *(output++) = 0x87; break;
					case 0x02c6: *(output++) = 0x88; break;
					case 0x2030: *(output++) = 0x89; break;
					case 0x0160: *(output++) = 0x8a; break;
					case 0x2039: *(output++) = 0x8b; break;
					case 0x0152: *(output++) = 0x8c; break;
					case 0x017d: *(output++) = 0x8e; break;
					case 0x2018: *(output++) = 0x91; break;
					case 0x2019: *(output++) = 0x92; break;
					case 0x201c: *(output++) = 0x93; break;
					case 0x201d: *(output++) = 0x94; break;
					case 0x2022: *(output++) = 0x95; break;
					case 0x2013: *(output++) = 0x96; break;
					case 0x2014: *(output++) = 0x97; break;
					case 0x02dc: *(output++) = 0x98; break;
					case 0x2122: *(output++) = 0x99; break;
					case 0x0161: *(output++) = 0x9a; break;
					case 0x203a: *(output++) = 0x9b; break;
					case 0x0153: *(output++) = 0x9c; break;
					case 0x017e: *(output++) = 0x9e; break;
					case 0x0178: *(output++) = 0x9f; break;
					case 0x80: *(output++) = '?'; break;
					case 0x82: *(output++) = '?'; break;
					case 0x83: *(output++) = '?'; break;
					case 0x84: *(output++) = '?'; break;
					case 0x85: *(output++) = '?'; break;
					case 0x86: *(output++) = '?'; break;
					case 0x87: *(output++) = '?'; break;
					case 0x88: *(output++) = '?'; break;
					case 0x89: *(output++) = '?'; break;
					case 0x8a: *(output++) = '?'; break;
					case 0x8b: *(output++) = '?'; break;
					case 0x8c: *(output++) = '?'; break;
					case 0x8e: *(output++) = '?'; break;
					case 0x91: *(output++) = '?'; break;
					case 0x92: *(output++) = '?'; break;
					case 0x93: *(output++) = '?'; break;
					case 0x94: *(output++) = '?'; break;
					case 0x95: *(output++) = '?'; break;
					case 0x96: *(output++) = '?'; break;
					case 0x97: *(output++) = '?'; break;
					case 0x98: *(output++) = '?'; break;
					case 0x99: *(output++) = '?'; break;
					case 0x9a: *(output++) = '?'; break;
					case 0x9b: *(output++) = '?'; break;
					case 0x9c: *(output++) = '?'; break;
					case 0x9e: *(output++) = '?'; break;
					case 0x9f: *(output++) = '?'; break;
					default:
						if (codePoint <= 255)
							*(output++) = codePoint;
						else
							*(output++) = '?';
						break;
					}
					break;
				case NeoEdit::Win32::Interop::GetLinesEncoding::UTF8:
					if (codePoint < 0x00000080)
						*(output++) = codePoint;
					else if (codePoint < 0x00000800)
					{
						*(output++) = 0xc0 | ((codePoint >> 0x06) & 0x1f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					else if (codePoint < 0x00010000)
					{
						*(output++) = 0xe0 | ((codePoint >> 0x0c) & 0x0f);
						*(output++) = 0x80 | ((codePoint >> 0x06) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					else if (codePoint < 0x00200000)
					{
						*(output++) = 0xf0 | ((codePoint >> 0x12) & 0x07);
						*(output++) = 0x80 | ((codePoint >> 0x0c) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x06) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					else if (codePoint < 0x04000000)
					{
						*(output++) = 0xf8 | ((codePoint >> 0x18) & 0x03);
						*(output++) = 0x80 | ((codePoint >> 0x12) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x0c) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x06) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					else if (codePoint < 0x80000000)
					{
						*(output++) = 0xfc | ((codePoint >> 0x1e) & 0x01);
						*(output++) = 0x80 | ((codePoint >> 0x18) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x12) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x0c) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x06) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					else
					{
						*(output++) = 0xfe;
						*(output++) = 0x80 | ((codePoint >> 0x1e) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x18) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x12) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x0c) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x06) & 0x3f);
						*(output++) = 0x80 | ((codePoint >> 0x00) & 0x3f);
					}
					break;
				case NeoEdit::Win32::Interop::GetLinesEncoding::UTF16LE:
					if (codePoint < 0x10000)
					{
						*(output++) = codePoint & 0xff;
						*(output++) = codePoint >> 8;
					}
					else if (codePoint <= 0x10ffff)
					{
						codePoint -= 0x10000;
						*(output++) = (codePoint >> 10) & 0xff;
						*(output++) = 0xd8 | (codePoint >> 18);
						*(output++) = codePoint & 0xff;
						*(output++) = 0xdc | ((codePoint >> 8) & 0x03);
					}
					else
					{
						// Unicode replacement character
						*(output++) = 0xfd;
						*(output++) = 0xff;
					}
					break;
				case NeoEdit::Win32::Interop::GetLinesEncoding::UTF16BE:
					if (codePoint < 0x10000)
					{
						*(output++) = codePoint >> 8;
						*(output++) = codePoint & 0xff;
					}
					else if (codePoint <= 0x10ffff)
					{
						codePoint -= 0x10000;
						*(output++) = 0xd8 | (codePoint >> 18);
						*(output++) = (codePoint >> 10) & 0xff;
						*(output++) = 0xdc | ((codePoint >> 8) & 0x03);
						*(output++) = codePoint & 0xff;
					}
					else
					{
						// Unicode replacement character
						*(output++) = 0xff;
						*(output++) = 0xfd;
					}
					break;
				case NeoEdit::Win32::Interop::GetLinesEncoding::UTF32LE:
					*(output++) = (codePoint >> 0x00) & 0xff;
					*(output++) = (codePoint >> 0x08) & 0xff;
					*(output++) = (codePoint >> 0x10) & 0xff;
					*(output++) = (codePoint >> 0x18) & 0xff;
					break;
				case NeoEdit::Win32::Interop::GetLinesEncoding::UTF32BE:
					*(output++) = (codePoint >> 0x18) & 0xff;
					*(output++) = (codePoint >> 0x10) & 0xff;
					*(output++) = (codePoint >> 0x08) & 0xff;
					*(output++) = (codePoint >> 0x00) & 0xff;
					break;
				}
			}
			inputUsed = (int)(input - inputPin);
			outputUsed = (int)(output - outputPin);
		}
	}
}
