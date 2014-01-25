using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records
{
	public class RecordAction
	{
		public enum ActionName
		{
			Rename,
		};

		public ActionName Name { get; private set; }
		public string DisplayName { get; private set; }
		public string MenuHeader { get; private set; }
		public int MinArgs { get; private set; }
		public int MaxArgs { get; private set; }

		static List<RecordAction> actions = new List<RecordAction>
		{
			new RecordAction { Name = ActionName.Rename, DisplayName = "Rename", MenuHeader = "_Rename", MinArgs = 1, MaxArgs = 1 },
		};

		public static RecordAction Get(ActionName name)
		{
			return actions.Single(a => a.Name == name);
		}

		public static ActionName ActionFromMenuHeader(string str)
		{
			return actions.Single(a => a.MenuHeader == str).Name;
		}

		public bool ValidNumArgs(int numArgs)
		{
			return (numArgs >= MinArgs) && (numArgs <= MaxArgs);
		}
	}
}
