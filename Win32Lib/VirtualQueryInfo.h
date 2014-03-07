#pragma once

#include "stdafx.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			struct VirtualQueryInfo
			{
				bool Committed, Mapped, NoAccess;
				int Protect;
				PVOID StartAddress, EndAddress;
				intptr_t RegionSize;
			};
		}
	}
}
