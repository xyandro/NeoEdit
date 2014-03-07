#include "stdafx.h"
#include "HandleInfo.h"

using namespace std;

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			HandleInfo::HandleInfo(DWORD PID, HANDLE Handle, wstring Type, wstring Name, wstring Data) :
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
