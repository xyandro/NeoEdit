#pragma once

#include "stdafx.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		class Win32Exception
		{
		public:
			std::wstring Message();
			Win32Exception(std::wstring message);
			static void Throw();
		private:
			std::wstring message;
		};
	}
}
