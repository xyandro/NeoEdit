parser grammar XMLParser;

options { tokenVocab = XMLLexer; }

document  : content* EOF ;

content   : comment | misc | text | element ;

comment   : COMMENT ;

misc      : COMMENT | DTD | SPECIAL ;

text      : (TEXT | WS | CHARREF | ENTITYREF)+ ;

element   : OPEN tagname attribute* CLOSE content* OPEN SLASH NAME CLOSE
          | OPEN tagname attribute* SLASH_CLOSE
          ;

tagname   : NAME ;

attribute : name=NAME EQUALS value=STRING ;
