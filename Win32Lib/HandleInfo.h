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
				int32_t PID;
				void *Handle;
				std::wstring Type, Name, Data;

				HandleInfo(int32_t PID, void *Handle, std::wstring Type, std::wstring Name, std::wstring Data);
			};
		}
	}
}
