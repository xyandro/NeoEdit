using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NeoEdit.GUI.Misc
{
	public class KeySet : List<Tuple<ModifierKeys, Key, Action>>
	{
		Action GetAction(ModifierKeys modifiers, Key key, Key systemKey)
		{
			if (key == Key.System)
				key = systemKey;
			return GetAction(modifiers, key);
		}

		Action GetAction(ModifierKeys modifiers, Key key)
		{
			var foundKey = this.SingleOrDefault(tuple => (tuple.Item1 == modifiers) && (tuple.Item2 == key));
			if (foundKey == null)
				return null;
			return foundKey.Item3;
		}

		public void Add(Key key, Action action) => Add(ModifierKeys.None, key, action);

		public void Add(ModifierKeys modifiers, Key key, Action action)
		{
			if (GetAction(modifiers, key) != null)
				throw new ArgumentException($"Duplicate key: {modifiers}+{key}");
			this.Add(Tuple.Create(modifiers, key, action));
		}

		public bool Run(ModifierKeys modifiers, Key key, Key systemKey)
		{
			var action = GetAction(modifiers, key, systemKey);
			if (action == null)
				return false;
			action();
			return true;
		}

		public bool Run(KeyEventArgs e) => Run(Keyboard.Modifiers, e.Key, e.SystemKey);
	}
}
