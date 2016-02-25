lexer grammar ExpressionLexer ;

fragment A   : [Aa] ;
fragment B   : [Bb] ;
fragment C   : [Cc] ;
fragment D   : [Dd] ;
fragment E   : [Ee] ;
fragment F   : [Ff] ;
fragment G   : [Gg] ;
fragment H   : [Hh] ;
fragment I   : [Ii] ;
fragment J   : [Jj] ;
fragment K   : [Kk] ;
fragment L   : [Ll] ;
fragment M   : [Mm] ;
fragment N   : [Nn] ;
fragment O   : [Oo] ;
fragment P   : [Pp] ;
fragment Q   : [Qq] ;
fragment R   : [Rr] ;
fragment S   : [Ss] ;
fragment T   : [Tt] ;
fragment U   : [Uu] ;
fragment V   : [Vv] ;
fragment W   : [Ww] ;
fragment X   : [Xx] ;
fragment Y   : [Yy] ;
fragment Z   : [Zz] ;

DEBUG        : '@' ;
LPAREN       : '(' ;
RPAREN       : ')' ;
COMMA        : ',' ;
METHOD1      : A B S | A C O S | A S I N | A T A N | C O N J U G A T E | C O S | C O S H | E V A L | F I L E N A M E | F R O M W O R D S | I M A G I N A R Y | L N | L O G | M A G N I T U D E | P H A S E | R E A L | R E C I P R O C A L | S I N | S I N H | S Q R T | T A N | T A N H | T O W O R D S | T Y P E | V A L I D R E ;
METHOD1VAR   : S T R F O R M A T;
METHOD2      : F R O M P O L A R | L O G | R E D U C E | R O O T ;
METHODVAR    : G C F | L C M  | M A X | M I N ;
BANG         : '!' ;
EXPOP        : '^' ;
MULTOP       : [*/%] | '**' | '//' ;
ADDOP        : [-+.] ;
SHIFTOP      : '<<' | '>>' ;
RELATIONALOP : '<' | '>' | '<=' | '>=' | 'i<' | 'i>' | 'i<=' | 'i>=' | I S ;
EQUALITYOP   : '=' | '==' | '!=' | '<>' | 'i==' | 'i!=' | 'i<>' ;
CONVERSION   : '=>' ;
BITWISENOT   : '~' ;
BITWISEAND   : '&' ;
BITWISEXOR   : '^^' ;
BITWISEOR    : '|' ;
LOGICALAND   : '&&' ;
LOGICALOR    : '||' ;
NULLCOALESCE : '??' ;
CONDITIONAL  : '?' ;
ELSE         : ':' ;
DOT          : '..' ;
CONSTANT     : P I | E | I | N O W | U T C N O W | D A T E | U T C D A T E | T I M E | U T C T I M E ;
PARAM        : '[' [0-9]+ ']' ;
CHARSTART    : '\'' -> pushMode(CHARVAL) ;
STRSTART     : '"' -> pushMode(STRING) ;
DATE         : '\'' ([0-9][0-9][0-9][0-9] [-/])? [01]?[0-9] [-/] [0-3]?[0-9] (Z | [-+] [0-2]?[0-9] (':' [0-5]?[0-9])? )? '\'' ;
TIME         : '\'' '-'? ([0-9]+ ':')? [0-2]?[0-9] ':' [0-5][0-9] (':' [0-5][0-9] ('.' [0-9]+)?)? ([ \t]* (A M | P M))? '\'' ;
DATETIME     : '\'' [0-9][0-9][0-9][0-9] [-/] [01]?[0-9] [-/] [0-3]?[0-9] [ \tT]+ [0-2]?[0-9] ':' [0-5]?[0-9] (':' [0-5]?[0-9] ('.' [0-9]+)?)? (Z | [-+] [0-2]?[0-9] (':' [0-5]?[0-9])? )? ([ \t]* (A M | P M))? '\'' ;
TRUE         : T R U E ;
FALSE        : F A L S E ;
NULL         : N U L L ;
INTEGER      : [0-9]+ ;
FLOAT        : [0-9]* '.'? [0-9]+ ([eE][-+]?[0-9]+)? ;
HEX          : '0x' [0-9a-fA-F]+ ;
VARIABLE     : [a-zA-Z][a-zA-Z0-9_]* ;
WHITESPACE   : [ \n\t\r]+ -> skip ;

mode CHARVAL;
CHARANY      : ~[\\'] ;
CHARESCAPE   : '\\' [\\'"0abfnrtv] ;
CHARUNICODE  : '\\x' [0-9a-fA-F] [0-9a-fA-F]? [0-9a-fA-F]? [0-9a-fA-F]? | '\\u' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] | '\\U' [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] [0-9a-fA-F] ;
CHAREND      : '\'' -> popMode ;

mode STRING;
STRCHARS     : ~[\\"]+ ;
STRESCAPE    : CHARESCAPE -> type(CHARESCAPE) ;
STRUNICODE   : CHARUNICODE -> type(CHARUNICODE) ;
STREND       : '"' -> popMode ;
