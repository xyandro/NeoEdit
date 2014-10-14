#pragma once

namespace NeoEdit
{
	namespace Win32LibNS
	{
		class Window
		{
		public:
			static intptr_t AllocConsole();
			static void SendChar(intptr_t handle, unsigned char ch);
		};

	}
}
