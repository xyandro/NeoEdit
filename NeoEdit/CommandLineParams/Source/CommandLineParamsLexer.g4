lexer grammar CommandLineParamsLexer;

STARTSTR       : '"' -> skip, pushMode(STR) ;

ABOUT          : '-about'                                                                               ;
COLUMN         : '-col' | '-column'                                                                     ;
DIFF           : '-diff'                                                                                ;
DISK           : '-disk' | '-disks'                                                                     ;
DISPLAY        : '-display' | '-displayname' | '-displaytext'                                           ;
HEXEDIT        : '-binary' | '-binaryedit' | '-binaryeditor' | '-hex' | '-hexedit' | '-hexeditor'       ;
LINE           : '-line'                                                                                ;
MULTI          : '-multi'                                                                               ;
PLAYLIST       : '-playlist' | '-playlists' | '-list' | '-lists'                                        ;
STREAMSAVE     : '-streamsave' | '-stream' | '-save'                                                    ;
TEXTEDIT       : '-edit' | '-text' | '-textedit' | '-texteditor'                                        ;
URL            : '-url' | '-urls'                                                                       ;
WAIT           : '-wait'                                                                                ;

EQUALS         : '=' ;
NUMBER         : [0-9]+ ;

STRING         : ~[-=" \r\n\t] ~[ \r\n\t]* ;
WS             : [ \r\n\t]+ -> skip ;

mode STR;

ABOUT2         : ABOUT         -> type(ABOUT)         ;
COLUMN2        : COLUMN        -> type(COLUMN)        ;
DIFF2          : DIFF          -> type(DIFF)          ;
DISK2          : DISK          -> type(DISK)          ;
DISPLAY2       : DISPLAY       -> type(DISPLAY)       ;
HEXEDIT2       : HEXEDIT       -> type(HEXEDIT)       ;
LINE2          : LINE          -> type(LINE)          ;
MULTI2         : MULTI         -> type(MULTI)         ;
PLAYLIST2      : PLAYLIST      -> type(PLAYLIST)      ;
STREAMSAVE2    : STREAMSAVE    -> type(STREAMSAVE)    ;
TEXTEDIT2      : TEXTEDIT      -> type(TEXTEDIT)      ;
URL2           : URL           -> type(URL)           ;
WAIT2          : WAIT          -> type(WAIT)          ;

EQUALS2        : EQUALS        -> type(EQUALS)        ;
NUMBER2        : NUMBER        -> type(NUMBER)        ;

STRING2        : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR         : '"' -> skip, popMode ;
