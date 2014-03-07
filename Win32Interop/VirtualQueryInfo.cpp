#include "stdafx.h"
#include "VirtualQueryInfo.h"

using namespace std;
using namespace System;

namespace NeoEdit
{
	namespace Win32Interop
	{
		bool VirtualQueryInfo::Committed::get() { return (*ptr)->Committed; }
		bool VirtualQueryInfo::Mapped::get() { return (*ptr)->Mapped; }
		bool VirtualQueryInfo::NoAccess::get() { return (*ptr)->NoAccess; }
		int VirtualQueryInfo::Protect::get() { return (*ptr)->Protect; }
		System::IntPtr VirtualQueryInfo::StartAddress::get() { return (IntPtr)(*ptr)->StartAddress; }
		System::IntPtr VirtualQueryInfo::EndAddress::get() { return (IntPtr)(*ptr)->EndAddress; }
		System::IntPtr VirtualQueryInfo::RegionSize::get() { return (IntPtr)(intptr_t)(*ptr)->RegionSize; }

		VirtualQueryInfo::VirtualQueryInfo(shared_ptr<Win32Lib::VirtualQueryInfo> _ptr)
		{
			ptr = new shared_ptr<Win32Lib::VirtualQueryInfo>(_ptr);
		}

		VirtualQueryInfo::~VirtualQueryInfo()
		{
			delete ptr;
		}

		shared_ptr<Win32Lib::VirtualQueryInfo> VirtualQueryInfo::Get()
		{
			return *ptr;
		}
	}
}
