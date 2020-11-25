using System;
using System.Linq;
using NeoEdit.Editor;
using NeoEdit.UI;

namespace NeoEdit
{
	class Program
	{
		[STAThread] static void Main() => App.RunProgram(() => new NEGlobal(), string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(str => $"\"{str}\"")));
	}
}
