#include "stdafx.h"
#include "HandleInfo.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			HandleInfo::HandleInfo(int32_t PID, void *Handle, wstring Type, wstring Name, wstring Data) :
				PID(PID),
				Handle(Handle),
				Type(Type),
				Name(Name),
				Data(Data)
			{
			}
		}
	}
}
