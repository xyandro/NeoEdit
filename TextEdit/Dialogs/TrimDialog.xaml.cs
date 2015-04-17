﻿using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class TrimDialog
	{
		public enum TrimLocation
		{
			Start,
			Both,
			End,
		}

		internal class Result
		{
			public char[] TrimChars { get; set; }
			public TrimLocation Location { get; set; }
		}

		[DepProp]
		public string TrimChars { get { return UIHelper<TrimDialog>.GetPropValue(() => this.TrimChars); } set { UIHelper<TrimDialog>.SetPropValue(() => this.TrimChars, value); } }
		[DepProp]
		public TrimLocation Location { get { return UIHelper<TrimDialog>.GetPropValue(() => this.Location); } set { UIHelper<TrimDialog>.SetPropValue(() => this.Location, value); } }

		static TrimDialog()
		{
			UIHelper<TrimDialog>.Register();
		}

		TrimDialog(bool numeric)
		{
			InitializeComponent();

			if (numeric)
				NumericClick(null, null);
			else
				StringClick(null, null);
		}

		void NumericClick(object sender, RoutedEventArgs e)
		{
			TrimChars = "0";
			Location = TrimLocation.Start;
		}

		void StringClick(object sender, RoutedEventArgs e)
		{
			TrimChars = @" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";
			Location = TrimLocation.Both;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { TrimChars = Regex.Unescape(TrimChars).ToCharArray(), Location = Location };
			DialogResult = true;
		}

		public static Result Run(bool numeric)
		{
			var dialog = new TrimDialog(numeric);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
