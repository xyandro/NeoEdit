using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class TextUnicodeDialog
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
		public string Search { get { return UIHelper<TextUnicodeDialog>.GetPropValue<string>(this); } set { UIHelper<TextUnicodeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public BaseTypeEnum BaseType { get { return UIHelper<TextUnicodeDialog>.GetPropValue<BaseTypeEnum>(this); } set { UIHelper<TextUnicodeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public CodePointData CodePoint { get { return UIHelper<TextUnicodeDialog>.GetPropValue<CodePointData>(this); } set { UIHelper<TextUnicodeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<CodePointData> CodePointInformation { get { return UIHelper<TextUnicodeDialog>.GetPropValue<List<CodePointData>>(this); } set { UIHelper<TextUnicodeDialog>.SetPropValue(this, value); } }

		List<CodePointData> codePointInformation;

		static TextUnicodeDialog()
		{
			UIHelper<TextUnicodeDialog>.Register();
			UIHelper<TextUnicodeDialog>.AddCallback(x => x.BaseType, (obj, o, n) => obj.BaseTypeChanged());
			UIHelper<TextUnicodeDialog>.AddCallback(x => x.Search, (obj, o, n) => obj.SearchChanged());
		}

		TextUnicodeDialog()
		{
			InitializeComponent();
			LoadCodePointInformation();
			BaseType = BaseTypeEnum.Hex;
		}

		void LoadCodePointInformation()
		{
			codePointInformation = new List<CodePointData>();
			var streamName = typeof(TextUnicodeDialog).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".Unicode.dat")).Single();
			using (var stream = typeof(TextUnicodeDialog).Assembly.GetManifestResourceStream(streamName))
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

		TextUnicodeDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (CodePoint == null)
				return;

			result = new TextUnicodeDialogResult { Value = CodePoint.Display };
			DialogResult = true;
		}

		public static TextUnicodeDialogResult Run(Window parent)
		{
			var dialog = new TextUnicodeDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
