using System;
using System.IO;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class NetworkFetchStreamDialogResult
	{
		public string Expression { get; set; }
		public string OutputDirectory { get; set; }
	}
}
