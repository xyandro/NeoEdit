using System.IO;

namespace NeoEdit
{
	public enum ParserType
	{
		None,
		Balanced,
		Columns,
		CPlusPlus,
		CSharp,
		CSV,
		ExactColumns,
		HTML,
		JSON,
		SQL,
		TSV,
		XML,
	}

	static public class ParserExtensions
	{
		public static bool IsTableType(this ParserType parserType) => (parserType == ParserType.Columns) || (parserType == ParserType.ExactColumns) || (parserType == ParserType.TSV) || (parserType == ParserType.CSV);

		static public ParserType GetParserType(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return ParserType.None;
			switch (Path.GetExtension(fileName).ToLowerInvariant())
			{
				case ".nec": return ParserType.Columns;
				case ".c": case ".cpp": return ParserType.CPlusPlus;
				case ".cs": return ParserType.CSharp;
				case ".csv": return ParserType.CSV;
				case ".htm": case ".html": return ParserType.HTML;
				case ".jsn": case ".json": return ParserType.JSON;
				case ".sql": return ParserType.SQL;
				case ".tsv": return ParserType.TSV;
				case ".csproj": case ".vbproj": case ".vcxproj": case ".xml": case ".xaml": return ParserType.XML;
				default: return ParserType.None;
			}
		}
	}
}
