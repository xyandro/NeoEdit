parser grammar CPlusPlusParser;

options { tokenVocab = CPlusPlusLexer; }

cplusplus : DATA EOF ;
