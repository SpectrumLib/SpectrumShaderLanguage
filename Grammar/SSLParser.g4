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
    | inputStatement
    | outputStatement
    | localsStatement
    | stageFunction
    | standardFunction
    ;

// Uniform statements
uniformStatement
    : uniformBlockDeclare
    | uniformHandleDeclare
    ;
uniformBlockDeclare // Block of data types
    : uniformDeclare 'block' typeBlock
    ;
uniformHandleDeclare // A single opaque handle
    : uniformDeclare variableDeclaration
    ;
uniformDeclare
    : 'uniform' '(' Index=INTEGER_LITERAL ')'
    ;
typeBlock
    : '{' variableDeclaration* '}'
    ;

// Vertex shader input (attributes)
inputStatement
    : 'input' typeBlock
    ;

// Fragment shader output
outputStatement
    : 'output' typeBlock
    ;

// Locals (values passed between shader stages)
localsStatement
    : 'locals' typeBlock
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
    : variableDeclaration
    | variableDefinition
    | assignment
    ;

// Declaring new variables
variableDeclaration
    : type Name=IDENTIFIER arrayIndexer? ';'
    ;
variableDefinition
    : type Name=IDENTIFIER '=' Value=expression ';'                   # ValueDefinition
    | type Name=IDENTIFIER arrayIndexer '=' Value=arrayLiteral ';'    # ArrayDefinition
    ;
arrayLiteral
    : '{' Values+=expression (',' Values+=expression)* '}'
    ;

// Assigment
assignment
    : Name=IDENTIFIER arrayIndexer? SWIZZLE? Op=('='|'+='|'-='|'*='|'/=') Value=expression ';'
    ;

// Array indexer
arrayIndexer
    : '[' Index=INTEGER_LITERAL ']'
    ;

// Expressions (anything that can evaluate to a type value) (enforce order of operation)
// See http://learnwebgl.brown37.net/12_shader_language/glsl_mathematical_operations.html for GLSL Order of Operations
expression
    : atom                              # AtomExpr
    // Unary operators
    | Expr=IDENTIFIER Op=('--'|'++')    # UnOpPostfix
    | Op=('--'|'++') Expr=IDENTIFIER    # UnOpPrefix
    | Op=('+'|'-') Expr=expression      # UnOpFactor
    | '!' Expr=expression               # UnOpBang
    // Binary operators
    | Left=expression Op=('*'|'/') Right=expression             # BinOpMulDiv
    | Left=expression Op=('+'|'-') Right=expression             # BinOpAddSub
    | Left=expression Op=('<'|'>'|'<='|'>=') Right=expression   # BinOpInequality
    | Left=expression Op=('=='|'!=') Right=expression           # BinOpEquality
    | Left=expression Op=('&&'|'||'|'^^') Right=expression      # BinOpLogic
    // Ternary (selection) operator
    | Cond=expression '?' TVal=expression ':' FVal=expression   # SelectionExpr
    ;

// Atom expression (an expression that cannot be subdivided into further expressions)
atom
    : '(' expression ')' SWIZZLE?   # ParenAtom
    | typeConstruction SWIZZLE?     # ConstructionAtom
    | builtinFunctionCall SWIZZLE?  # BuiltinCallAtom
    | functionCall SWIZZLE?         # FunctionCallAtom
    | valueLiteral                  # LiteralAtom
    | IDENTIFIER SWIZZLE?           # VariableAtom
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
    | valueTypeKeyword
    ;
valueTypeKeyword
    : KWT_BOOL | KWT_INT | KWT_UINT | KWT_FLOAT | KWT_DOUBLE
    | KWT_BOOL2 | KWT_INT2 | KWT_UINT2 | KWT_FLOAT2 | KWT_DOUBLE2
    | KWT_BOOL3 | KWT_INT3 | KWT_UINT3 | KWT_FLOAT3 | KWT_DOUBLE3
    | KWT_BOOL4 | KWT_INT4 | KWT_UINT4 | KWT_FLOAT4 | KWT_DOUBLE4
    | KWT_MAT2 | KWT_MAT3 | KWT_MAT4
    ;