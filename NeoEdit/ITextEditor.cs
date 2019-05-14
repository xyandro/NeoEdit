using NeoEdit.Content;

namespace NeoEdit
{
	public interface ITextEditor
	{
		Parser.ParserType ContentType { get; }
	}
}
