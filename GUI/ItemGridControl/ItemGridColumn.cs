﻿using System;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.GUI.ItemGridControl
{
	public class ItemGridColumn
	{
		public string Header { get; set; }
		public DependencyProperty DepProp { get; set; }
		public string StringFormat { get; set; }
		public HorizontalAlignment HorizontalAlignment { get; set; }
		public bool SortAscending { get; set; }
		public bool NumericStrings { get; set; }

		public ItemGridColumn(DependencyProperty depProp)
		{
			Header = depProp.Name;
			DepProp = depProp;
			HorizontalAlignment = (depProp.PropertyType.IsIntegerType()) || (depProp.PropertyType.IsDateType()) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
			StringFormat = depProp.PropertyType.IsIntegerType() ? "n0" : depProp.PropertyType.IsDateType() ? "yyyy/MM/dd HH:mm:ss" : null;
			SortAscending = NumericStrings = true;
		}

		public override string ToString()
		{
			return Header + ": " + DepProp.Name;
		}
	}
}
