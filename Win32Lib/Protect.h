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
				Protect(std::shared_ptr<void> handle, std::shared_ptr<VirtualQueryInfo> info, DWORD protect);
				~Protect();
			private:
				std::shared_ptr<void> handle;
				std::shared_ptr<VirtualQueryInfo> info;
				int protect;
			};
		}
	}
}
