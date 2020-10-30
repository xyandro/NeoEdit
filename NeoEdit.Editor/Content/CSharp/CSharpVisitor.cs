using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;

namespace NeoEdit.Editor.Content.CSharp
{
	class CSharpVisitor : CSharpSyntaxWalker
	{
		public static ParserNode Parse(string input, bool strict)
		{
			var tree = CSharpSyntaxTree.ParseText(input);
			var root = tree.GetCompilationUnitRoot();

			if (strict)
			{
				var error = tree.GetDiagnostics().FirstOrDefault();
				if (error != null)
					throw new Exception(error.ToString());
			}

			var walker = new CSharpVisitor(input);
			walker.Visit(root);
			return walker.document;
		}

		public static string Comment(NEText text, Range range)
		{
			var startLine = text.GetPositionLine(range.Start);
			var startIndex = text.GetPositionIndex(range.Start, startLine);
			var endLine = text.GetPositionLine(range.End);
			var endIndex = text.GetPositionIndex(range.End, endLine);
			var result = "";
			for (var line = startLine; line <= endLine; ++line)
			{
				var linePosition = text.GetPosition(line, 0);
				var start = line == startLine ? startIndex : 0;
				var contentEnd = line == endLine ? endIndex : text.GetLineLength(line);
				var end = line == endLine ? endIndex : contentEnd + text.GetEndingLength(line);
				var str = text.GetString(linePosition + start, end - start);
				if (start != contentEnd)
					str = $"//{str}";
				result += str;
			}
			return result;
		}

		public static string Uncomment(NEText text, Range range)
		{
			var startLine = text.GetPositionLine(range.Start);
			var startIndex = text.GetPositionIndex(range.Start, startLine);
			var endLine = text.GetPositionLine(range.End);
			var endIndex = text.GetPositionIndex(range.End, endLine);
			var result = "";
			for (var line = startLine; line <= endLine; ++line)
			{
				var linePosition = text.GetPosition(line, 0);
				var start = line == startLine ? startIndex : 0;
				var contentEnd = line == endLine ? endIndex : text.GetLineLength(line);
				var end = line == endLine ? endIndex : contentEnd + text.GetEndingLength(line);
				var str = text.GetString(linePosition + start, end - start);
				if (str.StartsWith("//"))
					str = str.Substring(2);
				result += str;
			}
			return result;
		}

		readonly string input;
		public CSharpVisitor(string input) : base(SyntaxWalkerDepth.Trivia)
		{
			this.input = input;
			document = curNode = new ParserNode { Type = "Document", Start = 0, End = input.Length };
		}

		readonly ParserNode document;
		ParserNode curNode;

		public override void Visit(SyntaxNode syntaxNode)
		{
			var kind = (SyntaxKind)syntaxNode.RawKind;
			var attributes = new Dictionary<string, object>();
			switch (kind)
			{
				case SyntaxKind.UsingDirective:
					attributes["Type"] = "UsingNS";
					break;
				case SyntaxKind.NamespaceDeclaration:
					attributes["Type"] = "Namespace";
					attributes["Name"] = (syntaxNode as NamespaceDeclarationSyntax).Name;
					break;
				case SyntaxKind.ClassDeclaration:
					attributes["Type"] = "Class";
					attributes["Name"] = (syntaxNode as ClassDeclarationSyntax).Identifier;
					break;
				case SyntaxKind.InterfaceDeclaration:
					attributes["Type"] = "Interface";
					attributes["Name"] = (syntaxNode as InterfaceDeclarationSyntax).Identifier;
					break;
				case SyntaxKind.ConstructorDeclaration:
					attributes["Type"] = "Method";
					attributes["Name"] = (syntaxNode as ConstructorDeclarationSyntax).Identifier;
					break;
				case SyntaxKind.MethodDeclaration:
					attributes["Type"] = "Method";
					attributes["Name"] = (syntaxNode as MethodDeclarationSyntax).Identifier;
					attributes["Return"] = (syntaxNode as MethodDeclarationSyntax).ReturnType;
					break;
				case SyntaxKind.FieldDeclaration:
					attributes["Type"] = "Field";
					attributes["Name"] = (syntaxNode as FieldDeclarationSyntax).Declaration.Variables[0].Identifier;
					break;
				case SyntaxKind.PropertyDeclaration:
					attributes["Type"] = "Property";
					attributes["Name"] = (syntaxNode as PropertyDeclarationSyntax).Identifier;
					break;
				case SyntaxKind.StringLiteralExpression:
					attributes["Type"] = "String";
					break;
				case SyntaxKind.InterpolatedStringExpression:
					attributes["Type"] = "String";
					break;
				case SyntaxKind.CharacterLiteralExpression:
					attributes["Type"] = "Character";
					break;
				default: base.Visit(syntaxNode); return;
			}

			var node = new ParserNode { Start = syntaxNode.Span.Start, End = syntaxNode.Span.End };

			foreach (var pair in attributes)
			{
				if (pair.Value == null)
				{ }
				else if (pair.Value is string)
					node.AddAttr(pair.Key, pair.Value as string);
				else if (pair.Value is SyntaxNode sn)
					node.AddAttr(pair.Key, input.Substring(sn.Span.Start, sn.Span.Length), sn.Span.Start, sn.Span.End);
				else if (pair.Value is SyntaxToken st)
					node.AddAttr(pair.Key, input.Substring(st.Span.Start, st.Span.Length), st.Span.Start, st.Span.End);
				else
					throw new Exception($"Unknown attribute: {pair.Key}");
			}

			node.Parent = curNode;
			curNode = node;

			base.Visit(syntaxNode);

			curNode = node.Parent;
		}
	}
}
