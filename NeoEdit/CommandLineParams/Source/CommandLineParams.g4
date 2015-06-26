grammar CommandLineParams;

expr : param parameter* EOF ;

parameter     : about | console | consolerunner | dbviewer | disk | gunzip | gzip | handles | hexdump | hexedit | hexpid | multi | processes | registry | systeminfo | textedit | textview ;

about         : ABOUT ;
console       : CONSOLE ;
consolerunner : CONSOLERUNNER param* ;
dbviewer      : DBVIEWER ;
disk          : DISK file=param? ;
gunzip        : GUNZIP input=param output=param ;
gzip          : GZIP input=param output=param ;
handles       : HANDLES pid=NUMBER? ;
hexdump       : HEXDUMP param* ;
hexedit       : HEXEDIT param* ;
hexpid        : HEXPID NUMBER* ;
multi         : MULTI ;
processes     : PROCESSES pid=NUMBER? ;
registry      : REGISTRY key=param? ;
systeminfo    : SYSTEMINFO ;
textedit      : TEXTEDIT texteditfile* ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? ;
textview      : TEXTVIEW param* ;

param         : STRING | PARAM ;

ABOUT         : '-about' ;
COLUMN        : '-col' | '-column' ;
CONSOLE       : '-console' ;
CONSOLERUNNER : '-consolerunner' ;
DBVIEWER      : '-db' | '-dbview' | '-dbviewer' ;
DISK          : '-disk' | '-disks' ;
GUNZIP        : '-gunzip' ;
GZIP          : '-gzip' ;
HANDLES       : '-handle' | '-handles' ;
HEXDUMP       : '-binarydump' | '-dump' | '-hexdump' ;
HEXEDIT       : '-binary' | '-binaryedit' | '-binaryeditor' | '-hex' | '-hexedit' | '-hexeditor' ;
HEXPID        : '-binarypid' | '-hexpid' ;
LINE          : '-line' ;
MULTI         : '-multi' ;
PROCESSES     : '-pid' | '-process' | '-processes' ;
REGISTRY      : '-registry' ;
SYSTEMINFO    : '-system' | '-systeminfo' ;
TEXTEDIT      : '-edit' | '-text' | '-textedit' | '-texteditor' ;
TEXTVIEW      : '-textview' | '-textviewer' | '-view' ;

EQUALS        : '=' ;
NUMBER        : [0-9]+ ;
STRING        : '"' ~'"'* '"' ;
PARAM         : ~[-= \r\n\r] ~[ \r\n\r]* ;
