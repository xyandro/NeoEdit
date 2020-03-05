parser grammar CommandLineParser;

options { tokenVocab = CommandLineLexer; }

expr       : parameter* EOF ;

parameter  : background | diff | multi | file | wait | waitpid ;

background : BACKGROUND ;
diff       : DIFF ;
multi      : MULTI ;
file       : filename=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (INDEX EQUALS? index=NUMBER)? (DISPLAY EQUALS? display=param)? ;
wait       : WAIT guid=STRING? ;
waitpid    : WAITPID EQUALS NUMBER ;

param      : STRING | NUMBER ;
