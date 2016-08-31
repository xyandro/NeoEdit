#pragma once

#include "HandleInfo.h"
#include "HandleList.h"
#include "Handle.h"

namespace NeoEdit
{
	namespace Win32
	{
		public ref class Interop
		{
		public:
			static HandleList ^GetAllHandles();
			static void GetTypeHandles(HandleList ^handles, System::String ^type);
			static void GetProcessHandles(HandleList ^handles, int pid);
			static System::Collections::Generic::List<HandleInfo^> ^GetHandleInfo(HandleList ^handles);
			static System::Collections::Generic::List<System::String^> ^GetHandleTypes();

			static System::Collections::Generic::List<int64_t> ^GetLinesDefault(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF8(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF16LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF16BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF32LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF32BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
		};
	}
}
