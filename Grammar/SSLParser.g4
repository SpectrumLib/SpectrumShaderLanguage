parser grammar SSLParser;

options {
	tokenVocab=SSLLexer;
}

// Top-level
file
	: shaderMetaStatement? EOF
	;

// Shader metadata statements
shaderMetaStatement
	: 'shader' STRING_LITERAL ';'
	;
