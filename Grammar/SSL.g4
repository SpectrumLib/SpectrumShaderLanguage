// This is the parser grammar for the SpectrumShaderLanguage, used by Antlr to generate a parser for
//    the language definition.
// This file is licensed under the MIT license.
// Copyright (c) Sean Moss 2019

parser grammar SSL;

options {
    tokenVocab=SSLLexer;
}

// Top-level file unit
file
    : versionMetaStatement? topLevelStatement* EOF
    ;

// Shader metadata statements
versionMetaStatement
    : 'version' Version=VERSION_LITERAL ';'
    ;

// All top-level statements that can appear in the file scope
topLevelStatement
    : uniformStatement
    | attrStatement
    | outStatement
    | localStatement
    | constStatement
    ;

// Uniform statements
uniformStatement
    : uniformHeader (('block' typeBlock)|uniformVariable) ';'
    ;
uniformHeader
    : 'unif' '(' Index1=INTEGER_LITERAL (',' Index2=INTEGER_LITERAL)? ')'
    ;
typeBlock
    : '{' (Types+=variableDeclaration ';')* '}'
    ;
uniformVariable
    : type Qualifier=uniformQualifier? Name=IDENTIFIER
    ;
uniformQualifier
    : '<' (type | INTEGER_LITERAL) '>' 
    ;

// Vertex attribute statement
attrStatement
    : 'attr' ('(' Index=INTEGER_LITERAL ')')? variableDeclaration ';'
    ;

// Fragment shader output
outStatement
    : 'out' '(' Index=INTEGER_LITERAL ')' variableDeclaration ';'
    ;

// Locals (values passed between shader stages)
localStatement
    : 'local' variableDeclaration ';'
    ;

// Constant (both internal constants and specialization constants)
constStatement
    : 'const' ('(' Index=INTEGER_LITERAL ')')? variableDeclaration '=' constantValue ';'
    ;
constantValue
    : Value=valueLiteral
    | type '(' valueLiteral (',' valueLiteral)* ')'
    ;

// Variable declaration/definition
variableDeclaration
    : type Name=IDENTIFIER arrayIndexer?
    ;

// Array indexer
arrayIndexer
    : '[' Index1=expression (',' Index2=expression)? ']'
    ;

// Expressions (anything that results in a value)
// Enforce order of operation here
// See https://www.khronos.org/files/opengl45-quick-reference-card.pdf for GLSL Order of Operations
expression
    : atom                              # AtomExpr
    // Unary operators
    | Expr=IDENTIFIER Op=('--'|'++')    # UnOpPostfix
    | Op=('--'|'++') Expr=IDENTIFIER    # UnOpPrefix
    | Op=('+'|'-') Expr=expression      # UnOpFactor
    | Op=('!'|'~') Expr=expression      # UnOpNegate
    // Binary operators
    | Left=expression Op=('*'|'/'|'%') Right=expression         # BinOpMulDivMod
    | Left=expression Op=('+'|'-') Right=expression             # BinOpAddSub
    | Left=expression Op=('<<'|'>>') Right=expression           # BinOpBitShift
    | Left=expression Op=('<'|'>'|'<='|'>=') Right=expression   # BinOpRelational
    | Left=expression Op=('=='|'!=') Right=expression           # BinOpEquality
    | Left=expression Op=('&'|'|'|'^') Right=expression         # BinOpBitLogic
    | Left=expression Op=('&&'|'||'|'^^') Right=expression      # BinOpBoolLogic
    // Ternary (selection) operator
    | Cond=expression '?' TVal=expression ':' FVal=expression   # SelectionExpr
    ;

// Atomic expression (an expression that cannot be subdivided into further expressions)
atom
    : '(' expression ')'    # ParenAtom
    | atom arrayIndexer     # ArrayAtom
    | atom SWIZZLE          # SwizzleAtom
    | typeConstruction      # ConstructionAtom
    | functionCall          # FunctionCallAtom
    | valueLiteral          # LiteralAtom
    | IDENTIFIER            # VariableAtom
    ;
typeConstruction // For built-in types, also how casting is performed
    : Type=type '(' Args+=expression (',' Args+=expression)* ')'
    ;
functionCall
    : FName=IDENTIFIER '(' (Args+=expression (',' Args+=expression)*)? ')'
    ;
valueLiteral
    : INTEGER_LITERAL
    | FLOAT_LITERAL
    | BOOLEAN_LITERAL
    ;

// Type keywords
type
      // Value Types
    : KWT_VOID
    | KWT_BOOL  | KWT_INT  | KWT_UINT  | KWT_FLOAT
    | KWT_BOOL2 | KWT_INT2 | KWT_UINT2 | KWT_FLOAT2
    | KWT_BOOL3 | KWT_INT3 | KWT_UINT3 | KWT_FLOAT3
    | KWT_BOOL4 | KWT_INT4 | KWT_UINT4 | KWT_FLOAT4
    | KWT_MAT2  | KWT_MAT3 | KWT_MAT4
      // Handle Types
    | KWT_TEX1D   | KWT_TEX2D   | KWT_TEX3D   | KWT_TEXCUBE     | KWT_TEX1D_ARR   | KWT_TEX2D_ARR
    | KWT_IMAGE1D | KWT_IMAGE2D | KWT_IMAGE3D | KWT_IMAGE1D_ARR | KWT_IMAGE2D_ARR
    | KWT_SUBPASSINPUT
    ;