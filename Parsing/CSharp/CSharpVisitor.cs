using System.Collections.Generic;
using Antlr4.Runtime;

namespace NeoEdit
{
	namespace Parsing
	{
		class CSharpVisitor : CSharp4BaseVisitor<object>
		{
			public CSharpNode Root { get; private set; }
			readonly Stack<CSharpNode> stack = new Stack<CSharpNode>();
			readonly string input;
			public CSharpVisitor(string input)
			{
				this.input = input;
				stack.Push(Root = new CSharpNode { NodeType = CSharpNodeType.Root });
			}

			CSharpNode CreateNode(CSharpNodeType type, ParserRuleContext context)
			{
				var node = new CSharpNode { NodeType = type, Parent = stack.Peek() };
				node.LocationContext = context;
				return node;
			}

			public override object VisitType_declaration(CSharp4Parser.Type_declarationContext context)
			{
				var classDef = context.class_definition();
				if (classDef == null)
					return null;

				var node = CreateNode(CSharpNodeType.Class, context);
				node.AddAttribute("Name", input, classDef.identifier());

				stack.Push(node);
				base.VisitType_declaration(context);
				stack.Pop();

				return null;
			}

			CSharpNode methodNode = null;
			public override object VisitClass_member_declaration(CSharp4Parser.Class_member_declarationContext context)
			{
				methodNode = CreateNode(CSharpNodeType.Method, context);
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
