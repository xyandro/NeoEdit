using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeoEdit.Common;

namespace NeoEdit.Editor.Content.CSharp
{
	class CSharpWalker : CSharpSyntaxWalker
	{
		NewNode document, curNode;

		public static NewNode Parse(string input, bool strict)
		{
			//Microsoft.CodeAnalysis.CSharp.CSharpCompilation
			var tree = CSharpSyntaxTree.ParseText(input);
			var root = tree.GetCompilationUnitRoot();

			if (strict)
			{
				var error = tree.GetDiagnostics().FirstOrDefault();
				if (error != null)
					throw new Exception(error.ToString());
			}

			var walker = new CSharpWalker(Range.FromIndex(0, input.Length));
			walker.Visit(root);
			return walker.document;
		}

		public CSharpWalker(Range range)
		{
			document = curNode = new NewNode("Document", range);
		}

		void AddNode(SyntaxNode node, Action action)
		{
			var kind = (SyntaxKind)node.RawKind;
			switch (kind)
			{
				case SyntaxKind.NamespaceDeclaration:
				case SyntaxKind.ClassDeclaration:
				case SyntaxKind.MethodDeclaration:
				case SyntaxKind.Block:
				case SyntaxKind.ArrowExpressionClause:
				case SyntaxKind.FieldDeclaration:
				case SyntaxKind.PropertyDeclaration:
				case SyntaxKind.AccessorList:
				case SyntaxKind.GetAccessorDeclaration:
				case SyntaxKind.SetAccessorDeclaration:
				case SyntaxKind.NumericLiteralExpression:
					break;
				default: action(); return;
			}

			var range = Range.FromIndex(node.Span.Start, node.Span.Length);
			var newNode = curNode;
			if (curNode.Range != range)
				newNode = new NewNode(node.Kind().ToString(), range);

			newNode.Parent = curNode;
			curNode.Children.Add(newNode);

			var prevNode = curNode;
			curNode = newNode;

			action();

			curNode = prevNode;
		}

		public override void Visit(SyntaxNode node) => AddNode(node, () => base.Visit(node));
	}
}
