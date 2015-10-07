lexer grammar RevRegExLexer;

LBRACE        : '{' -> pushMode(REPEAT) ;
LBRACKET      : '[' -> pushMode(CHARS);
LPAREN        : '(' ;
RPAREN        : ')' ;
PIPE          : '|' ;
QUESTION      : '?' ;
CHAR          : '\\' . | ~[-{\[()|?] ;

mode REPEAT;
COUNT         : [0-9]+ ;
COMMA         : ',' ;
RBRACE        : '}' -> popMode ;
REPEAT_WS     : [ \r\n\t]+ -> skip ;

mode CHARS;
HYPHEN        : '-' ;
RBRACKET      : ']' -> popMode ;
CHARS_CHAR    : '\\' . | ~[-\]] ;
