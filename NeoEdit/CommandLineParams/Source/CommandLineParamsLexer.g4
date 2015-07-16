lexer grammar CommandLineParamsLexer;

STARTSTR       : '"' -> skip, pushMode(STR) ;

ABOUT          : '-about'                                                                         ;
COLUMN         : '-col' | '-column'                                                               ;
CONSOLE        : '-console'                                                                       ;
CONSOLERUNNER  : '-consolerunner'                                                                 ;
DBVIEWER       : '-db' | '-dbview' | '-dbviewer'                                                  ;
DIFF           : '-diff'                                                                          ;
DISK           : '-disk' | '-disks'                                                               ;
GUNZIP         : '-gunzip'                                                                        ;
GZIP           : '-gzip'                                                                          ;
HANDLES        : '-handle' | '-handles'                                                           ;
HEXDUMP        : '-binarydump' | '-dump' | '-hexdump'                                             ;
HEXEDIT        : '-binary' | '-binaryedit' | '-binaryeditor' | '-hex' | '-hexedit' | '-hexeditor' ;
HEXPID         : '-binarypid' | '-hexpid'                                                         ;
LINE           : '-line'                                                                          ;
MULTI          : '-multi'                                                                         ;
PROCESSES      : '-pid' | '-process' | '-processes'                                               ;
REGISTRY       : '-registry'                                                                      ;
SYSTEMINFO     : '-system' | '-systeminfo'                                                        ;
TEXTEDIT       : '-edit' | '-text' | '-textedit' | '-texteditor'                                  ;
TEXTVIEW       : '-textview' | '-textviewer' | '-view'                                            ;

EQUALS         : '=' ;
NUMBER         : [0-9]+ ;

STRING         : ~[-=" \r\n\t] ~[ \r\n\t]* ;
WS             : [ \r\n\t]+ -> skip ;

mode STR;

ABOUT2         : ABOUT         -> type(ABOUT)         ;
COLUMN2        : COLUMN        -> type(COLUMN)        ;
CONSOLE2       : CONSOLE       -> type(CONSOLE)       ;
CONSOLERUNNER2 : CONSOLERUNNER -> type(CONSOLERUNNER) ;
DBVIEWER2      : DBVIEWER      -> type(DBVIEWER)      ;
DIFF2          : DIFF          -> type(DIFF)          ;
DISK2          : DISK          -> type(DISK)          ;
GUNZIP2        : GUNZIP        -> type(GUNZIP)        ;
GZIP2          : GZIP          -> type(GZIP)          ;
HANDLES2       : HANDLES       -> type(HANDLES)       ;
HEXDUMP2       : HEXDUMP       -> type(HEXDUMP)       ;
HEXEDIT2       : HEXEDIT       -> type(HEXEDIT)       ;
HEXPID2        : HEXPID        -> type(HEXPID)        ;
LINE2          : LINE          -> type(LINE)          ;
MULTI2         : MULTI         -> type(MULTI)         ;
PROCESSES2     : PROCESSES     -> type(PROCESSES)     ;
REGISTRY2      : REGISTRY      -> type(REGISTRY)      ;
SYSTEMINFO2    : SYSTEMINFO    -> type(SYSTEMINFO)    ;
TEXTEDIT2      : TEXTEDIT      -> type(TEXTEDIT)      ;
TEXTVIEW2      : TEXTVIEW      -> type(TEXTVIEW)      ;

EQUALS2        : EQUALS        -> type(EQUALS)        ;
NUMBER2        : NUMBER        -> type(NUMBER)        ;

STRING2        : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR         : '"' -> skip, popMode ;