using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ChooseTableColumnsDialog
	{
		internal class Result
		{
			public List<int> Columns { get; set; }
		}

		[DepProp]
		public Table Table { get { return UIHelper<ChooseTableColumnsDialog>.GetPropValue<Table>(this); } set { UIHelper<ChooseTableColumnsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int SelectedColumn { get { return UIHelper<ChooseTableColumnsDialog>.GetPropValue<int>(this); } set { UIHelper<ChooseTableColumnsDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<Tuple<int, string>> Columns { get { return UIHelper<ChooseTableColumnsDialog>.GetPropValue<ObservableCollection<Tuple<int, string>>>(this); } set { UIHelper<ChooseTableColumnsDialog>.SetPropValue(this, value); } }

		static ChooseTableColumnsDialog() { UIHelper<ChooseTableColumnsDialog>.Register(); }

		ChooseTableColumnsDialog(Table input)
		{
			InitializeComponent();
			Table = input;
			Reset();
		}

		void Reset()
		{
			Columns = new ObservableCollection<Tuple<int, string>>();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Space:
					{
						var found = Columns.FirstOrDefault(tuple => tuple.Item1 == SelectedColumn);
						if (found == null)
							Columns.Add(Tuple.Create(SelectedColumn, Table.GetHeader(SelectedColumn)));
						else
							Columns.Remove(found);
					}
					break;
				case Key.Home: SelectedColumn = 0; break;
				case Key.End: SelectedColumn = Table.NumColumns - 1; break;
				case Key.Up: case Key.Down: break;
				case Key.Left: --SelectedColumn; break;
				case Key.Right: ++SelectedColumn; break;
				default: e.Handled = false; break;
			}

			if (e.Handled)
				SelectedColumn = Math.Max(0, Math.Min(SelectedColumn, Table.NumColumns - 1));
		}

		void ResetClick(object sender, RoutedEventArgs e)
		{
			Reset();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Columns = Columns.Select(column => column.Item1).ToList() };
			DialogResult = true;
		}

		static public Result Run(Window parent, Table input)
		{
			var dialog = new ChooseTableColumnsDialog(input) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
