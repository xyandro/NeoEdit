using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using NeoEdit.TextEdit.Parsing.CSharp.Parser;

namespace NeoEdit.TextEdit.Parsing.CSharp
{
	class CSharpVisitor : CSharp4BaseVisitor<object>
	{
		const string ATTRIBUTE = "attribute";
		const string BASE = "base";
		const string BLOCK = "block";
		const string CASE = "case";
		const string CONDITION = "condition";
		const string CONSTRUCTOR = "constructor";
		const string DECLARATION = "declaration";
		const string DESTRUCTOR = "destructor";
		const string ENUM = "enum";
		const string FIELD = "field";
		const string INITIALIZER = "initializer";
		const string INTERFACEMEMBER = "interfacemember";
		const string ITERATOR = "iterator";
		const string LABEL = "label";
		const string METHOD = "method";
		const string MODIFIER = "modifier";
		const string NAME = "Name";
		const string OPERATOR = "operator";
		const string PARAMETER = "parameter";
		const string PROPERTY = "property";
		const string RETURNTYPE = "returntype";
		const string ROOTSTR = "Root";
		const string STATEMENT = "statement";
		const string USING = "using";
		const string USINGS = "usings";

		readonly ParserNode Root;
		ParserNode Parent { get { return stack.Peek(); } }
		readonly Stack<ParserNode> stack = new Stack<ParserNode>();
		readonly string input;
		CSharpVisitor(string input)
		{
			this.input = input;
			stack.Push(Root = new ParserNode { Type = ROOTSTR, Start = 0, End = input.Length });
		}

		public static ParserNode Parse(string input, IParseTree tree)
		{
			var visitor = new CSharpVisitor(input);
			visitor.Visit(tree);
			return visitor.Root;
		}

		public override object VisitAll_member_modifier(CSharp4Parser.All_member_modifierContext context)
		{
			Parent.AddAttr(MODIFIER, input, context);
			return null;
		}

		public override object VisitAttribute_section(CSharp4Parser.Attribute_sectionContext context)
		{
			Parent.AddAttr(ATTRIBUTE, input, context);
			return null;
		}

		public override object VisitBlock(CSharp4Parser.BlockContext context)
		{
			stack.Push(new ParserNode { Type = BLOCK, Parent = Parent, LocationParserRule = context });
			base.VisitBlock(context);
			stack.Pop();
			return null;
		}

		public override object VisitBreak_statement(CSharp4Parser.Break_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.BREAK());
			return base.VisitBreak_statement(context);
		}

		public override object VisitClass_base(CSharp4Parser.Class_baseContext context)
		{
			context.Take(c => c.class_type()).Do(c => Parent.AddAttr(BASE, input, c));
			context.Take(c => c.interface_type()).Do(c => Parent.AddAttr(BASE, input, c));
			return null;
		}

