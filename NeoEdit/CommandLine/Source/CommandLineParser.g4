parser grammar CommandLineParser;

options { tokenVocab = CommandLineLexer; }

expr       : parameter* EOF ;

parameter  : background | debug | existing | diff | file | wait | waitpid ;

background : BACKGROUND ;
debug      : DEBUG ;
existing   : EXISTING ;
diff       : DIFF ;
file       : filename=param (LINE EQUALS? line=NUMBER)? (COLUMN EQUALS? column=NUMBER)? (INDEX EQUALS? index=NUMBER)? (DISPLAY EQUALS? display=param)? ;
wait       : WAIT ;
waitpid    : WAITPID EQUALS pid=NUMBER ;

param      : STRING | NUMBER ;
