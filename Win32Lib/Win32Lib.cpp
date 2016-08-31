#include "stdafx.h"
#include "Win32Lib.h"

#include "Handle.h"

namespace NeoEdit
{
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetAllHandles)() = Win32LibNS::Handles::Handle::GetAllHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetTypeHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, std::wstring type) = Win32LibNS::Handles::Handle::GetTypeHandles;
	std::shared_ptr<const Win32Lib::HandleList> (*Win32Lib::GetProcessHandles)(std::shared_ptr<const Win32Lib::HandleList> handles, int32_t pid) = Win32LibNS::Handles::Handle::GetProcessHandles;
	std::shared_ptr<const std::vector<std::shared_ptr<const Win32Lib::HandleInfo>>> (*Win32Lib::GetHandleInfo)(std::shared_ptr<const Win32Lib::HandleList> handles) = Win32LibNS::Handles::Handle::GetHandleInfo;
	std::shared_ptr<const std::vector<std::wstring>> (*Win32Lib::GetHandleTypes)() = Win32LibNS::Handles::Handle::GetHandleTypes;
}
