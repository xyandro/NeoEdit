﻿using System.Reflection;
using System.Windows;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class AboutDialog
	{
		[DepProp]
		string Product { get { return UIHelper<AboutDialog>.GetPropValue<string>(this); } set { UIHelper<AboutDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Version { get { return UIHelper<AboutDialog>.GetPropValue<string>(this); } set { UIHelper<AboutDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Copyright { get { return UIHelper<AboutDialog>.GetPropValue<string>(this); } set { UIHelper<AboutDialog>.SetPropValue(this, value); } }

		static AboutDialog() { UIHelper<AboutDialog>.Register(); }

		AboutDialog()
		{
			InitializeComponent();

			Product = ((AssemblyProductAttribute)typeof(AboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)typeof(AboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)typeof(AboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}

		public static Window Run()
		{
			var dialog = new AboutDialog();
			dialog.Show();
			return dialog;
		}

		void LicenseClick(object sender, RoutedEventArgs e) => LicenseDialog.Run();

		void ChangeLogClick(object sender, RoutedEventArgs e) => ChangeLogDialog.Run();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
