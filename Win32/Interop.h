#pragma once

#include <stdint.h>

namespace NeoEdit
{
	namespace Win32
	{
		public ref class Interop
		{
		public:
			static System::Collections::Generic::List<int64_t> ^GetLinesDefault(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF8(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF16LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF16BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF32LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
			static System::Collections::Generic::List<int64_t> ^GetLinesUTF32BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine);
		};
	}
}
