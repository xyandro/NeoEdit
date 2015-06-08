parser grammar HTMLParser;

options { tokenVocab = HTMLLexer; }

document     : content* EOF ;

content      : comment | misc | text | element | elementopen | elementclose ;

comment      : COMMENT ;

misc         : DTD | SPECIAL ;

text         : TEXT ;

element      : OPEN voidname attribute* (CLOSE | SLASHCLOSE)
             | OPEN tagname attribute* SLASHCLOSE
             | tag=OPENSCRIPT attribute* CLOSE body=SCRIPTBODY
             | tag=OPENSTYLE attribute* CLOSE body=STYLEBODY
             | tag=OPENTEXTAREA attribute* CLOSE body=TEXTAREABODY
             | tag=OPENTITLE attribute* CLOSE body=TITLEBODY
             ;

elementopen  : OPEN tagname attribute* CLOSE ;

elementclose : OPEN SLASH name=(NAME | VOIDNAME) CLOSE ;

voidname     : VOIDNAME ;
tagname      : NAME ;

attribute    : name=NAME (EQUALS value=(ATTRSTRING | ATTRTEXT))?;
