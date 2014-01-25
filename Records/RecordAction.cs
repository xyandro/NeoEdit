﻿using System;
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
		};

		public ActionName Name { get; private set; }
		public string DisplayName { get; private set; }
		public string MenuHeader { get; private set; }
		public int MinArgs { get; private set; }
		public int MaxArgs { get; private set; }
		public Key AccessKey { get; private set; }
		public ModifierKeys AccessModifiers { get; private set; }

		static List<RecordAction> actions = new List<RecordAction>
		{
			new RecordAction { Name = ActionName.Rename, DisplayName = "Rename", MenuHeader = "_Rename", MinArgs = 1, MaxArgs = 1, AccessKey = Key.F2 },
			new RecordAction { Name = ActionName.Delete, DisplayName = "Delete", MenuHeader = "_Delete", MinArgs = 1, MaxArgs = Int32.MaxValue, AccessKey = Key.Delete },
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

		public bool ValidNumArgs(int numArgs)
		{
			return (numArgs >= MinArgs) && (numArgs <= MaxArgs);
		}
	}
}
