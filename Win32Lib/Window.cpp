#include "stdafx.h"
#include "Window.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		intptr_t Window::AllocConsole()
		{
			::AllocConsole();
			return (intptr_t)GetConsoleWindow();
		}

		void Window::SendChar(intptr_t handle, unsigned char ch)
		{
			PostMessage((HWND)handle, WM_CHAR, ch, 0);
		}
	}
}