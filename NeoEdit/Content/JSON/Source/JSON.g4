grammar JSON;

json     : item* EOF ;
item     : object | array | string | number | constant ;
object   : LBRACE (pair (COMMA pair)*)? RBRACE ;
pair     : (STRING | NUMBER) COLON item ;
array    : LBRACKET (item (COMMA item)*)? RBRACKET ;
string   : STRING ;
number   : NUMBER ;
constant : CONSTANT ;

STRING   : '"' ('\\' . | ~["\\])* '"' ;
NUMBER   : [-+0-9.eE]+ ;
LBRACE   : '{' ;
RBRACE   : '}' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
CONSTANT : ~["' \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\[\]{},:] ~[ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\[\]{},:]* ;
COMMA    : ',' ;
COLON    : ':' ;
WS       : [ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;