		public override object VisitClass_definition(CSharp4Parser.Class_definitionContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.CLASS());
			return base.VisitClass_definition(context);
		}

		public override object VisitClass_member_declaration(CSharp4Parser.Class_member_declarationContext context)
		{
			stack.Push(new ParserNode { Parent = Parent, LocationParserRule = context });
			base.VisitClass_member_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitConstant_declaration2(CSharp4Parser.Constant_declaration2Context context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.CONST());
			return base.VisitConstant_declaration2(context);
		}

		public override object VisitConstructor_declaration2(CSharp4Parser.Constructor_declaration2Context context)
		{
			Parent.Type = METHOD;
			Parent.AddAttr(ParserNode.TYPE, CONSTRUCTOR);
			return base.VisitConstructor_declaration2(context);
		}

		public override object VisitContinue_statement(CSharp4Parser.Continue_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.CONTINUE());
			return base.VisitContinue_statement(context);
		}

		public override object VisitConversion_operator_declarator(CSharp4Parser.Conversion_operator_declaratorContext context)
		{
			Parent.Type = OPERATOR;
			return base.VisitConversion_operator_declarator(context);
		}

		public override object VisitDeclaration_statement(CSharp4Parser.Declaration_statementContext context)
		{
			stack.Push(new ParserNode { Type = DECLARATION, Parent = Parent, LocationParserRule = context });
			base.VisitDeclaration_statement(context);
			stack.Pop();
			return null;
		}

		public override object VisitDelegate_definition(CSharp4Parser.Delegate_definitionContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.DELEGATE());
			return base.VisitDelegate_definition(context);
		}

		public override object VisitDestructor_body(CSharp4Parser.Destructor_bodyContext context)
		{
			Parent.Type = METHOD;
			Parent.AddAttr(ParserNode.TYPE, DESTRUCTOR);
			return base.VisitDestructor_body(context);
		}

		public override object VisitDo_statement(CSharp4Parser.Do_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.DO());
			return base.VisitDo_statement(context);
		}

		public override object VisitEnum_definition(CSharp4Parser.Enum_definitionContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.ENUM());
			return base.VisitEnum_definition(context);
		}

		public override object VisitEnum_member_declaration(CSharp4Parser.Enum_member_declarationContext context)
		{
			stack.Push(new ParserNode { Type = ENUM, Parent = Parent, LocationParserRule = context });
			base.VisitEnum_member_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitEvent_declaration2(CSharp4Parser.Event_declaration2Context context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.EVENT());
			return base.VisitEvent_declaration2(context);
		}

		public override object VisitField_declaration2(CSharp4Parser.Field_declaration2Context context)
		{
			Parent.Type = FIELD;
			return base.VisitField_declaration2(context);
		}

		public override object VisitFixed_parameter(CSharp4Parser.Fixed_parameterContext context)
		{
			Parent.AddAttr(PARAMETER, input, context);
			return null;
		}

		public override object VisitFixed_statement(CSharp4Parser.Fixed_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.FIXED());
			return base.VisitFixed_statement(context);
		}

		public override object VisitFor_statement(CSharp4Parser.For_statementContext context)
		{
			var node = Parent;
			node.AddAttr(ParserNode.TYPE, input, context.FOR());
			context.Take(c => c.for_initializer()).Do(c => node.AddAttr(INITIALIZER, input, c));
			context.Take(c => c.for_condition()).Do(c => node.AddAttr(CONDITION, input, c));
			context.Take(c => c.for_iterator()).Do(c => node.AddAttr(ITERATOR, input, c));
			Visit(context.embedded_statement());
			return null;
		}

		public override object VisitForeach_statement(CSharp4Parser.Foreach_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.FOREACH());
			return base.VisitForeach_statement(context);
		}

		public override object VisitGlobal_attribute_section(CSharp4Parser.Global_attribute_sectionContext context)
		{
			new ParserNode { Type = ATTRIBUTE, Parent = Parent, LocationParserRule = context };
			return null;
		}

		public override object VisitGoto_statement(CSharp4Parser.Goto_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.GOTO());
			return base.VisitGoto_statement(context);
		}

		public override object VisitIdentifier(CSharp4Parser.IdentifierContext context)
		{
			Parent.AddAttr(NAME, input, context);
			return null;
		}

		public override object VisitIf_statement(CSharp4Parser.If_statementContext context)
		{
			var node = Parent;
			node.AddAttr(ParserNode.TYPE, input, context.IF());

			while (true)
			{
				node.AddAttr(CONDITION, input, context.boolean_expression());
				var body = context.if_body();
				Visit(body[0]);
				if (body.Length == 1)
					break;

				context = body[1].Cast<CSharp4Parser.IfBodySingleContext>().Take(c => c.simple_embedded_statement()).Take(c => c.selection_statement()).Take(c => c.if_statement());
				if (context != null)
					continue;

				Visit(body[1]);
				break;
			}

			return null;
		}

		public override object VisitIndexer_declaration2(CSharp4Parser.Indexer_declaration2Context context)
		{
			Parent.Type = OPERATOR;
			return base.VisitIndexer_declaration2(context);
		}

		public override object VisitInterface_base(CSharp4Parser.Interface_baseContext context)
		{
			context.Take(c => c.interface_type_list()).Take(c => c.interface_type()).Do(c => Parent.AddAttr(BASE, input, c));
			return null;
		}

		public override object VisitInterface_definition(CSharp4Parser.Interface_definitionContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.INTERFACE());
			return base.VisitInterface_definition(context);
		}

		public override object VisitInterface_member_declaration(CSharp4Parser.Interface_member_declarationContext context)
		{
			stack.Push(new ParserNode { Type = INTERFACEMEMBER, Parent = Parent, LocationParserRule = context });
			base.VisitInterface_member_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitLabeled_statement(CSharp4Parser.Labeled_statementContext context)
		{
			var node = new ParserNode { Type = LABEL, Parent = Parent, LocationParserRule = context };
			node.End = context.COLON().Symbol.StopIndex + 1;
			stack.Push(node);
			Visit(context.identifier());
			stack.Pop();
			Visit(context.statement());
			return null;
		}

		public override object VisitLocal_variable_declaration(CSharp4Parser.Local_variable_declarationContext context)
		{
			Parent.AddAttr(RETURNTYPE, input, context.t);
			Visit(context.local_variable_declarators());
			return null;
		}

		public override object VisitLocal_variable_initializer(CSharp4Parser.Local_variable_initializerContext context)
		{
			stack.Push(new ParserNode { Type = INITIALIZER, Parent = Parent, LocationParserRule = context });
			base.VisitLocal_variable_initializer(context);
			stack.Pop();
			return null;
		}

		public override object VisitLock_statement(CSharp4Parser.Lock_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.LOCK());
			return base.VisitLock_statement(context);
		}

		public override object VisitMethod_declaration2(CSharp4Parser.Method_declaration2Context context)
		{
			Parent.Type = METHOD;
			return base.VisitMethod_declaration2(context);
		}

		public override object VisitNamespace_declaration(CSharp4Parser.Namespace_declarationContext context)
		{
			var node = new ParserNode { Parent = Parent, LocationParserRule = context };
			node.AddAttr(ParserNode.TYPE, input, context.NAMESPACE());
			stack.Push(node);
			base.VisitNamespace_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitOperator_declaration2(CSharp4Parser.Operator_declaration2Context context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.OPERATOR());
			return base.VisitOperator_declaration2(context);
		}

		public override object VisitParameter_array(CSharp4Parser.Parameter_arrayContext context)
		{
			Parent.AddAttr(PARAMETER, input, context);
			return null;
		}

		public override object VisitProperty_declaration2(CSharp4Parser.Property_declaration2Context context)
		{
			Parent.Type = PROPERTY;
			return base.VisitProperty_declaration2(context);
		}

		public override object VisitQualified_identifier(CSharp4Parser.Qualified_identifierContext context)
		{
			Parent.AddAttr(NAME, input, context);
			return null;
		}

		public override object VisitReturn_statement(CSharp4Parser.Return_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.RETURN());
			return base.VisitReturn_statement(context);
		}

		public override object VisitSimple_embedded_statement(CSharp4Parser.Simple_embedded_statementContext context)
		{
			stack.Push(new ParserNode { Parent = Parent, LocationParserRule = context });
			base.VisitSimple_embedded_statement(context);
			stack.Pop();
			return null;
		}

		public override object VisitStruct_definition(CSharp4Parser.Struct_definitionContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.STRUCT());
			return base.VisitStruct_definition(context);
		}

		public override object VisitStruct_interfaces(CSharp4Parser.Struct_interfacesContext context)
		{
			context.Take(c => c.interface_type_list()).Take(c => c.interface_type()).Do(c => Parent.AddAttr(BASE, input, c));
			return null;
		}

		public override object VisitStruct_member_declaration(CSharp4Parser.Struct_member_declarationContext context)
		{
			stack.Push(new ParserNode { Parent = Parent, LocationParserRule = context });
			base.VisitStruct_member_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitSwitch_label(CSharp4Parser.Switch_labelContext context)
		{
			Parent.AddAttr(LABEL, input, context);
			context.Take(c => c.constant_expression()).Do(c => Parent.AddAttr(CONDITION, input, c));
			return null;
		}

		public override object VisitSwitch_section(CSharp4Parser.Switch_sectionContext context)
		{
			stack.Push(new ParserNode { Type = CASE, Parent = Parent, LocationParserRule = context });
			base.VisitSwitch_section(context);
			stack.Pop();
			return null;
		}

		public override object VisitSwitch_statement(CSharp4Parser.Switch_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.SWITCH());
			return base.VisitSwitch_statement(context);
		}

		public override object VisitTry_statement(CSharp4Parser.Try_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.TRY());
			return base.VisitTry_statement(context);
		}

		public override object VisitType_declaration(CSharp4Parser.Type_declarationContext context)
		{
			stack.Push(new ParserNode { Parent = Parent, LocationParserRule = context });
			base.VisitType_declaration(context);
			stack.Pop();
			return null;
		}

		public override object VisitTyped_member_declaration(CSharp4Parser.Typed_member_declarationContext context)
		{
			context.Take(c => c.type()).Do(c => Parent.AddAttr(RETURNTYPE, input, c));
			foreach (var child in context.children)
			{
				if (child == context.type())
					continue;
				Visit(child);
			}
			return null;
		}

		public override object VisitUnchecked_statement(CSharp4Parser.Unchecked_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.UNCHECKED());
			return base.VisitUnchecked_statement(context);
		}

		public override object VisitUsing_directives(CSharp4Parser.Using_directivesContext context)
		{
			var parent = Parent;
			var usingDirectives = context.using_directive();
			if (usingDirectives.Length > 1)
				parent = new ParserNode { Type = USINGS, Parent = parent, LocationParserRule = context };
			foreach (var usingDirective in usingDirectives)
				new ParserNode { Type = USING, Parent = parent, LocationParserRule = usingDirective };
			return null;
		}

		public override object VisitUsing_statement(CSharp4Parser.Using_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.USING());
			return base.VisitUsing_statement(context);
		}

		public override object VisitWhile_statement(CSharp4Parser.While_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.WHILE());
			return base.VisitWhile_statement(context);
		}

		public override object VisitYield_statement(CSharp4Parser.Yield_statementContext context)
		{
			Parent.AddAttr(ParserNode.TYPE, input, context.yield_contextual_keyword());
			if (context.RETURN() != null)
				Parent.AddAttr(ParserNode.TYPE, input, context.RETURN());
			if (context.BREAK() != null)
				Parent.AddAttr(ParserNode.TYPE, input, context.BREAK());
			return base.VisitYield_statement(context);
		}
	}
}
