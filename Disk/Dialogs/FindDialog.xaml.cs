using System;
using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk.Dialogs
{
	internal partial class FindDialog
	{
		internal enum SourceControlStatusEnum
		{
			None,
			IgnoredItems,
			Standard,
			Modified,
		};

		internal class Result
		{
			public Regex Regex;
			public bool FullPath;
			public bool Recursive;
			public SourceControlStatusEnum SourceControlStatus;
			public long? MinSize;
			public long? MaxSize;
			public DateTime? StartDate;
			public DateTime? EndDate;
		}

		[DepProp]
		public string Expression { get { return UIHelper<FindDialog>.GetPropValue(() => this.Expression); } private set { UIHelper<FindDialog>.SetPropValue(() => this.Expression, value); } }
		[DepProp]
		public bool IsRegEx { get { return UIHelper<FindDialog>.GetPropValue(() => this.IsRegEx); } private set { UIHelper<FindDialog>.SetPropValue(() => this.IsRegEx, value); } }
		[DepProp]
		public bool FullPath { get { return UIHelper<FindDialog>.GetPropValue(() => this.FullPath); } private set { UIHelper<FindDialog>.SetPropValue(() => this.FullPath, value); } }
		[DepProp]
		public bool Recursive { get { return UIHelper<FindDialog>.GetPropValue(() => this.Recursive); } private set { UIHelper<FindDialog>.SetPropValue(() => this.Recursive, value); } }
		[DepProp]
		public SourceControlStatusEnum SourceControlStatus { get { return UIHelper<FindDialog>.GetPropValue(() => this.SourceControlStatus); } private set { UIHelper<FindDialog>.SetPropValue(() => this.SourceControlStatus, value); } }
		[DepProp]
		public long? MinSize { get { return UIHelper<FindDialog>.GetPropValue(() => this.MinSize); } private set { UIHelper<FindDialog>.SetPropValue(() => this.MinSize, value); } }
		[DepProp]
		public long? MaxSize { get { return UIHelper<FindDialog>.GetPropValue(() => this.MaxSize); } private set { UIHelper<FindDialog>.SetPropValue(() => this.MaxSize, value); } }
		[DepProp]
		public DateTime? StartDate { get { return UIHelper<FindDialog>.GetPropValue(() => this.StartDate); } private set { UIHelper<FindDialog>.SetPropValue(() => this.StartDate, value); } }
		[DepProp]
		public DateTime? EndDate { get { return UIHelper<FindDialog>.GetPropValue(() => this.EndDate); } private set { UIHelper<FindDialog>.SetPropValue(() => this.EndDate, value); } }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		FindDialog()
		{
			InitializeComponent();
			Expression = "*.*";
			expression.SelectAll();

			foreach (var status in Helpers.GetValues<SourceControlStatusEnum>())
				sourceControlStatus.Items.Add(status);
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
				SourceControlStatus = SourceControlStatus,
				MinSize = MinSize,
				MaxSize = MaxSize,
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
