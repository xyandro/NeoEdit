parser grammar CSharpParser;

options { tokenVocab = CSharpLexer; }

csharp : content* EOF ;
content : block # ContentBlock
        | expression SEMICOLON # ContentExpression
        | usingns+ # Usings
        | NAMESPACE savename block # Namespace
        | attribute* modifier* type=(CLASS | INTERFACE | STRUCT) savename (COLON savebase (COMMA savebase)*)? generic_constraints block # Class
        | attribute* modifier* ENUM savename (COLON name)? LBRACE (enumvalue (COMMA enumvalue)* COMMA?)? RBRACE # Enum
        | attribute* modifier* (name | TILDE)? (savename | OPERATOR (name | binary_operator)) LPAREN methoddeclparams RPAREN (COLON (THIS | BASE) LPAREN expressionlist RPAREN)? generic_constraints (block | SEMICOLON) # Method
        | attribute* modifier* name THIS LBRACKET methoddeclparams RBRACKET LBRACE ((GET | SET) content)* RBRACE # Indexer
        | attribute* modifier* DELEGATE name savename LPAREN methoddeclparams RPAREN SEMICOLON # Delegate
        | attribute* modifier* EVENT name savename (LBRACE ((ADD | REMOVE) block)* RBRACE | SEMICOLON) # Event
        | attribute* modifier* name savename LBRACE propertyaccess* RBRACE # Property
        | SWITCH LPAREN saveconditionexpression RPAREN LBRACE cases* RBRACE # Switch
        | YIELD? RETURN expression? SEMICOLON # Return
        | THROW expression? SEMICOLON # Throw
        | attribute* modifier* name saveassignedname (COMMA saveassignedname)* SEMICOLON # Declaration
        | type=IF LPAREN saveconditionexpression RPAREN saveblockcontent (ELSE IF LPAREN saveconditionexpression RPAREN saveblockcontent)* (ELSE saveblockcontent)? # If
        | IDENTIFIER COLON # Label
        | GOTO (savename | CASE (expression | DEFAULT)) SEMICOLON # Goto
        | FIXED LPAREN name name ASSIGN expression RPAREN content # Fixed
        | (CHECKED | UNCHECKED) block # Checked
        | USING LPAREN ((savename ASSIGN)? expression | name savename ASSIGN expression (COMMA savename ASSIGN expression)*) RPAREN content # Using
        | LOCK LPAREN expression RPAREN content # Lock
        | FOR LPAREN (fordecl (COMMA fordecl)*)? SEMICOLON expressionlist SEMICOLON expressionlist RPAREN content # For
        | FOREACH LPAREN name name IN saveconditionexpression RPAREN content # Foreach
        | WHILE LPAREN saveconditionexpression RPAREN content # While
        | DO content WHILE LPAREN saveconditionexpression RPAREN SEMICOLON # Do
        | YIELD? BREAK SEMICOLON # Break
        | CONTINUE SEMICOLON # Continue
        | TRY saveblock (CATCH (LPAREN name savename? RPAREN)? saveblock)? (FINALLY saveblock)? # Try
        | LBRACKET globalattrvalue (COMMA globalattrvalue)* RBRACKET # GlobalAttr
        | savesemicolon # Empty
        ;
usingns : USING (name ASSIGN)? savename SEMICOLON ;
saveblockcontent : saveblock | content ;
saveconditionexpression : expression ;
id : IDENTIFIER | ADD | ALIAS | ASCENDING | ASYNC | AWAIT | BY | DESCENDING | EQUALS | FROM | GET | GROUP | INTO | JOIN | LET | ON | ORDERBY | PARTIAL | REMOVE | SELECT | SET | WHERE | YIELD | BASE | THIS | DEFAULT | ASSEMBLY ;
name : (id DOUBLE_COLON)? id INTERR? MULT? (LT name? (COMMA name?)* GT)? (LBRACKET COMMA* RBRACKET)* (DOT name)* ;
savename : name ;
savebase : name ;
propertyaccess : modifier* (GET | SET) (savesemicolon | saveblock) ;
savesemicolon : SEMICOLON ;
cases : ((CASE saveconditionexpression | DEFAULT) COLON)+ content+ ;
modifier : ABSTRACT | ASYNC | CONST | EXTERN | INTERNAL | NEW | OVERRIDE | PARTIAL | PRIVATE | PROTECTED | PUBLIC | READONLY | SEALED | STATIC | UNSAFE | VIRTUAL | VOLATILE | IMPLICIT | EXPLICIT ;
attrvalue : ((RETURN | name) COLON)? name (LPAREN expressionlist RPAREN)? ;
attribute : LBRACKET attrvalue (COMMA attrvalue)* RBRACKET ;
globalattrvalue : ASSEMBLY COLON name (LPAREN expressionlist RPAREN)? ;
fordecl : name? name ASSIGN expression ;
assignedname : name (ASSIGN expression)? ;
saveassignedname : savename (ASSIGN expression)? ;
block : LBRACE content* RBRACE ;
saveblock : block ;
enumvalue : attribute* assignedname ;
generic_constraint : name | CLASS | STRUCT | NEW LPAREN RPAREN ;
generic_constraints : (WHERE name COLON generic_constraint (COMMA generic_constraint)*)* ;
methoddeclparam : attribute* (THIS | PARAMS | OUT | REF)* name assignedname ;
methoddeclparams : (methoddeclparam (COMMA methoddeclparam)*)? ;
expression : LPAREN expression RPAREN
           | LPAREN expression RPAREN expression
           | (name | LPAREN (name? name (COMMA name? name)*)? RPAREN) LAMBDA (expression | block)
           | (DEC | INC | MULT | BANG | MINUS | PLUS | TILDE | AND) expression
           | expression (DEC | INC)
           | expression binary_operator expression
           | name
           | (STACKALLOC | NEW) name? methodcallparams? (LBRACKET expressionlist RBRACKET)* inline?
           | expression methodcallparams
           | expression LBRACKET expression (COMMA expression)* RBRACKET
           | expression INTERR expression COLON expression
           | AWAIT expression
           | DELEGATE (LPAREN methoddeclparams RPAREN)? block
           | (CHECKED | UNCHECKED) LPAREN expression RPAREN
           | linq
           | inline
           | NUMBER | CHARACTER | STR | NULL | TRUE | FALSE
           ;
expressionlist: (expression (COMMA expression)*)? ;
binary_operator : AS | IS | AND | OR | XOR | DIV | GT | LT | MINUS | LAND | EQ | GE | LE | LEFT_SHIFT | GT GT | NE | LOR | MOD | PLUS | MULT | DOT | PTR | ASSIGN | PLUS_ASSIGN | AND_ASSIGN | DIV_ASSIGN | LEFT_SHIFT_ASSIGN | GT GE | MOD_ASSIGN | MULT_ASSIGN | OR_ASSIGN | MINUS_ASSIGN | XOR_ASSIGN | COALESCE ;
inline : LBRACE expressionlist COMMA? RBRACE ;
methodcallparam : OUT? REF? (name COLON)? expression ;
methodcallparams : LPAREN (methodcallparam (COMMA methodcallparam)*)? RPAREN ;

linq : linq_from linq_body ;
linq_from : FROM name? name IN expression ;
linq_body : (linq_from | LET name ASSIGN expression | WHERE expression | JOIN name? name IN expression ON expression EQUALS expression (INTO name)? | ORDERBY linq_order (COMMA linq_order)*)* (SELECT expression | GROUP expression BY expression) (INTO name linq_body)? ;
linq_order : expression (ASCENDING | DESCENDING)? ;
