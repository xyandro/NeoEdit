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
MULTI          : '-multi'                                                                               ;
NETWORK        : '-socket' | '-sockets' | '-network'                                                    ;
PLAYLIST       : '-playlist' | '-playlists' | '-list' | '-lists'                                        ;
PROCESSES      : '-pid' | '-process' | '-processes'                                                     ;
RIP            : '-rip' | '-riper' | '-ripper'                                                          ;
STREAMSAVE     : '-streamsave' | '-stream' | '-save'                                                    ;
TEXTEDIT       : '-edit' | '-text' | '-textedit' | '-texteditor'                                        ;
TEXTVIEW       : '-textview' | '-textviewer' | '-view'                                                  ;
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
HANDLES2       : HANDLES       -> type(HANDLES)       ;
HEXDUMP2       : HEXDUMP       -> type(HEXDUMP)       ;
HEXEDIT2       : HEXEDIT       -> type(HEXEDIT)       ;
HEXPID2        : HEXPID        -> type(HEXPID)        ;
IMAGEEDIT2     : IMAGEEDIT     -> type(IMAGEEDIT)     ;
LICENSE2       : LICENSE       -> type(LICENSE)       ;
LINE2          : LINE          -> type(LINE)          ;
MULTI2         : MULTI         -> type(MULTI)         ;
NETWORK2       : NETWORK       -> type(NETWORK)       ;
PLAYLIST2      : PLAYLIST      -> type(PLAYLIST)      ;
PROCESSES2     : PROCESSES     -> type(PROCESSES)     ;
RIP2           : RIP           -> type(RIP)           ;
STREAMSAVE2    : STREAMSAVE    -> type(STREAMSAVE)    ;
TEXTEDIT2      : TEXTEDIT      -> type(TEXTEDIT)      ;
TEXTVIEW2      : TEXTVIEW      -> type(TEXTVIEW)      ;
URL2           : URL           -> type(URL)           ;
WAIT2          : WAIT          -> type(WAIT)          ;

EQUALS2        : EQUALS        -> type(EQUALS)        ;
NUMBER2        : NUMBER        -> type(NUMBER)        ;

STRING2        : ~[-="] ~'"'* -> type(STRING) ;
ENDSTR         : '"' -> skip, popMode ;
