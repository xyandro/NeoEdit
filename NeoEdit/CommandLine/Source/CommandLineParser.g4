parser grammar CommandLineParser;

options { tokenVocab = CommandLineLexer; }

expr      : parameter* EOF ;

parameter : diff | multi | file | wait ;

diff      : DIFF ;
multi     : MULTI ;
file      : filename=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (DISPLAY EQUALS? display=param)? ;
wait      : WAIT guid=STRING? ;

param     : STRING | NUMBER ;
