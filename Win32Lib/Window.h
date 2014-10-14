#pragma once

namespace NeoEdit
{
	namespace Win32LibNS
	{
		class Window
		{
		public:
			static intptr_t CreateHiddenConsole();
			static void SendChar(intptr_t handle, unsigned char ch);
		};

	}
}
