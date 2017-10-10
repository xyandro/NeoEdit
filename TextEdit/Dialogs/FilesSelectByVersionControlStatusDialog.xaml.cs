using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesSelectByVersionControlStatusDialog
	{
		internal class Result
		{
			public HashSet<VCS.VCSStatus> Statuses { get; } = new HashSet<VCS.VCSStatus>();
		}

		[DepProp]
		public bool Unknown { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ignored { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Modified { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Normal { get { return UIHelper<FilesSelectByVersionControlStatusDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesSelectByVersionControlStatusDialog>.SetPropValue(this, value); } }

		static FilesSelectByVersionControlStatusDialog() { UIHelper<FilesSelectByVersionControlStatusDialog>.Register(); }

		FilesSelectByVersionControlStatusDialog()
		{
			InitializeComponent();
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result();
			if (Unknown)
				result.Statuses.Add(VCS.VCSStatus.Unknown);
			if (Ignored)
				result.Statuses.Add(VCS.VCSStatus.Ignored);
			if (Modified)
				result.Statuses.Add(VCS.VCSStatus.Modified);
			if (Normal)
				result.Statuses.Add(VCS.VCSStatus.Normal);
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new FilesSelectByVersionControlStatusDialog() { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
