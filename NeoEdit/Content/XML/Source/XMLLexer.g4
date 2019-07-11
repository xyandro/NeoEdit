lexer grammar XMLLexer;

COMMENT     : '<!--' .*? '-->' ;
DTD         : '<!' .*? '>' ; 
CHARREF     : '&#x' [a-fA-F0-9]+ ';' | '&#' [0-9]+ ';' ;
ENTITYREF   : '&' NAME ';' ;
WS          : [ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ ;

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
WS2         : [ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000] -> skip ;

fragment
NAMECHAR    : NAMESTART | '-' | '_' | '.' | [0-9]  | '\u00B7' | '\u0300'..'\u036F' | '\u203F'..'\u2040' ;

fragment
NAMESTART   : [:a-zA-Z] | '\u2070'..'\u218F'  | '\u2C00'..'\u2FEF'  | '\u3001'..'\uD7FF'  | '\uF900'..'\uFDCF'  | '\uFDF0'..'\uFFFD' ;
