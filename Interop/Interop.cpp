#include "stdafx.h"
#include "Interop.h"

using namespace std;

namespace NeoEdit
{
	namespace Interop
	{
		shared_ptr<hash_set<int>> GetThreadIDs(int pid)
		{
			shared_ptr<hash_set<int>> threadSet(new hash_set<int>);

			HANDLE toolHelp = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
			if (toolHelp == INVALID_HANDLE_VALUE)
				throw "CreateToolhelp32Snapshot";
			shared_ptr<void> toolHelpDeleter(toolHelp, CloseHandle);

			THREADENTRY32 te;
			te.dwSize = sizeof(te);
			if (!Thread32First(toolHelp, &te))
				return threadSet;

			do
			{
				if (te.th32OwnerProcessID != pid)
					continue;

				threadSet->insert(te.th32ThreadID);
			}
			while (Thread32Next(toolHelp, &te));

			return threadSet;
		}

		void NEInterop::SuspendProcess(int pid)
		{
			hash_set<int> suspended;

			// Keep going until we get them all; more might pop up as we're working
			while (true)
			{
				auto found = false;

				auto threadSet = GetThreadIDs(pid);
				for each (auto threadId in *threadSet)
				{
					if (suspended.find(threadId) == suspended.end())
					{
						found = true;
						suspended.insert(threadId);

						auto threadHandle = OpenThread(THREAD_SUSPEND_RESUME, FALSE, threadId);
						if (threadHandle == NULL)
							throw "OpenThread";
						shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

						if (SuspendThread(threadHandle) == -1)
							throw "SuspendThread";
					}
				}

				if (!found)
					break;
			}
		}

		void NEInterop::ResumeProcess(int pid)
		{
			auto threadSet = GetThreadIDs(pid);
			for each (auto threadId in *threadSet)
			{
				auto threadHandle = OpenThread(THREAD_SUSPEND_RESUME, FALSE, threadId);
				if (threadHandle == NULL)
					throw "OpenThread";
				shared_ptr<void> threadHandleDeleter(threadHandle, CloseHandle);

				if (ResumeThread(threadHandle) == -1)
					throw "ResumeThread";
			}
		}
	}
}
