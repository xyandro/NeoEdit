#include "stdafx.h"
#include "Window.h"

namespace NeoEdit
{
	namespace Win32LibNS
	{
		namespace
		{
			struct FindConsole
			{
				HWND hwnd;
				DWORD pid;
			};

			BOOL CALLBACK FindConsoleProc(HWND hwnd, LPARAM lParam)
			{
				FindConsole &find = *(FindConsole*)lParam;

				DWORD pid;
				GetWindowThreadProcessId(hwnd, &pid);
				if (pid != find.pid)
					return TRUE;

				char className[1024];
				GetClassNameA(hwnd, className, sizeof(className) / sizeof(*className));
				if (strcmp(className, "ConsoleWindowClass") != 0)
					return TRUE;

				find.hwnd = hwnd;
				return FALSE;
			}
		}

		intptr_t Window::CreateHiddenConsole()
		{
			auto active = GetForegroundWindow();
			AllocConsole();

			FindConsole find;
			memset(&find, 0, sizeof(find));
			find.pid = GetCurrentProcessId();
			EnumWindows(FindConsoleProc, (LPARAM)&find);

			if (find.hwnd == NULL)
				throw "Failed to find console window.";

			ShowWindow(find.hwnd, SW_HIDE);

			SetForegroundWindow(active);

			return (intptr_t)find.hwnd;
		}

		void Window::SendChar(intptr_t handle, unsigned char ch)
		{
			PostMessage((HWND)handle, WM_CHAR, ch, 0);
		}
	}
}