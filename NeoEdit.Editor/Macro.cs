using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public class Macro
	{
		readonly List<ExecuteState> actions = new List<ExecuteState>();
		public IReadOnlyList<ExecuteState> Actions => actions;

		public void AddAction(ExecuteState state)
		{
			var last = actions.LastOrDefault();
			if ((state.Command == NECommand.Internal_Text) && (last?.Command == NECommand.Internal_Text))
				last.Text += state.Text;
			else
				actions.Add(state);
		}

		public static readonly string MacroDirectory = Path.Combine(Helpers.NeoEditAppData, "Macro");

		public static string ChooseMacro(INEWindowUI filesWindow)
		{
			var result = filesWindow.RunDialog_Configure_FileMacro_Open_Open("xml", MacroDirectory, "Macro files|*.xml|All files|*.*");
			return result.FileNames[0];
		}

		public void Save(INEWindowUI filesWindow, string fileName = null, bool macroDirRelative = false)
		{
			Directory.CreateDirectory(MacroDirectory);
			if (fileName == null)
			{
				var result = filesWindow.RunSaveFileDialog("Macro.xml", "xml", MacroDirectory, "Macro files|*.xml|All files|*.*");
				fileName = result.FileName;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			using (var stream = File.Create(fileName))
			{
				XMLConverter.ToXML(this).Save(stream);
				var newLine = Encoding.UTF8.GetBytes("\r\n");
				stream.Write(newLine, 0, newLine.Length);
			}
		}

		public static Macro Load(INEWindowUI filesWindow, string fileName = null, bool macroDirRelative = false)
		{
			if (fileName == null)
			{
				fileName = ChooseMacro(filesWindow);
				if (fileName == null)
					return null;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			return XMLConverter.FromXML<Macro>(XElement.Load(fileName));
		}
	}
}
