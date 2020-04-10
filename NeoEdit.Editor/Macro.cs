using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public class Macro
	{
		readonly List<MacroAction> actions = new List<MacroAction>();

		public void AddAction(MacroAction state)
		{
			var last = actions.LastOrDefault();
			if ((state.Command == NECommand.Internal_Text) && (last?.Command == NECommand.Internal_Text))
				last.Text += state.Text;
			else
				actions.Add(state);
		}

		public readonly static string MacroDirectory = Path.Combine(Helpers.NeoEditAppData, "Macro");

		public static string ChooseMacro()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Macro files|*.xml|All files|*.*",
				InitialDirectory = MacroDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.FileName;
		}

		public void Save(string fileName = null, bool macroDirRelative = false)
		{
			Directory.CreateDirectory(MacroDirectory);
			if (fileName == null)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "xml",
					Filter = "Macro files|*.xml|All files|*.*",
					FileName = "Macro.xml",
					InitialDirectory = MacroDirectory,
				};
				if (dialog.ShowDialog() != true)
					return;

				fileName = dialog.FileName;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			XMLConverter.ToXML(this).Save(fileName);
		}

		public static Macro Load(string fileName = null, bool macroDirRelative = false)
		{
			if (fileName == null)
			{
				fileName = ChooseMacro();
				if (fileName == null)
					return null;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			return XMLConverter.FromXML<Macro>(XElement.Load(fileName));
		}

		public ExecuteState GetStep(int step) => step < actions.Count ? actions[step].GetExecuteState() : null;
	}
}
