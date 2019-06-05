using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.Content.JSON.Parser;

namespace NeoEdit.Content.JSON
{
	class JSONVisitor : JSONBaseVisitor<ParserNode>
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = ParserHelper.Parse<JSONLexer, JSONParser, JSONParser.JsonContext>(input, parser => parser.json(), strict);
			return new JSONVisitor().Visit(tree);
		}

		public static List<string> rFormat(ParserNode node, string input)
		{
			var result = new List<string>();
			var type = node.Type;
			var children = node.Children().ToList();
			switch (type)
			{
				case DOC:
					result.AddRange(children.SelectMany(child => rFormat(child, input)));
					break;
				case OBJECT:
					{
						if (!children.Any())
							result.Add("{}");
						else
						{
							result.Add("{");
							var last = children.Last();
							foreach (var child in children)
							{
								var lines = rFormat(child, input);
								if (child != last)
									lines[lines.Count - 1] += ",";
								result.AddRange(lines.Select(str => $"\t{str}"));
							}
							result.Add("}");
						}
					}
					break;
				case ARRAY:
					{
						if (!children.Any())
							result.Add("[]");
						else
						{
							var childResults = new List<List<string>>();
							var last = children.Last();
							foreach (var child in children)
							{
								var lines = rFormat(child, input);
								if (child != last)
									lines[lines.Count - 1] += ",";
								childResults.Add(lines);
							}
							string childResult = null;
							if (childResults.All(list => list.Count == 1))
							{
								childResult = string.Join("", childResults.Select(str => str[0]));
								if (childResult.Length > 200)
									childResult = null;
							}

							if (childResult != null)
								result.Add($"[{childResult}]");
							else
							{
								result.Add("[");
								result.AddRange(childResults.SelectMany(list => list).Select(str => $"\t{str}"));
								result.Add("]");
							}
						}
					}
					break;
				case PAIR:
					result.Add($"\"{node.GetAttrText(ID)}\":");
					var childData = children.SelectMany(child => rFormat(child, input)).ToList();
					if (childData.Any())
					{
						result[result.Count - 1] += $" {childData[0]}";
						childData.RemoveAt(0);
					}
					result.AddRange(childData);
					break;
				case NUMBER:
				case CONSTANT:
					result.Add(input.Substring(node.Start, node.Length));
					break;
				case STRING:
					result.Add($"\"{input.Substring(node.Start, node.Length)}\"");
					break;
			}
			return result;
		}

		public static string Format(ParserNode node, string input) => string.Join("", rFormat(node, input).Select(str => $"{str}\r\n"));

		const string DOC = "Doc";
		const string OBJECT = "Object";
		const string ARRAY = "Array";
		const string PAIR = "Pair";
		const string ID = "ID";
		const string STRING = "String";
		const string NUMBER = "Number";
		const string CONSTANT = "Constant";

		ParserNode GetNode(ParserRuleContext context, string type, IEnumerable<ParserRuleContext> children = null)
		{
			int start, end;
			context.GetBounds(out start, out end);
			if (type == STRING)
			{
				++start;
				--end;
			}

			var node = new ParserNode { Type = type, Start = start, End = end };
			if (children != null)
				foreach (var child in children)
					Visit(child).Parent = node;
			return node;
		}

		public override ParserNode VisitPair(JSONParser.PairContext context)
		{
			var node = GetNode(context, PAIR);
			var str = context.STRING();
			var isStr = str != null;
			str = str ?? context.NUMBER();
			int start, end;
			str.GetBounds(out start, out end);
			var id = str.GetText();
			if (isStr)
			{
				++start;
				--end;
				id = id.Substring(1, id.Length - 2);
			}
			node.AddAttr(ID, id, start, end);
			Visit(context.item()).Parent = node;
			return node;
		}

		public override ParserNode VisitJson(JSONParser.JsonContext context) => GetNode(context, DOC, context.item());
		public override ParserNode VisitObject(JSONParser.ObjectContext context) => GetNode(context, OBJECT, context.pair());
		public override ParserNode VisitArray(JSONParser.ArrayContext context) => GetNode(context, ARRAY, context.item());
		public override ParserNode VisitString(JSONParser.StringContext context) => GetNode(context, STRING);
		public override ParserNode VisitNumber(JSONParser.NumberContext context) => GetNode(context, NUMBER);
		public override ParserNode VisitConstant(JSONParser.ConstantContext context) => GetNode(context, CONSTANT);
	}
}
