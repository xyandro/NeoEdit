parser grammar CSharpParser;

options { tokenVocab = CSharpLexer; }

csharp : topcontent* EOF ;
topcontent : EXTERN ALIAS identifier SEMICOLON # ExternAlias
           | USING (type | identifier ASSIGN type) SEMICOLON # UsingNS
           | NAMESPACE Name=type LBRACE topcontent* RBRACE SEMICOLON? # Namespace
           | attribute* modifier* NodeType=(CLASS | INTERFACE | STRUCT) Name=type (COLON type (COMMA type)*)? where* LBRACE topcontent* RBRACE SEMICOLON? # Class
           | attribute* modifier* Return=type (Name=type | THIS LBRACKET paramlist RBRACKET) (LBRACE topcontent* RBRACE (ASSIGN expression SEMICOLON)? | LAMBDA expression SEMICOLON) # Property
           | attribute* modifier* Name=(GET | SET | ADD | REMOVE) (methodcontent | LAMBDA expression SEMICOLON) # Accessor
           | attribute* modifier* Return=type? (Name=type | OPERATOR Operator=operator) LPAREN paramlist RPAREN where* (COLON (THIS | BASE) LPAREN expressionlist RPAREN)? (methodcontent | LAMBDA expression SEMICOLON) # Method
           | attribute* modifier* Return=type vardecl (COMMA vardecl)* SEMICOLON # Field
           | attribute* modifier* EVENT Return=type Name=type (LBRACE topcontent* RBRACE | (ASSIGN expression)? SEMICOLON)? # Event
           | attribute* modifier* ENUM Name=identifier (COLON Return=type)? LBRACE enumvalue (COMMA enumvalue)* COMMA? RBRACE SEMICOLON? # Enum
           | attribute # GlobalAttribute // Keep this last so it doesn't grab attributes that should be assigned to other items
           ;
methodcontent : modifier* Return=type vardecl (COMMA vardecl)* SEMICOLON # Variable
              | Name=identifier COLON # GotoLabel
              | LBRACE methodcontent* RBRACE # Block
              | expression SEMICOLON # ContentExpression
              | IF LPAREN Condition=expression RPAREN methodcontent (ELSE IF LPAREN expression RPAREN methodcontent)* (ELSE methodcontent)? # If
              | WHILE LPAREN Condition=expression RPAREN methodcontent # While
              | DO methodcontent WHILE LPAREN Condition=expression RPAREN SEMICOLON # Do
              | FOR LPAREN forinitializer? SEMICOLON expression? SEMICOLON foriterator? RPAREN methodcontent # For
              | FOREACH LPAREN Return=type Variable=identifier IN List=expression RPAREN methodcontent # Foreach
              | SWITCH LPAREN Switch=expression RPAREN LBRACE switchcase* RBRACE # Switch
              | GOTO gotodestination SEMICOLON # Goto
              | Type=(CHECKED | UNCHECKED) methodcontent # Checked
              | UNSAFE methodcontent # Unsafe
              | LOCK LPAREN expression RPAREN methodcontent # Lock
              | Type=(USING | FIXED) LPAREN (Return=type vardecl (COMMA vardecl)* | expression) RPAREN methodcontent # Using
              | TRY methodcontent (CATCH (LPAREN type identifier? RPAREN)? methodcontent)* (FINALLY methodcontent)? # Try
              | YIELD? BREAK SEMICOLON # Break
              | CONTINUE SEMICOLON # Continue
              | YIELD? RETURN expression? SEMICOLON # Return
              | THROW expression? SEMICOLON # Throw
              | SEMICOLON # Empty
              ;
operator : type | AND | BANG | DEC | DIV | EQ | FALSE | GE | GT | GT GT | INC | LE | LEFT_SHIFT | LT | MINUS | MOD | MULT | NE | OR | PLUS | TILDE | TRUE | XOR ;
modifier : ABSTRACT | ASYNC | CONST | DELEGATE | EXPLICIT | EXTERN | FIXED | IMPLICIT | INTERNAL | NEW | OVERRIDE | PARTIAL | PRIVATE | PROTECTED | PUBLIC | READONLY | SEALED | STATIC | UNSAFE | VIRTUAL | VOLATILE ;

enumvalue : attribute* Name=identifier (ASSIGN expression)? ;
forinitializer : (type? vardecl (COMMA vardecl)*) ;
foriterator : expression (COMMA expression)* ;
vardecl : Name=type (ASSIGN initialize)? ;
switchcase : ((CASE expression | DEFAULT) COLON)* methodcontent+ ;
gotodestination : (identifier | CASE expression | DEFAULT) ;

