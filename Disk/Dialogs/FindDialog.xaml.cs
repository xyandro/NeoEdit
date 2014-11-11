using NeoEdit.GUI.Common;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace NeoEdit.Disk.Dialogs
{
	internal partial class FindDialog
	{
		internal class Result
		{
			public Regex Regex;
			public bool FullPath;
			public bool Recursive;
			public DateTime? StartDate;
			public DateTime? EndDate;
		}

		[DepProp]
		public string Expression { get { return UIHelper<FindDialog>.GetPropValue<string>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegEx { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool FullPath { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Recursive { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DateTime? StartDate { get { return UIHelper<FindDialog>.GetPropValue<DateTime?>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public DateTime? EndDate { get { return UIHelper<FindDialog>.GetPropValue<DateTime?>(this); } private set { UIHelper<FindDialog>.SetPropValue(this, value); } }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		FindDialog()
		{
			InitializeComponent();
			Expression = "*.*";
			expression.SelectAll();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			Regex regex = null;
			if ((!String.IsNullOrEmpty(Expression)) && (Expression != "*.*"))
			{
				var expr = Expression;
				if (!IsRegEx)
					expr = "^(" + Regex.Escape(expr).Replace(@"\*", @".*").Replace(@"\?", ".?").Replace(";", "|") + ")$";
				regex = new Regex(expr, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			}

			result = new Result
			{
				Regex = regex,
				FullPath = FullPath,
				Recursive = Recursive,
				StartDate = StartDate,
				EndDate = EndDate,
			};

			DialogResult = true;
		}

		static public Result Run()
		{
			var dialog = new FindDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
