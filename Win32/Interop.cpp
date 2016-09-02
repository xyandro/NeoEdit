#include "stdafx.h"
#include "Interop.h"

using namespace System::Collections::Generic;

namespace NeoEdit
{
	namespace Win32
	{
		template <typename type> List<int64_t> ^GetLinesTemplate(cli::array<uint8_t>^ data, int %lineLength, int %maxLine, bool bigEndian, type mask, type value)
		{
			type cr = !bigEndian ? 0x0d : sizeof(type) == 2 ? 0x0d00 : 0x0d000000;
			type lf = !bigEndian ? 0x0a : sizeof(type) == 2 ? 0x0a00 : 0x0a000000;
			type tab = !bigEndian ? 0x09 : sizeof(type) == 2 ? 0x0900 : 0x09000000;

			auto lineStart = gcnew List<int64_t>();
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

		List<int64_t> ^Interop::GetLinesDefault(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint8_t>(data, lineLength, maxLine, false, 0, 0);
		}

		List<int64_t> ^Interop::GetLinesUTF8(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint8_t>(data, lineLength, maxLine, false, 0xc0, 0xc0);
		}

		List<int64_t> ^Interop::GetLinesUTF16LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint16_t>(data, lineLength, maxLine, false, 0xfc00, 0xfc00);
		}

		List<int64_t> ^Interop::GetLinesUTF16BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint16_t>(data, lineLength, maxLine, true, 0x00fc, 0x00fc);
		}

		List<int64_t> ^Interop::GetLinesUTF32LE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint32_t>(data, lineLength, maxLine, false, 0, 0);
		}

		List<int64_t> ^Interop::GetLinesUTF32BE(cli::array<uint8_t>^ data, int %lineLength, int %maxLine)
		{
			return GetLinesTemplate<uint32_t>(data, lineLength, maxLine, true, 0, 0);
		}
	}
}
