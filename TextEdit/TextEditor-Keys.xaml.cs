using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit
{
	partial class TextEditor
	{
		delegate void GlobalKeysChangedDelegate();
		static GlobalKeysChangedDelegate globalKeysChanged;
		static bool globalKeys = true;
		public static bool GlobalKeys { get { return globalKeys; } set { globalKeys = value; globalKeysChanged?.Invoke(); } }

		[DepProp]
		public ObservableCollection<ObservableCollection<string>> KeysAndValues { get { return UIHelper<TextEditor>.GetPropValue<ObservableCollection<ObservableCollection<string>>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		static ObservableCollection<ObservableCollection<string>> staticKeysAndValues { get; set; }
		ObservableCollection<ObservableCollection<string>> localKeysAndValues { get; set; }
		Dictionary<string, int> keysHash => GlobalKeys ? staticKeysHash : localKeysHash;
		static Dictionary<string, int> staticKeysHash = new Dictionary<string, int>();
		Dictionary<string, int> localKeysHash = new Dictionary<string, int>();

		static void SetupStaticKeys()
		{
			staticKeysAndValues = new ObservableCollection<ObservableCollection<string>> { null, null, null, null, null, null, null, null, null, null };
			staticKeysAndValues.CollectionChanged += (s, e) => keysAndValues_CollectionChanged(staticKeysAndValues, staticKeysHash, e);
			for (var ctr = 0; ctr < staticKeysAndValues.Count; ++ctr)
				staticKeysAndValues[ctr] = new ObservableCollection<string>();
		}

		void SetupLocalKeys()
		{
			localKeysAndValues = new ObservableCollection<ObservableCollection<string>> { null, null, null, null, null, null, null, null, null, null };
			localKeysAndValues.CollectionChanged += (s, e) => keysAndValues_CollectionChanged(localKeysAndValues, localKeysHash, e);
			for (var ctr = 0; ctr < localKeysAndValues.Count; ++ctr)
				localKeysAndValues[ctr] = new ObservableCollection<string>();

			SetupLocalOrGlobalKeys();
			globalKeysChanged += SetupLocalOrGlobalKeys;
		}

		void SetupLocalOrGlobalKeys() => KeysAndValues = GlobalKeys ? staticKeysAndValues : localKeysAndValues;

		static void keysAndValues_CollectionChanged(ObservableCollection<ObservableCollection<string>> data, Dictionary<string, int> hash, NotifyCollectionChangedEventArgs e)
		{
			if ((e.Action != NotifyCollectionChangedAction.Replace) || (e.NewStartingIndex != 0))
				return;

			hash.Clear();
			for (var pos = 0; pos < data[0].Count; ++pos)
				hash[data[0][pos]] = pos;
		}

		internal void Command_Keys_Set(int index)
		{
			GlobalKeys = TabsParent.ActiveCount == 1;
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (values.Distinct().Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			KeysAndValues[index] = new ObservableCollection<string>(values);
		}

		internal void Command_Keys_Add(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (KeysAndValues[0].Concat(values).GroupBy(key => key).Any(group => group.Count() > 1)))
				throw new ArgumentException("Cannot have duplicate keys");
			foreach (var value in values)
				KeysAndValues[index].Add(value);
		}

		internal void Command_Keys_Replace(int index)
		{
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
