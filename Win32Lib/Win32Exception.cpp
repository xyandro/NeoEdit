#include "stdafx.h"
#include "Win32Exception.h"

#include "Win32Lib.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		wstring Win32Exception::Message()
		{
			return message;
		}

		Win32Exception::Win32Exception(wstring _message)
		{
			message = _message;
		}

		void Win32Exception::Throw()
		{
			wchar_t message[4096];
			FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, GetLastError(), MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), reinterpret_cast<LPWSTR>(message), sizeof(message), NULL);
			throw Win32Exception(L"Error: " + to_wstring(GetLastError()) + L" : " + message);
		}
	}
}
