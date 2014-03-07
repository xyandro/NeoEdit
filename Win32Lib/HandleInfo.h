#pragma once

#include "stdafx.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			struct HandleInfo
			{
				DWORD PID;
				HANDLE Handle;
				std::wstring Type, Name, Data;

				HandleInfo(DWORD PID, HANDLE Handle, std::wstring Type, std::wstring Name, std::wstring Data);
			};
		}
	}
}
