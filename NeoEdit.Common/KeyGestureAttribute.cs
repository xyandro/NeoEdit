using System;
using System.Collections.Generic;

namespace NeoEdit.Common
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class KeyGestureAttribute : Attribute
	{
		public string Key { get; }
		public Modifiers Modifiers { get; }
		public int Order { get; }
		public string GestureText { get; }

		public KeyGestureAttribute(string key, Modifiers modifiers = Modifiers.None, int order = 1)
		{
			Key = key;
			Modifiers = modifiers;
			Order = order;
			var mods = new List<string>();
			if (modifiers.HasFlag(Modifiers.Control))
				mods.Add("Ctrl");
			if (modifiers.HasFlag(Modifiers.Alt))
				mods.Add("Alt");
			if (modifiers.HasFlag(Modifiers.Shift))
				mods.Add("Shift");
			mods.Add(Key);
			GestureText = string.Join("+", mods);
		}
	}
}
