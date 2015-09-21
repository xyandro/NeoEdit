using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.Controls;
using NeoEdit.GUI.Converters;
using NeoEdit.GUI.Dialogs;
using NeoEdit.GUI.Misc;
using NeoEdit.TextEdit.Content;
using NeoEdit.TextEdit.Dialogs;

namespace NeoEdit.TextEdit
{
	public partial class TextEditor
	{
		TextData _data = new TextData();
		TextData Data
		{
			get { return _data; }
			set
			{
				_data = value;
				Selections.Clear();
				Regions.Clear();
				Searches.Clear();
				Bookmarks.Clear();
				undoRedo.Clear();
				CalculateDiff();
				CalculateBoundaries();
			}
		}
		readonly UndoRedo undoRedo;

		[DepProp]
		public string TabLabel { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsModified { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return UIHelper<TextEditor>.GetPropValue<Highlighting.HighlightingType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Parser.ParserType ContentType { get { return UIHelper<TextEditor>.GetPropValue<Parser.ParserType>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<TextEditor>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string AESKey { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? LineMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? ColumnMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? IndexMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMin { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int? PositionMax { get { return UIHelper<TextEditor>.GetPropValue<int?>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumSelections { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int NumRegions { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int xScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public int yScrollValue { get { return UIHelper<TextEditor>.GetPropValue<int>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public string LineEnding { get { return UIHelper<TextEditor>.GetPropValue<string>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ObservableCollection<string>> keysAndValues { get { return UIHelper<TextEditor>.GetPropValue<ObservableCollection<ObservableCollection<string>>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShareKeys { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollectionEx<string> Clipboard { get { return UIHelper<TextEditor>.GetPropValue<ObservableCollectionEx<string>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShareClipboard { get { return UIHelper<TextEditor>.GetPropValue<bool>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollectionEx<Table> Results { get { return UIHelper<TextEditor>.GetPropValue<ObservableCollectionEx<Table>>(this); } set { UIHelper<TextEditor>.SetPropValue(this, value); } }

		TextEditTabs TabsParent { get { return UIHelper.FindParent<TextEditTabs>(GetValue(Tabs.TabParentProperty) as Tabs); } }
		Window WindowParent { get { return UIHelper.FindParent<Window>(this); } }
		TextEditor diffTarget;
		public TextEditor DiffTarget
		{
			get { return diffTarget; }
			set
			{
				if (value == this)
					value = null;

				if (diffTarget != null)
				{
					BindingOperations.ClearBinding(this, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					BindingOperations.ClearBinding(diffTarget, UIHelper<TextEditor>.GetProperty(a => a.yScrollValue));
					Data.ClearDiff();
					diffTarget.Data.ClearDiff();
					CalculateBoundaries();
					diffTarget.CalculateBoundaries();
					diffTarget.diffTarget = null;
					diffTarget = null;
				}

				if (value != null)
				{
					value.DiffTarget = null;
					diffTarget = value;
					value.diffTarget = this;
					SetBinding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue), new Binding(UIHelper<TextEditor>.GetProperty(a => a.yScrollValue).Name) { Source = value, Mode = BindingMode.TwoWay });
				}

				CalculateDiff();
			}
		}

		int xScrollViewportFloor { get { return (int)Math.Floor(xScroll.ViewportSize); } }
		int xScrollViewportCeiling { get { return (int)Math.Ceiling(xScroll.ViewportSize); } }
		int yScrollViewportFloor { get { return (int)Math.Floor(yScroll.ViewportSize); } }
		int yScrollViewportCeiling { get { return (int)Math.Ceiling(yScroll.ViewportSize); } }

		readonly static byte[] EncryptedHeader = Encoding.UTF8.GetBytes("\u0000NEAES\u0000");
		readonly static byte[] EncryptedValidate = Encoding.UTF8.GetBytes("\u0000VALID\u0000");
		readonly static HashSet<string> EncryptionKeys = new HashSet<string>();

		readonly RangeList Selections, Searches, Regions, Bookmarks;

		static ThreadSafeRandom random = new ThreadSafeRandom();

		static ObservableCollection<ObservableCollection<string>> staticKeysAndValues { get; set; }
		ObservableCollection<ObservableCollection<string>> localKeysAndValues { get; set; }
		Dictionary<string, int> keysHash;
		static Dictionary<string, int> staticKeysHash = new Dictionary<string, int>();
		Dictionary<string, int> localKeysHash = new Dictionary<string, int>();
		static ObservableCollectionEx<string> staticClipboard { get; set; }
		ObservableCollectionEx<string> localClipboard { get; set; }
		static TextEditor()
		{
			UIHelper<TextEditor>.Register();
			UIHelper<TextEditor>.AddCallback(a => a.xScrollValue, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.yScrollValue, (obj, o, n) => { obj.canvasRenderTimer.Start(); obj.bookmarkRenderTimer.Start(); });
			UIHelper<TextEditor>.AddCallback(a => a.HighlightType, (obj, o, n) => obj.canvasRenderTimer.Start());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualWidthProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.canvas, Canvas.ActualHeightProperty, obj => obj.CalculateBoundaries());
			UIHelper<TextEditor>.AddCallback(a => a.ShareKeys, (obj, o, n) => obj.SetupStaticOrLocalData());
			UIHelper<TextEditor>.AddCallback(a => a.ShareClipboard, (obj, o, n) => obj.SetupStaticOrLocalData());
			UIHelper<TextEditor>.AddCoerce(a => a.xScrollValue, (obj, value) => (int)Math.Max(obj.xScroll.Minimum, Math.Min(obj.xScroll.Maximum, value)));
			UIHelper<TextEditor>.AddCoerce(a => a.yScrollValue, (obj, value) => (int)Math.Max(obj.yScroll.Minimum, Math.Min(obj.yScroll.Maximum, value)));

			staticKeysAndValues = new ObservableCollection<ObservableCollection<string>> { null, null, null, null, null, null, null, null, null, null };
			staticKeysAndValues.CollectionChanged += (s, e) => keysAndValues_CollectionChanged(staticKeysAndValues, staticKeysHash, e);
			for (var ctr = 0; ctr < staticKeysAndValues.Count; ++ctr)
				staticKeysAndValues[ctr] = new ObservableCollection<string>();

			staticClipboard = new ObservableCollectionEx<string>();
			NEClipboard.ClipboardChanged += () => ClipboardChanged(staticClipboard);
		}

		static void keysAndValues_CollectionChanged(ObservableCollection<ObservableCollection<string>> data, Dictionary<string, int> hash, NotifyCollectionChangedEventArgs e)
		{
			if ((e.Action != NotifyCollectionChangedAction.Replace) || (e.NewStartingIndex != 0))
				return;

			hash.Clear();
			for (var pos = 0; pos < data[0].Count; ++pos)
				hash[data[0][pos]] = pos;
		}

		static void ClipboardChanged(ObservableCollectionEx<string> clipboard)
		{
			// This is only for data coming from external sources
			if (NEClipboard.Extra as Type == typeof(TextEditor))
				return;

			clipboard.Clear();
			clipboard.AddRange(NEClipboard.Strings);
		}

		RunOnceTimer canvasRenderTimer, bookmarkRenderTimer;
		List<PropertyChangeNotifier> localCallbacks;

		public TextEditor(string filename = null, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null, int line = -1, int column = -1)
		{
			localKeysAndValues = new ObservableCollection<ObservableCollection<string>> { null, null, null, null, null, null, null, null, null, null };
			localKeysAndValues.CollectionChanged += (s, e) => keysAndValues_CollectionChanged(localKeysAndValues, localKeysHash, e);
			for (var ctr = 0; ctr < localKeysAndValues.Count; ++ctr)
				localKeysAndValues[ctr] = new ObservableCollection<string>();
			localClipboard = new ObservableCollectionEx<string>();
			NEClipboard.ClipboardChanged += () => ClipboardChanged(localClipboard);

			InitializeComponent();
			bookmarks.Width = Font.lineHeight;

			SetupTabLabel();

			ShareKeys = ShareClipboard = true;
			SetupStaticOrLocalData();
			SetupDropAccept();

			undoRedo = new UndoRedo(b => IsModified = b);
			Selections = new RangeList(SelectionsInvalidated);
			Searches = new RangeList(SearchesInvalidated);
			Regions = new RangeList(RegionsInvalidated);
			Bookmarks = new RangeList(BookmarksInvalidated);

			Results = new ObservableCollectionEx<Table>();

			canvasRenderTimer = new RunOnceTimer(() => canvas.InvalidateVisual());
			canvasRenderTimer.AddDependency(Selections.Timer, Searches.Timer, Regions.Timer);
			bookmarkRenderTimer = new RunOnceTimer(() => bookmarks.InvalidateVisual());

			OpenFile(filename, bytes, codePage, modified);
			Goto(line, column);

			localCallbacks = UIHelper<TextEditor>.GetLocalCallbacks(this);

			canvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
			canvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;
			canvas.MouseMove += OnCanvasMouseMove;
			canvas.Render += OnCanvasRender;

			bookmarks.Render += OnBookmarksRender;

			MouseWheel += (s, e) => yScrollValue -= e.Delta / 40;

			Loaded += (s, e) =>
			{
				EnsureVisible();
				canvasRenderTimer.Start();
			};
		}

		void SetupTabLabel()
		{
			var multiBinding = new MultiBinding { Converter = new NEExpressionConverter(), ConverterParameter = @"([0] == null?""[Untitled]"":FileName([0]))+([1]?""*"":"""")" };
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.FileName).Name) { Source = this });
			multiBinding.Bindings.Add(new Binding(UIHelper<TextEditor>.GetProperty(a => a.IsModified).Name) { Source = this });
			SetBinding(UIHelper<TextEditor>.GetProperty(a => a.TabLabel), multiBinding);
		}

		void SetupStaticOrLocalData()
		{
			if (ShareKeys)
			{
				keysAndValues = staticKeysAndValues;
				keysHash = staticKeysHash;
			}
			else
			{
				keysAndValues = localKeysAndValues;
				keysHash = localKeysHash;
			}

			Clipboard = ShareClipboard ? staticClipboard : localClipboard;
		}

		void SetupDropAccept()
		{
			AllowDrop = true;
			DragEnter += (s, e) => e.Effects = DragDropEffects.Link;
			Drop += (s, e) =>
			{
				var fileList = e.Data.GetData("FileDrop") as string[];
				if (fileList != null)
				{
					if (Selections.Count != 1)
						throw new Exception("Must have one selection.");

					var files = fileList.Select(file => file + Data.DefaultEnding).ToList();
					var offset = Selections.Single().Start;
					ReplaceSelections(String.Join("", files));
					Selections.Clear();
					foreach (var str in files)
					{
						Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
						offset += str.Length;
					}

					e.Handled = true;
				}
			};
		}

		void SetClipboard(object data)
		{
			var str = data.ToString();
			Clipboard.Clear();
			Clipboard.Add(str);
			NEClipboard.Set(data, str, typeof(TextEditor));
		}

		void SetClipboard(List<string> data)
		{
			Clipboard.Clear();
			foreach (var str in data)
				Clipboard.Add(str);
			NEClipboard.Set(data, String.Join(" ", data), typeof(TextEditor));
		}

		void SetClipboardFiles(List<string> data, bool isCut)
		{
			Clipboard.Clear();
			foreach (var str in data)
				Clipboard.Add(str);
			NEClipboard.SetFiles(data, isCut, typeof(TextEditor));
		}

		internal void Goto(int line, int column)
		{
			line = Math.Max(0, Math.Min(line, Data.NumLines) - 1);
			var index = Data.GetIndexFromColumn(line, Math.Max(0, column - 1), true);
			Selections.Add(new Range(Data.GetOffset(line, index)));

		}

		byte[] Encrypt(byte[] data)
		{
			if (AESKey == null)
				return data;

			EncryptionKeys.Add(AESKey);
			return EncryptedHeader.Concat(Cryptor.Encrypt(EncryptedValidate.Concat(data).ToArray(), Cryptor.Type.AES, AESKey)).ToArray();
		}

		static byte[] Decrypt(byte[] data, string key)
		{
			try
			{
				data = Cryptor.Decrypt(data, Cryptor.Type.AES, key);
				if ((data.Length < EncryptedValidate.Length) || (!data.Equal(EncryptedValidate, EncryptedValidate.Length)))
					return null;
				data = data.Skip(EncryptedValidate.Length).ToArray();
				return data;
			}
			catch { return null; }
		}

		void HandleDecrypt(ref byte[] bytes)
		{
			AESKey = null;
			if ((bytes.Length < EncryptedHeader.Length) || (!bytes.Equal(EncryptedHeader, EncryptedHeader.Length)))
				return;

			bytes = bytes.Skip(EncryptedHeader.Length).ToArray();
			foreach (var key in EncryptionKeys)
			{
				var result = Decrypt(bytes, key);
				if (result != null)
				{
					AESKey = key;
					bytes = result;
					return;
				}
			}

			var dialogResult = SymmetricKeyDialog.Run(WindowParent, Cryptor.Type.AES);
			if (dialogResult == null)
				throw new Exception("Failed to decrypt file");

			var result2 = Decrypt(bytes, dialogResult.Key);
			if (result2 == null)
				throw new Exception("Failed to decrypt file");

			bytes = result2;
			AESKey = dialogResult.Key;
			EncryptionKeys.Add(AESKey);
		}

		DateTime fileLastWrite;
		internal void OpenFile(string filename, byte[] bytes = null, Coder.CodePage codePage = Coder.CodePage.AutoByBOM, bool? modified = null)
		{
			FileName = filename;
			var isModified = modified ?? bytes != null;
			if (bytes == null)
			{
				if (FileName == null)
					bytes = new byte[0];
				else
					bytes = File.ReadAllBytes(FileName);
			}

			HandleDecrypt(ref bytes);

			if (codePage == Coder.CodePage.AutoByBOM)
				codePage = Coder.CodePageFromBOM(bytes);
			Data = new TextData(bytes, codePage);
			CodePage = codePage;
			HighlightType = Highlighting.Get(FileName);
			ContentType = Parser.GetParserType(FileName);
			if (File.Exists(FileName))
				fileLastWrite = new FileInfo(FileName).LastWriteTime;

			// If encoding can't exactly express bytes mark as modified (only for < 50 MB)
			if ((!isModified) && ((bytes.Length >> 20) < 50))
				isModified = !Coder.CanFullyEncode(bytes, CodePage);

			undoRedo.SetModified(isModified);
		}

		int BeginOffset()
		{
			return Data.GetOffset(0, 0);
		}

		int EndOffset()
		{
			return Data.GetOffset(Data.NumLines - 1, Data.GetLineLength(Data.NumLines - 1));
		}

		internal bool CanClose()
		{
			var answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		internal bool CanClose(ref Message.OptionsEnum answer)
		{
			if (!IsModified)
				return true;

			if ((answer != Message.OptionsEnum.YesToAll) && (answer != Message.OptionsEnum.NoToAll))
				answer = new Message
				{
					Title = "Confirm",
					Text = "Do you want to save changes?",
					Options = Message.OptionsEnum.YesNoYesAllNoAllCancel,
					DefaultCancel = Message.OptionsEnum.Cancel,
				}.Show();

			switch (answer)
			{
				case Message.OptionsEnum.Cancel:
					return false;
				case Message.OptionsEnum.No:
				case Message.OptionsEnum.NoToAll:
					return true;
				case Message.OptionsEnum.Yes:
				case Message.OptionsEnum.YesToAll:
					Command_File_Save_Save();
					return !IsModified;
			}
			return false;
		}

		internal void Closed()
		{
			DiffTarget = null;
		}

		internal enum GetPathType
		{
			FileName,
			FileNameWoExtension,
			Directory,
			Extension,
		}

		Range GetPathRange(GetPathType type, Range range)
		{
			var path = GetString(range);
			var dirLength = Math.Max(0, path.LastIndexOf('\\'));
			if ((path.StartsWith(@"\\")) && (dirLength == 1))
				dirLength = 0;
			var dirTotal = dirLength == 0 ? 0 : dirLength + 1;
			var extLen = Path.GetExtension(path).Length;

			switch (type)
			{
				case GetPathType.FileName: return new Range(range.End, range.Start + dirTotal);
				case GetPathType.FileNameWoExtension: return new Range(range.End - extLen, range.Start + dirTotal);
				case GetPathType.Directory: return new Range(range.Start + dirLength, range.Start);
				case GetPathType.Extension: return new Range(range.End, range.End - extLen);
				default: throw new ArgumentException();
			}
		}

		bool ConfirmVerifyCanFullyEncode()
		{
			return new Message
			{
				Title = "Confirm",
				Text = String.Format("The current encoding cannot fully represent this data.  Continue?"),
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes;
		}

		bool VerifyCanFullyEncode()
		{
			return (Data.CanFullyEncode(CodePage)) || (ConfirmVerifyCanFullyEncode());
		}

		bool VerifyCanFullyEncode(List<string> strs, Coder.CodePage codePage)
		{
			return (strs.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());
		}

		bool VerifyCanFullyEncode(List<byte[]> data, Coder.CodePage codePage)
		{
			return (data.AsParallel().All(str => Coder.CanFullyEncode(str, codePage))) || (ConfirmVerifyCanFullyEncode());
		}

		Coder.CodePage DetectUnicode(List<byte[]> data)
		{
			if (data.Count == 0)
				return Coder.CodePage.Default;

			return data.Select(a => Coder.GuessUnicodeEncoding(a)).GroupBy(a => a).OrderByDescending(a => a.Count()).First().Key;
		}

		delegate bool TryParse<T>(string str, out T value);
		List<object> InterpretType<T>(IEnumerable<string> strs, TryParse<T> tryParse)
		{
			var result = new List<object>();
			T value;
			foreach (var str in strs)
			{
				if (!tryParse(str, out value))
					return null;
				result.Add(value);
			}
			return result;
		}

		List<object> InterpretValues(IEnumerable<string> strs)
		{
			return InterpretType<bool>(strs, bool.TryParse) ?? InterpretType<long>(strs, long.TryParse) ?? InterpretType<double>(strs, double.TryParse) ?? strs.Cast<object>().ToList();
		}

		internal Dictionary<string, List<object>> GetExpressionData(int? count = null, NEExpression expression = null)
		{
			var sels = Selections.Take(count ?? Selections.Count).ToList();
			var strs = sels.Select(range => GetString(range)).ToList();
			var keyOrdering = strs.Select(str => keysHash.ContainsKey(str) ? (int?)keysHash[str] : null).ToList();

			// Can't access DependencyProperties from other threads; grab a copy:
			var FileName = this.FileName;
			var Clipboard = this.Clipboard;
			var keysAndValues = this.keysAndValues;

			var parallelDataActions = new Dictionary<HashSet<string>, Action<HashSet<string>, Action<string, List<object>>>>();
			parallelDataActions.Add(new HashSet<string> { "f" }, (items, addData) => addData("f", new List<object> { FileName }));
			parallelDataActions.Add(new HashSet<string> { "x" }, (items, addData) => addData("x", InterpretValues(strs)));
			parallelDataActions.Add(new HashSet<string> { "xl" }, (items, addData) => addData("xl", strs.Select(str => str.Length).Cast<object>().ToList()));
			parallelDataActions.Add(new HashSet<string> { "xn" }, (items, addData) => addData("xn", new List<object> { sels.Count }.ToList()));
			parallelDataActions.Add(new HashSet<string> { "y" }, (items, addData) => addData("y", Enumerable.Range(1, sels.Count).Cast<object>().ToList()));
			parallelDataActions.Add(new HashSet<string> { "z" }, (items, addData) => addData("z", Enumerable.Range(0, sels.Count).Cast<object>().ToList()));
			parallelDataActions.Add(new HashSet<string> { "c" }, (items, addData) => addData("c", InterpretValues(Clipboard)));
			parallelDataActions.Add(new HashSet<string> { "cl" }, (items, addData) => addData("cl", Clipboard.Select(str => str.Length).Cast<object>().ToList()));
			parallelDataActions.Add(new HashSet<string> { "cn" }, (items, addData) => addData("cn", new List<object> { Clipboard.Count }));
			parallelDataActions.Add(new HashSet<string> { "line", "col" }, (items, addData) =>
			{
				var lines = sels.AsParallel().AsOrdered().Select(range => Data.GetOffsetLine(range.Start)).ToList();
				if (items.Contains("line"))
					addData("line", lines.Select(line => line + 1).Cast<object>().ToList());
				if (items.Contains("col"))
					addData("col", sels.AsParallel().AsOrdered().Select((range, index) => Data.GetOffsetIndex(range.Start, lines[index]) + 1).Cast<object>().ToList());
			});
			parallelDataActions.Add(new HashSet<string> { "pos" }, (items, addData) => addData("pos", sels.Select(range => range.Start).Cast<object>().ToList()));
			for (var ctr = 0; ctr <= 9; ++ctr)
			{
				var num = ctr; // If we don't copy this the threads get the wrong value
				var prefix = ctr == 0 ? "k" : "v" + ctr;
				var kvName = prefix;
				var kvlName = prefix + "l";
				var rkvName = "r" + prefix;
				var rkvlName = "r" + prefix + "l";
				var rkvnName = "r" + prefix + "n";
				parallelDataActions.Add(new HashSet<string> { rkvName }, (items, addData) => addData(rkvName, InterpretValues(keysAndValues[num])));
				parallelDataActions.Add(new HashSet<string> { rkvlName }, (items, addData) => addData(rkvlName, keysAndValues[num].Select(str => str.Length).Cast<object>().ToList()));
				parallelDataActions.Add(new HashSet<string> { rkvnName }, (items, addData) => addData(rkvnName, new List<object> { keysAndValues[num].Count }));
				parallelDataActions.Add(new HashSet<string> { kvName, kvlName }, (items, addData) =>
				{
					List<string> values;
					if (keysAndValues[0].Count == keysAndValues[num].Count)
						values = keyOrdering.Select(order => order.HasValue ? keysAndValues[num][order.Value] : "").ToList();
					else
						values = new List<string>();

					if (items.Contains(kvName))
						addData(kvName, InterpretValues(values));
					if (items.Contains(kvlName))
						addData(kvlName, values.Select(str => str.Length).Cast<object>().ToList());
				});
			}

			// Add any keys/values that match count and aren't already defined
			if (keysAndValues[0].Count == keysAndValues[1].Count)
			{
				var availableVars = new HashSet<string>(parallelDataActions.SelectMany(action => action.Key));
				var keyVars = Enumerable.Range(0, keysAndValues[0].Count).Where(index => !availableVars.Contains(keysAndValues[0][index])).ToDictionary(index => keysAndValues[0][index], index => keysAndValues[1][index]);
				foreach (var pair in keyVars)
					parallelDataActions.Add(new HashSet<string> { pair.Key }, (items, addData) => addData(pair.Key, InterpretValues(new List<string> { pair.Value })));
			}

			var used = expression != null ? expression.Variables : new HashSet<string>(parallelDataActions.SelectMany(action => action.Key));
			var data = new Dictionary<string, List<object>>();
			Parallel.ForEach(parallelDataActions, pair =>
			{
				if (pair.Key.Any(key => used.Contains(key)))
					pair.Value(used, (key, value) =>
					{
						lock (data)
							data[key] = value;
					});
			});

			return data;
		}

		List<T> GetExpressionResults<T>(string expression, bool resizeToSelections = true, bool matchToSelections = true)
		{
			var neExpression = new NEExpression(expression);
			var results = neExpression.Evaluate<T>(GetExpressionData(expression: neExpression));
			if ((resizeToSelections) && (results.Count == 1))
				results = results.Resize(Selections.Count, results[0]).ToList();
			if ((matchToSelections) && (results.Count != Selections.Count))
				throw new Exception("Expression count doesn't match selection count");
			return results;
		}

		void CopyDirectory(string src, string dest)
		{
			var srcDirs = new List<string> { src };
			for (var ctr = 0; ctr < srcDirs.Count; ++ctr)
				srcDirs.AddRange(Directory.GetDirectories(srcDirs[ctr]));

			var srcFiles = new List<string>();
			foreach (var dir in srcDirs)
				srcFiles.AddRange(Directory.GetFiles(dir));

			var destDirs = srcDirs.Select(dir => dest + dir.Substring(src.Length)).ToList();
			var destFiles = srcFiles.Select(file => dest + file.Substring(src.Length)).ToList();
			destDirs.ForEach(dir => Directory.CreateDirectory(dir));
			for (var ctr = 0; ctr < srcFiles.Count; ++ctr)
				File.Copy(srcFiles[ctr], destFiles[ctr]);
		}

		void Save(string fileName)
		{
			if (((Data.NumChars >> 20) < 50) && (!VerifyCanFullyEncode()))
				return;

			File.WriteAllBytes(fileName, Encrypt(Data.GetBytes(CodePage)));
			fileLastWrite = new FileInfo(fileName).LastWriteTime;
			undoRedo.SetModified(false);
			FileName = fileName;
		}

		internal List<string> GetSelectionStrings()
		{
			return Selections.AsParallel().AsOrdered().Select(range => GetString(range)).ToList();
		}

		internal bool GetDialogResult(TextEditCommand command, out object dialogResult)
		{
			dialogResult = null;

			switch (command)
			{
				case TextEditCommand.File_Operations_Encryption: dialogResult = Command_File_Operations_Encryption_Dialog(); break;
				case TextEditCommand.File_Encoding_Encoding: dialogResult = Command_File_Encoding_Encoding_Dialog(); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: dialogResult = Command_File_Encoding_ReopenWithEncoding_Dialog(); break;
				case TextEditCommand.Edit_Find_Find: dialogResult = Command_Edit_Find_FindReplace_Dialog(false); break;
				case TextEditCommand.Edit_Find_Replace: dialogResult = Command_Edit_Find_FindReplace_Dialog(true); break;
				case TextEditCommand.Edit_Table_Edit: dialogResult = Command_Edit_Table_Edit_Dialog(); break;
				case TextEditCommand.Edit_Repeat: dialogResult = Command_Edit_Repeat_Dialog(); break;
				case TextEditCommand.Edit_URL_Absolute: dialogResult = Command_Edit_URL_Absolute_Dialog(); break;
				case TextEditCommand.Edit_Hash: dialogResult = Command_Edit_Hash_Dialog(); break;
				case TextEditCommand.Edit_Sort: dialogResult = Command_Edit_Sort_Dialog(); break;
				case TextEditCommand.Edit_Convert: dialogResult = Command_Edit_Convert_Dialog(); break;
				case TextEditCommand.Files_Names_MakeAbsolute: dialogResult = Command_Files_Names_MakeAbsolute_Dialog(); break;
				case TextEditCommand.Files_Names_GetUnique: dialogResult = Command_Files_Names_GetUnique_Dialog(); break;
				case TextEditCommand.Files_Set_Size: dialogResult = Command_Files_Set_Size_Dialog(); break;
				case TextEditCommand.Files_Set_WriteTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AccessTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_CreateTime: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_AllTimes: dialogResult = Command_Files_Set_Time_Dialog(); break;
				case TextEditCommand.Files_Set_Attributes: dialogResult = Command_Files_Set_Attributes_Dialog(); break;
				case TextEditCommand.Files_Hash: dialogResult = Command_Files_Hash_Dialog(); break;
				case TextEditCommand.Expression_Expression: dialogResult = Command_Expression_Expression_Dialog(); break;
				case TextEditCommand.Expression_Copy: dialogResult = Command_Expression_Expression_Dialog(); break;
				case TextEditCommand.Expression_SelectByExpression: dialogResult = Command_Expression_SelectByExpression_Dialog(); break;
				case TextEditCommand.Text_Select_ByWidth: dialogResult = Command_Text_Select_ByWidth_Dialog(); break;
				case TextEditCommand.Text_Width: dialogResult = Command_Text_Width_Dialog(); break;
				case TextEditCommand.Text_Trim: dialogResult = Command_Text_Trim_Dialog(); break;
				case TextEditCommand.Text_RandomText: dialogResult = Command_Text_RandomText_Dialog(); break;
				case TextEditCommand.Text_ReverseRegEx: dialogResult = Command_Text_ReverseRegEx_Dialog(); break;
				case TextEditCommand.Text_CombinationsPermutations: dialogResult = Command_Text_CombinationsPermutations_Dialog(); break;
				case TextEditCommand.Numeric_RandomNumber: dialogResult = Command_Numeric_RandomNumber_Dialog(); break;
				case TextEditCommand.Numeric_MinMaxValues: dialogResult = Command_Numeric_MinMaxValues_Dialog(); break;
				case TextEditCommand.DateTime_Convert: dialogResult = Command_DateTime_Convert_Dialog(); break;
				case TextEditCommand.Position_Goto_Lines: dialogResult = Command_Position_Goto_Dialog(GotoType.Line); break;
				case TextEditCommand.Position_Goto_Columns: dialogResult = Command_Position_Goto_Dialog(GotoType.Column); break;
				case TextEditCommand.Position_Goto_Positions: dialogResult = Command_Position_Goto_Dialog(GotoType.Position); break;
				case TextEditCommand.Content_Ancestor: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Parents); break;
				case TextEditCommand.Content_Attributes_ByAttribute: dialogResult = Command_Content_Attributes_ByAttribute_Dialog(); break;
				case TextEditCommand.Content_Children_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Children); break;
				case TextEditCommand.Content_Descendants_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Descendants); break;
				case TextEditCommand.Content_Select_ByAttribute: dialogResult = Command_Content_FindByAttribute_Dialog(ParserNode.ParserNodeListType.Self); break;
				case TextEditCommand.Network_Ping: dialogResult = Command_Network_Ping_Dialog(); break;
				case TextEditCommand.Network_ScanPorts: dialogResult = Command_Network_ScanPorts_Dialog(); break;
				case TextEditCommand.Database_Connect: dialogResult = Command_Database_Connect_Dialog(); break;
				case TextEditCommand.Database_Examine: Command_Database_Examine_Dialog(); break;
				case TextEditCommand.Select_Limit: dialogResult = Command_Select_Limit_Dialog(); break;
				case TextEditCommand.Select_ByCount: dialogResult = Command_Select_ByCount_Dialog(); break;
				default: return true;
			}

			return dialogResult != null;
		}

		static HashSet<string> drives = new HashSet<string>(DriveInfo.GetDrives().Select(drive => drive.Name));
		bool StringsAreFiles(List<string> strs)
		{
			if (!strs.Any())
				return false;
			if (strs.Any(str => str.IndexOfAny(Path.GetInvalidPathChars()) != -1))
				return false;
			if (strs.Any(str => !drives.Any(drive => str.StartsWith(drive))))
				return false;
			if (strs.Any(str => !FileOrDirectoryExists(str)))
				return false;
			return true;
		}

		class PreviousStruct
		{
			public TextEditCommand Command { get; set; }
			public bool ShiftDown { get; set; }
			public object DialogResult { get; set; }
		}
		PreviousStruct previous = null;

		bool timeNext = false;
		internal void HandleCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			doDrag = false;

			var start = DateTime.UtcNow;
			if (command != TextEditCommand.Macro_RepeatLastAction)
			{
				previous = new PreviousStruct
				{
					Command = command,
					ShiftDown = shiftDown,
					DialogResult = dialogResult,
				};
			}

			switch (command)
			{
				case TextEditCommand.File_Open_Selected: Command_File_Open_Selected(); break;
				case TextEditCommand.File_Save_Save: Command_File_Save_Save(); break;
				case TextEditCommand.File_Save_SaveAs: Command_File_Save_SaveAs(); break;
				case TextEditCommand.File_Operations_Rename: Command_File_Operations_Rename(); break;
				case TextEditCommand.File_Operations_Delete: Command_File_Operations_Delete(); break;
				case TextEditCommand.File_Operations_Explore: Command_File_Operations_Explore(); break;
				case TextEditCommand.File_Operations_Encryption: Command_File_Operations_Encryption(dialogResult as SymmetricKeyDialog.Result); break;
				case TextEditCommand.File_Operations_OpenDisk: Command_File_Operations_OpenDisk(); break;
				case TextEditCommand.File_Close: if (CanClose()) { TabsParent.Remove(this); } break;
				case TextEditCommand.File_Refresh: Command_File_Refresh(); break;
				case TextEditCommand.File_Revert: Command_File_Revert(); break;
				case TextEditCommand.File_Insert_Files: Command_File_Insert_Files(); break;
				case TextEditCommand.File_Insert_CopiedCut: Command_File_Insert_CopiedCut(); break;
				case TextEditCommand.File_Insert_Selected: Command_File_Insert_Selected(); break;
				case TextEditCommand.File_Copy_Path: Command_File_Copy_Path(); break;
				case TextEditCommand.File_Copy_Name: Command_File_Copy_Name(); break;
				case TextEditCommand.File_Copy_Count: Command_File_Copy_Count(); break;
				case TextEditCommand.File_Encoding_Encoding: Command_File_Encoding_Encoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_Encoding_ReopenWithEncoding: Command_File_Encoding_ReopenWithEncoding(dialogResult as EncodingDialog.Result); break;
				case TextEditCommand.File_HexEditor: if (Command_File_HexEditor()) { TabsParent.Remove(this, true); } break;
				case TextEditCommand.Edit_Undo: Command_Edit_Undo(); break;
				case TextEditCommand.Edit_Redo: Command_Edit_Redo(); break;
				case TextEditCommand.Edit_Copy_Copy: Command_Edit_Copy_CutCopy(false); break;
				case TextEditCommand.Edit_Copy_Cut: Command_Edit_Copy_CutCopy(true); break;
				case TextEditCommand.Edit_Paste_Paste: Command_Edit_Paste_Paste(shiftDown); break;
				case TextEditCommand.Edit_Diff_Break: Command_Edit_Diff_Break(); break;
				case TextEditCommand.Edit_Diff_Next: Command_Edit_Diff_NextPrevious(true); break;
				case TextEditCommand.Edit_Diff_Previous: Command_Edit_Diff_NextPrevious(false); break;
				case TextEditCommand.Edit_Diff_CopyLeft: Command_Edit_Diff_CopyLeftRight(true); break;
				case TextEditCommand.Edit_Diff_CopyRight: Command_Edit_Diff_CopyLeftRight(false); break;
				case TextEditCommand.Edit_Find_Find: Command_Edit_Find_FindReplace(false, shiftDown, dialogResult as FindTextDialog.Result); break;
				case TextEditCommand.Edit_Find_Next: Command_Edit_Find_NextPrevious(true, shiftDown); break;
				case TextEditCommand.Edit_Find_Previous: Command_Edit_Find_NextPrevious(false, shiftDown); break;
				case TextEditCommand.Edit_Find_Replace: Command_Edit_Find_FindReplace(true, shiftDown, dialogResult as FindTextDialog.Result); break;
				case TextEditCommand.Edit_Table_Edit: Command_Edit_Table_Edit(dialogResult as EditTablesDialog.Result); break;
				case TextEditCommand.Edit_Table_RegionsSelectionsToTable: Command_Edit_Table_RegionsSelectionsToTable(); break;
				case TextEditCommand.Edit_CopyDown: Command_Edit_CopyDown(); break;
				case TextEditCommand.Edit_Repeat: Command_Edit_Repeat(dialogResult as RepeatDialog.Result); break;
				case TextEditCommand.Edit_Markup_Escape: Command_Edit_Markup_Escape(); break;
				case TextEditCommand.Edit_Markup_Unescape: Command_Edit_Markup_Unescape(); break;
				case TextEditCommand.Edit_RegEx_Escape: Command_Edit_RegEx_Escape(); break;
				case TextEditCommand.Edit_RegEx_Unescape: Command_Edit_RegEx_Unescape(); break;
				case TextEditCommand.Edit_URL_Escape: Command_Edit_URL_Escape(); break;
				case TextEditCommand.Edit_URL_Unescape: Command_Edit_URL_Unescape(); break;
				case TextEditCommand.Edit_URL_Absolute: Command_Edit_URL_Absolute(dialogResult as MakeAbsoluteDialog.Result); break;
				case TextEditCommand.Edit_Hash: Command_Edit_Hash(dialogResult as HashTextDialog.Result); break;
				case TextEditCommand.Edit_Sort: Command_Edit_Sort(dialogResult as SortDialog.Result); break;
				case TextEditCommand.Edit_Convert: Command_Edit_Convert(dialogResult as ConvertDialog.Result); break;
				case TextEditCommand.Edit_Bookmarks_Toggle: Command_Edit_Bookmarks_Toggle(); break;
				case TextEditCommand.Edit_Bookmarks_Next: Command_Edit_Bookmarks_NextPreviousBookmark(true, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Previous: Command_Edit_Bookmarks_NextPreviousBookmark(false, shiftDown); break;
				case TextEditCommand.Edit_Bookmarks_Clear: Command_Edit_Bookmarks_Clear(); break;
				case TextEditCommand.Files_Create_Files: Command_Files_Create_Files(); break;
				case TextEditCommand.Files_Create_Directories: Command_Files_Create_Directories(); break;
				case TextEditCommand.Files_Names_Simplify: Command_Files_Names_Simplify(); break;
				case TextEditCommand.Files_Names_MakeAbsolute: Command_Files_Names_MakeAbsolute(dialogResult as MakeAbsoluteDialog.Result); break;
				case TextEditCommand.Files_Names_GetUnique: Command_Files_Names_GetUnique(dialogResult as GetUniqueNamesDialog.Result); break;
				case TextEditCommand.Files_Names_Sanitize: Command_Files_Names_Sanitize(); break;
				case TextEditCommand.Files_Get_Size: Command_Files_Get_Size(); break;
				case TextEditCommand.Files_Get_WriteTime: Command_Files_Get_WriteTime(); break;
				case TextEditCommand.Files_Get_AccessTime: Command_Files_Get_AccessTime(); break;
				case TextEditCommand.Files_Get_CreateTime: Command_Files_Get_CreateTime(); break;
				case TextEditCommand.Files_Get_Attributes: Command_Files_Get_Attributes(); break;
				case TextEditCommand.Files_Set_Size: Command_Files_Set_Size(dialogResult as SetSizeDialog.Result); break;
				case TextEditCommand.Files_Set_WriteTime: Command_Files_Set_Time(TextEditor.TimestampType.Write, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_AccessTime: Command_Files_Set_Time(TextEditor.TimestampType.Access, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_CreateTime: Command_Files_Set_Time(TextEditor.TimestampType.Create, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_AllTimes: Command_Files_Set_Time(TextEditor.TimestampType.All, dialogResult as ChooseDateTimeDialog.Result); break;
				case TextEditCommand.Files_Set_Attributes: Command_Files_Set_Attributes(dialogResult as SetAttributesDialog.Result); break;
				case TextEditCommand.Files_Select_Name_Directory: Command_Files_Select_Name(TextEditor.GetPathType.Directory); break;
				case TextEditCommand.Files_Select_Name_Name: Command_Files_Select_Name(TextEditor.GetPathType.FileName); break;
				case TextEditCommand.Files_Select_Name_FileNamewoExtension: Command_Files_Select_Name(TextEditor.GetPathType.FileNameWoExtension); break;
				case TextEditCommand.Files_Select_Name_Extension: Command_Files_Select_Name(TextEditor.GetPathType.Extension); break;
				case TextEditCommand.Files_Select_Files: Command_Files_Select_Files(); break;
				case TextEditCommand.Files_Select_Directories: Command_Files_Select_Directories(); break;
				case TextEditCommand.Files_Select_Existing: Command_Files_Select_Existing(true); break;
				case TextEditCommand.Files_Select_NonExisting: Command_Files_Select_Existing(false); break;
				case TextEditCommand.Files_Select_Roots: Command_Files_Select_Roots(true); break;
				case TextEditCommand.Files_Select_NonRoots: Command_Files_Select_Roots(false); break;
				case TextEditCommand.Files_Hash: Command_Files_Hash(dialogResult as HashDialog.Result); break;
				case TextEditCommand.Files_Operations_Copy: Command_Files_Operations_CopyMove(false); break;
				case TextEditCommand.Files_Operations_Move: Command_Files_Operations_CopyMove(true); break;
				case TextEditCommand.Files_Operations_Delete: Command_Files_Operations_Delete(); break;
				case TextEditCommand.Files_Operations_SaveClipboards: Command_Files_Operations_SaveClipboards(); break;
				case TextEditCommand.Files_Operations_DragDrop: Command_Files_Operations_DragDrop(); break;
				case TextEditCommand.Files_Operations_OpenDisk: Command_Files_Operations_OpenDisk(); break;
				case TextEditCommand.Expression_Expression: Command_Expression_Expression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_Copy: Command_Expression_Copy(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Expression_EvaluateSelected: Command_Expression_EvaluateSelected(); break;
				case TextEditCommand.Expression_SelectByExpression: Command_Expression_SelectByExpression(dialogResult as GetExpressionDialog.Result); break;
				case TextEditCommand.Text_Copy_Length: Command_Text_Copy_Length(); break;
				case TextEditCommand.Text_Copy_Min_Text: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Copy_Min_Length: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Copy_Max_Text: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Copy_Max_Length: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Select_Trim: Command_Text_Select_Trim(); break;
				case TextEditCommand.Text_Select_ByWidth: Command_Text_Select_ByWidth(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Text_Select_Min_Text: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Select_Min_Length: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Select_Max_Text: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.String); break;
				case TextEditCommand.Text_Select_Max_Length: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.Length); break;
				case TextEditCommand.Text_Case_Upper: Command_Text_Case_Upper(); break;
				case TextEditCommand.Text_Case_Lower: Command_Text_Case_Lower(); break;
				case TextEditCommand.Text_Case_Proper: Command_Text_Case_Proper(); break;
				case TextEditCommand.Text_Case_Toggle: Command_Text_Case_Toggle(); break;
				case TextEditCommand.Text_Length: Command_Text_Length(); break;
				case TextEditCommand.Text_Width: Command_Text_Width(dialogResult as WidthDialog.Result); break;
				case TextEditCommand.Text_Trim: Command_Text_Trim(dialogResult as TrimDialog.Result); break;
				case TextEditCommand.Text_SingleLine: Command_Text_SingleLine(); break;
				case TextEditCommand.Text_GUID: Command_Text_GUID(); break;
				case TextEditCommand.Text_RandomText: Command_Text_RandomText(dialogResult as RandomDataDialog.Result); break;
				case TextEditCommand.Text_ReverseRegEx: Command_Text_ReverseRegEx(dialogResult as RevRegExDialog.Result); break;
				case TextEditCommand.Text_CombinationsPermutations: Command_Text_CombinationsPermutations(dialogResult as CombinationsPermutationsDialog.Result); break;
				case TextEditCommand.Numeric_Copy_Min: Command_Type_Copy_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Copy_Max: Command_Type_Copy_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Copy_Sum: Command_Numeric_Copy_Sum(); break;
				case TextEditCommand.Numeric_Select_Min: Command_Type_Select_MinMax(true, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Select_Max: Command_Type_Select_MinMax(false, TextEditor.Command_MinMax_Type.Numeric); break;
				case TextEditCommand.Numeric_Hex_ToHex: Command_Numeric_Hex_ToHex(); break;
				case TextEditCommand.Numeric_Hex_FromHex: Command_Numeric_Hex_FromHex(); break;
				case TextEditCommand.Numeric_Series: Command_Numeric_Series(); break;
				case TextEditCommand.Numeric_RandomNumber: Command_Numeric_RandomNumber(dialogResult as RandomNumberDialog.Result); break;
				case TextEditCommand.Numeric_MinMaxValues: Command_Numeric_MinMaxValues(dialogResult as MinMaxValuesDialog.Result); break;
				case TextEditCommand.DateTime_Now: Command_DateTime_Now(); break;
				case TextEditCommand.DateTime_Convert: Command_DateTime_Convert(dialogResult as ConvertDateTimeDialog.Result); break;
				case TextEditCommand.Position_Goto_Lines: Command_Position_Goto(GotoType.Line, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Columns: Command_Position_Goto(GotoType.Column, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Goto_Positions: Command_Position_Goto(GotoType.Position, shiftDown, dialogResult as GotoDialog.Result); break;
				case TextEditCommand.Position_Copy_Lines: Command_Position_Copy(GotoType.Line); break;
				case TextEditCommand.Position_Copy_Columns: Command_Position_Copy(GotoType.Column); break;
				case TextEditCommand.Position_Copy_Positions: Command_Position_Copy(GotoType.Position); break;
				case TextEditCommand.Content_Reformat: Command_Content_Reformat(); break;
				case TextEditCommand.Content_Comment: Command_Content_Comment(); break;
				case TextEditCommand.Content_Uncomment: Command_Content_Uncomment(); break;
				case TextEditCommand.Content_TogglePosition: Command_Content_TogglePosition(shiftDown); break;
				case TextEditCommand.Content_Parent: Command_Content_Parent(); break;
				case TextEditCommand.Content_Ancestor: Command_Content_List(ParserNode.ParserNodeListType.Parents, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Attributes_Attributes: Command_Content_List(ParserNode.ParserNodeListType.Attributes); break;
				case TextEditCommand.Content_Attributes_First: Command_Content_List(ParserNode.ParserNodeListType.Attributes, true); break;
				case TextEditCommand.Content_Attributes_ByAttribute: Command_Content_Attributes_ByAttribute(dialogResult as SelectContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Children_Children: Command_Content_List(ParserNode.ParserNodeListType.Children); break;
				case TextEditCommand.Content_Children_SelfAndChildren: Command_Content_List(ParserNode.ParserNodeListType.SelfAndChildren); break;
				case TextEditCommand.Content_Children_First: Command_Content_List(ParserNode.ParserNodeListType.Children, first: true); break;
				case TextEditCommand.Content_Children_ByAttribute: Command_Content_List(ParserNode.ParserNodeListType.Children, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Descendants_Descendants: Command_Content_List(ParserNode.ParserNodeListType.Descendants); break;
				case TextEditCommand.Content_Descendants_SelfAndDescendants: Command_Content_List(ParserNode.ParserNodeListType.SelfAndDescendants); break;
				case TextEditCommand.Content_Descendants_First: Command_Content_List(ParserNode.ParserNodeListType.Children, first: true); break;
				case TextEditCommand.Content_Descendants_ByAttribute: Command_Content_List(ParserNode.ParserNodeListType.Descendants, findAttr: dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Next: Command_Content_NextPrevious(true); break;
				case TextEditCommand.Content_Previous: Command_Content_NextPrevious(false); break;
				case TextEditCommand.Content_Select_ByAttribute: Command_Content_Select_ByAttribute(dialogResult as FindContentAttributeDialog.Result); break;
				case TextEditCommand.Content_Select_Topmost: Command_Content_Select_Topmost(); break;
				case TextEditCommand.Content_Select_Deepest: Command_Content_Select_Deepest(); break;
				case TextEditCommand.Content_Select_MaxTopmost: Command_Content_Select_MaxTopmost(); break;
				case TextEditCommand.Content_Select_MaxDeepest: Command_Content_Select_MaxDeepest(); break;
				case TextEditCommand.Network_Fetch: Command_Network_Fetch(); break;
				case TextEditCommand.Network_Lookup_IP: Command_Network_Lookup_IP(); break;
				case TextEditCommand.Network_Lookup_HostName: Command_Network_Lookup_HostName(); break;
				case TextEditCommand.Network_AdaptersInfo: Command_Network_AdaptersInfo(); break;
				case TextEditCommand.Network_Ping: Command_Network_Ping(dialogResult as PingDialog.Result); break;
				case TextEditCommand.Network_ScanPorts: Command_Network_ScanPorts(dialogResult as ScanPortsDialog.Result); break;
				case TextEditCommand.Database_Connect: Command_Database_Connect(dialogResult as DatabaseConnectDialog.Result); break;
				case TextEditCommand.Database_Execute: Command_Database_Execute(); break;
				case TextEditCommand.Database_ClearResults: Command_Database_ClearResults(); break;
				case TextEditCommand.Keys_Set_Keys: Command_Keys_Set(0); break;
				case TextEditCommand.Keys_Set_Values1: Command_Keys_Set(1); break;
				case TextEditCommand.Keys_Set_Values2: Command_Keys_Set(2); break;
				case TextEditCommand.Keys_Set_Values3: Command_Keys_Set(3); break;
				case TextEditCommand.Keys_Set_Values4: Command_Keys_Set(4); break;
				case TextEditCommand.Keys_Set_Values5: Command_Keys_Set(5); break;
				case TextEditCommand.Keys_Set_Values6: Command_Keys_Set(6); break;
				case TextEditCommand.Keys_Set_Values7: Command_Keys_Set(7); break;
				case TextEditCommand.Keys_Set_Values8: Command_Keys_Set(8); break;
				case TextEditCommand.Keys_Set_Values9: Command_Keys_Set(9); break;
				case TextEditCommand.Keys_Add_Keys: Command_Keys_Add(0); break;
				case TextEditCommand.Keys_Add_Values1: Command_Keys_Add(1); break;
				case TextEditCommand.Keys_Add_Values2: Command_Keys_Add(2); break;
				case TextEditCommand.Keys_Add_Values3: Command_Keys_Add(3); break;
				case TextEditCommand.Keys_Add_Values4: Command_Keys_Add(4); break;
				case TextEditCommand.Keys_Add_Values5: Command_Keys_Add(5); break;
				case TextEditCommand.Keys_Add_Values6: Command_Keys_Add(6); break;
				case TextEditCommand.Keys_Add_Values7: Command_Keys_Add(7); break;
				case TextEditCommand.Keys_Add_Values8: Command_Keys_Add(8); break;
				case TextEditCommand.Keys_Add_Values9: Command_Keys_Add(9); break;
				case TextEditCommand.Keys_Replace_Values1: Command_Keys_Replace(1); break;
				case TextEditCommand.Keys_Replace_Values2: Command_Keys_Replace(2); break;
				case TextEditCommand.Keys_Replace_Values3: Command_Keys_Replace(3); break;
				case TextEditCommand.Keys_Replace_Values4: Command_Keys_Replace(4); break;
				case TextEditCommand.Keys_Replace_Values5: Command_Keys_Replace(5); break;
				case TextEditCommand.Keys_Replace_Values6: Command_Keys_Replace(6); break;
				case TextEditCommand.Keys_Replace_Values7: Command_Keys_Replace(7); break;
				case TextEditCommand.Keys_Replace_Values8: Command_Keys_Replace(8); break;
				case TextEditCommand.Keys_Replace_Values9: Command_Keys_Replace(9); break;
				case TextEditCommand.Keys_Find_Keys: Command_Keys_Find(0); break;
				case TextEditCommand.Keys_Find_Values1: Command_Keys_Find(1); break;
				case TextEditCommand.Keys_Find_Values2: Command_Keys_Find(2); break;
				case TextEditCommand.Keys_Find_Values3: Command_Keys_Find(3); break;
				case TextEditCommand.Keys_Find_Values4: Command_Keys_Find(4); break;
				case TextEditCommand.Keys_Find_Values5: Command_Keys_Find(5); break;
				case TextEditCommand.Keys_Find_Values6: Command_Keys_Find(6); break;
				case TextEditCommand.Keys_Find_Values7: Command_Keys_Find(7); break;
				case TextEditCommand.Keys_Find_Values8: Command_Keys_Find(8); break;
				case TextEditCommand.Keys_Find_Values9: Command_Keys_Find(9); break;
				case TextEditCommand.Keys_Copy_Keys: Command_Keys_Copy(0); break;
				case TextEditCommand.Keys_Copy_Values1: Command_Keys_Copy(1); break;
				case TextEditCommand.Keys_Copy_Values2: Command_Keys_Copy(2); break;
				case TextEditCommand.Keys_Copy_Values3: Command_Keys_Copy(3); break;
				case TextEditCommand.Keys_Copy_Values4: Command_Keys_Copy(4); break;
				case TextEditCommand.Keys_Copy_Values5: Command_Keys_Copy(5); break;
				case TextEditCommand.Keys_Copy_Values6: Command_Keys_Copy(6); break;
				case TextEditCommand.Keys_Copy_Values7: Command_Keys_Copy(7); break;
				case TextEditCommand.Keys_Copy_Values8: Command_Keys_Copy(8); break;
				case TextEditCommand.Keys_Copy_Values9: Command_Keys_Copy(9); break;
				case TextEditCommand.Keys_Hits_Keys: Command_Keys_HitsMisses(0, true); break;
				case TextEditCommand.Keys_Hits_Values1: Command_Keys_HitsMisses(1, true); break;
				case TextEditCommand.Keys_Hits_Values2: Command_Keys_HitsMisses(2, true); break;
				case TextEditCommand.Keys_Hits_Values3: Command_Keys_HitsMisses(3, true); break;
				case TextEditCommand.Keys_Hits_Values4: Command_Keys_HitsMisses(4, true); break;
				case TextEditCommand.Keys_Hits_Values5: Command_Keys_HitsMisses(5, true); break;
				case TextEditCommand.Keys_Hits_Values6: Command_Keys_HitsMisses(6, true); break;
				case TextEditCommand.Keys_Hits_Values7: Command_Keys_HitsMisses(7, true); break;
				case TextEditCommand.Keys_Hits_Values8: Command_Keys_HitsMisses(8, true); break;
				case TextEditCommand.Keys_Hits_Values9: Command_Keys_HitsMisses(9, true); break;
				case TextEditCommand.Keys_Misses_Keys: Command_Keys_HitsMisses(0, false); break;
				case TextEditCommand.Keys_Misses_Values1: Command_Keys_HitsMisses(1, false); break;
				case TextEditCommand.Keys_Misses_Values2: Command_Keys_HitsMisses(2, false); break;
				case TextEditCommand.Keys_Misses_Values3: Command_Keys_HitsMisses(3, false); break;
				case TextEditCommand.Keys_Misses_Values4: Command_Keys_HitsMisses(4, false); break;
				case TextEditCommand.Keys_Misses_Values5: Command_Keys_HitsMisses(5, false); break;
				case TextEditCommand.Keys_Misses_Values6: Command_Keys_HitsMisses(6, false); break;
				case TextEditCommand.Keys_Misses_Values7: Command_Keys_HitsMisses(7, false); break;
				case TextEditCommand.Keys_Misses_Values8: Command_Keys_HitsMisses(8, false); break;
				case TextEditCommand.Keys_Misses_Values9: Command_Keys_HitsMisses(9, false); break;
				case TextEditCommand.Select_All: Command_Select_All(); break;
				case TextEditCommand.Select_Limit: Command_Select_Limit(dialogResult as LimitDialog.Result); break;
				case TextEditCommand.Select_Lines: Command_Select_Lines(); break;
				case TextEditCommand.Select_Empty: Command_Select_Empty(true); break;
				case TextEditCommand.Select_NonEmpty: Command_Select_Empty(false); break;
				case TextEditCommand.Select_Unique: Command_Select_Unique(); break;
				case TextEditCommand.Select_Duplicates: Command_Select_Duplicates(); break;
				case TextEditCommand.Select_ByCount: Command_Select_ByCount(dialogResult as CountDialog.Result); break;
				case TextEditCommand.Select_Regions: Command_Select_Regions(); break;
				case TextEditCommand.Select_FindResults: Command_Select_FindResults(); break;
				case TextEditCommand.Select_Selection_First: Command_Select_Selection_First(); break;
				case TextEditCommand.Select_Selection_ShowCurrent: Command_Select_Selection_ShowCurrent(); break;
				case TextEditCommand.Select_Selection_Next: Command_Select_Selection_Next(); break;
				case TextEditCommand.Select_Selection_Previous: Command_Select_Selection_Previous(); break;
				case TextEditCommand.Select_Selection_Single: Command_Select_Selection_Single(); break;
				case TextEditCommand.Select_Selection_Remove: Command_Select_Selection_Remove(); break;
				case TextEditCommand.Region_ToggleRegionsSelections: Command_Region_ToggleRegionsSelections(); break;
				case TextEditCommand.Region_SetSelection: Command_Region_SetSelection(); break;
				case TextEditCommand.Region_SetFindResults: Command_Region_SetFindResults(); break;
				case TextEditCommand.Region_ClearRegions: Command_Region_ClearRegions(); break;
				case TextEditCommand.Region_LimitToSelection: Command_Region_LimitToSelection(); break;
				case TextEditCommand.Region_SelectEnclosingRegion: Command_Region_SelectEnclosingRegion(); break;
				case TextEditCommand.Macro_RepeatLastAction: if (previous != null) HandleCommand(previous.Command, previous.ShiftDown, previous.DialogResult); break;
				case TextEditCommand.Macro_TimeNextAction: timeNext = !timeNext; break;
			}

			var end = DateTime.UtcNow;
			var elapsed = (end - start).TotalMilliseconds;

			if ((command != TextEditCommand.Macro_TimeNextAction) && (timeNext))
			{
				timeNext = false;
				new Message
				{
					Title = "Timer",
					Text = String.Format("Elapsed time: {0:n} ms", elapsed),
					Options = Message.OptionsEnum.Ok,
				}.Show();
			}
		}

		internal void Command_File_Save_Save()
		{
			if (FileName == null)
				Command_File_Save_SaveAs();
			else
				Save(FileName);
		}

		string GetSaveFileName()
		{
			var dialog = new SaveFileDialog
			{
				Filter = "All files|*.*",
				FileName = Path.GetFileName(FileName),
				InitialDirectory = Path.GetDirectoryName(FileName),
				DefaultExt = "txt",
			};
			if (dialog.ShowDialog() != true)
				return null;

			if (Directory.Exists(dialog.FileName))
				throw new Exception("A directory by that name already exists");
			if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
				throw new Exception("Directory doesn't exist");
			return dialog.FileName;
		}

		internal void Command_File_Save_SaveAs()
		{
			var fileName = GetSaveFileName();
			if (fileName != null)
				Save(fileName);
		}

		internal void Command_File_Operations_Rename()
		{
			if (String.IsNullOrEmpty(FileName))
			{
				Command_File_Save_SaveAs();
				return;
			}

			var fileName = GetSaveFileName();
			if (fileName == null)
				return;

			File.Delete(fileName);
			File.Move(FileName, fileName);
			FileName = fileName;
		}

		internal void Command_File_Refresh()
		{
			if (String.IsNullOrEmpty(FileName))
				return;
			if (fileLastWrite != new FileInfo(FileName).LastWriteTime)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "This file has been updated on disk.  Reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() == Message.OptionsEnum.Yes)
					Command_File_Revert();
			}
		}

		internal void Command_File_Revert()
		{
			if (IsModified)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			OpenFile(FileName);
		}

		internal void Command_File_Operations_Delete()
		{
			if (FileName == null)
				throw new Exception("No filename.");

			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete this file?",
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			File.Delete(FileName);
		}

		void InsertFiles(IEnumerable<string> fileNames)
		{
			if ((Selections.Count != 1) && (Selections.Count != fileNames.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			var strs = new List<string>();
			foreach (var fileName in fileNames)
			{
				var bytes = File.ReadAllBytes(fileName);
				strs.Add(Coder.BytesToString(bytes, Coder.CodePage.AutoByBOM, true));
			}

			if (Selections.Count == 1)
				ReplaceOneWithMany(strs);
			if (Selections.Count == fileNames.Count())
				ReplaceSelections(strs);
		}

		internal void Command_File_Insert_Files()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var dialog = new OpenFileDialog { DefaultExt = "txt", Filter = "Text files|*.txt|All files|*.*", FilterIndex = 2, Multiselect = true };
			if (dialog.ShowDialog() == true)
				InsertFiles(dialog.FileNames);
		}

		internal void Command_File_Insert_CopiedCut()
		{
			if (Selections.Count != 1)
			{
				new Message
				{
					Title = "Error",
					Text = "Must have one selection.",
					Options = Message.OptionsEnum.Ok,
				}.Show();
				return;
			}

			var files = Clipboard;
			if (files.Count == 0)
				return;

			if ((files.Count > 5) && (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to insert these {0} files?", files.Count),
				Options = Message.OptionsEnum.YesNoCancel,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.Cancel,
			}.Show() != Message.OptionsEnum.Yes))
				return;

			InsertFiles(files);
		}

		internal void Command_File_Copy_Path()
		{
			SetClipboardFiles(new List<string> { FileName }, false);
		}

		internal void Command_File_Copy_Name()
		{
			SetClipboard(Path.GetFileName(FileName));
		}

		internal void Command_File_Operations_Explore()
		{
			Process.Start("explorer.exe", "/select,\"" + FileName + "\"");
		}

		internal EncodingDialog.Result Command_File_Encoding_Encoding_Dialog()
		{
			return EncodingDialog.Run(WindowParent, CodePage, lineEndings: LineEnding ?? "");
		}

		internal void Command_File_Encoding_Encoding(EncodingDialog.Result result)
		{
			CodePage = result.CodePage;

			if (result.LineEndings != "")
			{
				var lines = Data.NumLines;
				var sel = new List<Range>();
				for (var line = 0; line < lines; ++line)
				{
					var current = Data.GetEnding(line);
					if ((current.Length == 0) || (current == result.LineEndings))
						continue;
					var start = Data.GetOffset(line, Data.GetLineLength(line));
					sel.Add(Range.FromIndex(start, current.Length));
				}
				Replace(sel, sel.Select(str => result.LineEndings).ToList());
			}
		}

		internal EncodingDialog.Result Command_File_Encoding_ReopenWithEncoding_Dialog()
		{
			return EncodingDialog.Run(WindowParent, CodePage);
		}

		internal void Command_File_Encoding_ReopenWithEncoding(EncodingDialog.Result result)
		{
			if (IsModified)
			{
				if (new Message
				{
					Title = "Confirm",
					Text = "You have unsaved changes.  Are you sure you want to reload?",
					Options = Message.OptionsEnum.YesNo,
					DefaultAccept = Message.OptionsEnum.Yes,
					DefaultCancel = Message.OptionsEnum.No,
				}.Show() != Message.OptionsEnum.Yes)
					return;
			}

			OpenFile(FileName, codePage: result.CodePage);
		}

		internal SymmetricKeyDialog.Result Command_File_Operations_Encryption_Dialog()
		{
			return SymmetricKeyDialog.Run(WindowParent, Cryptor.Type.AES, true);
		}

		internal void Command_File_Operations_Encryption(SymmetricKeyDialog.Result result)
		{
			AESKey = String.IsNullOrEmpty(result.Key) ? null : result.Key;
		}

		internal void Command_File_Operations_OpenDisk()
		{
			Launcher.Static.LaunchDisk(FileName);
		}

		internal bool Command_File_HexEditor()
		{
			if (!VerifyCanFullyEncode())
				return false;
			Launcher.Static.LaunchHexEditor(FileName, Data.GetBytes(CodePage), CodePage, IsModified);
			return true;
		}

		internal void Command_Edit_Undo()
		{
			var undo = undoRedo.GetUndo();
			if (undo == null)
				return;
			Selections.Replace(undo.ranges);
			ReplaceSelections(undo.text, replaceType: ReplaceType.Undo);
		}

		internal void Command_Edit_Redo()
		{
			var redo = undoRedo.GetRedo();
			if (redo == null)
				return;
			Selections.Replace(redo.ranges);
			ReplaceSelections(redo.text, replaceType: ReplaceType.Redo);
		}

		internal void Command_Edit_Copy_CutCopy(bool isCut)
		{
			var strs = GetSelectionStrings();

			if (StringsAreFiles(strs))
				SetClipboardFiles(strs, isCut);
			else
				SetClipboard(strs);
			if (isCut)
				ReplaceSelections("");
		}

		void ReplaceOneWithMany(List<string> strs)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var text = new List<string> { String.Join("", strs) };

			var offset = Selections.Single().Start;
			ReplaceSelections(text);
			Selections.Clear();
			foreach (var str in strs)
			{
				Selections.Add(Range.FromIndex(offset, str.Length - Data.DefaultEnding.Length));
				offset += str.Length;
			}
		}

		internal void Command_Edit_Paste_Paste(bool highlight)
		{
			var clipboardStrings = Clipboard.ToList();
			if (clipboardStrings.Count == 0)
				return;

			if (clipboardStrings.Count == 1)
				clipboardStrings = Selections.Select(str => clipboardStrings[0]).ToList();

			if ((Selections.Count != 1) && (Selections.Count != clipboardStrings.Count()))
				throw new Exception("Must have either one or equal number of selections.");

			if (Selections.Count == clipboardStrings.Count)
			{
				ReplaceSelections(clipboardStrings, highlight);
				return;
			}

			clipboardStrings = clipboardStrings.Select(str => str.TrimEnd('\r', '\n') + Data.DefaultEnding).ToList();
			ReplaceOneWithMany(clipboardStrings);
		}

		internal void Command_Edit_Paste_AllFiles(string str, bool highlight)
		{
			ReplaceSelections(Selections.Select(value => str).ToList(), highlight);
		}

		internal FindTextDialog.Result Command_Edit_Find_FindReplace_Dialog(bool isReplace)
		{
			string text = null;
			var selectionOnly = Selections.AsParallel().AsOrdered().Any(range => range.HasSelection);

			if (Selections.Count == 1)
			{
				var sel = Selections.Single();
				if ((selectionOnly) && (Data.GetOffsetLine(sel.Cursor) == Data.GetOffsetLine(sel.Highlight)) && (sel.Length < 1000))
				{
					selectionOnly = false;
					text = GetString(sel);
				}
			}

			return FindTextDialog.Run(WindowParent, isReplace ? FindTextDialog.FindTextType.Replace : FindTextDialog.FindTextType.Selections, text, selectionOnly);
		}

		internal void Command_Edit_Find_FindReplace(bool replace, bool selecting, FindTextDialog.Result result)
		{
			if ((result.KeepMatching) || (result.RemoveMatching))
			{
				var keep = result.KeepMatching;
				Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => result.Regex.IsMatch(GetString(range)) == keep).ToList());
				return;
			}

			RunSearch(result);

			if ((replace) || (result.ResultType == FindTextDialog.GetRegExResultType.All))
			{
				Selections.Replace(Searches);
				Searches.Clear();

				if (replace)
					ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => result.Regex.Replace(GetString(range), result.Replace)).ToList());

				return;
			}

			FindNext(true, selecting);
		}

		void SetTableSelection()
		{
			if ((Selections.Count == 0) || ((Selections.Count == 1) && (!Selections[0].HasSelection)))
				Selections.Replace(new Range(BeginOffset(), EndOffset()));
		}

		internal EditTablesDialog.Result Command_Edit_Table_Edit_Dialog()
		{
			SetTableSelection();
			return EditTablesDialog.Run(WindowParent, GetSelectionStrings(), null);
		}

		internal void Command_Edit_Table_Edit(EditTablesDialog.Result result)
		{
			SetTableSelection();
			var output = new List<string>();
			var inputs = result.Results.Select((tableResult, index) => new Table(GetString(Selections[index]), tableResult.InputTableType, tableResult.InputHasHeaders)).ToList();
			for (var ctr = 0; ctr < result.Results.Count; ++ctr)
			{
				var tableResult = result.Results[ctr];
				if (tableResult.OutputTableType == Table.TableType.None)
				{
					output.Add("");
					continue;
				}

				var outputTable = inputs[ctr];
				foreach (var joinInfo in tableResult.JoinInfos)
					outputTable = Table.Join(outputTable, inputs[joinInfo.RightTable], joinInfo.LeftColumn, joinInfo.RightColumn, joinInfo.JoinType);

				outputTable = outputTable.Aggregate(tableResult.GroupByColumns, tableResult.AggregateColumns);
				outputTable = outputTable.Sort(tableResult.SortColumns);
				output.Add(outputTable.ConvertToString(Data.DefaultEnding, tableResult.OutputTableType, tableResult.OutputHasHeaders));
			}

			ReplaceSelections(output);
		}

		internal void Command_Edit_Table_RegionsSelectionsToTable()
		{
			if (!Selections.Any())
				return;

			var regions = GetEnclosingRegions();
			var lines = Enumerable.Range(0, Selections.Count).GroupBy(index => regions[index]).Select(group => String.Join("\t", group.Select(index => Table.ToTCSV(GetString(Selections[index]), '\t')))).ToList();
			Selections.Replace(Regions);
			Regions.Clear();
			ReplaceSelections(lines);
		}

		internal void Command_Edit_Find_NextPrevious(bool next, bool selecting)
		{
			FindNext(next, selecting);
		}

		internal GotoDialog.Result Command_Position_Goto_Dialog(GotoType gotoType)
		{
			int line = 1, index = 1, position = 0;
			var range = Selections.FirstOrDefault();
			if (range != null)
			{
				line = Data.GetOffsetLine(range.Start) + 1;
				index = Data.GetOffsetIndex(range.Start, line - 1) + 1;
				position = range.Start;
			}
			int startValue;
			switch (gotoType)
			{
				case GotoType.Line: startValue = Data.GetDiffLine(line - 1) + 1; break;
				case GotoType.Column: startValue = index; break;
				case GotoType.Position: startValue = position; break;
				default: throw new ArgumentException("GotoType invalid");
			}
			return GotoDialog.Run(WindowParent, gotoType, startValue, GetExpressionData(count: 10));
		}

		internal void Command_Position_Goto(GotoType gotoType, bool selecting, GotoDialog.Result result)
		{
			var offsets = GetExpressionResults<int>(result.Expression, false, false);
			if (!offsets.Any())
				return;

			var sels = Selections.ToList();

			if ((sels.Count == 0) && (gotoType == GotoType.Line))
				sels.Add(new Range(BeginOffset()));
			if (sels.Count == 1)
				sels = sels.Resize(offsets.Count, sels[0]).ToList();
			if (offsets.Count == 1)
				offsets = offsets.Expand(sels.Count, offsets[0]).ToList();
			if (offsets.Count != sels.Count)
				throw new Exception("Expression count doesn't match selection count");

			if (gotoType != GotoType.Position)
				offsets = offsets.Select(ofs => ofs - 1).ToList();

			switch (gotoType)
			{
				case GotoType.Line:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, Data.GetNonDiffLine(offsets[ctr]), 0, selecting, false, false)).ToList());
					break;
				case GotoType.Column:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, 0, offsets[ctr], selecting, true, false)).ToList());
					break;
				case GotoType.Position:
					Selections.Replace(sels.AsParallel().AsOrdered().Select((range, ctr) => MoveCursor(range, offsets[ctr], selecting)).ToList());
					break;
			}
		}

		internal void Command_Edit_Bookmarks_Toggle()
		{
			var linePairs = Selections.AsParallel().AsOrdered().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End) }).ToList();
			if (linePairs.Any(pair => pair.start != pair.end))
				throw new Exception("Selections must be on a single line.");

			var lineRanges = linePairs.AsParallel().AsOrdered().Select(pair => new Range(Data.GetOffset(pair.start, 0))).ToList();
			var comparer = Comparer<Range>.Create((r1, r2) => r1.Start.CompareTo(r2.Start));
			var indexes = lineRanges.AsParallel().Select(range => new { range = range, index = Bookmarks.BinarySearch(range, comparer) }).Reverse().ToList();

			if (indexes.Any(index => index.index < 0))
			{
				foreach (var pair in indexes)
					if (pair.index < 0)
						Bookmarks.Insert(~pair.index, pair.range);
			}
			else
			{
				foreach (var pair in indexes)
					Bookmarks.RemoveAt(pair.index);
			}
		}

		Range GetNextPrevBookmark(Range range, bool next, bool selecting)
		{
			int index;
			if (next)
			{
				index = Bookmarks.BinaryFindFirst(r => r.Start > range.Cursor);
				if (index == -1)
					index = 0;
			}
			else
			{
				index = Bookmarks.BinaryFindLast(r => r.Start < range.Cursor);
				if (index == -1)
					index = Bookmarks.Count - 1;
			}
			return MoveCursor(range, Bookmarks[index].Start, selecting);
		}

		internal void Command_Edit_Bookmarks_NextPreviousBookmark(bool next, bool selecting)
		{
			if (!Bookmarks.Any())
				return;
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetNextPrevBookmark(range, next, selecting)).ToList());
		}

		internal void Command_Edit_Bookmarks_Clear()
		{
			Bookmarks.Clear();
		}

		internal void Command_File_Open_Selected()
		{
			var files = GetSelectionStrings();
			foreach (var file in files)
				TextEditTabs.Create(file);
		}

		internal void Command_File_Insert_Selected()
		{
			InsertFiles(GetSelectionStrings());
		}

		internal void Command_Files_Operations_SaveClipboards()
		{
			var clipboardStrings = Clipboard;
			if (clipboardStrings.Count != Selections.Count)
				throw new Exception("Clipboard count must match selection count.");

			for (var ctr = 0; ctr < clipboardStrings.Count; ++ctr)
			{
				var fileName = GetString(Selections[ctr]);
				var data = clipboardStrings[ctr];
				File.WriteAllText(fileName, data, Coder.GetEncoding(CodePage));
			}
		}

		internal void Command_Files_Create_Files()
		{
			var files = GetSelectionStrings();
			if (files.Any(file => Directory.Exists(file)))
				throw new Exception("Directory already exists");
			files = files.Where(file => !File.Exists(file)).ToList();
			var data = new byte[0];
			foreach (var file in files)
				File.WriteAllBytes(file, data);
		}

		[Flags]
		internal enum TimestampType
		{
			Write = 1,
			Access = 2,
			Create = 4,
			All = Write | Access | Create,
		}

		internal void Command_Files_Create_Directories()
		{
			var files = GetSelectionStrings();
			foreach (var file in files)
				Directory.CreateDirectory(file);
		}

		internal void Command_Files_Operations_Delete()
		{
			if (new Message
			{
				Title = "Confirm",
				Text = "Are you sure you want to delete these files/directories?",
				Options = Message.OptionsEnum.YesNo,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() == Message.OptionsEnum.Yes)
			{
				var files = GetSelectionStrings();
				foreach (var file in files)
				{
					if (File.Exists(file))
						File.Delete(file);
					if (Directory.Exists(file))
						Directory.Delete(file, true);
				}
			}
		}

		internal void Command_Files_Operations_DragDrop()
		{
			var strs = GetSelectionStrings();
			if (!StringsAreFiles(strs))
				throw new Exception("Selections must be files.");
			doDrag = true;
		}

		internal void Command_Files_Names_Simplify()
		{
			ReplaceSelections(Selections.Select(range => Path.GetFullPath(GetString(range))).ToList());
		}

		internal MakeAbsoluteDialog.Result Command_Files_Names_MakeAbsolute_Dialog()
		{
			return MakeAbsoluteDialog.Run(WindowParent, GetExpressionData(count: 10), true);
		}

		internal void Command_Files_Names_MakeAbsolute(MakeAbsoluteDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) => new Uri(new Uri(results[index] + (result.Type == MakeAbsoluteDialog.ResultType.Directory ? "\\" : "")), str).LocalPath).ToList());
		}

		internal GetUniqueNamesDialog.Result Command_Files_Names_GetUnique_Dialog()
		{
			return GetUniqueNamesDialog.Run(WindowParent);
		}

		internal void Command_Files_Names_GetUnique(GetUniqueNamesDialog.Result result)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (!result.Format.Contains("{Unique}"))
				throw new Exception("Format must contain \"{Unique}\" tag");
			var newNames = new List<string>();
			var format = result.Format.Replace("{Path}", "{0}").Replace("{Name}", "{1}").Replace("{Unique}", "{2}").Replace("{Ext}", "{3}");
			foreach (var fileName in GetSelectionStrings())
			{
				var path = Path.GetDirectoryName(fileName);
				if (!String.IsNullOrEmpty(path))
					path += @"\";
				var name = Path.GetFileNameWithoutExtension(fileName);
				var ext = Path.GetExtension(fileName);
				var newFileName = fileName;
				for (var num = result.RenameAll ? 1 : 2; ; ++num)
				{
					if ((result.CheckExisting) && (FileOrDirectoryExists(newFileName)))
						used.Add(newFileName);
					if (((num != 1) || (!result.RenameAll)) && (!used.Contains(newFileName)))
						break;
					var unique = result.UseGUIDs ? Guid.NewGuid().ToString() : num.ToString();

					newFileName = String.Format(format, path, name, unique, ext);
				}
				newNames.Add(newFileName);
				used.Add(newFileName);
			}

			ReplaceSelections(newNames);
		}

		string SanitizeFileName(string fileName)
		{
			fileName = fileName.Trim();
			var start = "";
			if ((fileName.Length >= 2) && (fileName[1] == ':'))
				start = fileName.Substring(0, 2);
			fileName = fileName.Replace("/", @"\");
			fileName = Regex.Replace(fileName, "[<>:\"|?*\u0000-\u001f]", "_");
			fileName = start + fileName.Substring(start.Length);
			return fileName;
		}

		internal void Command_Files_Names_Sanitize()
		{
			ReplaceSelections(Selections.Select(range => SanitizeFileName(GetString(range))).ToList());
		}

		string GetSize(string path)
		{
			if (File.Exists(path))
			{
				var fileinfo = new FileInfo(path);
				return fileinfo.Length.ToString();
			}

			if (Directory.Exists(path))
			{
				var dirs = new List<string> { path };
				for (var ctr = 0; ctr < dirs.Count; ++ctr)
					dirs.AddRange(Directory.EnumerateDirectories(dirs[ctr]));
				int files = 0;
				long totalSize = 0;
				foreach (var dir in dirs)
				{
					foreach (var file in Directory.EnumerateFiles(dir))
					{
						++files;
						var fileinfo = new FileInfo(file);
						totalSize += fileinfo.Length;
					}
				}

				return String.Format("{0} directories, {1} files, {2} bytes", dirs.Count, files, totalSize);
			}

			return "INVALID";
		}

		internal void Command_Files_Get_Size()
		{
			ReplaceSelections(Selections.Select(range => GetSize(GetString(range))).ToList());
		}

		internal void Command_Files_Get_WriteTime()
		{
			var files = GetSelectionStrings();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_AccessTime()
		{
			var files = GetSelectionStrings();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_CreateTime()
		{
			var files = GetSelectionStrings();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal void Command_Files_Get_Attributes()
		{
			var files = GetSelectionStrings();
			var strs = new List<string>();
			foreach (var file in files)
			{
				if (File.Exists(file))
				{
					var fileinfo = new FileInfo(file);
					strs.Add(fileinfo.Attributes.ToString());
				}
				else if (Directory.Exists(file))
				{
					var dirinfo = new DirectoryInfo(file);
					strs.Add(dirinfo.Attributes.ToString());
				}
				else
					strs.Add("INVALID");
			}
			ReplaceSelections(strs);
		}

		internal SetSizeDialog.Result Command_Files_Set_Size_Dialog()
		{
			return SetSizeDialog.Run(WindowParent, GetExpressionData(count: 10));
		}

		void SetFileSize(string fileName, SetSizeDialog.SizeType type, long value)
		{
			var fileInfo = new FileInfo(fileName);
			if (!fileInfo.Exists)
				throw new Exception(String.Format("File doesn't exist: {0}", fileName));

			long length;
			switch (type)
			{
				case SetSizeDialog.SizeType.Absolute: length = value; break;
				case SetSizeDialog.SizeType.Relative: length = fileInfo.Length + value; break;
				case SetSizeDialog.SizeType.Minimum: length = Math.Max(fileInfo.Length, value); break;
				case SetSizeDialog.SizeType.Maximum: length = Math.Min(fileInfo.Length, value); break;
				case SetSizeDialog.SizeType.Multiple: length = fileInfo.Length + value - 1 - (fileInfo.Length + value - 1) % value; break;
				default: throw new ArgumentException("Invalid width type");
			}

			length = Math.Max(0, length);

			if (fileInfo.Length == length)
				return;

			using (var file = File.Open(fileName, FileMode.Open))
				file.SetLength(length);
		}

		internal void Command_Files_Set_Size(SetSizeDialog.Result result)
		{
			var results = GetExpressionResults<long>(result.Expression).Select(size => size * result.Factor).ToList();
			for (var ctr = 0; ctr < Selections.Count; ++ctr)
				SetFileSize(GetString(Selections[ctr]), result.Type, results[ctr]);
		}

		internal ChooseDateTimeDialog.Result Command_Files_Set_Time_Dialog()
		{
			return ChooseDateTimeDialog.Run(WindowParent, DateTime.Now);
		}

		internal void Command_Files_Set_Time(TimestampType type, ChooseDateTimeDialog.Result result)
		{
			var files = GetSelectionStrings();
			foreach (var file in files)
			{
				if (!FileOrDirectoryExists(file))
					File.WriteAllBytes(file, new byte[0]);

				if (File.Exists(file))
				{
					var info = new FileInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
				else if (Directory.Exists(file))
				{
					var info = new DirectoryInfo(file);
					if (type.HasFlag(TimestampType.Write))
						info.LastWriteTime = result.Value;
					if (type.HasFlag(TimestampType.Access))
						info.LastAccessTime = result.Value;
					if (type.HasFlag(TimestampType.Create))
						info.CreationTime = result.Value;
				}
			}
		}

		internal SetAttributesDialog.Result Command_Files_Set_Attributes_Dialog()
		{
			var filesAttrs = Selections.Select(range => GetString(range)).Select(file => new DirectoryInfo(file).Attributes).ToList();
			var availAttrs = Helpers.GetValues<FileAttributes>();
			var current = new Dictionary<FileAttributes, bool?>();
			foreach (var fileAttrs in filesAttrs)
				foreach (var availAttr in availAttrs)
				{
					var fileHasAttr = fileAttrs.HasFlag(availAttr);
					if (!current.ContainsKey(availAttr))
						current[availAttr] = fileHasAttr;
					if (current[availAttr] != fileHasAttr)
						current[availAttr] = null;
				}

			return SetAttributesDialog.Run(WindowParent, current);
		}

		internal void Command_Files_Set_Attributes(SetAttributesDialog.Result result)
		{
			FileAttributes andMask = 0, orMask = 0;
			foreach (var pair in result.Attributes)
			{
				andMask |= pair.Key;
				if ((pair.Value.HasValue) && (pair.Value.Value))
					orMask |= pair.Key;
			}
			foreach (var file in Selections.Select(range => GetString(range)))
				new FileInfo(file).Attributes = new FileInfo(file).Attributes & ~andMask | orMask;
		}

		internal void Command_Files_Select_Name(GetPathType type)
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => GetPathRange(type, range)).ToList());
		}

		static bool FileOrDirectoryExists(string name)
		{
			return (Directory.Exists(name)) || (File.Exists(name));
		}

		internal void Command_Files_Select_Existing(bool existing)
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => FileOrDirectoryExists(GetString(range)) == existing).ToList());
		}

		internal void Command_Files_Select_Files()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => File.Exists(GetString(range))).ToList());
		}

		internal void Command_Files_Select_Directories()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => Directory.Exists(GetString(range))).ToList());
		}

		internal void Command_Files_Select_Roots(bool include)
		{
			var sels = Selections.Select(range => new { range = range, str = GetString(range).ToLower().Replace(@"\\", @"\").TrimEnd('\\') }).ToList();
			var files = sels.Select(obj => obj.str).Distinct().OrderBy(str => str).ToList();
			var roots = new HashSet<string>();
			string root = null;
			foreach (var file in files)
			{
				if ((root != null) && (file.StartsWith(root)))
					continue;

				roots.Add(file);
				root = file + @"\";
			}

			Selections.Replace(sels.AsParallel().AsOrdered().Where(sel => roots.Contains(sel.str) == include).Select(sel => sel.range).ToList());
		}

		internal HashDialog.Result Command_Files_Hash_Dialog()
		{
			return HashDialog.Run(WindowParent);
		}

		internal void Command_Files_Hash(HashDialog.Result result)
		{
			ReplaceSelections(Selections.Select(range => Hasher.Get(GetString(range), result.HashType)).ToList());
		}

		internal void Command_Files_Operations_CopyMove(bool move)
		{
			var strs = Selections.Select(range => GetString(range).Split(new string[] { "=>" }, StringSplitOptions.None).Select(str => str.Trim()).ToList()).ToList();
			if (strs.Any(pair => pair.Count != 2))
				throw new Exception("Format: Source => Destination");

			var sels = strs.Select(pair => new { source = pair[0], dest = pair[1] }).Where(pair => pair.source != pair.dest).ToList();
			if (sels.Count == 0)
				throw new Exception("Nothing to do!");

			if (sels.Any(pair => (String.IsNullOrEmpty(pair.source)) || (String.IsNullOrEmpty(pair.dest))))
				throw new Exception("Can't have empty items in list");

			const int invalidCount = 10;
			var invalid = sels.Select(pair => pair.source).Concat(sels.Select(pair => pair.dest)).GroupBy(str => str).Where(group => group.Count() > 1).Select(group => group.Key).Distinct().Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Some items are listed more than once:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => pair.source).Where(file => !FileOrDirectoryExists(file)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Source file/directory doesn't exist:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => pair.dest).Where(pair => FileOrDirectoryExists(pair)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Destination file/directory already exist:\n{0}", String.Join("\n", invalid)));

			invalid = sels.Select(pair => Path.GetDirectoryName(pair.dest)).Distinct().Where(dir => !Directory.Exists(dir)).Take(invalidCount);
			if (invalid.Any())
				throw new Exception(String.Format("Directory doesn't exist:\n{0}", String.Join("\n", invalid)));

			if (new Message
			{
				Title = "Confirm",
				Text = String.Format("Are you sure you want to " + (move ? "move" : "copy") + " these {0} file(s)?", sels.Count),
				Options = Message.OptionsEnum.YesNo,
				DefaultAccept = Message.OptionsEnum.Yes,
				DefaultCancel = Message.OptionsEnum.No,
			}.Show() != Message.OptionsEnum.Yes)
				return;

			foreach (var pair in sels)
				if (Directory.Exists(pair.source))
				{
					if (move)
						Directory.Move(pair.source, pair.dest);
					else
						CopyDirectory(pair.source, pair.dest);
				}
				else
				{
					if (move)
						File.Move(pair.source, pair.dest);
					else
						File.Copy(pair.source, pair.dest);
				}
		}

		internal void Command_Files_Operations_OpenDisk()
		{
			Launcher.Static.LaunchDisk(files: Selections.Select(range => GetString(range)));
		}

		internal void Command_Text_Case_Upper()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToUpperInvariant()).ToList());
		}

		internal void Command_Text_Case_Lower()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToLowerInvariant()).ToList());
		}

		internal void Command_Text_Case_Proper()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToProper()).ToList());
		}

		internal void Command_Text_Case_Toggle()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).ToToggled()).ToList());
		}

		internal void Command_Numeric_Hex_ToHex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Int64.Parse(GetString(range)).ToString("x")).ToList());
		}

		internal void Command_Numeric_Hex_FromHex()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Int64.Parse(GetString(range), NumberStyles.HexNumber).ToString()).ToList());
		}

		internal void Command_DateTime_Now()
		{
			ReplaceSelections(DateTime.Now.ToString("O"));
		}

		internal ConvertDateTimeDialog.Result Command_DateTime_Convert_Dialog()
		{
			if (Selections.Count < 1)
				return null;

			return ConvertDateTimeDialog.Run(WindowParent, GetString(Selections.First()));
		}

		internal void Command_DateTime_Convert(ConvertDateTimeDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => ConvertDateTimeDialog.ConvertFormat(GetString(range), result.InputFormat, result.InputUTC, result.OutputFormat, result.OutputUTC)).ToList());
		}

		internal ConvertDialog.Result Command_Edit_Convert_Dialog()
		{
			return ConvertDialog.Run(WindowParent);
		}

		internal void Command_Edit_Convert(ConvertDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, num) => Coder.BytesToString(Coder.StringToBytes(GetString(range), result.InputType, result.InputBOM), result.OutputType, result.OutputBOM)).ToList());
		}

		internal void Command_Text_Length()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => range.Length.ToString()).ToList());
		}

		internal WidthDialog.Result Command_Text_Width_Dialog()
		{
			var minLength = Selections.Any() ? Selections.AsParallel().Min(range => range.Length) : 0;
			var maxLength = Selections.Any() ? Selections.AsParallel().Max(range => range.Length) : 0;
			var numeric = Selections.Any() ? Selections.AsParallel().All(range => GetString(range).IsNumeric()) : false;
			return WidthDialog.Run(WindowParent, minLength, maxLength, numeric, false, GetExpressionData(count: 10));
		}

		internal void Command_Text_Width(WidthDialog.Result result)
		{
			var results = GetExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => SetWidth(GetString(range), result, results[index])).ToList());
		}

		internal TrimDialog.Result Command_Text_Trim_Dialog()
		{
			var numeric = Selections.AsParallel().All(range => GetString(range).IsNumeric());
			return TrimDialog.Run(WindowParent, numeric);
		}

		string TrimString(string str, TrimDialog.Result result)
		{
			switch (result.Location)
			{
				case TrimDialog.TrimLocation.Start: return str.TrimStart(result.TrimChars);
				case TrimDialog.TrimLocation.Both: return str.Trim(result.TrimChars);
				case TrimDialog.TrimLocation.End: return str.TrimEnd(result.TrimChars);
				default: throw new Exception("Invalid location");
			}
		}

		internal void Command_Text_Trim(TrimDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(str => TrimString(GetString(str), result)).ToList());
		}

		internal void Command_Text_SingleLine()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => GetString(range).Replace("\r", "").Replace("\n", "")).ToList());
		}

		internal GetExpressionDialog.Result Command_Expression_Expression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		internal void Command_Expression_Expression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression);
			ReplaceSelections(results);
		}

		internal void Command_Expression_Copy(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression);
			SetClipboard(results);
		}

		internal void Command_Expression_EvaluateSelected()
		{
			var expression = new NEExpression("Eval([0])");
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => expression.Evaluate(GetString(range)).ToString()).ToList());
		}

		internal void Command_Numeric_Series()
		{
			ReplaceSelections(Selections.Select((range, index) => (index + 1).ToString()).ToList());
		}

		internal void Command_Edit_CopyDown()
		{
			var strs = GetSelectionStrings();
			var index = 0;
			for (var ctr = 0; ctr < strs.Count; ++ctr)
				if (String.IsNullOrWhiteSpace(strs[ctr]))
					strs[ctr] = strs[index];
				else
					index = ctr;
			ReplaceSelections(strs);
		}

		internal void Command_File_Copy_Count()
		{
			SetClipboard(Selections.Count);
		}

		internal void Command_Text_Copy_Length()
		{
			SetClipboard(Selections.Select(range => range.Length.ToString()).ToList());
		}

		internal enum Command_MinMax_Type { String, Numeric, Length }
		internal void DoCommand_Type_Copy_MinMax<T>(bool min, Func<Range, T> sortBy, Func<Range, string> value)
		{
			if (!Selections.Any())
				throw new Exception("No selections");
			var strs = Selections.AsParallel().AsOrdered().Select(range => new { range = range, sort = sortBy(range) }).OrderBy(obj => obj.sort).ToList();
			var found = min ? strs.First().range : strs.Last().range;
			SetClipboard(value(found));
		}

		internal void Command_Type_Copy_MinMax(bool min, Command_MinMax_Type type)
		{
			switch (type)
			{
				case Command_MinMax_Type.String: DoCommand_Type_Copy_MinMax(min, range => GetString(range), range => GetString(range)); break;
				case Command_MinMax_Type.Numeric: DoCommand_Type_Copy_MinMax(min, range => NumericSort(GetString(range)), range => GetString(range)); break;
				case Command_MinMax_Type.Length: DoCommand_Type_Copy_MinMax(min, range => range.Length, range => range.Length.ToString()); break;
			}
		}

		internal void Command_Numeric_Copy_Sum()
		{
			SetClipboard(Selections.AsParallel().Select(range => Double.Parse(GetString(range))).Sum());
		}

		internal void Command_Position_Copy(GotoType gotoType)
		{
			if (gotoType == GotoType.Position)
			{
				SetClipboard(Selections.Select(range => range.Start.ToString() + (range.HasSelection ? "-" + range.End : "")).ToList());
				return;
			}

			var starts = Selections.Select(range => range.Start).ToList();
			var lines = starts.Select(pos => Data.GetOffsetLine(pos)).ToList();
			if (gotoType == GotoType.Line)
			{
				SetClipboard(lines.Select(pos => (Data.GetDiffLine(pos) + 1).ToString()).ToList());
				return;
			}

			var indexes = starts.Select((pos, line) => Data.GetOffsetIndex(pos, lines[line])).ToList();
			SetClipboard(indexes.Select(pos => (pos + 1).ToString()).ToList());
		}

		internal RepeatDialog.Result Command_Edit_Repeat_Dialog()
		{
			return RepeatDialog.Run(WindowParent, Selections.Count == 1, GetExpressionData(count: 10));
		}

		internal void Command_Edit_Repeat(RepeatDialog.Result result)
		{
			var results = GetExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => RepeatString(GetString(range), results[index])).ToList());
			if (result.SelectRepetitions)
			{
				var sels = new List<Range>();
				for (var ctr = 0; ctr < Selections.Count; ++ctr)
				{
					var selection = Selections[ctr];
					var repeatCount = results[ctr];
					var len = selection.Length / repeatCount;
					for (var index = selection.Start; index < selection.End; index += len)
						sels.Add(new Range(index + len, index));
				}
				Selections.Replace(sels);
			}
		}

		internal void Command_Edit_Markup_Escape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlEncode(GetString(range))).ToList());
		}

		internal void Command_Edit_RegEx_Escape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Escape(GetString(range))).ToList());
		}

		internal void Command_Edit_URL_Escape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlEncode(GetString(range))).ToList());
		}

		internal void Command_Edit_Markup_Unescape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.HtmlDecode(GetString(range))).ToList());
		}

		internal void Command_Edit_RegEx_Unescape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => Regex.Unescape(GetString(range))).ToList());
		}

		internal void Command_Edit_URL_Unescape()
		{
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select(range => HttpUtility.UrlDecode(GetString(range))).ToList());
		}

		internal HashTextDialog.Result Command_Edit_Hash_Dialog()
		{
			return HashTextDialog.Run(WindowParent, CodePage);
		}

		internal void Command_Edit_Hash(HashTextDialog.Result result)
		{
			var strs = GetSelectionStrings();
			if (!VerifyCanFullyEncode(strs, result.CodePage))
				return;

			ReplaceSelections(strs.AsParallel().AsOrdered().Select(str => Hasher.Get(Coder.StringToBytes(str, result.CodePage), result.HashType)).ToList());
		}

		async Task<string> GetURL(string url)
		{
			using (var client = new WebClient())
				return await client.DownloadStringTaskAsync(new Uri(url));
		}

		async Task<List<Tuple<string, string, bool>>> GetURLs(List<string> urls)
		{
			var tasks = urls.Select(url => GetURL(url)).ToList();
			var results = new List<Tuple<string, string, bool>>();
			for (var ctr = 0; ctr < tasks.Count; ++ctr)
			{
				string data;
				bool error = false;
				try { data = await tasks[ctr]; }
				catch (Exception ex)
				{
					error = true;
					data = "<error>" + Data.DefaultEnding;
					data += "\t<url>" + urls[ctr] + "</url>" + Data.DefaultEnding;
					data += "\t<data>" + Data.DefaultEnding;
					for (; ex != null; ex = ex.InnerException)
						data += "\t\t" + ex.Message + Data.DefaultEnding;
					data += "\t</data>" + Data.DefaultEnding;
					data += "</error>" + Data.DefaultEnding;
				}
				results.Add(Tuple.Create(urls[ctr], data, error));
			}
			return results;
		}

		internal MakeAbsoluteDialog.Result Command_Edit_URL_Absolute_Dialog()
		{
			return MakeAbsoluteDialog.Run(WindowParent, GetExpressionData(count: 10), false);
		}

		internal void Command_Edit_URL_Absolute(MakeAbsoluteDialog.Result result)
		{
			var results = GetExpressionResults<string>(result.Expression);
			ReplaceSelections(GetSelectionStrings().Select((str, index) =>
			{
				var uri = new Uri(new Uri(results[index]), str);
				return uri.AbsoluteUri;
			}).ToList());
		}

		internal void Command_Edit_Diff_Break()
		{
			DiffTarget = null;
		}

		Tuple<int, int> GetDiffNextPrevious(Range range, bool next)
		{
			var offset = next ? range.End : Math.Max(0, range.Start - 1);
			var line = Data.GetOffsetLine(offset);
			var delta = next ? 1 : -1;
			int? start = null;
			while (true)
			{
				line += delta;
				if ((line < 0) || (line >= Data.NumLines) || ((start.HasValue) && (Data.GetLineDiffStatus(line) == LCS.MatchType.Match)))
				{
					line = Math.Max(-1, Math.Min(line, Data.NumLines - 1));
					if (!start.HasValue)
						start = line;
					if (next)
						return Tuple.Create(start.Value, line);
					return Tuple.Create(line + 1, start.Value + 1);
				}
				if ((!start.HasValue) && (Data.GetLineDiffStatus(line) != LCS.MatchType.Match))
					start = line;
			}
		}

		internal void Command_Edit_Diff_NextPrevious(bool next)
		{
			if (DiffTarget == null)
				return;

			var lines = Selections.AsParallel().AsOrdered().Select(range => GetDiffNextPrevious(range, next)).ToList();
			for (var pass = 0; pass < 2; ++pass)
			{
				var target = pass == 0 ? this : DiffTarget;
				target.Selections.Replace(lines.Select(tuple => new Range(target.Data.GetOffset(tuple.Item1, 0), target.Data.GetOffset(tuple.Item2, 0))).ToList());
			}
		}

		internal void Command_Edit_Diff_CopyLeftRight(bool moveLeft)
		{
			if (DiffTarget == null)
				return;

			TextEditor left, right;
			if (TabsParent.GetIndex(this) < DiffTarget.TabsParent.GetIndex(DiffTarget))
			{
				left = this;
				right = DiffTarget;
			}
			else
			{
				left = DiffTarget;
				right = this;
			}

			if (moveLeft)
				left.ReplaceSelections(right.GetSelectionStrings());
			else
				right.ReplaceSelections(left.GetSelectionStrings());
		}

		internal void Command_Text_GUID()
		{
			ReplaceSelections(Selections.AsParallel().Select(range => Guid.NewGuid().ToString()).ToList());
		}

		internal RandomNumberDialog.Result Command_Numeric_RandomNumber_Dialog()
		{
			return RandomNumberDialog.Run(WindowParent);
		}

		internal void Command_Numeric_RandomNumber(RandomNumberDialog.Result result)
		{
			ReplaceSelections(Selections.AsParallel().Select(range => random.Next(result.MinValue, result.MaxValue + 1).ToString()).ToList());
		}

		internal RandomDataDialog.Result Command_Text_RandomText_Dialog()
		{
			return RandomDataDialog.Run(GetExpressionData(count: 10), WindowParent);
		}

		string GetRandomData(string chars, int length)
		{
			return new string(Enumerable.Range(0, length).Select(num => chars[random.Next(chars.Length)]).ToArray());
		}

		internal void Command_Text_RandomText(RandomDataDialog.Result result)
		{
			var results = GetExpressionResults<int>(result.Expression);
			ReplaceSelections(Selections.AsParallel().AsOrdered().Select((range, index) => GetRandomData(result.Chars, results[index])).ToList());
		}

		internal MinMaxValuesDialog.Result Command_Numeric_MinMaxValues_Dialog()
		{
			return MinMaxValuesDialog.Run(WindowParent);
		}

		internal void Command_Numeric_MinMaxValues(MinMaxValuesDialog.Result result)
		{
			ReplaceSelections(String.Join(" ", new List<string> { result.Min ? result.CodePage.MinValue() : null, result.Max ? result.CodePage.MaxValue() : null }.Where(str => !String.IsNullOrEmpty(str))));
		}

		internal CombinationsPermutationsDialog.Result Command_Text_CombinationsPermutations_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return CombinationsPermutationsDialog.Run(WindowParent);
		}

		internal void Command_Text_CombinationsPermutations(CombinationsPermutationsDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var output = new List<string>();
			var nums = new int[result.UseCount];
			var used = new bool[result.Items.Length + 1];
			var onNum = 0;
			while (true)
			{
				++nums[onNum];
				if (nums[onNum] > result.Items.Length)
				{
					--onNum;
					if (onNum < 0)
						break;
					used[nums[onNum]] = false;
					continue;
				}
				if ((!result.Repeat) && (used[nums[onNum]]))
					continue;

				used[nums[onNum]] = true;
				++onNum;
				if (onNum < result.UseCount)
				{
					if (result.Type == CombinationsPermutationsDialog.CombinationsPermutationsType.Combinations)
						nums[onNum] = nums[onNum - 1] - 1;
					else
						nums[onNum] = 0;
				}
				else
				{
					output.Add(String.Concat(nums.Select(num => result.Items[num - 1])) + Data.DefaultEnding);
					--onNum;
					used[nums[onNum]] = false;
				}
			}

			ReplaceSelections(String.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - Data.DefaultEnding.Length));
				start += str.Length;
			}
			Selections.Replace(sels);
		}

		internal RevRegExDialog.Result Command_Text_ReverseRegEx_Dialog()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			return RevRegExDialog.Run(WindowParent);
		}

		internal void Command_Text_ReverseRegEx(RevRegExDialog.Result result)
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = RevRegEx.RevRegExVisitor.Parse(result.RegEx);
			var output = data.GetPossibilities().Select(str => str + Data.DefaultEnding).ToList();
			ReplaceSelections(String.Join("", output));

			var start = Selections.Single().Start;
			var sels = new List<Range>();
			foreach (var str in output)
			{
				sels.Add(Range.FromIndex(start, str.Length - Data.DefaultEnding.Length));
				start += str.Length;
			}
			Selections.Replace(sels);
		}

		internal void Command_Network_Fetch()
		{
			var urls = GetSelectionStrings();
			var results = Task.Run(() => GetURLs(urls).Result).Result;
			if (results.Any(result => result.Item3))
				new Message
				{
					Title = "Error",
					Text = "Failed to fetch the URLs:\n" + String.Join("\n", results.Where(result => result.Item3).Select(result => result.Item1)),
					Options = Message.OptionsEnum.Ok,
				}.Show();
			ReplaceSelections(results.Select(result => result.Item2).ToList());
		}

		internal void Command_Network_Lookup_IP()
		{
			ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return String.Join(" / ", (await Dns.GetHostEntryAsync(name)).AddressList.Select(address => address.ToString()).Distinct()); } catch { return "<ERROR>"; } }).ToList())).Result.ToList());
		}

		internal void Command_Network_Lookup_HostName()
		{
			ReplaceSelections(Task.Run(async () => await Task.WhenAll(GetSelectionStrings().Select(async name => { try { return (await Dns.GetHostEntryAsync(name)).HostName; } catch { return "<ERROR>"; } }).ToList())).Result.ToList());
		}

		internal void Command_Network_AdaptersInfo()
		{
			if (Selections.Count != 1)
				throw new Exception("Must have one selection.");

			var data = new List<List<string>>();
			data.Add(new List<string>
			{
				"Name",
				"Desc",
				"Status",
				"Type",
				"IPs"
			});
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().OrderBy(inter => inter.OperationalStatus).ThenBy(inter => inter.Name))
			{
				var props = networkInterface.GetIPProperties();
				data.Add(new List<string>
				{
					networkInterface.Name,
					networkInterface.Description,
					networkInterface.OperationalStatus.ToString(),
					networkInterface.NetworkInterfaceType.ToString(),
					String.Join(" / ", props.UnicastAddresses.Select(info=>info.Address)),
				});
			}
			var columnLens = data[0].Select((item, column) => data.Max(row => row[column].Length)).ToList();
			ReplaceOneWithMany(data.Select(row => String.Join("│", row.Select((item, column) => item + new string(' ', columnLens[column] - item.Length))) + Data.DefaultEnding).ToList());
		}

		internal PingDialog.Result Command_Network_Ping_Dialog()
		{
			return PingDialog.Run(WindowParent);
		}

		internal void Command_Network_Ping(PingDialog.Result result)
		{
			var replies = Task.Run(async () =>
			{
				var strs = GetSelectionStrings().Select(async str =>
				{
					try
					{
						using (var ping = new Ping())
						{
							var reply = await ping.SendPingAsync(IPAddress.Parse(str), result.Timeout);
							return str + ": " + reply.Status.ToString() + (reply.Status == IPStatus.Success ? ": " + reply.RoundtripTime + " ms" : "");
						}
					}
					catch (Exception ex)
					{
						return str + ": " + ex.Message;
					}
				}).ToList();
				return await Task.WhenAll(strs);
			}).Result.ToList();
			ReplaceSelections(replies);
		}

		internal ScanPortsDialog.Result Command_Network_ScanPorts_Dialog()
		{
			return ScanPortsDialog.Run(WindowParent);
		}

		internal void Command_Network_ScanPorts(ScanPortsDialog.Result result)
		{
			var strs = GetSelectionStrings();
			var results = PortScanner.ScanPorts(strs.Select(str => IPAddress.Parse(str)).ToList(), result.Ports, result.Attempts, TimeSpan.FromMilliseconds(result.Timeout), result.Concurrency);
			ReplaceSelections(strs.Zip(results, (str, strResult) => str + ": " + String.Join(", ", strResult)).ToList());
		}

		internal DatabaseConnectDialog.Result Command_Database_Connect_Dialog()
		{
			return DatabaseConnectDialog.Run(WindowParent);
		}

		DbConnection dbConnection;
		internal void Command_Database_Connect(DatabaseConnectDialog.Result result)
		{
			if (dbConnection != null)
			{
				dbConnection.Dispose();
				dbConnection = null;
			}
			dbConnection = result.DBConnectInfo.GetConnection();
		}

		Table RunDBSelect(string commandText)
		{
			using (var command = dbConnection.CreateCommand())
			{
				command.CommandText = commandText;
				using (var reader = command.ExecuteReader())
					return new Table(reader);
			}
		}

		void ValidateConnection()
		{
			if (dbConnection == null)
				throw new Exception("No connection.");
		}

		internal void Command_Database_Execute()
		{
			ValidateConnection();

			var results = GetSelectionStrings().Select(str => RunDBSelect(str)).Where(table => table.Headers.Count != 0).ToList();

			Results.Clear();
			Results.AddRange(results);
		}

		internal void Command_Database_ClearResults()
		{
			Results.Clear();
		}

		internal void Command_Database_Examine_Dialog()
		{
			ValidateConnection();
			ExamineDatabaseDialog.Run(WindowParent, dbConnection);
		}

		internal void Command_Keys_Set(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (values.Distinct().Count() != values.Count))
				throw new ArgumentException("Cannot have duplicate keys");
			keysAndValues[index] = new ObservableCollection<string>(values);
		}

		internal void Command_Keys_Add(int index)
		{
			// Handles keys as well as values
			var values = GetSelectionStrings();
			if ((index == 0) && (keysAndValues[0].Concat(values).GroupBy(key => key).Any(group => group.Count() > 1)))
				throw new ArgumentException("Cannot have duplicate keys");
			foreach (var value in values)
				keysAndValues[index].Add(value);
		}

		internal void Command_Keys_Replace(int index)
		{
			if (keysAndValues[0].Count != keysAndValues[index].Count)
				throw new Exception("Keys and values count must match");

			var strs = new List<string>();
			foreach (var range in Selections)
			{
				var str = GetString(range);
				if (!keysHash.ContainsKey(str))
					strs.Add(str);
				else
					strs.Add(keysAndValues[index][keysHash[str]]);
			}
			ReplaceSelections(strs);
		}

		internal void Command_Keys_Find(int index)
		{
			var searcher = new Searcher(keysAndValues[index].ToList(), true);
			var ranges = new List<Range>();
			var selections = Selections.ToList();
			if ((Selections.Count == 1) && (!Selections[0].HasSelection))
				selections = new List<Range> { new Range(BeginOffset(), EndOffset()) };
			foreach (var selection in selections)
				ranges.AddRange(Data.StringMatches(searcher, selection.Start, selection.Length).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));

			ranges = ranges.OrderBy(range => range.Start).ToList();
			Selections.Replace(ranges);
		}

		internal void Command_Keys_Copy(int index)
		{
			SetClipboard(keysAndValues[index].ToList());
		}

		internal void Command_Keys_HitsMisses(int index, bool hits)
		{
			var set = new HashSet<string>(keysAndValues[index]);
			Selections.Replace(Selections.AsParallel().AsOrdered().Where(range => set.Contains(GetString(range)) == hits).ToList());
		}

		internal void Command_Region_ToggleRegionsSelections()
		{
			if (Selections.Count > 1)
			{
				Regions.AddRange(Selections);
				Selections.Replace(Selections[visibleIndex]);
			}
			else if (Regions.Count != 0)
			{
				Selections.Replace(Regions);
				Regions.Clear();
			}
		}

		internal void Command_Select_All()
		{
			Selections.Replace(new Range(EndOffset(), BeginOffset()));
		}

		internal LimitDialog.Result Command_Select_Limit_Dialog()
		{
			return LimitDialog.Run(WindowParent, Selections.Count);
		}

		internal void Command_Select_Limit(LimitDialog.Result result)
		{
			if (result.IgnoreBlank)
				Selections.Replace(Selections.Where(sel => sel.HasSelection).ToList());
			if (result.SelMult > 1)
				Selections.Replace(Selections.AsParallel().AsOrdered().Where((sel, index) => index % result.SelMult == 0).ToList());
			var sels = Math.Min(Selections.Count, result.NumSels);
			Selections.RemoveRange(sels, Selections.Count - sels);
		}

		internal void Command_Select_Lines()
		{
			var lineSets = Selections.AsParallel().Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(Math.Max(range.Start, range.End - 1)) }).ToList();

			var hasLine = new bool[Data.NumLines];
			foreach (var set in lineSets)
				for (var ctr = set.start; ctr <= set.end; ++ctr)
					hasLine[ctr] = true;

			var lines = new List<int>();
			for (var line = 0; line < hasLine.Length; ++line)
				if (hasLine[line])
					lines.Add(line);

			Selections.Replace(lines.AsParallel().AsOrdered().Select(line => new Range(Data.GetOffset(line, Data.GetLineLength(line)), Data.GetOffset(line, 0))).ToList());
		}

		internal void Command_Select_Empty(bool include)
		{
			Selections.Replace(Selections.Where(range => range.HasSelection != include).ToList());
		}

		Range TrimRange(Range range)
		{
			var index = range.Start;
			var length = range.Length;
			Data.Trim(ref index, ref length);
			if ((index == range.Start) && (length == range.Length))
				return range;
			return Range.FromIndex(index, length);
		}

		internal void Command_Text_Select_Trim()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => TrimRange(range)).ToList());
		}

		internal WidthDialog.Result Command_Text_Select_ByWidth_Dialog()
		{
			var minLength = Selections.Any() ? Selections.AsParallel().Min(range => range.Length) : 0;
			var maxLength = Selections.Any() ? Selections.AsParallel().Max(range => range.Length) : 0;
			return WidthDialog.Run(WindowParent, minLength, maxLength, false, true, GetExpressionData(count: 10));
		}

		bool WidthMatch(string str, WidthDialog.Result result, int value)
		{
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: return str.Length == value;
				case WidthDialog.WidthType.Minimum: return str.Length >= value;
				case WidthDialog.WidthType.Maximum: return str.Length <= value;
				case WidthDialog.WidthType.Multiple: return str.Length % value == 0;
				default: throw new ArgumentException("Invalid width type");
			}
		}

		internal void Command_Text_Select_ByWidth(WidthDialog.Result result)
		{
			var results = GetExpressionResults<int>(result.Expression);
			Selections.Replace(Selections.AsParallel().AsOrdered().Where((range, index) => WidthMatch(GetString(range), result, results[index])).ToList());
		}

		internal void Command_Select_Unique()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().GroupBy(range => GetString(range)).Select(list => list.First()).ToList());
		}

		internal void Command_Select_Duplicates()
		{
			Selections.Replace(Selections.AsParallel().AsOrdered().GroupBy(range => GetString(range)).SelectMany(list => list.Skip(1)).ToList());
		}

		internal CountDialog.Result Command_Select_ByCount_Dialog()
		{
			return CountDialog.Run(WindowParent);
		}

		internal void Command_Select_ByCount(CountDialog.Result result)
		{
			var strs = Selections.Select((range, index) => Tuple.Create(GetString(range), index)).ToList();
			var counts = new Dictionary<string, int>(result.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
			foreach (var tuple in strs)
			{
				if (!counts.ContainsKey(tuple.Item1))
					counts[tuple.Item1] = 0;
				++counts[tuple.Item1];
			}
			strs = strs.Where(tuple => (counts[tuple.Item1] >= result.MinCount) && (counts[tuple.Item1] <= result.MaxCount)).ToList();
			Selections.Replace(strs.Select(tuple => Selections[tuple.Item2]).ToList());
		}

		internal void Command_Select_Regions()
		{
			Selections.Replace(Regions);
		}

		internal void Command_Select_FindResults()
		{
			Selections.Replace(Searches);
			Searches.Clear();
		}

		void DoCommand_Type_Select_Min<T>(bool min, Func<Range, T> sortBy)
		{
			if (!Selections.Any())
				throw new Exception("No selections");
			var selections = Selections.AsParallel().AsOrdered().Select(range => new { range = range, sort = sortBy(range) }).OrderBy(obj => obj.sort).ToList();
			var found = min ? selections.First().sort : selections.Last().sort;
			Selections.Replace(selections.AsParallel().AsOrdered().Where(obj => obj.sort.Equals(found)).Select(obj => obj.range).ToList());
		}

		internal void Command_Type_Select_MinMax(bool min, Command_MinMax_Type type)
		{
			switch (type)
			{
				case Command_MinMax_Type.String: DoCommand_Type_Select_Min(min, range => GetString(range)); break;
				case Command_MinMax_Type.Numeric: DoCommand_Type_Select_Min(min, range => NumericSort(GetString(range))); break;
				case Command_MinMax_Type.Length: DoCommand_Type_Select_Min(min, range => range.Length); break;
			}
		}

		internal GetExpressionDialog.Result Command_Expression_SelectByExpression_Dialog()
		{
			return GetExpressionDialog.Run(WindowParent, GetExpressionData(10));
		}

		internal void Command_Expression_SelectByExpression(GetExpressionDialog.Result result)
		{
			var results = GetExpressionResults<bool>(result.Expression);
			Selections.Replace(Selections.Where((str, num) => results[num]).ToList());
		}

		internal void Command_Select_Selection_First()
		{
			visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_ShowCurrent()
		{
			EnsureVisible(true);
		}

		internal void Command_Select_Selection_Next()
		{
			++visibleIndex;
			if (visibleIndex >= Selections.Count)
				visibleIndex = 0;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_Previous()
		{
			--visibleIndex;
			if (visibleIndex < 0)
				visibleIndex = Selections.Count - 1;
			EnsureVisible(true);
			canvasRenderTimer.Start();
		}

		internal void Command_Select_Selection_Single()
		{
			if (!Selections.Any())
				return;
			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			Selections.Replace(Selections[visibleIndex]);
			visibleIndex = 0;
		}

		internal void Command_Select_Selection_Remove()
		{
			if (!Selections.Any())
				return;
			Selections.RemoveAt(visibleIndex);
		}

		internal void Command_Region_SetSelection()
		{
			Regions.AddRange(Selections);
		}

		internal void Command_Region_SetFindResults()
		{
			Regions.AddRange(Searches);
			Searches.Clear();
		}

		internal void Command_Region_ClearRegions()
		{
			if (!Selections.Any(range => range.HasSelection))
				Regions.Clear();
			else
			{
				foreach (var selection in Selections)
				{
					var toRemove = Regions.Where(region => (region.Start >= selection.Start) && (region.End <= selection.End)).ToList();
					toRemove.ForEach(region => Regions.Remove(region));
				}
			}
		}

		internal void Command_Region_LimitToSelection()
		{
			Regions.Replace(Regions.Where(region => Selections.Any(selection => (region.Start >= selection.Start) && (region.End <= selection.End))).ToList());
		}

		internal void Command_Region_SelectEnclosingRegion()
		{
			Selections.Replace(GetEnclosingRegions());
		}

		int visibleIndex = 0;
		internal void EnsureVisible(bool highlight = false)
		{
			visibleIndex = Math.Max(0, Math.Min(visibleIndex, Selections.Count - 1));
			if (!Selections.Any())
			{
				LineMin = LineMax = IndexMin = IndexMax = PositionMin = PositionMax = ColumnMin = ColumnMax = null;
				return;
			}

			var range = Selections[visibleIndex];
			var lineMin = Data.GetOffsetLine(range.Start);
			var lineMax = Data.GetOffsetLine(range.End);
			var indexMin = Data.GetOffsetIndex(range.Start, lineMin);
			var indexMax = Data.GetOffsetIndex(range.End, lineMax);
			LineMin = Data.GetDiffLine(lineMin) + 1;
			LineMax = Data.GetDiffLine(lineMax) + 1;
			IndexMin = indexMin + 1;
			IndexMax = indexMax + 1;
			PositionMin = range.Start;
			PositionMax = range.End;
			ColumnMin = Data.GetColumnFromIndex(lineMin, indexMin) + 1;
			ColumnMax = Data.GetColumnFromIndex(lineMax, indexMax) + 1;
			var line = Data.GetOffsetLine(range.Cursor);
			var index = Data.GetOffsetIndex(range.Cursor, line);
			if (highlight)
				yScrollValue = line - yScrollViewportFloor / 2;
			yScrollValue = Math.Min(line, Math.Max(line - yScrollViewportFloor + 1, yScrollValue));
			var x = Data.GetColumnFromIndex(line, index);
			xScrollValue = Math.Min(x, Math.Max(x - xScrollViewportFloor + 1, xScrollValue));
		}

		void SelectionsInvalidated()
		{
			var visible = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;
			Selections.DeOverlap();
			if (visible != null)
			{
				visibleIndex = Selections.FindIndex(range => (range.Start == visible.Start) && (range.End == visible.End));
				if (visibleIndex < 0)
					visibleIndex = 0;
			}

			EnsureVisible();
			canvasRenderTimer.Start();
		}

		void SearchesInvalidated()
		{
			Searches.Replace(Searches.Where(range => range.HasSelection).ToList());
			Searches.DeOverlap();
			canvasRenderTimer.Start();
		}

		void RegionsInvalidated()
		{
			Regions.DeOverlap();
			canvasRenderTimer.Start();
		}

		void BookmarksInvalidated()
		{
			Bookmarks.Replace(Bookmarks.Select(range => MoveCursor(range, 0, 0, false, lineRel: true, indexRel: false)).ToList());
			Bookmarks.DeOverlap();
			bookmarkRenderTimer.Start();
		}

		void OnCanvasRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (xScrollViewportCeiling == 0))
				return;

			var brushes = new List<Tuple<RangeList, Brush>>
			{
				Tuple.Create(Selections, Misc.selectionBrush),
				Tuple.Create(Searches, Misc.searchBrush),
				Tuple.Create(Regions, Misc.regionBrush),
			};

			NumSelections = Selections.Count;
			NumRegions = Regions.Count;

			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			var startColumn = xScrollValue;
			var endColumn = Math.Min(Data.MaxColumn + 1, startColumn + xScrollViewportCeiling);

			var lines = Enumerable.Range(startLine, endLine - startLine).ToList();
			var lineRanges = lines.ToDictionary(line => line, line => new Range(Data.GetOffset(line, 0), Data.GetOffset(line, Data.GetLineLength(line) + 1)));
			var screenStart = lineRanges.First().Value.Start;
			var screenEnd = lineRanges.Last().Value.End + 1;
			var startIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, startColumn, true));
			var endIndexes = lines.ToDictionary(line => line, line => Data.GetIndexFromColumn(line, endColumn, true));
			var y = lines.ToDictionary(line => line, line => (line - startLine) * Font.lineHeight);
			var cursorLineDone = new HashSet<int>();
			var visibleCursor = (visibleIndex >= 0) && (visibleIndex < Selections.Count) ? Selections[visibleIndex] : null;

			foreach (var entry in brushes)
			{
				var hasSelection = entry.Item1.Any(range => range.HasSelection);

				foreach (var range in entry.Item1)
				{
					if ((range.End < screenStart) || (range.Start > screenEnd))
						continue;

					var entryStartLine = Data.GetOffsetLine(range.Start);
					var entryEndLine = Data.GetOffsetLine(range.End);
					var cursorLine = range.Cursor == range.Start ? entryStartLine : entryEndLine;
					entryStartLine = Math.Max(startLine, entryStartLine);
					entryEndLine = Math.Min(endLine, entryEndLine + 1);

					if ((entry.Item1 == Selections) && (!hasSelection) && (cursorLine >= entryStartLine) && (cursorLine < entryEndLine))
					{
						if (range == visibleCursor)
							dc.DrawRectangle(Misc.visibleCursorBrush, null, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.lineHeight));

						if (!cursorLineDone.Contains(cursorLine))
						{
							dc.DrawRectangle(Misc.cursorBrush, Misc.cursorPen, new Rect(0, y[cursorLine], canvas.ActualWidth, Font.lineHeight));
							cursorLineDone.Add(cursorLine);
						}

						var cursor = Data.GetOffsetIndex(range.Cursor, cursorLine);
						if ((cursor >= startIndexes[cursorLine]) && (cursor <= endIndexes[cursorLine]))
						{
							cursor = Data.GetColumnFromIndex(cursorLine, cursor);
							dc.DrawRectangle(Brushes.Black, null, new Rect((cursor - startColumn) * Font.charWidth - 1, y[cursorLine], 2, Font.lineHeight));
						}
					}

					for (var line = entryStartLine; line < entryEndLine; ++line)
					{
						var start = Math.Max(lineRanges[line].Start, range.Start);
						var end = Math.Min(lineRanges[line].End, range.End);
						start = Data.GetOffsetIndex(start, line);
						end = Data.GetOffsetIndex(end, line);

						if ((start > endIndexes[line]) || (end < startIndexes[line]))
							continue;

						start = Data.GetColumnFromIndex(line, start);
						end = Data.GetColumnFromIndex(line, end);

						start = Math.Max(0, start - startColumn);
						end = Math.Max(0, Math.Min(endColumn, end) - startColumn);
						var width = Math.Max(0, end - start);

						var steps = range == visibleCursor ? 2 : 1;
						for (var ctr = 0; ctr < steps; ++ctr)
							dc.DrawRectangle(entry.Item2, null, new Rect(start * Font.charWidth, y[line], width * Font.charWidth + 1, Font.lineHeight));
					}
				}
			}

			var highlightDictionary = Highlighting.Get(HighlightType).GetDictionary();

			for (var line = startLine; line < endLine; ++line)
			{
				var lineDiffStatus = Data.GetLineDiffStatus(line);
				Brush brush = null;
				switch (lineDiffStatus)
				{
					case LCS.MatchType.Gap: brush = Misc.diffMajorBrush; break;
					case LCS.MatchType.Mismatch: brush = Misc.diffMinorBrush; break;
				}
				if (brush != null)
					dc.DrawRectangle(brush, null, new Rect(0, y[line], canvas.ActualWidth, Font.lineHeight));

				foreach (var tuple in Data.GetLineColumnDiffs(line))
				{
					var start = Math.Max(0, tuple.Item1 - startColumn);
					var len = tuple.Item2 + Math.Min(0, tuple.Item1 - startColumn);
					if (len > 0)
						dc.DrawRectangle(Misc.diffMajorBrush, null, new Rect(Font.charWidth * start, y[line], len * Font.charWidth, Font.lineHeight));
				}

				var str = Data.GetLineColumns(line);
				if (str.Length <= startColumn)
					continue;

				var highlight = new List<Tuple<Brush, int, int>>();
				foreach (var entry in highlightDictionary)
				{
					var matches = entry.Key.Matches(str);
					foreach (Match match in matches)
						highlight.Add(new Tuple<Brush, int, int>(entry.Value, match.Index, match.Length));
				}

				str = str.Substring(startColumn, Math.Min(endColumn, str.Length) - startColumn);
				var text = Font.GetText(str);
				foreach (var entry in highlight)
				{
					var start = entry.Item2 - startColumn;
					var count = entry.Item3;
					if (start < 0)
					{
						count += start;
						start = 0;
					}
					count = Math.Min(count, str.Length - start);
					if (count <= 0)
						continue;
					text.SetForegroundBrush(entry.Item1, start, count);
				}
				dc.DrawText(text, new Point(0, y[line]));
			}
		}

		void OnBookmarksRender(object sender, DrawingContext dc)
		{
			if ((Data == null) || (yScrollViewportCeiling == 0) || (!Bookmarks.Any()))
				return;
			var startLine = yScrollValue;
			var endLine = Math.Min(Data.NumLines, startLine + yScrollViewportCeiling);
			for (var line = startLine; line < endLine; ++line)
			{
				var offset = Data.GetOffset(line, 0);
				if (!Bookmarks.Any(range => range.Start == offset))
					continue;

				var y = (line - startLine) * Font.lineHeight;

				dc.DrawRoundedRectangle(Brushes.CadetBlue, new Pen(Brushes.Black, 1), new Rect(1, y + 1, Font.lineHeight - 2, Font.lineHeight - 2), 2, 2);
			}
		}

		void BlockSelDown()
		{
			foreach (var range in Selections.ToList())
			{
				var cursorLine = Data.GetOffsetLine(range.Cursor);
				var highlightLine = Data.GetOffsetLine(range.Highlight);
				var cursorIndex = Data.GetOffsetIndex(range.Cursor, cursorLine);
				var highlightIndex = Data.GetOffsetIndex(range.Highlight, highlightLine);

				cursorLine = Math.Max(0, Math.Min(cursorLine + 1, Data.NumLines - 1));
				highlightLine = Math.Max(0, Math.Min(highlightLine + 1, Data.NumLines - 1));
				cursorIndex = Math.Max(0, Math.Min(cursorIndex, Data.GetLineLength(cursorLine)));
				highlightIndex = Math.Max(0, Math.Min(highlightIndex, Data.GetLineLength(highlightLine)));

				Selections.Add(new Range(Data.GetOffset(cursorLine, cursorIndex), Data.GetOffset(highlightLine, highlightIndex)));
			}
		}

		void BlockSelUp()
		{
			var found = new HashSet<string>();
			foreach (var range in Selections)
				found.Add(range.ToString());

			var sels = new List<Range>();
			foreach (var range in Selections.ToList())
			{
				var startLine = Data.GetOffsetLine(range.Start);
				var endLine = Data.GetOffsetLine(range.End);
				var startIndex = Data.GetOffsetIndex(range.Start, startLine);
				var endIndex = Data.GetOffsetIndex(range.End, endLine);

				startLine = Math.Max(0, Math.Min(startLine - 1, Data.NumLines - 1));
				endLine = Math.Max(0, Math.Min(endLine - 1, Data.NumLines - 1));
				startIndex = Math.Max(0, Math.Min(startIndex, Data.GetLineLength(startLine)));
				endIndex = Math.Max(0, Math.Min(endIndex, Data.GetLineLength(endLine)));

				var prevLineRange = new Range(Data.GetOffset(startLine, startIndex), Data.GetOffset(endLine, endIndex));
				if (found.Contains(prevLineRange.ToString()))
					sels.Add(prevLineRange);
				else
					sels.Add(range);
			}

			Selections.Replace(sels);
		}

		internal bool HandleKey(Key key, bool shiftDown, bool controlDown)
		{
			var ret = true;
			switch (key)
			{
				case Key.Back:
				case Key.Delete:
					{
						if (Selections.Any(range => range.HasSelection))
						{
							ReplaceSelections("");
							break;
						}

						Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var offset = range.Start;

							if (controlDown)
							{
								if (key == Key.Back)
									offset = GetPrevWord(offset);
								else
									offset = GetNextWord(offset);
							}
							else
							{
								var line = Data.GetOffsetLine(offset);
								var index = Data.GetOffsetIndex(offset, line);

								if (key == Key.Back)
									--index;
								else
									++index;

								if (index < 0)
								{
									--line;
									if (line < 0)
										return null;
									index = Data.GetLineLength(line);
								}
								if (index > Data.GetLineLength(line))
								{
									++line;
									if (line >= Data.NumLines)
										return null;
									index = 0;
								}

								offset = Data.GetOffset(line, index);
							}

							return new Range(offset, range.Highlight);
						}).Where(range => range != null).ToList(), null);
					}
					break;
				case Key.Escape:
					Searches.Clear();
					doDrag = false;
					break;
				case Key.Left:
					{
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if (controlDown)
								return MoveCursor(range, GetPrevWord(range.Cursor), shiftDown);
							else if ((!shiftDown) && (hasSelection))
								return new Range(range.Start);
							else if ((index == 0) && (line != 0))
								return MoveCursor(range, -1, Int32.MaxValue, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, -1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Right:
					{
						var hasSelection = Selections.Any(range => range.HasSelection);
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var line = Data.GetOffsetLine(range.Cursor);
							var index = Data.GetOffsetIndex(range.Cursor, line);
							if (controlDown)
								return MoveCursor(range, GetNextWord(range.Cursor), shiftDown);
							else if ((!shiftDown) && (hasSelection))
								return new Range(range.End);
							else if ((index == Data.GetLineLength(line)) && (line != Data.NumLines - 1))
								return MoveCursor(range, 1, 0, shiftDown, indexRel: false);
							else
								return MoveCursor(range, 0, 1, shiftDown);
						}).ToList());
					}
					break;
				case Key.Up:
				case Key.Down:
					{
						var mult = key == Key.Up ? -1 : 1;
						if (!controlDown)
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, mult, 0, shiftDown)).ToList());
						else if (!shiftDown)
							yScrollValue += mult;
						else if (key == Key.Down)
							BlockSelDown();
						else
							BlockSelUp();
					}
					break;
				case Key.Home:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, BeginOffset(), shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(new Range(BeginOffset()));
						Selections.Replace(sels);
					}
					else
					{
						bool changed = false;
						for (var ctr = 0; ctr < Selections.Count; ++ctr)
						{
							var line = Data.GetOffsetLine(Selections[ctr].Cursor);
							var index = Data.GetOffsetIndex(Selections[ctr].Cursor, line);

							int first;
							var end = Data.GetLineLength(line);
							for (first = 0; first < end; ++first)
							{
								if (!Char.IsWhiteSpace(Data[line, first]))
									break;
							}
							if (first == end)
								first = 0;

							if (first != index)
								changed = true;
							Selections[ctr] = MoveCursor(Selections[ctr], 0, first, shiftDown, indexRel: false);
						}
						if (!changed)
						{
							Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, 0, shiftDown, indexRel: false)).ToList());
							xScrollValue = 0;
						}
					}
					break;
				case Key.End:
					if (controlDown)
					{
						var sels = Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, EndOffset(), shiftDown)).ToList(); // Have to use MoveCursor for selection
						if ((!sels.Any()) && (!shiftDown))
							sels.Add(new Range(EndOffset()));
						Selections.Replace(sels);
					}
					else
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 0, Int32.MaxValue, shiftDown, indexRel: false)).ToList());
					break;
				case Key.PageUp:
					if (controlDown)
						yScrollValue -= yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, 1 - savedYScrollViewportFloor, 0, shiftDown)).ToList());
					}
					break;
				case Key.PageDown:
					if (controlDown)
						yScrollValue += yScrollViewportFloor / 2;
					else
					{
						var savedYScrollViewportFloor = yScrollViewportFloor;
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => MoveCursor(range, savedYScrollViewportFloor - 1, 0, shiftDown)).ToList());
					}
					break;
				case Key.Tab:
					{
						if (Selections.AsParallel().All(range => (!range.HasSelection) || (Data.GetOffsetLine(range.Start) == Data.GetOffsetLine(range.End))))
						{
							if (!shiftDown)
								HandleText("\t");
							else
							{
								var tabs = Selections.AsParallel().AsOrdered().Where(range => (range.Start != 0) && (Data.GetString(range.Start - 1, 1) == "\t")).Select(range => Range.FromIndex(range.Start - 1, 1)).ToList();
								Replace(tabs, null);
							}
							break;
						}

						var selLines = Selections.AsParallel().AsOrdered().Where(a => a.HasSelection).Select(range => new { start = Data.GetOffsetLine(range.Start), end = Data.GetOffsetLine(range.End - 1) }).ToList();
						var lines = selLines.SelectMany(entry => Enumerable.Range(entry.start, entry.end - entry.start + 1)).Distinct().OrderBy(line => line).ToDictionary(line => line, line => Data.GetOffset(line, 0));
						int offset;
						string replace;
						if (shiftDown)
						{
							offset = 1;
							replace = "";
							lines = lines.Where(entry => (Data.GetLineLength(entry.Key) != 0) && (Data[entry.Key, 0] == '\t')).ToDictionary(entry => entry.Key, entry => entry.Value);
						}
						else
						{
							offset = 0;
							replace = "\t";
							lines = lines.Where(entry => Data.GetLineLength(entry.Key) != 0).ToDictionary(entry => entry.Key, entry => entry.Value);
						}

						var sels = lines.Select(line => Range.FromIndex(line.Value, offset)).ToList();
						var insert = sels.Select(range => replace).ToList();
						Replace(sels, insert);
					}
					break;
				case Key.Enter:
					HandleText(Data.DefaultEnding);
					break;
				case Key.OemCloseBrackets:
					if (controlDown)
					{
						Selections.Replace(Selections.AsParallel().AsOrdered().Select(range =>
						{
							var newPos = Data.GetOppositeBracket(range.Cursor);
							if (newPos == -1)
								return range;

							return MoveCursor(range, newPos, shiftDown);
						}).ToList());
					}
					else
						ret = false;
					break;
				case Key.Space:
					if (controlDown)
					{
						if (Selections.Any())
							Selections.Replace(Selections.Select(range => new Range(range.Highlight, range.Cursor)).ToList());
						else
						{
							var line = Math.Min(yScrollValue, Data.NumLines - 1);
							Selections.Replace(new Range(Data.GetOffset(line, 0)));
						}
					}
					else
						ret = false;
					break;
				default: ret = false; break;
			}

			if (Selections.Changed)
				EnsureVisible();

			return ret;
		}

		enum WordSkipType
		{
			None,
			Char,
			Symbol,
			Space,
		}

		int GetNextWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line) - 1);
			while (true)
			{
				if (index >= Data.GetLineLength(line))
				{
					++line;
					if (line >= Data.NumLines)
						return EndOffset();
					index = -1;
				}

				++index;
				WordSkipType current;
				if (index >= Data.GetLineLength(line))
					current = WordSkipType.Space;
				else
				{
					var c = Data[line, index];
					if (Char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((Char.IsLetterOrDigit(c)) || (c == '_'))
						current = WordSkipType.Char;
					else
						current = WordSkipType.Symbol;
				}

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(line, index);
			}
		}

		int GetPrevWord(int offset)
		{
			WordSkipType moveType = WordSkipType.None;

			var line = Data.GetOffsetLine(offset);
			var index = Math.Min(Data.GetLineLength(line), Data.GetOffsetIndex(offset, line));
			int lastLine = -1, lastIndex = -1;
			while (true)
			{
				if (index < 0)
				{
					--line;
					if (line < 0)
						return BeginOffset();
					index = Data.GetLineLength(line);
					continue;
				}

				lastLine = line;
				lastIndex = index;

				--index;
				WordSkipType current;
				if (index < 0)
					current = WordSkipType.Space;
				else
				{
					var c = Data[line, index];
					if (Char.IsWhiteSpace(c))
						current = WordSkipType.Space;
					else if ((Char.IsLetterOrDigit(c)) || (Data[line, index] == '_'))
						current = WordSkipType.Char;
					else
						current = WordSkipType.Symbol;
				}

				if (moveType == WordSkipType.None)
					moveType = current;

				if (current != moveType)
					return Data.GetOffset(lastLine, lastIndex);
			}
		}

		Range MoveCursor(Range range, int cursor, bool selecting)
		{
			cursor = Math.Max(BeginOffset(), Math.Min(cursor, EndOffset()));
			if (selecting)
				if (range.Cursor == cursor)
					return range;
				else
					return new Range(cursor, range.Highlight);

			if ((range.Cursor == cursor) && (range.Highlight == cursor))
				return range;
			return new Range(cursor);
		}

		Range MoveCursor(Range range, int line, int index, bool selecting, bool lineRel = true, bool indexRel = true)
		{
			if ((lineRel) || (indexRel))
			{
				var startLine = Data.GetOffsetLine(range.Cursor);
				var startIndex = Data.GetOffsetIndex(range.Cursor, startLine);

				if (lineRel)
					line = Data.SkipDiffGaps(line + startLine, line > 0 ? 1 : -1);
				if (indexRel)
					index += startIndex;
			}

			line = Math.Max(0, Math.Min(line, Data.NumLines - 1));
			index = Math.Max(0, Math.Min(index, Data.GetLineLength(line)));
			return MoveCursor(range, Data.GetOffset(line, index), selecting);
		}

		void MouseHandler(Point mousePos, int clickCount, bool selecting)
		{
			var line = Math.Min(Data.NumLines - 1, (int)(mousePos.Y / Font.lineHeight) + yScrollValue);
			var index = Math.Min(Data.GetLineLength(line), Data.GetIndexFromColumn(line, (int)(mousePos.X / Font.charWidth) + xScrollValue, true));
			var offset = Data.GetOffset(line, index);
			var mouseRange = visibleIndex < Selections.Count ? Selections[visibleIndex] : null;

			if (selecting)
			{
				if (mouseRange != null)
				{
					Selections.Remove(mouseRange);
					Selections.Add(MoveCursor(mouseRange, offset, true));
					visibleIndex = Selections.Count - 1;
				}
				return;
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None)
				Selections.Clear();

			if (clickCount == 1)
				Selections.Add(new Range(offset));
			else
			{
				if (mouseRange != null)
					Selections.Remove(mouseRange);
				Selections.Add(new Range(GetNextWord(offset), GetPrevWord(offset + 1)));
			}
			visibleIndex = Selections.Count - 1;
		}

		bool doDrag = false;
		void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (doDrag)
			{
				var strs = GetSelectionStrings();
				if (!StringsAreFiles(strs))
					throw new Exception("Selections must be files.");

				DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, strs.ToArray()), DragDropEffects.Copy);
				doDrag = false;
				return;
			}

			MouseHandler(e.GetPosition(canvas), e.ClickCount, (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None);
			canvas.CaptureMouse();
			e.Handled = true;
		}

		void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			canvas.ReleaseMouseCapture();
			e.Handled = true;
		}

		void OnCanvasMouseMove(object sender, MouseEventArgs e)
		{
			if (!canvas.IsMouseCaptured)
				return;

			MouseHandler(e.GetPosition(canvas), 0, true);
			e.Handled = true;
		}

		void RunSearch(FindTextDialog.Result result)
		{
			if ((result == null) || (result.Regex == null))
				return;

			Searches.Clear();

			var regions = result.SelectionOnly ? Selections.ToList() : new List<Range> { new Range(EndOffset(), BeginOffset()) };
			foreach (var region in regions)
				Searches.AddRange(Data.RegexMatches(result.Regex, region.Start, region.Length, result.IncludeEndings, result.RegexGroups, false).Select(tuple => Range.FromIndex(tuple.Item1, tuple.Item2)));
		}

		string GetString(Range range)
		{
			return Data.GetString(range.Start, range.Length);
		}

		enum ReplaceType
		{
			Normal,
			Undo,
			Redo,
		}

		void ReplaceSelections(string str, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			ReplaceSelections(Selections.Select(range => str).ToList(), highlight, replaceType, tryJoinUndo);
		}

		void ReplaceSelections(List<string> strs, bool highlight = true, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			Replace(Selections, strs, replaceType, tryJoinUndo);

			if (highlight)
				Selections.Replace(Selections.AsParallel().AsOrdered().Select((range, index) => new Range(range.End, range.End - (strs == null ? 0 : strs[index].Length))).ToList());
			else
				Selections.Replace(Selections.AsParallel().AsOrdered().Select(range => new Range(range.End)).ToList());
		}

		void CalculateDiff()
		{
			if (diffTarget == null)
				return;

			TextData.CalculateDiff(Data, diffTarget.Data);

			CalculateBoundaries();
			diffTarget.CalculateBoundaries();
		}

		void Replace(List<Range> ranges, List<string> strs, ReplaceType replaceType = ReplaceType.Normal, bool tryJoinUndo = false)
		{
			if (!ranges.Any())
				return;

			if (strs == null)
				strs = Enumerable.Repeat("", ranges.Count).ToList();

			if (ranges.Count != strs.Count)
				throw new Exception("Invalid string count");

			var undoRanges = new List<Range>();
			var undoText = new List<string>();

			var change = 0;
			for (var ctr = 0; ctr < ranges.Count; ++ctr)
			{
				var undoRange = new Range(ranges[ctr].Start + change, ranges[ctr].Start + strs[ctr].Length + change);
				undoRanges.Add(undoRange);
				undoText.Add(GetString(ranges[ctr]));
				change = undoRange.Highlight - ranges[ctr].End;
			}

			// Abort if no changes
			if (!Enumerable.Range(0, ranges.Count).Any(ctr => undoText[ctr] != strs[ctr]))
				return;

			var textCanvasUndoRedo = new UndoRedo.UndoRedoStep(undoRanges, undoText, tryJoinUndo);
			switch (replaceType)
			{
				case ReplaceType.Undo: undoRedo.AddUndone(textCanvasUndoRedo); break;
				case ReplaceType.Redo: undoRedo.AddRedone(textCanvasUndoRedo); break;
				case ReplaceType.Normal: undoRedo.AddUndo(textCanvasUndoRedo); break;
			}

			Data.Replace(ranges.Select(range => range.Start).ToList(), ranges.Select(range => range.Length).ToList(), strs);
			CalculateDiff();

			var translateMap = RangeList.GetTranslateMap(ranges, strs, Selections, Regions, Searches, Bookmarks);
			Selections.Translate(translateMap);
			Regions.Translate(translateMap);
			var searchLens = Searches.Select(range => range.Length).ToList();
			Searches.Translate(translateMap);
			Searches.Replace(Searches.Where((range, index) => searchLens[index] == range.Length).ToList());
			Bookmarks.Translate(translateMap);

			CalculateBoundaries();
		}

		void FindNext(bool forward, bool selecting)
		{
			if (Searches.Count == 0)
			{
				Selections.Clear();
				return;
			}

			for (var ctr = 0; ctr < Selections.Count; ++ctr)
			{
				int index;
				if (forward)
				{
					index = Searches.BinaryFindFirst(range => range.Start >= Selections[ctr].End);
					if (index == -1)
						index = 0;
				}
				else
				{
					index = Searches.BinaryFindLast(range => range.Start < Selections[ctr].Start);
					if (index == -1)
						index = Searches.Count - 1;
				}

				if (!selecting)
					Selections[ctr] = new Range(Searches[index].End, Searches[index].Start);
				else if (forward)
					Selections[ctr] = new Range(Searches[index].End, Selections[ctr].Start);
				else
					Selections[ctr] = new Range(Searches[index].Start, Selections[ctr].End);
			}
		}

		string SetWidth(string str, WidthDialog.Result result, int value)
		{
			int length;
			switch (result.Type)
			{
				case WidthDialog.WidthType.Absolute: length = value; break;
				case WidthDialog.WidthType.Relative: length = Math.Max(0, str.Length + value); break;
				case WidthDialog.WidthType.Minimum: length = Math.Max(str.Length, value); break;
				case WidthDialog.WidthType.Maximum: length = Math.Min(str.Length, value); break;
				case WidthDialog.WidthType.Multiple: length = str.Length + value - 1 - (str.Length + value - 1) % value; break;
				default: throw new ArgumentException("Invalid width type");
			}

			if (str.Length == length)
				return str;

			if (str.Length > length)
			{
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str.Substring(0, length);
					case WidthDialog.TextLocation.Middle: return str.Substring((str.Length - length + 1) / 2, length);
					case WidthDialog.TextLocation.End: return str.Substring(str.Length - length);
					default: throw new ArgumentException("Invalid");
				}
			}
			else
			{
				var len = length - str.Length;
				switch (result.Location)
				{
					case WidthDialog.TextLocation.Start: return str + new string(result.PadChar, len);
					case WidthDialog.TextLocation.Middle: return new string(result.PadChar, (len + 1) / 2) + str + new string(result.PadChar, len / 2);
					case WidthDialog.TextLocation.End: return new string(result.PadChar, len) + str;
					default: throw new ArgumentException("Invalid");
				}
			}
		}

		public string RepeatString(string input, int count)
		{
			var builder = new StringBuilder(input.Length * count);
			for (int ctr = 0; ctr < count; ++ctr)
				builder.Append(input);
			return builder.ToString();
		}

		internal bool Empty()
		{
			return (FileName == null) && (!IsModified) && (BeginOffset() == EndOffset());
		}

		internal bool HandleText(string text)
		{
			if (text.Length == 0)
				return true;

			ReplaceSelections(text, false, tryJoinUndo: true);
			if (Selections.Changed)
				EnsureVisible();
			return true;
		}

		void CalculateBoundaries()
		{
			if ((canvas.ActualWidth <= 0) || (canvas.ActualHeight <= 0))
				return;

			xScroll.ViewportSize = canvas.ActualWidth / Font.charWidth;
			xScroll.Minimum = 0;
			xScroll.Maximum = Data.MaxColumn - xScrollViewportFloor;
			xScroll.SmallChange = 1;
			xScroll.LargeChange = Math.Max(0, xScroll.ViewportSize - 1);
			xScrollValue = xScrollValue;

			yScroll.ViewportSize = canvas.ActualHeight / Font.lineHeight;
			yScroll.Minimum = 0;
			yScroll.Maximum = Data.NumLines - yScrollViewportFloor;
			yScroll.SmallChange = 1;
			yScroll.LargeChange = Math.Max(0, yScroll.ViewportSize - 1);
			yScrollValue = yScrollValue;

			LineEnding = Data.OnlyEnding;

			canvasRenderTimer.Start();
		}

		void ResultsEditClick(object sender, RoutedEventArgs e)
		{
			var inputs = Results.ToList();
			var result = EditTablesDialog.Run(WindowParent, null, inputs);
			var outputs = new List<Table>();
			for (var ctr = 0; ctr < result.Results.Count; ++ctr)
			{
				var tableResult = result.Results[ctr];
				if (tableResult.OutputTableType == Table.TableType.None)
					continue;

				var outputTable = inputs[ctr];
				foreach (var joinInfo in tableResult.JoinInfos)
					outputTable = Table.Join(outputTable, inputs[joinInfo.RightTable], joinInfo.LeftColumn, joinInfo.RightColumn, joinInfo.JoinType);

				outputTable = outputTable.Aggregate(tableResult.GroupByColumns, tableResult.AggregateColumns);
				outputTable = outputTable.Sort(tableResult.SortColumns);
				outputs.Add(outputTable);
			}

			Results.Clear();
			Results.AddRange(outputs);
		}

		void ResultsCopyClick(object sender, RoutedEventArgs e)
		{
			SetClipboard(Results.Select(result => result.ConvertToString(Data.DefaultEnding)).ToList());
		}

		public override string ToString()
		{
			return FileName;
		}
	}
}
