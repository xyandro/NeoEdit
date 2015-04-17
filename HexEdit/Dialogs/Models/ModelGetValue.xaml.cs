using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class ModelGetValue
	{
		[DepProp]
		public string Value { get { return UIHelper<ModelGetValue>.GetPropValue(() => this.Value); } set { UIHelper<ModelGetValue>.SetPropValue(() => this.Value, value); } }

		static ModelGetValue() { UIHelper<ModelGetValue>.Register(); }

		ModelGetValue(string value)
		{
			InitializeComponent();
			Value = value;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static string Run(string value)
		{
			var dialog = new ModelGetValue(value);
			return dialog.ShowDialog() == true ? dialog.Value : null;
		}
	}
}
