using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using NeoEdit.Common;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		delegate void GlobalKeysChangedDelegate();

		static GlobalKeysChangedDelegate globalKeysChanged;
		static bool globalKeys = true;
		public static bool GlobalKeys { get { return globalKeys; } set { globalKeys = value; globalKeysChanged?.Invoke(); } }

		ObservableCollection<ObservableCollection<string>> _keysAndValues;
		public ObservableCollection<ObservableCollection<string>> KeysAndValues
		{
			get => _keysAndValues;
			set
			{
				RemoveKeysAndValuesCallback();
				_keysAndValues = value;
				SetKeysAndValuesCallback();
			}
		}

		void SetKeysAndValuesCallback()
		{
			if (KeysAndValues == null)
				return;
			KeysAndValues.CollectionChanged += KeysAndValuesChanged;
			foreach (var coll in KeysAndValues)
				coll.CollectionChanged += KeysAndValuesChanged;
		}

		void RemoveKeysAndValuesCallback()
		{
			if (KeysAndValues == null)
				return;
			foreach (var coll in KeysAndValues)
				coll.CollectionChanged -= KeysAndValuesChanged;
			KeysAndValues.CollectionChanged -= KeysAndValuesChanged;
		}

		void KeysAndValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RemoveKeysAndValuesCallback();
			SetKeysAndValuesCallback();
			statusBarRenderTimer.Start();
		}

		static ObservableCollection<ObservableCollection<string>> staticKeysAndValues { get; set; }
		ObservableCollection<ObservableCollection<string>> localKeysAndValues { get; set; }
		Dictionary<string, int> keysHash => GlobalKeys ? staticKeysHash : localKeysHash;
		static Dictionary<string, int> staticKeysHash = new Dictionary<string, int>();
		Dictionary<string, int> localKeysHash = new Dictionary<string, int>();

		void SetupLocalKeys()
		{
			localKeysAndValues = new ObservableCollection<ObservableCollection<string>>(Enumerable.Repeat(default(ObservableCollection<string>), 10));
			for (var ctr = 0; ctr < localKeysAndValues.Count; ++ctr)
				localKeysAndValues[ctr] = new ObservableCollection<string>();

			SetupLocalOrGlobalKeys();
			globalKeysChanged += SetupLocalOrGlobalKeys;
		}

		void SetupLocalOrGlobalKeys() => KeysAndValues = GlobalKeys ? staticKeysAndValues : localKeysAndValues;

		static void SetupStaticKeys()
		{
			staticKeysAndValues = new ObservableCollection<ObservableCollection<string>>(Enumerable.Repeat(default(ObservableCollection<string>), 10));
			for (var ctr = 0; ctr < staticKeysAndValues.Count; ++ctr)
				staticKeysAndValues[ctr] = new ObservableCollection<string>();
		}

		void Command_Keys_Set(int index, bool caseSensitive = true)
		{
			GlobalKeys = TabsParent.ActiveCount == 1;
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (values.Distinct(str => caseSensitive ? str : str.ToLowerInvariant()).Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			KeysAndValues[index] = new ObservableCollection<string>(values);
			if (index == 0)
				CalculateKeysHash(caseSensitive);
		}

		void CalculateKeysHash(bool caseSensitive)
		{
			var hash = new Dictionary<string, int>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			for (var pos = 0; pos < KeysAndValues[0].Count; ++pos)
				hash[KeysAndValues[0][pos]] = pos;
			if (GlobalKeys)
				staticKeysHash = hash;
			else
				localKeysHash = hash;
		}

		void Command_Keys_Add(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			var caseSensitive = keysHash.Comparer == StringComparer.Ordinal;
			if ((index == 0) && (KeysAndValues[0].Concat(values).GroupBy(key => caseSensitive ? key : key.ToLowerInvariant()).Any(group => group.Count() > 1)))
				throw new ArgumentException("Cannot have duplicate keys");
			foreach (var value in values)
				KeysAndValues[index].Add(value);
			if (index == 0)
				CalculateKeysHash(caseSensitive);
		}

		void Command_Keys_Remove(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings().Distinct().ToList();
			foreach (var value in values)
				KeysAndValues[index].Remove(value);
		}

		void Command_Keys_Replace(int index)
		{
			// Handles keys as well as values
			if (KeysAndValues[0].Count != KeysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(KeysAndValues[index][keysHash[str]]);
			}
			ReplaceSelections(strs);
		}
	}
}
