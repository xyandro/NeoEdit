lexer grammar CommandLineLexer;

STARTSTR    : '"' -> skip, pushMode(STR) ;

ADMIN       : '-admin'                                     ;
BACKGROUND  : '-background'                                ;
COLUMN      : '-col' | '-column'                           ;
DEBUG       : '-debug'                                     ;
DIFF        : '-diff'                                      ;
DISPLAY     : '-display' | '-displayname' | '-displaytext' ;
EXISTING    : '-exist' | '-existing'                       ;
INDEX       : '-in' | '-index'                             ;
LINE        : '-line'                                      ;
WAIT        : '-wait'                                      ;
WAITPID     : '-waitpid'                                   ;

EQUALS      : '=' ;
NUMBER      : [0-9]+ ;

STRING      : ~[-=" \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000] ~[ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]* ;
WS          : [ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;

mode STR;

ADMIN2      : ADMIN      -> type(ADMIN)      ;
BACKGROUND2 : BACKGROUND -> type(BACKGROUND) ;
COLUMN2     : COLUMN     -> type(COLUMN)     ;
DEBUG2      : DEBUG      -> type(DEBUG)      ;
DIFF2       : DIFF       -> type(DIFF)       ;
DISPLAY2    : DISPLAY    -> type(DISPLAY)    ;
EXISTING2   : EXISTING   -> type(EXISTING)   ;
INDEX2      : INDEX      -> type(INDEX)      ;
LINE2       : LINE       -> type(LINE)       ;
WAIT2       : WAIT       -> type(WAIT)       ;
WAITPID2    : WAITPID    -> type(WAITPID)    ;

EQUALS2     : EQUALS     -> type(EQUALS)     ;
NUMBER2     : NUMBER     -> type(NUMBER)     ;

STRING2     : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR      : '"' -> skip, popMode ;
