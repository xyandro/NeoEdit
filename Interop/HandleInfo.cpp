#include "stdafx.h"
#include "HandleInfo.h"

using namespace System;

namespace NeoEdit
{
	namespace Interop
	{
		int HandleInfo::PID::get() { return (*ptr)->PID; }
		IntPtr HandleInfo::Handle::get() { return (IntPtr)(*ptr)->Handle; }
		String ^HandleInfo::Type::get() { return gcnew String((*ptr)->Type.c_str()); }
		String ^HandleInfo::Name::get() { return gcnew String((*ptr)->Name.c_str()); }
		String ^HandleInfo::Data::get() { return gcnew String((*ptr)->Data.c_str()); }

		HandleInfo::HandleInfo(std::shared_ptr<Win32Lib::HandleInfo> _ptr)
		{
			ptr = new std::shared_ptr<Win32Lib::HandleInfo>(_ptr);
		}

		HandleInfo::~HandleInfo()
		{
			delete ptr;
		}

		String ^HandleInfo::ToString()
		{
			return String::Format("{0} {1} {2} {3} {4}", PID, Handle, Type, Name, Data);
		}
	}
}
