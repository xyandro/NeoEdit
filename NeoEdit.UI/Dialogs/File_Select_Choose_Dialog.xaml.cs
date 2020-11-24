using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class File_Select_Choose_Dialog : EnhancedWindow
	{
		[DepProp]
		ObservableCollection<INEFile> NEFiles { get { return UIHelper<File_Select_Choose_Dialog>.GetPropValue<ObservableCollection<INEFile>>(this); } set { UIHelper<File_Select_Choose_Dialog>.SetPropValue(this, value); } }

		readonly Func<IEnumerable<INEFile>, bool> canClose;
		readonly Action<IEnumerable<INEFile>, IEnumerable<INEFile>, INEFile> updateFiles;

		static File_Select_Choose_Dialog() => UIHelper<File_Select_Choose_Dialog>.Register();

		File_Select_Choose_Dialog(IEnumerable<INEFile> neFiles, IEnumerable<INEFile> activeFiles, INEFile focused, Func<IEnumerable<INEFile>, bool> canClose, Action<IEnumerable<INEFile>, IEnumerable<INEFile>, INEFile> updateFiles)
		{
			this.canClose = canClose;
			this.updateFiles = updateFiles;

			InitializeComponent();
			NEFiles = new ObservableCollection<INEFile>(neFiles);

			NEFiles.CollectionChanged += (s, e) => SetUpdateNEWindow();
			Loaded += (s, e) =>
			{
				activeFiles.ForEach(files.SelectedItems.Add);
				files.SetFocus(focused);
			};
		}

		DispatcherTimer setUpdateNEWindowTimer;
		void ClearUpdateNEWindow()
		{
			if (setUpdateNEWindowTimer != null)
			{
				setUpdateNEWindowTimer.Stop();
				setUpdateNEWindowTimer = null;
			}
		}

		void SetUpdateNEWindow()
		{
			if (setUpdateNEWindowTimer != null)
				return;

			setUpdateNEWindowTimer = new DispatcherTimer();
			setUpdateNEWindowTimer.Tick += (s, e) =>
			{
				ClearUpdateNEWindow();
				UpdateNEWindow();
			};
			setUpdateNEWindowTimer.Start();
		}

		void UpdateNEWindow() => updateFiles(files.ItemsSource.Cast<INEFile>(), files.SelectedItems.Cast<INEFile>(), files.Focused as INEFile);

		void OnFilesSelectionChanged(object sender, SelectionChangedEventArgs e) => SetUpdateNEWindow();

		void OnFilesFocusedChanged(object sender, EventArgs e) => SetUpdateNEWindow();

		IReadOnlyList<int> GetSelectedIndexes()
		{
			var selected = new HashSet<INEFile>(files.SelectedItems.OfType<INEFile>());
			return NEFiles.Indexes(neFile => selected.Contains(neFile)).ToList();
		}

		void OnFileMoveUpClick(object sender, RoutedEventArgs e)
		{
			var focused = files.Focused as INEFile;
			var indexes = GetSelectedIndexes();
			indexes = indexes.Where((value, index) => value != index).ToList();
			foreach (var index in indexes)
				NEFiles.Move(index, index - 1);
			files.SetFocus(focused);
		}

		void OnFileMoveDownClick(object sender, RoutedEventArgs e)
		{
			var focused = files.Focused as INEFile;
			var indexes = GetSelectedIndexes();
			indexes = indexes.Where((value, index) => value != NEFiles.Count - indexes.Count + index).ToList();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				NEFiles.Move(indexes[ctr], indexes[ctr] + 1);
			files.SetFocus(focused);
		}

		void OnFileMoveToTopClick(object sender, RoutedEventArgs e)
		{
			var focused = files.Focused as INEFile;
			var indexes = GetSelectedIndexes();
			for (var ctr = 0; ctr < indexes.Count; ++ctr)
				NEFiles.Move(indexes[ctr], ctr);
			files.SetFocus(focused);
		}

		void OnFileMoveToBottomClick(object sender, RoutedEventArgs e)
		{
			var focused = files.Focused as INEFile;
			var indexes = GetSelectedIndexes();
			for (var ctr = indexes.Count - 1; ctr >= 0; --ctr)
				NEFiles.Move(indexes[ctr], NEFiles.Count - indexes.Count + ctr);
			files.SetFocus(focused);
		}

		void OnFileCloseClick(object sender, RoutedEventArgs e)
		{
			var focused = files.Focused as INEFile;
			var selected = files.SelectedItems.Cast<INEFile>().ToList();
			if (!selected.Any())
				return;

			Opacity = 0;
			try
			{
				if (!canClose(selected))
					return;
			}
			finally
			{
				Opacity = 1;
			}

			selected.ForEach(item => NEFiles.Remove(item));
			files.SetFocus(focused);
		}

		void OnOKClick(object sender, RoutedEventArgs e) => DialogResult = true;

		public static void Run(Window window, IEnumerable<INEFile> neFiles, IEnumerable<INEFile> activeFiles, INEFile focused, Func<IEnumerable<INEFile>, bool> canClose, Action<IEnumerable<INEFile>, IEnumerable<INEFile>, INEFile> updateFiles)
		{
			var dialog = new File_Select_Choose_Dialog(neFiles, activeFiles, focused, canClose, updateFiles) { Owner = window };
			var result = dialog.ShowDialog();
			dialog.ClearUpdateNEWindow();
			if (!result)
				throw new OperationCanceledException();
		}
	}
}
