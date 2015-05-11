using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextView.Dialogs
{
	partial class CombineDialog
	{
		internal class Result
		{
			public List<string> Files { get; set; }
			public string OutputFile { get; set; }
			public bool OpenFile { get; set; }
		}

		[DepProp]
		public ObservableCollection<string> Files { get { return UIHelper<CombineDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<CombineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputFile { get { return UIHelper<CombineDialog>.GetPropValue<string>(this); } set { UIHelper<CombineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool OpenFile { get { return UIHelper<CombineDialog>.GetPropValue<bool>(this); } set { UIHelper<CombineDialog>.SetPropValue(this, value); } }

		static CombineDialog() { UIHelper<CombineDialog>.Register(); }

		CombineDialog(bool isMerge)
		{
			InitializeComponent();
			Title = isMerge ? "Merge Files" : "Combine Files";
			Files = new ObservableCollection<string>();
			OpenFile = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (Files.Count <= 1)
				return;
			if (String.IsNullOrEmpty(OutputFile))
				return;

			if (Files.GroupBy(file => file).Any(group => group.Count() > 1))
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "Some files are in the list more than once.  Continue anyway?",
					Options = Message.OptionsEnum.YesNoCancel,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			result = new Result { Files = Files.ToList(), OutputFile = OutputFile, OpenFile = OpenFile };
			DialogResult = true;
		}

		static public Result Run(Window parent, bool isMerge)
		{
			var dialog = new CombineDialog(isMerge) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void BrowseOutputFile(object sender, RoutedEventArgs e)
		{
			var dialog = new SaveFileDialog { Filter = "Text files|*.txt|All files|*.*" };
			if (dialog.ShowDialog() == true)
				OutputFile = dialog.FileName;
		}

		void AddFiles(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "txt",
				Filter = "Text files|*.txt|All files|*.*",
				FilterIndex = 2,
				Multiselect = true,
			};
			if (dialog.ShowDialog() != true)
				return;

			foreach (var file in dialog.FileNames)
				Files.Add(file);
		}

		void AddDir(object sender, RoutedEventArgs e)
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;
			foreach (var file in Directory.GetFiles(dialog.SelectedPath))
				Files.Add(file);
		}

		void Remove(object sender, RoutedEventArgs e)
		{
			foreach (var file in files.SelectedItems)
				Files.Remove(file as string);
		}

		void MoveUp(object sender, RoutedEventArgs e)
		{
			var selectedFiles = files.SelectedItems.Cast<string>().OrderBy(file => Files.IndexOf(file)).ToList();
			foreach (var file in selectedFiles)
			{
				var index = Files.IndexOf(file);
				if ((index != 0) && (!selectedFiles.Contains(Files[index - 1])))
					Files.Move(index, index - 1);
			}
		}

		void MoveDown(object sender, RoutedEventArgs e)
		{
			var selectedFiles = files.SelectedItems.Cast<string>().OrderByDescending(file => Files.IndexOf(file)).ToList();
			foreach (var file in selectedFiles)
			{
				var index = Files.IndexOf(file);
				if ((index != Files.Count - 1) && (!selectedFiles.Contains(Files[index + 1])))
					Files.Move(index, index + 1);
			}
		}
	}
}
