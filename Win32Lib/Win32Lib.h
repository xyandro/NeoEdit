#pragma once

#include "HandleList.h"
#include "HandleInfo.h"
#include "Win32Exception.h"

namespace NeoEdit
{
	class Win32Lib
	{
	public:
		typedef Win32LibNS::Handles::HandleInfo HandleInfo;
		typedef Win32LibNS::Handles::HandleList HandleList;
		typedef Win32LibNS::Win32Exception Win32Exception;

		static std::shared_ptr<const HandleList> (*GetAllHandles)();
		static std::shared_ptr<const HandleList> (*GetTypeHandles)(std::shared_ptr<const HandleList> handles, std::wstring type);
		static std::shared_ptr<const HandleList> (*GetProcessHandles)(std::shared_ptr<const HandleList> handles, int32_t pid);
		static std::shared_ptr<const std::vector<std::shared_ptr<const HandleInfo>>> (*GetHandleInfo)(std::shared_ptr<const HandleList> handles);
		static std::shared_ptr<const std::vector<std::wstring>> (*GetHandleTypes)();
	};
}
