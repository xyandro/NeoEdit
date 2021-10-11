using System.Collections.Generic;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	static class KeyExtensions
	{
		public static Modifiers ToModifiers(this ModifierKeys modifierKeys)
		{
			var modifiers = Modifiers.None;
			if (modifierKeys.HasFlag(ModifierKeys.Alt))
				modifiers |= Modifiers.Alt;
			if (modifierKeys.HasFlag(ModifierKeys.Control))
				modifiers |= Modifiers.Control;
			if (modifierKeys.HasFlag(ModifierKeys.Shift))
				modifiers |= Modifiers.Shift;
			return modifiers;
		}

		public static string FromKey(this Key key)
		{
			switch (key)
			{
				case Key.Add: case Key.OemPlus: return "+";
				case Key.Subtract: case Key.OemMinus: return "-";
				case Key.OemPeriod: return ".";
				case Key.OemQuestion: return "/";
				case Key.OemCloseBrackets: return "]";
				case Key.Return: return "Enter";
				case Key.Capital: return "CapsLock";
				case Key.Prior: return "PageUp";
				case Key.Next: return "PageDown";
				case Key.Snapshot: return "PrintScreen";
			}

			var str = key.ToString();
			if ((str.Length == 2) && (str[0] == 'D') && (str[1] >= '0') && (str[1] <= '9'))
				return str.Substring(1);
			return str;
		}

		public static string ToText(this KeyGesture keyGesture)
		{
			var parts = new List<string>();
			if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
				parts.Add("Ctrl");
			if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
				parts.Add("Alt");
			if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift))
				parts.Add("Shift");
			parts.Add(keyGesture.Key.FromKey());
			return string.Join("+", parts);
		}
	}
}
