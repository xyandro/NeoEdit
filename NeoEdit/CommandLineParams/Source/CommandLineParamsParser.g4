parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : LIVE | about | console | consolerunner | diff | disk | handles | hexdump | hexedit | hexpid | multi | processes | registry | systeminfo | tableedit | textedit | textview | tools ;

about         : ABOUT ;
console       : CONSOLE ;
consolerunner : CONSOLERUNNER param* ;
diff          : DIFF file1=param? file2=param? ;
disk          : DISK file=param? ;
handles       : HANDLES pid=NUMBER? ;
hexdump       : HEXDUMP param* ;
hexedit       : HEXEDIT param* ;
hexpid        : HEXPID NUMBER* ;
multi         : MULTI ;
processes     : PROCESSES pid=NUMBER? ;
registry      : REGISTRY key=param? ;
systeminfo    : SYSTEMINFO ;
tableedit     : TABLEEDIT param* ;
textedit      : TEXTEDIT texteditfile* ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? ;
textview      : TEXTVIEW param* ;
tools         : TOOLS ;

param         : STRING | NUMBER ;
