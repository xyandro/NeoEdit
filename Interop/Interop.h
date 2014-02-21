#pragma once

namespace NeoEdit
{
	namespace Interop
	{
		public ref class NEInterop
		{
		public:
			static void SuspendProcess(int pid);
			static void ResumeProcess(int pid);
		};
	}
}
