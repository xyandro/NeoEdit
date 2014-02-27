#include "stdafx.h"
#include "Types.h"

using namespace System;

namespace NeoEdit
{
	namespace Interop
	{
		HandleInfo::HandleInfo(int PID, IntPtr Handle, String ^Type, String ^Name)
		{
			this->PID = PID;
			this->Handle = Handle;
			this->Type = Type;
			this->Name = Name;
		}

		String ^HandleInfo::ToString()
		{
			return String::Format("{0} {1} {2} {3}", PID, Handle, Type, Name);
		}
	}
}
