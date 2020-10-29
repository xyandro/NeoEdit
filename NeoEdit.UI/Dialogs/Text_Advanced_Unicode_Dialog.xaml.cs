using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Advanced_Unicode_Dialog
	{
		public class CodePointData : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

			public void SetBaseType(BaseTypeEnum baseType)
			{
				switch (baseType)
				{
					case BaseTypeEnum.Octal: CodePointDisplay = Convert.ToString(CodePoint, 8); break;
					case BaseTypeEnum.Decimal: CodePointDisplay = CodePoint.ToString(); break;
					case BaseTypeEnum.Hex: CodePointDisplay = Convert.ToString(CodePoint, 16); break;
					default: throw new Exception("Invalid type");
				}
			}

			public int CodePoint { get; set; }
			string codePointDisplay;
			public string CodePointDisplay
			{
				get => codePointDisplay;
				set
				{
					codePointDisplay = value;
					OnPropertyChanged();
				}
			}
			public string Description { get; set; }

			public string Display => char.ConvertFromUtf32(CodePoint);

			public override string ToString() => CodePointDisplay;
		}

		public enum BaseTypeEnum
		{
			Octal,
			Decimal,
			Hex,
		}

		[DepProp]
		public string Search { get { return UIHelper<Text_Advanced_Unicode_Dialog>.GetPropValue<string>(this); } set { UIHelper<Text_Advanced_Unicode_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public BaseTypeEnum BaseType { get { return UIHelper<Text_Advanced_Unicode_Dialog>.GetPropValue<BaseTypeEnum>(this); } set { UIHelper<Text_Advanced_Unicode_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public CodePointData CodePoint { get { return UIHelper<Text_Advanced_Unicode_Dialog>.GetPropValue<CodePointData>(this); } set { UIHelper<Text_Advanced_Unicode_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<CodePointData> CodePointInformation { get { return UIHelper<Text_Advanced_Unicode_Dialog>.GetPropValue<List<CodePointData>>(this); } set { UIHelper<Text_Advanced_Unicode_Dialog>.SetPropValue(this, value); } }

		List<CodePointData> codePointInformation;

		static Text_Advanced_Unicode_Dialog()
		{
			UIHelper<Text_Advanced_Unicode_Dialog>.Register();
			UIHelper<Text_Advanced_Unicode_Dialog>.AddCallback(x => x.BaseType, (obj, o, n) => obj.BaseTypeChanged());
			UIHelper<Text_Advanced_Unicode_Dialog>.AddCallback(x => x.Search, (obj, o, n) => obj.SearchChanged());
		}

		Text_Advanced_Unicode_Dialog()
		{
			InitializeComponent();
			LoadCodePointInformation();
			BaseType = BaseTypeEnum.Hex;
		}

		void LoadCodePointInformation()
		{
			codePointInformation = new List<CodePointData>();
			var streamName = typeof(Text_Advanced_Unicode_Dialog).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Unicode.dat")).Single();
			using (var stream = typeof(Text_Advanced_Unicode_Dialog).Assembly.GetManifestResourceStream(streamName))
			using (var reader = new BinaryReader(stream))
			{
				var count = reader.ReadInt32();
				for (var ctr = 0; ctr < count; ++ctr)
					codePointInformation.Add(new CodePointData { CodePoint = reader.ReadInt32(), Description = reader.ReadString() });
			}
			CodePointInformation = codePointInformation;
		}

		void BaseTypeChanged() => codePointInformation.ForEach(codePoint => codePoint.SetBaseType(BaseType));

		bool Match(string str, List<string> searchTerms) => searchTerms.All(term => str.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);

		void SearchChanged()
		{
			var terms = Search.Split(' ').Select(str => str.Trim()).Where(str => str.Length != 0).ToList();
			if (!terms.Any())
			{
				CodePointInformation = codePointInformation;
				return;
			}

			CodePointInformation = codePointInformation.Where(codePoint => Match(codePoint.Description, terms)).ToList();
		}

		void OnSearchPreviewKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Up) || (e.Key == Key.Down))
			{
				codePoints.Focus();
				e.Handled = true;
			}
		}

		void OnCodePointsDoubleClick(object sender, MouseButtonEventArgs e) => OkClick(null, null);

		Configuration_Text_Advanced_Unicode result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (CodePoint == null)
				return;

			result = new Configuration_Text_Advanced_Unicode { Value = CodePoint.Display };
			DialogResult = true;
		}

		public static Configuration_Text_Advanced_Unicode Run(Window parent)
		{
			var dialog = new Text_Advanced_Unicode_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