identifier : IDENTIFIER | ADD | ALIAS | ASCENDING | ASSEMBLY | ASYNC | AWAIT | BY | DESCENDING | EQUALS | FIELD | FROM | GET | GROUP | INTO | JOIN | LET | METHOD | MODULE | ON | ORDERBY | PARAM | PARTIAL | PROPERTY | REMOVE | SELECT | SET | TYPE | WHERE | YIELD | THIS | BASE ;
typepart : identifier (LT generic_type (COMMA generic_type)* GT)? ;
type : (identifier DOUBLE_COLON)? TILDE? typepart (DOT typepart)* unnamed_type ;
unnamed_type : (INTERR | MULT)? (LBRACKET NUMBER? (COMMA NUMBER?)* RBRACKET)* ;
generic_type : attribute* (IN | OUT | REF)* type? ;

initialize : expression | LBRACE initialize_list RBRACE | LBRACKET expression RBRACKET ASSIGN expression ;
initialize_list : (initialize (COMMA initialize)* COMMA?)? ;

methodparamslist : (methodparam (COMMA methodparam)*)? ;
methodparam : (identifier COLON)? (IN | OUT | REF)? expression ;
expressionlist : (expression (COMMA expression)*)? ;
expression : identifier # ExpressionIdentifier
           | expression (AND | AND_ASSIGN | ASSIGN | DIV | DIV_ASSIGN | INTERR? DOT | EQ | GE | GT | GT GE | GT GT | LAND | LE | LEFT_SHIFT | LEFT_SHIFT_ASSIGN | LOR | LT | MINUS | MINUS_ASSIGN | MOD | MOD_ASSIGN | MULT | MULT_ASSIGN | NE | OR | OR_ASSIGN | PLUS | PLUS_ASSIGN | PTR | XOR | XOR_ASSIGN) expression # ExpressionBinaryOp
           | expression INTERR expression COLON expression # ExpressionTernaryOp
           | (INC | DEC | PLUS | MINUS | BANG | TILDE | AND | MULT) expression # ExpressionUnary
           | expression (INC | DEC) # ExpressionUnary
           | expression LBRACKET expression (COMMA expression)* RBRACKET # ExpressionArray
           | (NEW | STACKALLOC) (type | unnamed_type) ((LPAREN expressionlist RPAREN) | (LBRACE initialize_list RBRACE))* # ExpressionNew
           | (CHECKED | UNCHECKED) LPAREN expression RPAREN # ExpressionChecked
           | expression (AS | IS) type # ExpressionCheckType
           | expression COALESCE expression # ExpressionCoalesce
           | LPAREN type RPAREN expression # ExpressionCast
           | LPAREN expression RPAREN # ExpressionParens
           | type # ExpressionType
           | AWAIT expression # ExpressionAwait
           | expression LPAREN methodparamslist RPAREN # ExpressionMethodCall
           | DELEGATE (LPAREN paramlist RPAREN)? methodcontent # ExpressionDelegate
           | ASYNC? (identifier | LPAREN (type? identifier (COMMA type? identifier)*)? RPAREN) LAMBDA (expression | methodcontent) # ExpressionLambda
           | TYPEOF LPAREN type RPAREN # ExpressionTypeOf
           | DEFAULT LPAREN type RPAREN # ExpressionDefault
           | linq # ExpressionLinq
           | (STR | CHARACTER | NUMBER | TRUE | FALSE | NULL) # ExpressionLiteral
           ;

param : attribute* (IN | OUT | REF | PARAMS | THIS)? type Name=identifier (ASSIGN expression)? ;
paramlist : (param (COMMA param)*)? | ARGLIST ;

attrvalue : type (LPAREN expressionlist RPAREN)? ;
attribute : LBRACKET ((ASSEMBLY | EVENT | FIELD | METHOD | MODULE | PARAM | PROPERTY | RETURN | TYPE | TYPEVAR) COLON)? attrvalue (COMMA attrvalue)* RBRACKET ;

wherelimit : (type | CLASS | STRUCT | NEW LPAREN RPAREN) ;
where : WHERE identifier COLON wherelimit (COMMA wherelimit)* ;

linq : linq_from linq_body ;
linq_from : FROM type? identifier IN expression ;
linq_body : (linq_from | LET identifier ASSIGN expression | WHERE expression | JOIN type? identifier IN expression ON expression EQUALS expression (INTO identifier)? | ORDERBY linq_order (COMMA linq_order)*)* (SELECT expression | GROUP expression BY expression) (INTO identifier linq_body)? ;
linq_order : expression (ASCENDING | DESCENDING)? ;
