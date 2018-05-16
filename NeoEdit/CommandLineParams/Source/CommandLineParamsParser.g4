parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : about | diff | disk | hexedit | multi | streamsave | textedit | wait ;

about         : ABOUT ;
diff          : DIFF texteditfile? texteditfile? ;
disk          : DISK file=param? ;
hexedit       : HEXEDIT param* ;
multi         : MULTI ;
streamsave    : STREAMSAVE ((URL | playlist=PLAYLIST)? STRING+)?;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
wait          : WAIT guid=STRING? ;

param         : STRING | NUMBER ;
