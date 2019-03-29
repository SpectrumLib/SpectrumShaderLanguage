parser grammar SSLParser;

options {
    tokenVocab=SSLLexer;
}

// Top-level file unit
file
    : shaderMetaStatement? topLevelStatement* EOF
    ;

// Shader metadata statements
shaderMetaStatement
    : 'shader' STRING_LITERAL ';'
    ;

// All top-level statements that can appear in the file scope
topLevelStatement
    : uniformStatement
    ;

// Uniform statements
uniformStatement
    : uniformBlockDeclare
    ;
uniformBlockDeclare // Block of data types
    : uniformDeclare 'block' '{' variableDeclareNoAssign* '}'
    ;
uniformHandleDeclare // A single opaque handle
    : uniformDeclare variableDeclareNoAssign
    ;
uniformDeclare
    : 'uniform' '(' INTEGER_LITERAL ')'
    ;

// Variable declaration
variableDeclareNoAssign
    : TYPE_KEYWORD IDENTIFIER ';'
    ;
