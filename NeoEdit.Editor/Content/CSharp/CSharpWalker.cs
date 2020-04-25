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
			document = curNode = new NewNode(range);
		}

		void AddNode(SyntaxNode node, Action action)
		{
			var range = Range.FromIndex(node.Span.Start, node.Span.Length);
			var newNode = curNode;
			if (curNode.Range != range)
				newNode = new NewNode(range);

			newNode.Parent = curNode;
			curNode.Children.Add(newNode);

			var prevNode = curNode;
			curNode = newNode;

			action();

			curNode = prevNode;
		}

		public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
		{
			AddNode(node, () => base.VisitNamespaceDeclaration(node));
		}

		public override void VisitClassDeclaration(ClassDeclarationSyntax node)
		{
			AddNode(node, () => base.VisitClassDeclaration(node));
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			AddNode(node, () => base.VisitMethodDeclaration(node));
		}

		public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
		{
			AddNode(node, () => base.VisitPropertyDeclaration(node));
		}

		public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
		{
			AddNode(node, () => base.VisitFieldDeclaration(node));
		}
	}
}
