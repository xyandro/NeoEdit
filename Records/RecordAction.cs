using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NeoEdit.Records
{
	public class RecordAction
	{
		public enum ActionName
		{
			Rename,
			Delete,
			Copy,
			Cut,
			Paste,
			MD5,
			Identify,
			SyncSource,
			SyncTarget,
			Sync,
			Open,
		};

		public ActionName Name { get; private set; }
		public string MenuHeader { get; private set; }
		public int MinChildren { get; private set; }
		public int MaxChildren { get; private set; }
		public bool ClipboardHasRecords { get; private set; }
		public bool ParentAction { get; private set; }
		public Key AccessKey { get; private set; }
		public ModifierKeys AccessModifiers { get; private set; }

		RecordAction()
		{
			MinChildren = 1;
			MaxChildren = Int32.MaxValue;
		}

		static List<RecordAction> actions = new List<RecordAction>
		{
			new RecordAction { Name = ActionName.Rename, MenuHeader = "_Rename", MaxChildren = 1, AccessKey = Key.F2 },
			new RecordAction { Name = ActionName.Delete, MenuHeader = "_Delete", AccessKey = Key.Delete },
			new RecordAction { Name = ActionName.Copy, MenuHeader = "_Copy", AccessKey = Key.C, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.Cut, MenuHeader = "C_ut", AccessKey = Key.X, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.Paste, MenuHeader = "_Paste", MinChildren = 0, ParentAction = true, ClipboardHasRecords = true, AccessKey = Key.V, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.MD5, MenuHeader = "_MD5", AccessKey = Key.M, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.Identify, MenuHeader = "_Identify", AccessKey = Key.I, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.SyncSource, MenuHeader = "S_ync Source", MaxChildren = 1, AccessKey = Key.S, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.SyncTarget, MenuHeader = "Sync T_arget", MaxChildren = 1, AccessKey = Key.T, AccessModifiers = ModifierKeys.Control },
			new RecordAction { Name = ActionName.Sync, MenuHeader = "_Sync", MinChildren = 0, AccessKey = Key.F3 },
			new RecordAction { Name = ActionName.Open, MenuHeader = "_Open", MaxChildren = 1, AccessKey = Key.Return, AccessModifiers = ModifierKeys.Control },
		};

		public static RecordAction Get(ActionName name)
		{
			return actions.Single(a => a.Name == name);
		}

		public static ActionName ActionFromMenuHeader(string str)
		{
			return actions.Single(a => a.MenuHeader == str).Name;
		}

		public static ActionName? ActionFromAccessKey(Key key, ModifierKeys modifierKeys)
		{
			var action = actions.SingleOrDefault(a => (a.AccessKey == key) && ((a.AccessModifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift)) == modifierKeys));
			if (action == null)
				return null;
			return action.Name;
		}

		public static IEnumerable<ActionName> Actions(Record parent, IEnumerable<Record> children, int clipboardCount)
		{
			var parentActions = parent.Actions.Where(a => Get(a).ParentAction).ToList();
			var childActions = children.SelectMany(a => a.Actions).Where(a => !Get(a).ParentAction).ToList();
			var actions = parentActions.Concat(childActions).ToList();
			var actionCounts = actions.GroupBy(a => a).ToDictionary(a => a.Key, a => a.Count());
			actions = Helpers.GetValues<ActionName>().Where(a => actionCounts.ContainsKey(a)).Where(a => Get(a).IsValid(actionCounts[a], clipboardCount > 0)).ToList();
			return actions;
		}

		public bool IsValid(int numChildren, bool clipboardHasRecords)
		{
			if (ParentAction)
			{
				if ((ClipboardHasRecords) && (!clipboardHasRecords))
					return false;
			}
			else
			{
				if ((numChildren < MinChildren) || (numChildren > MaxChildren))
					return false;
			}

			return true;
		}

		public string GetInputGestureText()
		{
			if (AccessKey == Key.None)
				return "";

			var modifier = "";
			if ((AccessModifiers & ModifierKeys.Control) != 0)
				modifier += "Ctrl-";
			if ((AccessModifiers & ModifierKeys.Alt) != 0)
				modifier += "Alt-";
			if ((AccessModifiers & ModifierKeys.Shift) != 0)
				modifier += "Shift-";
			return modifier + AccessKey.ToString();
		}
	}
}
