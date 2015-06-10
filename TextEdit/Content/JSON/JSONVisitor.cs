using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using NeoEdit.TextEdit.Content.JSON.Parser;

namespace NeoEdit.TextEdit.Content.JSON
{
	class JSONVisitor : JSONBaseVisitor<object>
	{
		public static ParserNode Parse(string input)
		{
			var inputStream = new AntlrInputStream(input);
			var lexer = new JSONLexer(inputStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new JSONParser(tokens);
			parser.Interpreter.PredictionMode = PredictionMode.Sll;

			JSONParser.JsonContext tree;
			try
			{
				tree = parser.json();
			}
			catch
			{
				tokens.Reset();
				parser.Reset();
				parser.Interpreter.PredictionMode = PredictionMode.Ll;
				tree = parser.json();
			}

			var visitor = new JSONVisitor(input);
			visitor.Visit(tree);
			return visitor.Doc;
		}

		public static List<string> rFormat(ParserNode node, string input)
		{
			var result = new List<string>();
			var type = node.Type;
			var children = node.List(ParserNode.ParserNodeListType.Children).ToList();
			switch (type)
			{
				case DOC:
					result.AddRange(children.SelectMany(child => rFormat(child, input)));
					break;
				case OBJECT:
					{
						result.Add("{");
						var last = children.Last();
						foreach (var child in children)
						{
							var lines = rFormat(child, input);
							if (child != last)
								lines[lines.Count - 1] += ",";
							result.AddRange(lines.Select(str => "\t" + str));
						}
						result.Add("}");
					}
					break;
				case ARRAY:
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
							childResult = String.Join("", childResults.Select(str => str[0]));
							if (childResult.Length > 200)
								childResult = null;
						}

						if (childResult != null)
							result.Add("[" + childResult + "]");
						else
						{
							result.Add("[");
							result.AddRange(childResults.SelectMany(list => list).Select(str => "\t" + str));
							result.Add("]");
						}
					}
					break;
				case PAIR:
					result.Add("\"" + node.GetAttrText(ID) + "\":");
					var childData = children.SelectMany(child => rFormat(child, input)).ToList();
					if (childData.Any())
					{
						result[result.Count - 1] += " " + childData[0];
						childData.RemoveAt(0);
					}
					result.AddRange(childData);
					break;
				case NUMBER:
				case CONSTANT:
					result.Add(input.Substring(node.Start, node.Length));
					break;
				case STRING:
					result.Add("\"" + input.Substring(node.Start, node.Length) + "\"");
					break;
			}
			return result;
		}

		public static string Format(ParserNode node, string input)
		{
			return String.Join("", rFormat(node, input).Select(str => str + "\r\n"));
		}

		const string DOC = "Doc";
		const string OBJECT = "Object";
		const string ARRAY = "Array";
		const string PAIR = "Pair";
		const string ID = "ID";
		const string STRING = "String";
		const string NUMBER = "Number";
		const string CONSTANT = "Constant";

		readonly ParserNode Doc;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		JSONVisitor(string input)
		{
			this.input = input;
			stack.Push(Doc = new ParserNode { Type = DOC, Start = 0, End = input.Length });
		}

		ParserNode AddNode(ParserRuleContext context, string type)
		{
			int start, end;
			context.GetBounds(out start, out end);
			if (type == STRING)
			{
				++start;
				--end;
			}

			var node = new ParserNode { Type = type, Parent = Parent, Start = start, End = end };
			stack.Push(node);
			VisitChildren(context);
			stack.Pop();
			return node;
		}

		public override object VisitObject(JSONParser.ObjectContext context) { return AddNode(context, OBJECT); }
		public override object VisitArray(JSONParser.ArrayContext context) { return AddNode(context, ARRAY); }
		public override object VisitString(JSONParser.StringContext context) { return AddNode(context, STRING); }
		public override object VisitNumber(JSONParser.NumberContext context) { return AddNode(context, NUMBER); }
		public override object VisitConstant(JSONParser.ConstantContext context) { return AddNode(context, CONSTANT); }

		public override object VisitPair(JSONParser.PairContext context)
		{
			var node = AddNode(context, PAIR);
			var start = context.name.StartIndex + 1;
			var end = context.name.StopIndex;
			node.AddAttr(ID, input.Substring(start, end - start), start, end);
			return node;
		}
	}
}
