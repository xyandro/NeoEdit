parser grammar HTMLParser;

options { tokenVocab = HTMLLexer; }

document     : content* EOF ;

content      : comment | misc | text | element | elementopen | elementclose ;

comment      : COMMENT ;

misc         : DTD | SPECIAL ;

text         : TEXT ;

element      : OPEN voidname attribute* (CLOSE | SLASHCLOSE)
             | OPEN tagname attribute* SLASHCLOSE
             | tag=SCRIPT attribute* CLOSE body=SCRIPTBODY
             | tag=STYLE attribute* CLOSE body=STYLEBODY
             | tag=TEXTAREA attribute* CLOSE body=TEXTAREABODY
             | tag=TITLE attribute* CLOSE body=TITLEBODY
             ;

elementopen  : OPEN tagname attribute* CLOSE ;

elementclose : OPEN SLASH name=(NAME | VOIDNAME) CLOSE ;

voidname     : VOIDNAME ;
tagname      : NAME ;

attribute    : name=NAME (EQUALS value=(ATTRSTRING | ATTRTEXT))?;
