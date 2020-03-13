using System;

namespace NeoEdit.Program.Controls
{
	[AttributeUsage(AttributeTargets.Field)]
	public class NoMacroAttribute : Attribute
	{
		public bool IncludeHandled { get; set; } = false;
	}
}
