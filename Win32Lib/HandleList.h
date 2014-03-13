#pragma once

#include <memory>
#include <vector>

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace Handles
		{
			class HandleList
			{
			public:
				HandleList(std::shared_ptr<const std::vector<std::shared_ptr<const void>>> handles);
				std::shared_ptr<const std::vector<std::shared_ptr<const void>>> Get() const;
			private:
				std::shared_ptr<const std::vector<std::shared_ptr<const void>>> handles;
			};
		}
	}
}
