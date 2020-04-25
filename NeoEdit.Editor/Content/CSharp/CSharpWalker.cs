using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NeoEdit.Common;

namespace NeoEdit.Editor.Content.CSharp
{
	class CSharpWalker : CSharpSyntaxWalker
	{
		NewNode document, curNode;
		readonly List<NewNode> nodesToAdd = new List<NewNode>();

		public static NewNode Parse(string input, bool strict)
		{
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
			walker.AddOutOfOrderNodes();
			return walker.document;
		}

		public CSharpWalker(Range range) : base(SyntaxWalkerDepth.Trivia)
		{
			document = curNode = new NewNode("Document", range);
		}

		void AddOutOfOrderNodes()
		{
			var parentNode = document;
			foreach (var node in nodesToAdd)
			{
				while (!parentNode.Range.Contains(node.Range))
					parentNode = parentNode.Parent;

				while (true)
				{
					var child = parentNode.Children.FirstOrDefault(x => x.Range.Contains(node.Range));
					if (child != null)
						parentNode = child;
					else
						break;
				}

				var index = parentNode.Children.FindIndex(x => x.Range.Start > node.Range.Start);
				if (index == -1)
					index = parentNode.Children.Count;
				parentNode.Children.Insert(index, node);
			}
			nodesToAdd.Clear();
		}

		public override void VisitTrivia(SyntaxTrivia trivia)
		{
			var kind = (SyntaxKind)trivia.RawKind;
			var createNode = false;
			switch (kind)
			{
				case SyntaxKind.SingleLineCommentTrivia:
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
				case SyntaxKind.MultiLineCommentTrivia:
				case SyntaxKind.RegionDirectiveTrivia:
				case SyntaxKind.EndRegionDirectiveTrivia:
					createNode = true;
					break;
			}

			if (createNode)
			{
				var range = Range.FromIndex(trivia.Span.Start, trivia.Span.Length);
				var newNode = new NewNode(kind.ToString(), range);
				nodesToAdd.Add(newNode);
			}

			base.VisitTrivia(trivia);
		}

		public override void Visit(SyntaxNode node)
		{
			var kind = (SyntaxKind)node.RawKind;
			var createNode = false;
			switch (kind)
			{
				case SyntaxKind.QualifiedName:
				case SyntaxKind.UsingDirective:
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
					createNode = true;
					break;
			}

			NewNode prevNode = null;
			if (createNode)
			{
				var range = Range.FromIndex(node.Span.Start, node.Span.Length);
				var newNode = curNode;
				if (curNode.Range != range)
					newNode = new NewNode(node.Kind().ToString(), range);

				newNode.Parent = curNode;
				curNode.Children.Add(newNode);

				prevNode = curNode;
				curNode = newNode;
			}

			base.Visit(node);

			if (createNode)
				curNode = prevNode;
		}
	}
}
