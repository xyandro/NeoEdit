#include "Tidy.h"

#include "HTMLTidy\tidy.h"
#include "HTMLTidy\buffio.h"

namespace NeoEdit
{
	namespace Lib
	{
		namespace HTMLTidy
		{
			std::wstring Tidier::Tidy(std::wstring input)
			{
				if (input.length() == 0)
					return L"";

				std::wstring result;

				auto tdoc = tidyCreate();

				int status = tidySetCharEncoding(tdoc, "UTF16LE");
				tidyOptSetInt(tdoc, TidyIndentContent, TidyYesState);
				tidyOptSetInt(tdoc, TidyWrapLen, 2000000000);

				TidyBuffer buf = { 0 };
				tidyBufAttach(&buf, (byte*)input.c_str(), (uint)input.length() * sizeof(wchar_t));
				status = tidyParseBuffer(tdoc, &buf);
				status = tidyCleanAndRepair(tdoc);

				TidyBuffer outbuf = { 0 };
				status = tidySaveBuffer(tdoc, &outbuf);
				result = std::wstring((wchar_t*)outbuf.bp, outbuf.size / sizeof(wchar_t));
				tidyBufFree(&outbuf);

				tidyRelease(tdoc);

				return result;
			}
		}
	}
}
