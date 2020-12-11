using System;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.UI
{
	static class KeyExtensions
	{
		public static ModifierKeys ToModifierKeys(this Modifiers modifiers)
		{
			var modifierKeys = ModifierKeys.None;
			if (modifiers.HasFlag(Modifiers.Alt))
				modifierKeys |= ModifierKeys.Alt;
			if (modifiers.HasFlag(Modifiers.Control))
				modifierKeys |= ModifierKeys.Control;
			if (modifiers.HasFlag(Modifiers.Shift))
				modifierKeys |= ModifierKeys.Shift;
			return modifierKeys;
		}

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

		public static Key ToKey(this string str)
		{
			switch (str)
			{
				case "+": return Key.Add;
				case "-": return Key.Subtract;
				case ".": return Key.OemPeriod;
				case "/": return Key.OemQuestion;
				case "]": return Key.OemCloseBrackets;
			}

			if ((str.Length == 1) && (str[0] >= '0') && (str[0] <= '9'))
				str = $"D{str}";
			if (Enum.TryParse<Key>(str, out var key))
				return key;
			throw new Exception($"Can't interpret key: {key}");
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
	}
}
