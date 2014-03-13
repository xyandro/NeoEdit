#include "stdafx.h"
#include "Protect.h"

#include <Windows.h>

#include "Win32Exception.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			Protect::Protect(shared_ptr<void> handle, shared_ptr<const VirtualQueryInfo> info, uint32_t protect) :
				handle(handle),
				info(info),
				protect(protect)
			{
				if (protect == info->Protect)
					return;

				DWORD oldProtect;
				if (!VirtualProtectEx((HANDLE)handle.get(), info->StartAddress, info->RegionSize, protect, &oldProtect))
					Win32Exception::Throw();
			}

			Protect::~Protect()
			{
				if (protect == info->Protect)
					return;

				DWORD oldProtect;
				if (!VirtualProtectEx((HANDLE)handle.get(), info->StartAddress, info->RegionSize, info->Protect, &oldProtect))
					return;
				if (!FlushInstructionCache((HANDLE)handle.get(), info->StartAddress, info->RegionSize))
					return;
			}
		}
	}
}
