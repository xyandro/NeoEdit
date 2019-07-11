lexer grammar CommandLineLexer;

STARTSTR : '"' -> skip, pushMode(STR) ;

COLUMN   : '-col' | '-column'                                                                     ;
DIFF     : '-diff'                                                                                ;
DISPLAY  : '-display' | '-displayname' | '-displaytext'                                           ;
LINE     : '-line'                                                                                ;
MULTI    : '-multi'                                                                               ;
WAIT     : '-wait'                                                                                ;

EQUALS   : '=' ;
NUMBER   : [0-9]+ ;

STRING   : ~[-=" \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000] ~[ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]* ;
WS       : [ \t\r\n\v\f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;

mode STR;

COLUMN2  : COLUMN        -> type(COLUMN)        ;
DIFF2    : DIFF          -> type(DIFF)          ;
DISPLAY2 : DISPLAY       -> type(DISPLAY)       ;
LINE2    : LINE          -> type(LINE)          ;
MULTI2   : MULTI         -> type(MULTI)         ;
WAIT2    : WAIT          -> type(WAIT)          ;

EQUALS2  : EQUALS        -> type(EQUALS)        ;
NUMBER2  : NUMBER        -> type(NUMBER)        ;

STRING2  : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR   : '"' -> skip, popMode ;
