parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : LIVE | about | diff | disk | handles | hexdump | hexedit | hexpid | multi | network | processes | textedit | textview ;

about         : ABOUT ;
diff          : DIFF texteditfile? texteditfile? ;
disk          : DISK file=param? ;
handles       : HANDLES pid=NUMBER? ;
hexdump       : HEXDUMP param* ;
hexedit       : HEXEDIT param* ;
hexpid        : HEXPID NUMBER* ;
multi         : MULTI ;
network       : NETWORK ;
processes     : PROCESSES pid=NUMBER? ;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
textview      : TEXTVIEW param* ;

param         : STRING | NUMBER ;
