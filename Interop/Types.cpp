#include "stdafx.h"
#include "Types.h"

using namespace System;

namespace NeoEdit
{
	namespace Interop
	{
		HandleInfo::HandleInfo(int PID, System::String ^Type, System::String ^Name)
		{
			this->PID = PID;
			this->Type = Type;
			this->Name = Name;
		}

		String ^HandleInfo::ToString()
		{
			return String::Format("{0} {1} {2}", PID, Type,	Name);
		}
	}
}
