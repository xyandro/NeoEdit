using System.Collections.Generic;
using Antlr4.Runtime;

namespace NeoEdit.Parsing
{
	class CSharpVisitor : CSharp4BaseVisitor<object>
	{
		const string RootStr = "Root";
		const string Name = "Name";

		public ParserNode Root { get; private set; }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		public CSharpVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = RootStr, Start = 0, End = input.Length });
		}

		public override object VisitType_declaration(CSharp4Parser.Type_declarationContext ctx)
		{
			var classDefCtx = ctx.class_definition();
			if (classDefCtx == null)
				return null;

			var node = new ParserNode { Type = "Class", Parent = stack.Peek(), LocationContext = ctx };
			node.AddAttr(Name, input, classDefCtx.identifier());
			stack.Push(node);
			base.VisitType_declaration(ctx);
			stack.Pop();

			return null;
		}

		ParserNode methodNode = null;
		public override object VisitClass_member_declaration(CSharp4Parser.Class_member_declarationContext ctx)
		{
			methodNode = new ParserNode { Type = "Method", Parent = stack.Peek(), LocationContext = ctx };
			return base.VisitClass_member_declaration(ctx);
		}

		public override object VisitMethod_member_name2(CSharp4Parser.Method_member_name2Context ctx)
		{
			if (methodNode != null)
			{
				methodNode.AddAttr(Name, input, ctx);
				methodNode = null;
			}
			return base.VisitMethod_member_name2(ctx);
		}
	}
}
