using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Common.Models;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class File_Select_Choose_Dialog : EnhancedWindow
	{
		readonly WindowActiveFilesDialogData data;

		static File_Select_Choose_Dialog()
		{
			UIHelper<File_Select_Choose_Dialog>.Register();
		}

		File_Select_Choose_Dialog(WindowActiveFilesDialogData data)
		{
			this.data = data;
			InitializeComponent();
			SyncData();
		}

		void SyncData()
		{
			var newdata = data.AllFiles.Select((str, index) => Tuple.Create(str, index)).ToList();

			files.SelectionChanged -= OnFilesSelectionChanged;
			files.UnselectAll();
			files.ItemsSource = newdata;
			data.ActiveIndexes.ForEach(index => files.SelectedItems.Add(newdata[index]));
			data.FocusedIndex = data.FocusedIndex;
			files.SelectionChanged += OnFilesSelectionChanged;
		}

		List<int> SelectedIndexes() => files.SelectedItems.OfType<Tuple<string, int>>().Select(pair => pair.Item2).ToList();

		void OnFilesSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			data.SetActiveIndexes(SelectedIndexes());
			SyncData();
		}

		void OnFileMoveUpClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != index).ToList();
			var moves = new List<(int, int)>();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				moves.Add((indexes[ctr], indexes[ctr] - 1));
			data.DoMoves(moves);
			SyncData();
		}

		void OnFileMoveDownClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			indexes = indexes.Where((value, index) => value != data.AllFiles.Count - indexes.Count + index).ToList();
			var moves = new List<(int, int)>();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				moves.Add((indexes[ctr], indexes[ctr] + 1));
			data.DoMoves(moves);
			SyncData();
		}

		void OnFileMoveToTopClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			var moves = new List<(int, int)>();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				moves.Add((indexes[ctr], ctr));
			data.DoMoves(moves);
			SyncData();
		}

		void OnFileMoveToBottomClick(object sender, RoutedEventArgs e)
		{
			var indexes = SelectedIndexes();
			var moves = new List<(int, int)>();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				moves.Add((indexes[ctr], data.AllFiles.Count - indexes.Count + ctr));
			data.DoMoves(moves);
			SyncData();
		}

		void OnFileCloseClick(object sender, RoutedEventArgs e)
		{
			data.CloseFiles(SelectedIndexes());
			SyncData();
		}

		void OnOKClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static void Run(Window window, WindowActiveFilesDialogData data)
		{
			if (!new File_Select_Choose_Dialog(data) { Owner = window }.ShowDialog())
				throw new OperationCanceledException();
		}
	}
}
