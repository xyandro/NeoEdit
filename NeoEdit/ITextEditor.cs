using NeoEdit.Content;
using NeoEdit.Parsing;

namespace NeoEdit
{
	public interface ITextEditor
	{
		Parser.ParserType ContentType { get; set; }
		CacheValue previousData { get; }
		Parser.ParserType previousType { get; set; }
		ParserNode previousRoot { get; set; }
		TextData Data { get; }
	}
}
