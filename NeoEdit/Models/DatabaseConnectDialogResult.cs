using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.Common.Transform;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class DatabaseConnectDialogResult
	{
		public DBConnectInfo DBConnectInfo { get; internal set; }
	}
}
