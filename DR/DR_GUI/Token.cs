using System;

namespace DR_GUI
{
    public enum TokenType
    {
        // Keywords
        KEYWORD_PLAN, KEYWORD_MAIN, KEYWORD_OUTPUT, KEYWORD_INPUT,
        KEYWORD_IF, KEYWORD_ELSE, KEYWORD_WHILE, KEYWORD_FOR,
        KEYWORD_RETURN, KEYWORD_NAMESPACE, KEYWORD_INCLUDE,

        // Data Types
        TYPE_FILE, TYPE_DURATION, TYPE_NOTE, TYPE_STATUS,

        // Literals
        LITERAL_STRING, LITERAL_INTEGER, LITERAL_DOUBLE, LITERAL_BOOLEAN,

        // Identifiers
        IDENTIFIER,

        // Operators and Symbols
        OP_ASSIGN, OP_EQUAL, OP_NOT_EQUAL, OP_LESS, OP_LESS_EQUAL, OP_GREATER, OP_GREATER_EQUAL,
        OP_INCREMENT, OP_DECREMENT, OP_ADD, OP_SUBTRACT, OP_MULTIPLY, OP_DIVIDE,
        SEP_LPAREN, SEP_RPAREN, SEP_LBRACE, SEP_RBRACE, SEP_SEMICOLON, SEP_COMMA,
        SEP_DOLLAR,
        EOF
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }

        public Token(TokenType type, string value, int line)
        {
            Type = type;
            Value = value;
            Line = line;
        }
    }
}

