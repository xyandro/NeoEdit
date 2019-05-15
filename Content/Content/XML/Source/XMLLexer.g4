lexer grammar XMLLexer;

COMMENT     : '<!--' .*? '-->' ;
DTD         : '<!' .*? '>' ; 
CHARREF     : '&#x' [a-fA-F0-9]+ ';' | '&#' [0-9]+ ';' ;
ENTITYREF   : '&' NAME ';' ;
WS          : [ \t\r\n]+ ;

SPECIAL     : '<?' .*? '?>' ;
OPEN        : '<' -> pushMode(INTAG) ;

TEXT        : ~[<&]+ ;

mode INTAG;

SLASH_CLOSE : '/>' -> popMode ;
CLOSE       : '>' -> popMode ;
SLASH       : '/' ;
EQUALS      : '=' ;
STRING      : '"' ~[<"]* '"' | '\'' ~[<']* '\'' ;
NAME        : NAMESTART NAMECHAR* ;
WS2         : [ \t\r\n] -> skip ;

fragment
NAMECHAR    : NAMESTART | '-' | '_' | '.' | [0-9]  | '\u00B7' | '\u0300'..'\u036F' | '\u203F'..'\u2040' ;

fragment
NAMESTART   : [:a-zA-Z] | '\u2070'..'\u218F'  | '\u2C00'..'\u2FEF'  | '\u3001'..'\uD7FF'  | '\uF900'..'\uFDCF'  | '\uFDF0'..'\uFFFD' ;
