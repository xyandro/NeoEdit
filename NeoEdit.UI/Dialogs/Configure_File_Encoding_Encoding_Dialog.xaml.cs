﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_File_Encoding_Encoding_Dialog
	{
		[DepProp]
		Coder.CodePage CodePage { get { return UIHelper<Configure_File_Encoding_Encoding_Dialog>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<Configure_File_Encoding_Encoding_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string DetectedStr { get { return UIHelper<Configure_File_Encoding_Encoding_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_File_Encoding_Encoding_Dialog>.SetPropValue(this, value); } }

		readonly Coder.CodePage Detected;

		static Configure_File_Encoding_Encoding_Dialog() { UIHelper<Configure_File_Encoding_Encoding_Dialog>.Register(); }

		Configure_File_Encoding_Encoding_Dialog(Coder.CodePage _CodePage, Coder.CodePage _Detected)
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

		Configuration_File_Encoding_Encoding result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_File_Encoding_Encoding { CodePage = CodePage };
			DialogResult = true;
		}

		public static Configuration_File_Encoding_Encoding Run(Window parent, Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None)
		{
			var dialog = new Configure_File_Encoding_Encoding_Dialog(codePage ?? Coder.DefaultCodePage, detected) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}

		void SetDetected(object sender, RoutedEventArgs e) => CodePage = Detected;
	}
}