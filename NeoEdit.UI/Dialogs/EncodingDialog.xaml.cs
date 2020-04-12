﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class EncodingDialog
	{
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<EncodingDialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }
		[DepProp]
		string DetectedStr { get { return UIHelper<EncodingDialog>.GetPropValue<string>(this); } set { UIHelper<EncodingDialog>.SetPropValue(this, value); } }

		readonly Coder.CodePage Detected;

		static EncodingDialog() { UIHelper<EncodingDialog>.Register(); }

		EncodingDialog(Coder.CodePage _CodePage, Coder.CodePage _Detected)
		{
			InitializeComponent();

			codePage.ItemsSource = Coder.GetAllCodePages().ToDictionary(page => page, page => Coder.GetDescription(page));
			codePage.SelectedValuePath = "Key";
			codePage.DisplayMemberPath = "Value";

			CodePage = _CodePage;
			if (_Detected == Coder.CodePage.None)
				content.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == Grid.GetRow(setDetected)).ToList().ForEach(child => child.Visibility = Visibility.Collapsed);
			else
			{
				Detected = _Detected;
				DetectedStr = Coder.GetDescription(Detected);
			}
		}

		EncodingDialogResult result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EncodingDialogResult { CodePage = CodePage };
			DialogResult = true;
		}

		public static EncodingDialogResult Run(Window parent, Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None)
		{
			var dialog = new EncodingDialog(codePage ?? Coder.DefaultCodePage, detected) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}

		void SetDetected(object sender, RoutedEventArgs e) => CodePage = Detected;
	}
}