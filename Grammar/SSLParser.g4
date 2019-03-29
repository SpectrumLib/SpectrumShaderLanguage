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
    | inputStatement
    | outputStatement
    | localsStatement
    | stageFunction
    ;

// Uniform statements
uniformStatement
    : uniformBlockDeclare
    ;
uniformBlockDeclare // Block of data types
    : uniformDeclare 'block' blockOfTypes
    ;
uniformHandleDeclare // A single opaque handle
    : uniformDeclare variableDeclareNoAssign
    ;
uniformDeclare
    : 'uniform' '(' INTEGER_LITERAL ')'
    ;

// Vertex shader input (attributes)
inputStatement
    : 'input' blockOfTypes
    ;

// Fragment shader output
outputStatement
    : 'output' blockOfTypes
    ;

// Locals (values passed between shader stages)
localsStatement
    : 'locals' blockOfTypes
    ;

// Stage functions
stageFunction
    : vertFunction
    | fragFunction
    ;
vertFunction
    : '@vert' statementBlock
    ;
fragFunction
    : '@frag' statementBlock
    ;

// Standard function (non-stage function)
standardFunction
    : TYPE_KEYWORD IDENTIFIER '(' argList? ')' statementBlock
    ;
argList
    : TYPE_KEYWORD IDENTIFIER (',' TYPE_KEYWORD IDENTIFIER)*
    ;

// Block of statements
statementBlock
    : '{' statement* '}'
    ;
statement
    : variableDeclareNoAssign // TODO: THIS IS THE BASE STATEMENT TYPE, THIS WILL GET EXPANDED A LOT
    ;

// Variable declaration
blockOfTypes
    : '{' variableDeclareNoAssign* '}'
    ;
variableDeclareNoAssign
    : TYPE_KEYWORD IDENTIFIER ';'
    ;
