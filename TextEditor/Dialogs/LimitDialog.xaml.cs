using System.Windows;
using System.Xml.Linq;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class LimitDialog
	{
		internal class Result : DialogResult
		{
			public int SelMult { get; set; }
			public bool IgnoreBlank { get; set; }
			public int NumSels { get; set; }

			public override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Attribute(a => a.SelMult),
					neXml.Attribute(a => a.IgnoreBlank),
					neXml.Attribute(a => a.NumSels)
				);
			}

			public static Result FromXML(XElement xml)
			{
				return new Result
				{
					SelMult = NEXML<Result>.Attribute(xml, a => a.SelMult),
					IgnoreBlank = NEXML<Result>.Attribute(xml, a => a.IgnoreBlank),
					NumSels = NEXML<Result>.Attribute(xml, a => a.NumSels)
				};
			}
		}

		[DepProp]
		public int SelMult { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IgnoreBlank { get { return UIHelper<LimitDialog>.GetPropValue<bool>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSels { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxSels { get { return UIHelper<LimitDialog>.GetPropValue<int>(this); } set { UIHelper<LimitDialog>.SetPropValue(this, value); } }

		static LimitDialog() { UIHelper<LimitDialog>.Register(); }

		LimitDialog(int maxSels)
		{
			InitializeComponent();

			NumSels = MaxSels = maxSels;
			SelMult = 1;
			IgnoreBlank = false;
		}

		Result response = null;
		void OkClick(object sender, RoutedEventArgs e)
		{
			response = new Result { SelMult = SelMult, IgnoreBlank = IgnoreBlank, NumSels = NumSels };
			DialogResult = true;
		}

		public static Result Run(int numSels)
		{
			var dialog = new LimitDialog(numSels);
			if (dialog.ShowDialog() != true)
				return null;

			return dialog.response;
		}
	}
}
