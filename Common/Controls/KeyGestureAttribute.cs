using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NeoEdit.Common.Controls
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class KeyGestureAttribute : Attribute
	{
		public Key Key { get; }
		public ModifierKeys Modifiers { get; }
		public int Order { get; }
		public string GestureText { get; }

		public KeyGestureAttribute(Key key, ModifierKeys modifiers = ModifierKeys.None, int order = 1)
		{
			Key = key;
			Modifiers = modifiers;
			Order = order;
			var mods = new List<string>();
			if ((modifiers & ModifierKeys.Control) != 0)
				mods.Add("Ctrl");
			if ((modifiers & ModifierKeys.Windows) != 0)
				mods.Add("Win");
			if ((modifiers & ModifierKeys.Alt) != 0)
				mods.Add("Alt");
			if ((modifiers & ModifierKeys.Shift) != 0)
				mods.Add("Shift");
			switch (key)
			{
				case Key.D0:
				case Key.D1:
				case Key.D2:
				case Key.D3:
				case Key.D4:
				case Key.D5:
				case Key.D6:
				case Key.D7:
				case Key.D8:
				case Key.D9: mods.Add(key.ToString().Substring(1)); break;
				case Key.OemPlus: mods.Add("+"); break;
				case Key.OemMinus: mods.Add("-"); break;
				case Key.OemPeriod: mods.Add("."); break;
				case Key.OemOpenBrackets: mods.Add("["); break;
				case Key.OemCloseBrackets: mods.Add("]"); break;
				case Key.OemQuestion: mods.Add("/"); break;
				default: mods.Add(key.ToString()); break;
			}
			GestureText = string.Join("+", mods);
		}
	}
}
