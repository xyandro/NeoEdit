#pragma once

#include "VirtualQueryInfo.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Processes
		{
			class Protect
			{
			public:
				Protect(std::shared_ptr<void> handle, std::shared_ptr<const VirtualQueryInfo> info, uint32_t protect);
				~Protect();
			private:
				std::shared_ptr<void> handle;
				std::shared_ptr<const VirtualQueryInfo> info;
				uint32_t protect;
			};
		}
	}
}
