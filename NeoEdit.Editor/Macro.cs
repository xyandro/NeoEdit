using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public class Macro
	{
		readonly List<MacroAction> actions = new List<MacroAction>();
		public IReadOnlyList<MacroAction> Actions => actions;

		public void AddAction(MacroAction state)
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
			if (result == null)
				return null;
			return result.FileNames[0];
		}

		public void Save(INEWindowUI filesWindow, string fileName = null, bool macroDirRelative = false)
		{
			Directory.CreateDirectory(MacroDirectory);
			if (fileName == null)
			{
				var result = filesWindow.RunSaveFileDialog("Macro.xml", "xml", MacroDirectory, "Macro files|*.xml|All files|*.*");
				if (result == null)
					return;

				fileName = result.FileName;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			XMLConverter.ToXML(this).Save(fileName);
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
