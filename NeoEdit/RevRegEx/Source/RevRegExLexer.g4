lexer grammar RevRegExLexer;

LBRACE        : '{' -> pushMode(REPEAT) ;
LBRACKET      : '[' -> pushMode(CHARS);
LPAREN        : '(' ;
RPAREN        : ')' ;
PIPE          : '|' ;
ASTERISK      : '*' ;
PLUS          : '+' ;
QUESTION      : '?' ;
CHAR          : '\\' . | ~[{\[()|?] ;

mode REPEAT;
COUNT         : [0-9]+ ;
COMMA         : ',' ;
RBRACE        : '}' -> popMode ;
REPEAT_WS     : [ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;

mode CHARS;
HYPHEN        : '-' ;
RBRACKET      : ']' -> popMode ;
CHARS_CHAR    : '\\' . | ~[-\]] ;
