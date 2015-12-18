lexer grammar CommandLineParamsLexer;

STARTSTR       : '"' -> skip, pushMode(STR) ;

ABOUT          : '-about'                                                                               ;
COLUMN         : '-col' | '-column'                                                                     ;
CONSOLE        : '-console'                                                                             ;
CONSOLERUNNER  : '-consolerunner'                                                                       ;
DIFF           : '-diff'                                                                                ;
DISK           : '-disk' | '-disks'                                                                     ;
HANDLES        : '-handle' | '-handles'                                                                 ;
HEXDUMP        : '-binarydump' | '-dump' | '-hexdump'                                                   ;
HEXEDIT        : '-binary' | '-binaryedit' | '-binaryeditor' | '-hex' | '-hexedit' | '-hexeditor'       ;
HEXPID         : '-binarypid' | '-hexpid'                                                               ;
LINE           : '-line'                                                                                ;
LIVE           : '-live'                                                                                ;
MULTI          : '-multi'                                                                               ;
NETWORK        : '-socket' | '-sockets' | '-network'                                                                ;
PROCESSES      : '-pid' | '-process' | '-processes'                                                     ;
REGISTRY       : '-registry'                                                                            ;
SYSTEMINFO     : '-system' | '-systeminfo'                                                              ;
TABLEEDIT      : '-table' | '-tables' | '-tableedit' | '-tablesedit' | '-tableeditor' | '-tableseditor' ;
TEXTEDIT       : '-edit' | '-text' | '-textedit' | '-texteditor'                                        ;
TEXTVIEW       : '-textview' | '-textviewer' | '-view'                                                  ;
TOOLS          : '-tool' | '-tools'                                                                     ;

EQUALS         : '=' ;
NUMBER         : [0-9]+ ;

STRING         : ~[-=" \r\n\t] ~[ \r\n\t]* ;
WS             : [ \r\n\t]+ -> skip ;

mode STR;

ABOUT2         : ABOUT         -> type(ABOUT)         ;
COLUMN2        : COLUMN        -> type(COLUMN)        ;
CONSOLE2       : CONSOLE       -> type(CONSOLE)       ;
CONSOLERUNNER2 : CONSOLERUNNER -> type(CONSOLERUNNER) ;
DIFF2          : DIFF          -> type(DIFF)          ;
DISK2          : DISK          -> type(DISK)          ;
HANDLES2       : HANDLES       -> type(HANDLES)       ;
HEXDUMP2       : HEXDUMP       -> type(HEXDUMP)       ;
HEXEDIT2       : HEXEDIT       -> type(HEXEDIT)       ;
HEXPID2        : HEXPID        -> type(HEXPID)        ;
LINE2          : LINE          -> type(LINE)          ;
LIVE2          : LIVE          -> type(LIVE)          ;
MULTI2         : MULTI         -> type(MULTI)         ;
NETWORK2       : NETWORK       -> type(NETWORK)       ;
PROCESSES2     : PROCESSES     -> type(PROCESSES)     ;
REGISTRY2      : REGISTRY      -> type(REGISTRY)      ;
SYSTEMINFO2    : SYSTEMINFO    -> type(SYSTEMINFO)    ;
TABLEEDIT2     : TABLEEDIT     -> type(TABLEEDIT)     ;
TEXTEDIT2      : TEXTEDIT      -> type(TEXTEDIT)      ;
TEXTVIEW2      : TEXTVIEW      -> type(TEXTVIEW)      ;
TOOLS2         : TOOLS         -> type(TOOLS)         ;

EQUALS2        : EQUALS        -> type(EQUALS)        ;
NUMBER2        : NUMBER        -> type(NUMBER)        ;

STRING2        : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR         : '"' -> skip, popMode ;
