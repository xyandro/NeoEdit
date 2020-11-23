using System;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Execute_Internal_Activate()
		{
			LastActive = DateTime.Now;
			NEFiles.ForEach(neFile => neFile.CheckForRefresh());
		}
	}
}
