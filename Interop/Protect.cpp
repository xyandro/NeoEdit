#include "stdafx.h"
#include "Protect.h"

#ifdef __cplusplus_cli

namespace NeoEdit
{
	namespace Interop
	{
		Protect::Protect(Handle ^handle, VirtualQueryInfo ^info, int protect)
		{
			this->handle = handle;
			this->info = info;
			this->protect = protect;

			if (protect == info->Protect)
				return;

			DWORD oldProtect;
			if (!VirtualProtectEx(handle->Get(), (void*)info->StartAddress, (SIZE_T)info->RegionSize.ToPointer(), protect, &oldProtect))
				throw gcnew System::ComponentModel::Win32Exception();
		}

		Protect::~Protect()
		{
			if (protect == info->Protect)
				return;

			DWORD oldProtect;
			if (!VirtualProtectEx(handle->Get(), (void*)info->StartAddress, (SIZE_T)info->RegionSize.ToPointer(), info->Protect, &oldProtect))
				return;
			if (!FlushInstructionCache(handle->Get(), (void*)info->StartAddress, (SIZE_T)info->RegionSize.ToPointer()))
				return;
		}
	}
}

#endif
