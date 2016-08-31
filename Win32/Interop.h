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
			static int64_t GetSharedMemorySize(int pid, System::IntPtr handle);
			static void ReadSharedMemory(int pid, System::IntPtr handle, int64_t index, cli::array<uint8_t> ^bytes, int bytesIndex, int numBytes);
			static void WriteSharedMemory(int pid, System::IntPtr handle, int64_t index, cli::array<uint8_t> ^bytes);

			enum class GetLinesEncoding
			{
				Default,
				UTF8,
				UTF16LE,
				UTF16BE,
				UTF32LE,
				UTF32BE,
			};
			static System::Collections::Generic::List<int64_t> ^GetLines(GetLinesEncoding encoding, cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static void ConvertEncoding(cli::array<uint8_t>^ inputArray, int inputSize, GetLinesEncoding inputEncoding, array<uint8_t>^ outputArray, GetLinesEncoding outputEncoding, [System::Runtime::InteropServices::Out]int %inputUsed, [System::Runtime::InteropServices::Out]int %outputUsed);
		};
	}
}
