#include "stdafx.h"
#include "HandleList.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			HandleList::HandleList(std::shared_ptr<const std::vector<std::shared_ptr<const void>>> _handles)
			{
				handles = _handles;
			}

			std::shared_ptr<const std::vector<std::shared_ptr<const void>>> HandleList::Get() const
			{
				return handles;
			}
		}
	}
}
