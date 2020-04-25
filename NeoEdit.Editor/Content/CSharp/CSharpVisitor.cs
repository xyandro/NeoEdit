using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;
using NeoEdit.Editor.Transactional;
using NeoEdit.Editor.Transactional.View;

namespace NeoEdit.Editor.Content.CSharp
{
	class CSharpVisitor : CSharpSyntaxWalker
	{
		public static ParserNode Parse(string input, bool strict)
		{
			//Microsoft.CodeAnalysis.CSharp.CSharpCompilation
			var tree = CSharpSyntaxTree.ParseText(input);
			var root = tree.GetCompilationUnitRoot();

			var error = tree.GetDiagnostics().FirstOrDefault();
			if (error != null)
				throw new Exception(error.ToString());

			var walker = new CSharpVisitor();
			walker.Visit(root);

			return new ParserNode { Start = 0, End = 0 };
		}

		public static string Comment(NEText text, INEView textView, Range range)
		{
			return "";
		}

		public static string Uncomment(NEText text, INEView textView, Range range)
		{
			return "";
		}

		public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			base.VisitMethodDeclaration(node);
		}
	}
}
