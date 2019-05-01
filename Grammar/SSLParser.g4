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
    : 'shader' '{' '}' ';'
    ;

// All top-level statements that can appear in the file scope
topLevelStatement
    : uniformStatement
    | attributesStatement
    | outputsStatement
    | internalsStatement
    | stageFunction
    | standardFunction
    ;

// Uniform statements
uniformStatement
    : uniformHeader (('block' typeBlock)|uniformVariable) ';'
    ;
uniformHeader
    : 'uniform' '(' Index=INTEGER_LITERAL ')'
    ;
typeBlock
    : '{' (Types+=variableDeclaration ';')* '}'
    ;
uniformVariable
    : type ('<' Qualifier=uniformQualifier '>')? Name=IDENTIFIER
    ;
uniformQualifier
    : imageLayoutQualifier
    | INTEGER_LITERAL
    ;
imageLayoutQualifier
    : IFQ_F4 | IFQ_I4 | IFQ_U4 | IFQ_F2 | IFQ_I2 | IFQ_U2 | IFQ_F1 | IFQ_I1 | IFQ_U1
    ;

// Vertex shader input (attributes)
attributesStatement
    : 'attributes' typeBlock ';'
    ;

// Fragment shader output
outputsStatement
    : 'outputs' typeBlock ';'
    ;

// Internals (values passed between shader stages)
internalsStatement
    : 'internals' typeBlock ';'
    ;

// Stage functions
stageFunction
    : '@vert' block     #vertFunction
    | '@frag' block     #fragFunction
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
    | functionCall ';'
    | builtinFunctionCall ';'
    | ifStatement
    | forLoop
    | whileLoop
    | doLoop
    | controlFlowStatement
    ;

// Declaring new variables
variableDeclaration
    : 'flat'? type Name=IDENTIFIER arrayIndexer?
    ;
variableDefinition
    : 'const'? type Name=IDENTIFIER arrayIndexer? '=' (expression|arrayLiteral)
    ;
arrayLiteral
    : '{' Values+=expression (',' Values+=expression)* '}'
    ;

// Assigment
assignment
    : Name=IDENTIFIER arrayIndexer? SWIZZLE? Op=('='|'+='|'-='|'*='|'/='|'%='|'<<='|'>>='|'&='|'|='|'^=') Value=expression
    ;

// Array indexer
arrayIndexer
    : '[' Index1=expression (',' Index2=expression)? ']'
    ;

// Conditional statements
ifStatement
    : 'if' '(' Cond=expression ')' (Block=block|Statement=statement) (Elifs+=elifStatement)* Else=elseStatement?
    ;
elifStatement
    : 'elif' '(' Cond=expression ')' (Block=block|Statement=statement)
    ;
elseStatement
    : 'else' (Block=block|Statement=statement)
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
    : variableDefinition
    | Assigns+=assignment (',' Assigns+=assignment)*
    ;
forLoopUpdate
    : (assignment|expression) (',' (assignment|expression))*
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
    | builtinFunctionCall SWIZZLE?                  # BuiltinCallAtom
    | functionCall SWIZZLE?                         # FunctionCallAtom
    | valueLiteral                                  # LiteralAtom
    | IDENTIFIER arrayIndexer? SWIZZLE?             # VariableAtom
    ;
typeConstruction // For built-in types, also how casting is performed
    : Type=type '(' Args+=expression (',' Args+=expression)* ')'
    ;
builtinFunctionCall
    : FName=builtinArg1 '(' A1=expression ')'                                      # BuiltinCall1
    | FName=builtinArg2 '(' A1=expression ',' A2=expression ')'                    # BuiltinCall2
    | FName=builtinArg3 '(' A1=expression ',' A2=expression ',' A3=expression ')'  # BuiltinCall3
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
    | KWT_SUBPASSINPUT
    ;

// Built-in functions
builtinArg1 // All 1-argument builtin functions
    : BIF_DEG2RAD | BIF_RAD2DEG | BIF_SIN | BIF_COS | BIF_TAN | BIF_ASIN | BIF_ACOS | BIF_ATAN | BIF_EXP
    | BIF_LOG | BIF_EXP2 | BIF_LOG2 | BIF_SQRT | BIF_INVSQRT | BIF_ABS | BIF_SIGN | BIF_FLOOR | BIF_TRUNC
    | BIF_ROUND | BIF_ROUNDEVEN | BIF_CEIL | BIF_FRACT | BIF_LENGTH | BIF_NORMALIZE | BIF_TRANSPOSE
    | BIF_DETERMINANT | BIF_INVERSE | BIF_VECANY | BIF_VECALL | BIF_VECNOT | BIF_TEXSIZE | BIF_IMAGESIZE
    | BIF_SUBPASSLOAD
    ;
builtinArg2 // All 2-argument builtin functions
    : BIF_ATAN2 | BIF_POW | BIF_MOD | BIF_MIN | BIF_MAX | BIF_STEP | BIF_DISTANCE | BIF_DOT | BIF_CROSS
    | BIF_REFLECT | BIF_MATCOMPMUL | BIF_VECLT | BIF_VECLE | BIF_VECGT | BIF_VECGE | BIF_VECEQ | BIF_VECNE
    | BIF_TEXTURE | BIF_TEXFETCH | BIF_IMAGELOAD
    ;
builtinArg3 // All 3-argument builtin functions
    : BIF_CLAMP | BIF_MIX | BIF_SSTEP | BIF_FFORWARD | BIF_REFRACT | BIF_IMAGESTORE
    ;
