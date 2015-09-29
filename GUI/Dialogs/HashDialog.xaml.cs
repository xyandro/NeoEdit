﻿using System;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	partial class HashDialog
	{
		public class Result
		{
			public Hasher.Type HashType { get; set; }
			public byte[] HMACKey { get; set; }
		}

		[DepProp]
		Hasher.Type HashType { get { return UIHelper<HashDialog>.GetPropValue<Hasher.Type>(this); } set { UIHelper<HashDialog>.SetPropValue(this, value); } }
		[DepProp]
		byte[] HMACKey { get { return UIHelper<HashDialog>.GetPropValue<byte[]>(this); } set { UIHelper<HashDialog>.SetPropValue(this, value); } }

		static HashDialog() { UIHelper<HashDialog>.Register(); }

		HashDialog()
		{
			InitializeComponent();

			hashType.ItemsSource = Enum.GetValues(typeof(Hasher.Type)).Cast<Hasher.Type>().Where(type => type != Hasher.Type.None).ToList();
		}

		Result result = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { HashType = HashType, HMACKey = HMACKey };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new HashDialog() { Owner = parent };
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}