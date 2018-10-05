lexer grammar ExpressionLexer ;

fragment A    : [Aa] ;
fragment B    : [Bb] ;
fragment C    : [Cc] ;
fragment D    : [Dd] ;
fragment E    : [Ee] ;
fragment F    : [Ff] ;
fragment G    : [Gg] ;
fragment H    : [Hh] ;
fragment I    : [Ii] ;
fragment J    : [Jj] ;
fragment K    : [Kk] ;
fragment L    : [Ll] ;
fragment M    : [Mm] ;
fragment N    : [Nn] ;
fragment O    : [Oo] ;
fragment P    : [Pp] ;
fragment Q    : [Qq] ;
fragment R    : [Rr] ;
fragment S    : [Ss] ;
fragment T    : [Tt] ;
fragment U    : [Uu] ;
fragment V    : [Vv] ;
fragment W    : [Ww] ;
fragment X    : [Xx] ;
fragment Y    : [Yy] ;
fragment Z    : [Zz] ;

DEBUG         : '#' ;
LPAREN        : '(' ;
RPAREN        : ')' ;
COMMA         : ',' ;
METHOD        : A B S | A C O S | A S I N | A T A N | C O S | D A T E | D I R E C T O R Y N A M E | E V A L | E X T E N S I O N | F A C T O R | F I L E N A M E | F I L E N A M E W I T H O U T E X T E N S I O N | F R O M D A T E | F R O M W O R D S | G C F | L C M | L E N | L N | L O G | M A X | M I N | M U L T I P L E | N O W | R A N D O M | R E C I P R O C A L | R E D U C E | R O O T | S I N | S Q R T | S T R F O R M A T | T A N | T M A X | T M I N | T O D A T E | T O U T C D A T E | T O W O R D S | T Y P E | U T C D A T E | V A L I D E V A L | V A L I D R E ;
BANG          : '!' ;
EXPOP         : '^' ;
MULTOP        : [*/%] | '//' | '///' | 't*' ;
ADDOP         : [-+] | 't' ('+' | '++' | '--' | '---') ;
SHIFTOP       : '<<' | '>>' ;
EQUALITYOP    : ('o' | 't' 'i'?)? ('=' | '==' | '!=' | '<>') ;
RELATIONALOP  : ('t' 'i'?)? [<>] '='? | I S ;
CONVERSION    : '=>' ;
BITWISENOT    : '~' ;
BITWISEAND    : '&' ;
BITWISEXOR    : '^^' ;
BITWISEOR     : '|' ;
LOGICALAND    : '&&' ;
LOGICALOR     : '||' ;
NULLCOALESCE  : '??' ;
CONDITIONAL   : '?' ;
ELSE          : ':' ;
DOT           : '.' ;
CONSTANT      : P I | E ;
STRSTART      : '"' -> pushMode(STRING) ;
VSTRSTART     : '@"' -> pushMode(VERBATIMSTRING) ;
ISTRSTART     : '$"' -> pushMode(INTERPOLATEDSTRING) ;
VISTRSTART    : ('@$"' | '$@"') -> pushMode(VERBATIMINTERPOLATEDSTRING) ;
TRUE          : T R U E ;
FALSE         : F A L S E ;
NULL          : N U L L ;
INTEGER       : [0-9]+ ;
FLOAT         : [0-9]* '.'? [0-9]+ ([eE][-+]?[0-9]+)? ;
HEX           : '0x' [0-9a-fA-F]+ ;
VARIABLE      : [a-zA-Z_][a-zA-Z0-9_]* ;
CURRENCY      : '$' | '$' B | '$' U | '.\u062f.\u0628' | '\u0af1' | '\u0bf9' | '\u0cb0' | '\u0dd4' | '\u0e3f' | '\u09f3' | '\u17db' | '\u20a0' .. '\u20cf' | '\u043b\u0432' | '\u043c\u0430\u043d' | '\u058f' | '\u060b' | '\u062c.\u0645.' | '\u062f.\u062a' | '\u062f.\u0625' | '\u062f.\u0639' | '\u062f.\u0643' | '\u062f.\u0645.' | '\u062f\u062c' | '\u211b\u2133' | '\u0414\u0438\u043d.' | '\u0434\u0435\u043d' | '\u0434\u0438\u043d' | '\u0440\u0443\u0431' | '\u0441\u043e\u043c' | '\u0631.\u0633' | '\u0631.\u0639.' | '\u0631.\u0642' | '\u0644.\u062f' | '\u2133' | '\u5143' | '\ua838' | '\ud800\udd96' | '\ufdfc' | '\uffe5' | '�' .. '�' | '�' | B '/.' | B S '.' | B S '.' F '.' | B Z '$' | C '$' | C H '.' | C R '$' | G H '\u20b5' | J '$' | K '\u010d' | K '\u010d' S | M O P '$' | N T '$' | N U '.' | P '.' | P T '.' | R '$' | R D '$' | R F '.' | S '/.' | S H '.' S O '.' | T T '$' | W S '$' | Z '$' | Z '\u0142' ;
WHITESPACE    : [ \n\t\r]+ -> skip ;
ISTRINTEREND  : '}' -> popMode ;

mode STRING;
STRCHARS      : ~[\\"]+ ;
STRESCAPE     : '\\' [\\'"0abfnrtv] ;
STRUNICODE    : '\\x' [0-9a-fA-F] [0-9a-fA-F]? [0-9a-fA-F]? [0-9a-fA-F]? | '\\u' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] | '\\U' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] ;
STREND        : '"' -> popMode ;

mode VERBATIMSTRING;
VSTRCHARS     : ~["]+ ;
VSTRQUOTE     : '""' ;
VSTREND       : '"' -> popMode ;

mode INTERPOLATEDSTRING;
ISTRCHARS     : ~[\\"{}]+ ;
ISTRESCAPE    : STRESCAPE -> type(STRESCAPE) ;
ISTRUNICODE   : STRUNICODE -> type(STRUNICODE) ;
ISTRLITERAL   : '{{' | '}}' ;
ISTRINTERSTA  : '{' -> pushMode(DEFAULT_MODE) ;
ISTREND       : '"' -> popMode ;

mode VERBATIMINTERPOLATEDSTRING;
VISTRCHARS    : ~["{}]+ ;
VISTRLITERAL  : '""' | '{{' | '}}' ;
VISTRINTERSTA : '{' -> pushMode(DEFAULT_MODE) ;
VISTREND      : '"' -> popMode ;
