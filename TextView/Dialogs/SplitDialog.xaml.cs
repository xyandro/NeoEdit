﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextView.Dialogs
{
	partial class SplitDialog
	{
		internal class Result
		{
			public List<Tuple<string, long, long>> SplitData { get; set; }
		}

		public enum SizeTypeEnum
		{
			GB,
			MB,
			KB,
			Bytes,
		}

		[DepProp]
		public long Size { get { return UIHelper<SplitDialog>.GetPropValue<long>(this); } set { UIHelper<SplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public SizeTypeEnum SizeType { get { return UIHelper<SplitDialog>.GetPropValue<SizeTypeEnum>(this); } set { UIHelper<SplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumFiles { get { return UIHelper<SplitDialog>.GetPropValue<int>(this); } set { UIHelper<SplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputDir { get { return UIHelper<SplitDialog>.GetPropValue<string>(this); } set { UIHelper<SplitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string OutputTemplate { get { return UIHelper<SplitDialog>.GetPropValue<string>(this); } set { UIHelper<SplitDialog>.SetPropValue(this, value); } }

		static SplitDialog()
		{
			UIHelper<SplitDialog>.Register();
			UIHelper<SplitDialog>.AddCallback(a => a.Size, (obj, o, n) => obj.calcNumFiles.Start());
			UIHelper<SplitDialog>.AddCallback(a => a.SizeType, (obj, o, n) => obj.calcNumFiles.Start());
			UIHelper<SplitDialog>.AddCallback(a => a.NumFiles, (obj, o, n) => obj.calcSize.Start());
		}

		readonly TextData data;
		readonly RunOnceTimer calcNumFiles, calcSize;
		SplitDialog(TextData data)
		{
			InitializeComponent();
			calcNumFiles = new RunOnceTimer(() => CalculateNumFiles());
			calcSize = new RunOnceTimer(() => CalculateSize());
			this.data = data;
			OutputDir = Path.GetDirectoryName(data.FileName);
			OutputTemplate = Path.GetFileNameWithoutExtension(data.FileName) + " - {0}" + Path.GetExtension(data.FileName);

			foreach (var value in Helpers.GetValues<SizeTypeEnum>())
				sizeType.Items.Add(value);

			Size = 50;
			SizeType = SizeTypeEnum.MB;
		}

		long GetSize()
		{
			var size = Size;
			switch (SizeType)
			{
				case SizeTypeEnum.KB: size <<= 10; break;
				case SizeTypeEnum.MB: size <<= 20; break;
				case SizeTypeEnum.GB: size <<= 30; break;
			}
			return size;
		}

		void CalculateNumFiles()
		{
			NumFiles = data.CalculateSplit(GetSize());
			calcSize.Stop();
		}

		void CalculateSize()
		{
			if (NumFiles <= 0)
				return;

			long min = 1, max = data.Size;
			while (min != max)
			{
				var mid = (min + max) / 2;
				var count = data.CalculateSplit(mid);
				if (count > NumFiles)
					min = mid + 1;
				else
					max = mid;
			}
			Size = min;
			SizeType = SizeTypeEnum.Bytes;
			calcNumFiles.Stop();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			var format = Path.Combine(OutputDir, OutputTemplate);
			var splitData = data.CalculateSplit(format, GetSize());

			var existing = splitData.Select(tuple => tuple.Item1).Where(file => File.Exists(file));
			if (existing.Any())
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "The following files already exist:\n\n" + String.Join("\n", existing) + "\n\nAre you sure you want to overwrite them?",
					Options = Message.OptionsEnum.YesNoCancel,
					DefaultAccept = Message.OptionsEnum.No,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}
			result = new Result { SplitData = splitData };
			if (result.SplitData.Count <= 1)
				return;
			DialogResult = true;
		}

		static public Result Run(Window parent, TextData data)
		{
			var dialog = new SplitDialog(data) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void BrowseOutputDir(object sender, RoutedEventArgs e)
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = OutputDir };
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				OutputDir = dialog.SelectedPath;
		}
	}
}
