using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	class ModelResultGrid : ItemGrid<ModelResult> { }

	partial class ModelResultsDialog
	{
		Action<long, long> changeSelection;
		Func<long, byte> getByte;
		Action<long, long, byte[]> changeBytes;

		[DepProp]
		public ObservableCollection<ModelResult> Results { get { return UIHelper<ModelResultsDialog>.GetPropValue<ObservableCollection<ModelResult>>(this); } set { UIHelper<ModelResultsDialog>.SetPropValue(this, value); } }

		static ModelResultsDialog() { UIHelper<ModelResultsDialog>.Register(); }

		ModelResultsDialog(List<ModelResult> _results, Action<long, long> _changeSelection, Func<long, byte> _getByte, Action<long, long, byte[]> _changeBytes)
		{
			InitializeComponent();

			changeSelection = _changeSelection;
			getByte = _getByte;
			changeBytes = _changeBytes;

			Results = new ObservableCollection<ModelResult>(_results);
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Num)) { SortAscending = true });
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Name)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Value)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Location)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Length)));
			results.Accept += s => ModifyValues(s, null);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public void SelectionChanged(object sender = null)
		{
			var selected = results.Selected.OrderBy(a => a.StartByte).ToList();
			var contiguous = false;
			if (selected.Count > 0)
			{
				contiguous = true;
				var start = selected.First().StartByte;
				foreach (var result in selected)
				{
					if (result.StartByte != start)
					{
						contiguous = false;
						break;
					}
					start = result.EndByte;
				}
			}

			if (contiguous)
				changeSelection(selected.First().StartByte, selected.Last().EndByte);
			else
				changeSelection(0, 0);
		}

		void ModifyValues(object sender, RoutedEventArgs e)
		{
			try
			{
				string value = "";
				var values = results.Selected.Select(result => result.Value).Distinct().ToList();
				if (values.Count == 1)
					value = values.Single();
				value = ModelGetValue.Run(value);
				if (value == null)
					return;

				foreach (var result in results.Selected)
					ModifyValue(result, value);

				SelectionChanged(); // Update highlighting in case lengths changed
			}
			finally { Activate(); }
		}

		void ModifyBitValue(ModelResult result, string value)
		{
			var bytePower = (byte)(1 << (7 - result.StartBit));
			byte orMask;
			switch (value)
			{
				case "0": orMask = 0; break;
				case "1": orMask = bytePower; break;
				default: throw new Exception("Invalid value for bit");
			}
			var byteVal = getByte(result.StartByte);
			byteVal = (byte)(byteVal & ~bytePower | orMask);
			changeBytes(result.StartByte, result.StartByte + 1, new byte[] { byteVal });
			result.Value = value;
		}

		void ModifyBasicTypeValue(ModelResult result, string value)
		{
			var bytes = Coder.StringToBytes(value, result.Action.CodePage);
			changeBytes(result.StartByte, result.EndByte, bytes);
			result.Value = value;
		}

		void ModifyStringValue(ModelResult result, string value)
		{
			var oldLen = result.EndByte - result.StartByte;
			byte[] bytes;
			switch (result.Action.StringType)
			{
				case ModelAction.ActionStringType.StringWithLength:
					{
						var str = Coder.StringToBytes(value, result.Action.Encoding);
						var len = Coder.StringToBytes(str.Length.ToString(), result.Action.CodePage);
						bytes = new byte[len.Length + str.Length];
						Array.Copy(len, bytes, len.Length);
						Array.Copy(str, 0, bytes, len.Length, str.Length);
					}
					break;
				case ModelAction.ActionStringType.StringNullTerminated:
					bytes = Coder.StringToBytes(value + "\u0000", result.Action.Encoding);
					break;
				case ModelAction.ActionStringType.StringFixedWidth:
					if (value.Length != oldLen)
						throw new Exception("Can't change length of fixed-width string");
					bytes = Coder.StringToBytes(value, result.Action.Encoding);
					break;
				default: throw new InvalidOperationException();
			}
			changeBytes(result.StartByte, result.EndByte, bytes);

			if (bytes.Length != oldLen)
			{
				var diff = bytes.Length - oldLen;
				foreach (var onResult in Results)
				{
					if (onResult.StartByte > result.StartByte)
						onResult.StartByte += diff;
					if (onResult.EndByte > result.StartByte)
						onResult.EndByte += diff;
				}
			}

			result.Value = value;
		}

		void ModifyValue(ModelResult result, string value)
		{
			switch (result.Action.Type)
			{
				case ModelAction.ActionType.Bit: ModifyBitValue(result, value); break;
				case ModelAction.ActionType.BasicType: ModifyBasicTypeValue(result, value); break;
				case ModelAction.ActionType.String: ModifyStringValue(result, value); break;
			}
		}

		public static void Run(List<ModelResult> results, Action<long, long> changeSelection, Func<long, byte> getByte, Action<long, long, byte[]> changeBytes)
		{
			new ModelResultsDialog(results, changeSelection, getByte, changeBytes).Show();
		}
	}
}
