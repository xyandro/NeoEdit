#pragma once

#include<string>

namespace NeoEdit
{
	namespace Lib
	{
		namespace HTMLTidy
		{
			class Tidier
			{
			public:
				static std::wstring Tidy(std::wstring input);
			};
		}
	}
}
