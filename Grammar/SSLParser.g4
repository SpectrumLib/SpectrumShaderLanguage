// This is the parser grammar for the SpectrumShaderLanguage, used by Antlr to generate a parser for
//    the language definition.
// This file is licensed under the MIT license.
// Copyright (c) Sean Moss 2019

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
    : 'shader' Name=STRING_LITERAL ';'
    ;

// All top-level statements that can appear in the file scope
topLevelStatement
    : uniformStatement
    | attributesStatement
    | outputsStatement
    | localsStatement
    | stageFunction
    | standardFunction
    ;

// Uniform statements
uniformStatement
    : uniformHeader (('block' typeBlock)|variableDeclaration) ';'
    ;
uniformHeader
    : 'uniform' '(' Index=INTEGER_LITERAL ')'
    ;
typeBlock
    : '{' (Types+=variableDeclaration ';')* '}'
    ;

// Vertex shader input (attributes)
attributesStatement
    : 'attributes' typeBlock ';'
    ;

// Fragment shader output
outputsStatement
    : 'outputs' typeBlock ';'
    ;

// Locals (values passed between shader stages)
localsStatement
    : 'locals' typeBlock ';'
    ;

// Stage functions
stageFunction
    : vertFunction
    | fragFunction
    ;
vertFunction
    : '@vert' block
    ;
fragFunction
    : '@frag' block
    ;

// Standard function (non-stage function)
standardFunction
    : type Name=IDENTIFIER '(' (Params=parameterList|'void')? ')' block
    ;
parameterList
    : PList+=parameter (',' PList+=parameter)*
    ;
parameter
    : Access=('in'|'out'|'inout')? type Name=IDENTIFIER
    ;

// Statements (basically, any line that can stand on its own in a function body)
block
    : '{' statement* '}'
    ;
statement
    : variableDeclaration ';'
    | variableDefinition ';'
    | assignment ';'
    | ifStatement
    | forLoop
    | whileLoop
    | doLoop
    | controlFlowStatement
    ;

// Declaring new variables
variableDeclaration
    : type Name=IDENTIFIER arrayIndexer?
    ;
variableDefinition
    : 'const'? type Name=IDENTIFIER '=' Value=expression                   # ValueDefinition
    | 'const'? type Name=IDENTIFIER arrayIndexer '=' Value=arrayLiteral    # ArrayDefinition
    ;
arrayLiteral
    : '{' Values+=expression (',' Values+=expression)* '}'
    ;

// Assigment
assignment
    : Name=IDENTIFIER arrayIndexer? SWIZZLE? Op=OP_ALL_ASSIGN Value=expression
    ;

// Array indexer
arrayIndexer
    : '[' Index=INTEGER_LITERAL ']'
    ;

// Conditional statements
ifStatement
    : 'if' '(' Cond=expression ')' (IfBlock=block|IfStatement=statement) ('else' (ElseBlock=block|ElseStatement=statement))?
    ;

// Looping constructs
forLoop
    : 'for' '('
            forLoopInit? ';'
            Condition=expression? ';'
            forLoopUpdate?
      ')' (block|statement)
    ;
forLoopInit
    : (variableDefinition|assignment) (',' (variableDefinition|assignment))*
    ;
forLoopUpdate
    : assignment (',' assignment)*
    ;
whileLoop
    : 'while' '(' Condition=expression ')' (block|statement)
    ;
doLoop
    : 'do' block 'while' '(' Condition=expression ')' ';'
    ;

// Control flow statements
controlFlowStatement
    : 'return' RVal=expression? ';'
    | 'break' ';'
    | 'continue' ';'
    | 'discard' ';'
    ;

// Expressions (anything that can evaluate to a type value) (enforce order of operation)
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

// Atom expression (an expression that cannot be subdivided into further expressions)
atom
    : '(' expression ')' arrayIndexer? SWIZZLE?     # ParenAtom
    | typeConstruction arrayIndexer? SWIZZLE?       # ConstructionAtom
    | builtinFunctionCall arrayIndexer? SWIZZLE?    # BuiltinCallAtom
    | functionCall arrayIndexer? SWIZZLE?           # FunctionCallAtom
    | valueLiteral                                  # LiteralAtom
    | IDENTIFIER arrayIndexer? SWIZZLE?             # VariableAtom
    ;
typeConstruction // For built-in types, also how casting is performed
    : Type=type '(' Args+=expression (',' Args+=expression)* ')'
    ;
builtinFunctionCall
    : FName=BIF_ALL_ARG1 '(' A1=expression ')'                                      # BuiltinCall1
    | FName=BIF_ALL_ARG2 '(' A1=expression ',' A2=expression ')'                    # BuiltinCall2
    | FName=BIF_ALL_ARG3 '(' A1=expression ',' A2=expression ',' A3=expression ')'  # BuiltinCall3
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
    : KWT_VOID
    | KWT_BOOL | KWT_INT | KWT_UINT | KWT_FLOAT
    | KWT_BOOL2 | KWT_INT2 | KWT_UINT2 | KWT_FLOAT2
    | KWT_BOOL3 | KWT_INT3 | KWT_UINT3 | KWT_FLOAT3
    | KWT_BOOL4 | KWT_INT4 | KWT_UINT4 | KWT_FLOAT4
    | KWT_MAT2 | KWT_MAT3 | KWT_MAT4
    | KWT_TEX1D | KWT_TEX2D | KWT_TEX3D | KWT_TEXCUBE | KWT_TEX1D_ARR | KWT_TEX2D_ARR
    | KWT_IMAGE1D | KWT_IMAGE2D | KWT_IMAGE3D | KWT_IMAGE1D_ARR | KWT_IMAGE2D_ARR
    ;
