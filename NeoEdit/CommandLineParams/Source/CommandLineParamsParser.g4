parser grammar CommandLineParamsParser;

options { tokenVocab = CommandLineParamsLexer; }

expr          : parameter* EOF ;

parameter     : about | diff | multi | textedit | wait ;

about         : ABOUT ;
diff          : DIFF texteditfile? texteditfile? ;
multi         : MULTI ;
textedit      : TEXTEDIT texteditfile* | texteditfile+ ;
texteditfile  : file=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
wait          : WAIT guid=STRING? ;

param         : STRING | NUMBER ;
