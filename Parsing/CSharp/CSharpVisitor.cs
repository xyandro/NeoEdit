using System.Collections.Generic;
using Antlr4.Runtime;

namespace NeoEdit
{
	namespace Parsing
	{
		class CSharpVisitor : CSharp4BaseVisitor<object>
		{
			public ParserNode Root { get; private set; }
			readonly Stack<ParserNode> stack = new Stack<ParserNode>();
			readonly string input;
			public CSharpVisitor(string input)
			{
				this.input = input;
				stack.Push(Root = new ParserNode { Type = "Root" });
			}

			ParserNode CreateNode(string type, ParserRuleContext context)
			{
				var node = new ParserNode { Type = type, Parent = stack.Peek() };
				node.LocationContext = context;
				return node;
			}

			public override object VisitType_declaration(CSharp4Parser.Type_declarationContext context)
			{
				var classDef = context.class_definition();
				if (classDef == null)
					return null;

				var node = CreateNode("Class", context);
				node.AddAttribute("Name", input, classDef.identifier());

				stack.Push(node);
				base.VisitType_declaration(context);
				stack.Pop();

				return null;
			}

			ParserNode methodNode = null;
			public override object VisitClass_member_declaration(CSharp4Parser.Class_member_declarationContext context)
			{
				methodNode = CreateNode("Method", context);
				return base.VisitClass_member_declaration(context);
			}

			public override object VisitMethod_member_name2(CSharp4Parser.Method_member_name2Context context)
			{
				if (methodNode != null)
				{
					methodNode.AddAttribute("Name", input, context);
					methodNode = null;
				}
				return base.VisitMethod_member_name2(context);
			}
		}
	}
}
