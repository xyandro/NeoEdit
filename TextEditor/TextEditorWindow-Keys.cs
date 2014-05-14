using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using NeoEdit.GUI;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorWindow
	{
		public static RoutedCommand Command_Keys_SetKeys = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues1 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues2 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues3 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues4 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues5 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues6 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues7 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues8 = new RoutedCommand();
		public static RoutedCommand Command_Keys_SetValues9 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues1 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues2 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues3 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues4 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues5 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues6 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues7 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues8 = new RoutedCommand();
		public static RoutedCommand Command_Keys_KeysToValues9 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyKeys = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues1 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues2 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues3 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues4 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues5 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues6 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues7 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues8 = new RoutedCommand();
		public static RoutedCommand Command_Keys_CopyValues9 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsKeys = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues1 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues2 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues3 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues4 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues5 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues6 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues7 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues8 = new RoutedCommand();
		public static RoutedCommand Command_Keys_HitsValues9 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesKeys = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues1 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues2 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues3 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues4 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues5 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues6 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues7 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues8 = new RoutedCommand();
		public static RoutedCommand Command_Keys_MissesValues9 = new RoutedCommand();

		static List<string>[] keysAndValues = new List<string>[10] { new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>(), new List<string>() };

		ICommand GetKeysValuesCommand(ICommand command)
		{
			if ((command == Command_Keys_SetKeys) || (command == Command_Keys_SetValues1) || (command == Command_Keys_SetValues2) || (command == Command_Keys_SetValues3) || (command == Command_Keys_SetValues4) || (command == Command_Keys_SetValues5) || (command == Command_Keys_SetValues6) || (command == Command_Keys_SetValues7) || (command == Command_Keys_SetValues8) || (command == Command_Keys_SetValues9))
				return Command_Keys_SetValues1;
			if ((command == Command_Keys_KeysToValues1) || (command == Command_Keys_KeysToValues2) || (command == Command_Keys_KeysToValues3) || (command == Command_Keys_KeysToValues4) || (command == Command_Keys_KeysToValues5) || (command == Command_Keys_KeysToValues6) || (command == Command_Keys_KeysToValues7) || (command == Command_Keys_KeysToValues8) || (command == Command_Keys_KeysToValues9))
				return Command_Keys_KeysToValues1;
			if ((command == Command_Keys_CopyKeys) || (command == Command_Keys_CopyValues1) || (command == Command_Keys_CopyValues2) || (command == Command_Keys_CopyValues3) || (command == Command_Keys_CopyValues4) || (command == Command_Keys_CopyValues5) || (command == Command_Keys_CopyValues6) || (command == Command_Keys_CopyValues7) || (command == Command_Keys_CopyValues8) || (command == Command_Keys_CopyValues9))
				return Command_Keys_CopyValues1;
			if ((command == Command_Keys_HitsKeys) || (command == Command_Keys_HitsValues1) || (command == Command_Keys_HitsValues2) || (command == Command_Keys_HitsValues3) || (command == Command_Keys_HitsValues4) || (command == Command_Keys_HitsValues5) || (command == Command_Keys_HitsValues6) || (command == Command_Keys_HitsValues7) || (command == Command_Keys_HitsValues8) || (command == Command_Keys_HitsValues9))
				return Command_Keys_HitsValues1;
			if ((command == Command_Keys_MissesKeys) || (command == Command_Keys_MissesValues1) || (command == Command_Keys_MissesValues2) || (command == Command_Keys_MissesValues3) || (command == Command_Keys_MissesValues4) || (command == Command_Keys_MissesValues5) || (command == Command_Keys_MissesValues6) || (command == Command_Keys_MissesValues7) || (command == Command_Keys_MissesValues8) || (command == Command_Keys_MissesValues9))
				return Command_Keys_MissesValues1;

			return null;
		}

		int GetKeysValuesIndex(ICommand command)
		{
			if ((command == Command_Keys_SetKeys) || (command == Command_Keys_CopyKeys) || (command == Command_Keys_HitsKeys) || (command == Command_Keys_MissesKeys))
				return 0;
			if ((command == Command_Keys_SetValues1) || (command == Command_Keys_KeysToValues1) || (command == Command_Keys_CopyValues1) || (command == Command_Keys_HitsValues1) || (command == Command_Keys_MissesValues1))
				return 1;
			if ((command == Command_Keys_SetValues2) || (command == Command_Keys_KeysToValues2) || (command == Command_Keys_CopyValues2) || (command == Command_Keys_HitsValues2) || (command == Command_Keys_MissesValues2))
				return 2;
			if ((command == Command_Keys_SetValues3) || (command == Command_Keys_KeysToValues3) || (command == Command_Keys_CopyValues3) || (command == Command_Keys_HitsValues3) || (command == Command_Keys_MissesValues3))
				return 3;
			if ((command == Command_Keys_SetValues4) || (command == Command_Keys_KeysToValues4) || (command == Command_Keys_CopyValues4) || (command == Command_Keys_HitsValues4) || (command == Command_Keys_MissesValues4))
				return 4;
			if ((command == Command_Keys_SetValues5) || (command == Command_Keys_KeysToValues5) || (command == Command_Keys_CopyValues5) || (command == Command_Keys_HitsValues5) || (command == Command_Keys_MissesValues5))
				return 5;
			if ((command == Command_Keys_SetValues6) || (command == Command_Keys_KeysToValues6) || (command == Command_Keys_CopyValues6) || (command == Command_Keys_HitsValues6) || (command == Command_Keys_MissesValues6))
				return 6;
			if ((command == Command_Keys_SetValues7) || (command == Command_Keys_KeysToValues7) || (command == Command_Keys_CopyValues7) || (command == Command_Keys_HitsValues7) || (command == Command_Keys_MissesValues7))
				return 7;
			if ((command == Command_Keys_SetValues8) || (command == Command_Keys_KeysToValues8) || (command == Command_Keys_CopyValues8) || (command == Command_Keys_HitsValues8) || (command == Command_Keys_MissesValues8))
				return 8;
			if ((command == Command_Keys_SetValues9) || (command == Command_Keys_KeysToValues9) || (command == Command_Keys_CopyValues9) || (command == Command_Keys_HitsValues9) || (command == Command_Keys_MissesValues9))
				return 9;
			throw new Exception("Invalid keys/values command");
		}

		bool RunKeysCommand(ICommand command)
		{
			var result = true;

			if (GetKeysValuesCommand(command) == Command_Keys_SetValues1)
			{
				// Handles keys as well as values
				var index = GetKeysValuesIndex(command);
				var values = Selections.Select(range => GetString(range)).ToList();
				if ((index == 0) && (values.Distinct().Count() != values.Count))
					throw new ArgumentException("Cannot have duplicate keys.");
				keysAndValues[index] = values;
			}
			else if (GetKeysValuesCommand(command) == Command_Keys_KeysToValues1)
			{
				var index = GetKeysValuesIndex(command);
				if (keysAndValues[0].Count != keysAndValues[index].Count)
					throw new Exception("Keys and values count must match.");

				var strs = new List<string>();
				foreach (var range in Selections)
				{
					var str = GetString(range);
					var found = keysAndValues[0].IndexOf(str);
					if (found == -1)
						strs.Add(str);
					else
						strs.Add(keysAndValues[index][found]);
				}
				Replace(Selections, strs, true);
			}
			else if (GetKeysValuesCommand(command) == Command_Keys_CopyValues1)
				ClipboardWindow.Set(keysAndValues[GetKeysValuesIndex(command)].ToArray());
			else if (GetKeysValuesCommand(command) == Command_Keys_HitsValues1)
			{
				var index = GetKeysValuesIndex(command);
				Selections.Replace(Selections.Where(range => keysAndValues[index].Contains(GetString(range))).ToList());
			}
			else if (GetKeysValuesCommand(command) == Command_Keys_MissesValues1)
			{
				var index = GetKeysValuesIndex(command);
				Selections.Replace(Selections.Where(range => !keysAndValues[index].Contains(GetString(range))).ToList());
			}
			else
				result = false;

			return result;
		}
	}
}
