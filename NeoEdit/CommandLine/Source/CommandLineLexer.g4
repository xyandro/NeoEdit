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

STRING   : ~[-=" \r\n\t] ~[ \r\n\t]* ;
WS       : [ \r\n\t]+ -> skip ;

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
