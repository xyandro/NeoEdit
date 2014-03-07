#include "stdafx.h"
#include "Protect.h"

#include "Win32Exception.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			Protect::Protect(shared_ptr<void> handle, shared_ptr<VirtualQueryInfo> info, DWORD protect) :
				handle(handle),
				info(info),
				protect(protect)
			{
				if (protect == info->Protect)
					return;

				DWORD oldProtect;
				if (!VirtualProtectEx(handle.get(), info->StartAddress, info->RegionSize, protect, &oldProtect))
					Win32Exception::Throw();
			}

			Protect::~Protect()
			{
				if (protect == info->Protect)
					return;

				DWORD oldProtect;
				if (!VirtualProtectEx(handle.get(), info->StartAddress, info->RegionSize, info->Protect, &oldProtect))
					return;
				if (!FlushInstructionCache(handle.get(), info->StartAddress, info->RegionSize))
					return;
			}
		}
	}
}
