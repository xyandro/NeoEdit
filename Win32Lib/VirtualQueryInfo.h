#pragma once

#include <stdint.h>

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			struct VirtualQueryInfo
			{
				bool Committed, Mapped, NoAccess;
				uint32_t Protect;
				uint8_t *StartAddress, *EndAddress;
				uintptr_t RegionSize;
			};
		}
	}
}
