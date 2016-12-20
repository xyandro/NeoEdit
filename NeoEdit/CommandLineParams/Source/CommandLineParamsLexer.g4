lexer grammar CommandLineParamsLexer;

STARTSTR       : '"' -> skip, pushMode(STR) ;

ABOUT          : '-about'                                                                               ;
COLUMN         : '-col' | '-column'                                                                     ;
DIFF           : '-diff'                                                                                ;
DISK           : '-disk' | '-disks'                                                                     ;
DISPLAY        : '-display' | '-displayname' | '-displaytext'                                           ;
HANDLES        : '-handle' | '-handles'                                                                 ;
HEXDUMP        : '-binarydump' | '-dump' | '-hexdump'                                                   ;
HEXEDIT        : '-binary' | '-binaryedit' | '-binaryeditor' | '-hex' | '-hexedit' | '-hexeditor'       ;
HEXPID         : '-binarypid' | '-hexpid'                                                               ;
IMAGEEDIT      : '-grab' | '-grabber' | '-image' | '-imageedit' | '-imageeditor'                        ;
LICENSE        : '-license'                                                                             ;
LINE           : '-line'                                                                                ;
LIVE           : '-live'                                                                                ;
MULTI          : '-multi'                                                                               ;
NETWORK        : '-socket' | '-sockets' | '-network'                                                    ;
PROCESSES      : '-pid' | '-process' | '-processes'                                                     ;
TEXTEDIT       : '-edit' | '-text' | '-textedit' | '-texteditor'                                        ;
TEXTVIEW       : '-textview' | '-textviewer' | '-view'                                                  ;

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
HANDLES2       : HANDLES       -> type(HANDLES)       ;
HEXDUMP2       : HEXDUMP       -> type(HEXDUMP)       ;
HEXEDIT2       : HEXEDIT       -> type(HEXEDIT)       ;
HEXPID2        : HEXPID        -> type(HEXPID)        ;
IMAGEEDIT2     : IMAGEEDIT     -> type(IMAGEEDIT)     ;
LICENSE2       : LICENSE       -> type(LICENSE)       ;
LINE2          : LINE          -> type(LINE)          ;
LIVE2          : LIVE          -> type(LIVE)          ;
MULTI2         : MULTI         -> type(MULTI)         ;
NETWORK2       : NETWORK       -> type(NETWORK)       ;
PROCESSES2     : PROCESSES     -> type(PROCESSES)     ;
TEXTEDIT2      : TEXTEDIT      -> type(TEXTEDIT)      ;
TEXTVIEW2      : TEXTVIEW      -> type(TEXTVIEW)      ;

EQUALS2        : EQUALS        -> type(EQUALS)        ;
NUMBER2        : NUMBER        -> type(NUMBER)        ;

STRING2        : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR         : '"' -> skip, popMode ;
