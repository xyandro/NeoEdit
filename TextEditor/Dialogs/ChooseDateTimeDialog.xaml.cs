using System;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class ChooseDateTimeDialog
	{
		internal class Result : DialogResult
		{
			public DateTime Value { get; set; }

			public override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name, neXml.Attribute(a => a.Value));
			}

			public static Result FromXML(XElement xml)
			{
				return new Result
				{
					Value = NEXML<Result>.Attribute(xml, a => a.Value),
				};
			}
		}

		[DepProp]
		public DateTime Value { get { return UIHelper<ChooseDateTimeDialog>.GetPropValue<DateTime>(this); } set { UIHelper<ChooseDateTimeDialog>.SetPropValue(this, value); } }

		static ChooseDateTimeDialog() { UIHelper<ChooseDateTimeDialog>.Register(); }

		ChooseDateTimeDialog(DateTime value)
		{
			InitializeComponent();

			Value = value;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Value = Value };
			DialogResult = true;
		}

		static public Result Run(DateTime datetime)
		{
			var dialog = new ChooseDateTimeDialog(datetime);
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
