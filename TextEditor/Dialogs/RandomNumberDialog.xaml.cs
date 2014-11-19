﻿using System;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class RandomNumberDialog
	{
		internal class Result : DialogResult
		{
			public int MinValue { get; set; }
			public int MaxValue { get; set; }

			public override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Attribute(a => a.MinValue),
					neXml.Attribute(a => a.MaxValue)
				);
			}

			public static Result FromXML(XElement xml)
			{
				return new Result
				{
					MinValue = NEXML<Result>.Attribute(xml, a => a.MinValue),
					MaxValue = NEXML<Result>.Attribute(xml, a => a.MaxValue)
				};
			}
		}

		[DepProp]
		public int MinValue { get { return UIHelper<RandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<RandomNumberDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxValue { get { return UIHelper<RandomNumberDialog>.GetPropValue<int>(this); } set { UIHelper<RandomNumberDialog>.SetPropValue(this, value); } }

		static RandomNumberDialog() { UIHelper<RandomNumberDialog>.Register(); }

		RandomNumberDialog()
		{
			InitializeComponent();

			MinValue = 1;
			MaxValue = 1000;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { MinValue = Math.Min(MinValue, MaxValue), MaxValue = Math.Max(MinValue, MaxValue) };
			DialogResult = true;
		}

		static public Result Run()
		{
			var dialog = new RandomNumberDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
