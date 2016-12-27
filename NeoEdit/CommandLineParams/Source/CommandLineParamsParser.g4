parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : about | diff | disk | handles | hexdump | hexedit | hexpid | imageedit | license | multi | network | processes | textedit | textview | wait ;

about         : ABOUT ;
diff          : DIFF texteditfile? texteditfile? ;
disk          : DISK file=param? ;
handles       : HANDLES pid=NUMBER? ;
hexdump       : HEXDUMP param* ;
hexedit       : HEXEDIT param* ;
hexpid        : HEXPID NUMBER* ;
imageedit     : IMAGEEDIT param* ;
license       : LICENSE ;
multi         : MULTI ;
network       : NETWORK ;
processes     : PROCESSES pid=NUMBER? ;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
textview      : TEXTVIEW param* ;
wait          : WAIT guid=STRING? ;

param         : STRING | NUMBER ;
