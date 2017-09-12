parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : about | changelog | diff | disk | hexdump | hexedit | hexpid | license | multi | rip | streamsave | textedit | textview | wait ;

about         : ABOUT ;
changelog     : CHANGELOG ;
diff          : DIFF texteditfile? texteditfile? ;
disk          : DISK file=param? ;
hexdump       : HEXDUMP param* ;
hexedit       : HEXEDIT param* ;
hexpid        : HEXPID NUMBER* ;
license       : LICENSE ;
multi         : MULTI ;
rip           : RIP ;
streamsave    : STREAMSAVE ((URL | playlist=PLAYLIST)? STRING+)?;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
textview      : TEXTVIEW param* ;
wait          : WAIT guid=STRING? ;

param         : STRING | NUMBER ;
