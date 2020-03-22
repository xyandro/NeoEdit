using System;
using System.IO;
using System.Windows;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class NetworkFetchStreamDialogResult
	{
		public string Expression { get; set; }
		public string OutputDirectory { get; set; }
	}
}
