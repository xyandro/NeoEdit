using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NeoEdit.Common;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor
{
	public class NEFileData : INEFileData
	{
		public long NESerial { get; } = NESerialTracker.NESerial;
		public NEFile NEFile { get; }

		NETextPoint neTextPoint;
		public NETextPoint NETextPoint
		{
			get => neTextPoint;
			set
			{
				neTextPoint = value;
				NEFile.NEWindow?.SetTextChanged();
			}
		}
		public IReadOnlyList<NERange> Selections { get; set; }
		public IReadOnlyList<NERange>[] Regions { get; set; }
		public bool AllowOverlappingSelections { get; set; }
		public string DBName { get; set; }
		public int CurrentSelection { get; set; }
		public string DisplayName { get; set; }
		public string FileName { get; set; }
		public bool AutoRefresh { get; set; }
		public ParserType ContentType { get; set; }
		public Coder.CodePage CodePage { get; set; }
		public bool HasBOM { get; set; }
		public string AESKey { get; set; }
		public bool Compressed { get; set; }
		public bool DiffIgnoreWhitespace { get; set; }
		public bool DiffIgnoreCase { get; set; }
		public bool DiffIgnoreNumbers { get; set; }
		public bool DiffIgnoreLineEndings { get; set; }
		public HashSet<char> DiffIgnoreCharacters { get; set; }
		public bool KeepSelections { get; set; }
		public bool HighlightSyntax { get; set; }
		public bool StrictParsing { get; set; }
		public JumpByType JumpBy { get; set; }
		public bool ViewBinary { get; set; }
		public HashSet<Coder.CodePage> ViewBinaryCodePages { get; set; }
		public IReadOnlyList<HashSet<string>> ViewBinarySearches { get; set; }
		public NEFile DiffTarget { get; set; }
		public DbConnection dbConnection { get; set; }

		NEFileData() { }

		public NEFileData(NEFile neFile)
		{
			NEFile = neFile;
			Regions = new IReadOnlyList<NERange>[9];
			DiffIgnoreCharacters = new HashSet<char>();
		}

		public INEFileData Next()
		{
			return new NEFileData(NEFile)
			{
				neTextPoint = neTextPoint,
				Selections = Selections,
				Regions = Regions.ToArray(),
				AllowOverlappingSelections = AllowOverlappingSelections,
				DBName = DBName,
				CurrentSelection = CurrentSelection,
				DisplayName = DisplayName,
				FileName = FileName,
				AutoRefresh = AutoRefresh,
				ContentType = ContentType,
				CodePage = CodePage,
				HasBOM = HasBOM,
				AESKey = AESKey,
				Compressed = Compressed,
				DiffIgnoreWhitespace = DiffIgnoreWhitespace,
				DiffIgnoreCase = DiffIgnoreCase,
				DiffIgnoreNumbers = DiffIgnoreNumbers,
				DiffIgnoreLineEndings = DiffIgnoreLineEndings,
				DiffIgnoreCharacters = DiffIgnoreCharacters,
				KeepSelections = KeepSelections,
				HighlightSyntax = HighlightSyntax,
				StrictParsing = StrictParsing,
				JumpBy = JumpBy,
				ViewBinary = ViewBinary,
				ViewBinaryCodePages = ViewBinaryCodePages,
				ViewBinarySearches = ViewBinarySearches,
				DiffTarget = DiffTarget,
				dbConnection = dbConnection,
			};
		}

		public override string ToString() => NESerial.ToString();
	}
}
