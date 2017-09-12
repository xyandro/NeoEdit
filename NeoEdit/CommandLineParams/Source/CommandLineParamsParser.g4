parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : about | changelog | diff | disk | hexedit | license | multi | streamsave | textedit | textview | wait ;

about         : ABOUT ;
changelog     : CHANGELOG ;
diff          : DIFF texteditfile? texteditfile? ;
disk          : DISK file=param? ;
hexedit       : HEXEDIT param* ;
license       : LICENSE ;
multi         : MULTI ;
streamsave    : STREAMSAVE ((URL | playlist=PLAYLIST)? STRING+)?;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
textview      : TEXTVIEW param* ;
wait          : WAIT guid=STRING? ;

param         : STRING | NUMBER ;
