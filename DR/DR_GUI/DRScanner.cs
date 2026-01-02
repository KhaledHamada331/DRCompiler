using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace DR_GUI
{
    public class DRScanner
    {
        private static readonly Dictionary<string, TokenType> ReservedWords = new Dictionary<string, TokenType>
        {
            {"plan", TokenType.KEYWORD_PLAN}, {"MORNING_COFFEE", TokenType.KEYWORD_MAIN},
            {"SHOW", TokenType.KEYWORD_OUTPUT}, {"RECEIVE", TokenType.KEYWORD_INPUT},
            {"CHECK", TokenType.KEYWORD_IF}, {"REJECT", TokenType.KEYWORD_ELSE},
            {"REWORK", TokenType.KEYWORD_WHILE}, {"CHECKLIST", TokenType.KEYWORD_FOR},
            {"SUBMIT", TokenType.KEYWORD_RETURN}, {"OFFICE", TokenType.KEYWORD_NAMESPACE},
            {"#ATTACH", TokenType.KEYWORD_INCLUDE},
            {"file", TokenType.TYPE_FILE}, {"duration", TokenType.TYPE_DURATION},
            {"note", TokenType.TYPE_NOTE}, {"status", TokenType.TYPE_STATUS},
            {"true", TokenType.LITERAL_BOOLEAN}, {"false", TokenType.LITERAL_BOOLEAN}
        };

        private const string TokenPattern =
            @"(?<COMMENT>//NOTE[^\n]*)|(?<WHITESPACE>[ \t\r]+)|(?<NEWLINE>\n)|" +
            @"(?<STRING>\""[^\""]*\"")|(?<DOUBLE>\d+\.\d+)|(?<INTEGER>\d+)|" +
            @"(?<OPERATOR_MULTI>\+\+|--|<=|>=|==|!=)|" +
            @"(?<SYMBOL>[\(\)\{\};,\$=+\-*/])|" +
            @"(?<INCLUDE>#ATTACH)|(?<IDENTIFIER>[a-zA-Z_][a-zA-Z0-9_]*)";

        public List<Token> Scan(string sourceCode)
        {
            var tokens = new List<Token>();
            var line = 1;
            sourceCode += Environment.NewLine;

            var matches = Regex.Matches(sourceCode, TokenPattern, RegexOptions.Multiline | RegexOptions.ExplicitCapture);

            foreach (Match match in matches)
            {
                var value = match.Value;

                if (match.Groups["WHITESPACE"].Success || match.Groups["COMMENT"].Success) continue;
                if (match.Groups["NEWLINE"].Success) { line++; continue; }

                TokenType type;
                if (match.Groups["IDENTIFIER"].Success || match.Groups["INCLUDE"].Success)
                {
                    if (!ReservedWords.TryGetValue(value, out type)) type = TokenType.IDENTIFIER;
                }
                else if (match.Groups["STRING"].Success) { type = TokenType.LITERAL_STRING; value = value.Trim('"'); }
                else if (match.Groups["DOUBLE"].Success) { type = TokenType.LITERAL_DOUBLE; }
                else if (match.Groups["INTEGER"].Success) { type = TokenType.LITERAL_INTEGER; }
                else if (match.Groups["OPERATOR_MULTI"].Success) { type = GetMultiOpType(value); }
                else if (match.Groups["SYMBOL"].Success) { type = GetSingleSymbolType(value); }
                else { throw new Exception($"Lexical Error: Unexpected token '{value}' at line {line}"); }

                tokens.Add(new Token(type, value, line));
            }

            tokens.Add(new Token(TokenType.EOF, "EOF", line));
            return tokens;
        }

        private TokenType GetMultiOpType(string value)
        {
            switch (value)
            {
                case "++": return TokenType.OP_INCREMENT;
                case "--": return TokenType.OP_DECREMENT;
                case "<=": return TokenType.OP_LESS_EQUAL;
                case ">=": return TokenType.OP_GREATER_EQUAL;
                case "==": return TokenType.OP_EQUAL;
                case "!=": return TokenType.OP_NOT_EQUAL;
                default: throw new KeyNotFoundException("Unknown multi-char operator: " + value);
            }
        }

        private TokenType GetSingleSymbolType(string value)
        {
            switch (value)
            {
                case "(": return TokenType.SEP_LPAREN;
                case ")": return TokenType.SEP_RPAREN;
                case "{": return TokenType.SEP_LBRACE;
                case "}": return TokenType.SEP_RBRACE;
                case ";": return TokenType.SEP_SEMICOLON;
                case ",": return TokenType.SEP_COMMA;
                case "=": return TokenType.OP_ASSIGN;
                case "$": return TokenType.SEP_DOLLAR;
                case "+": return TokenType.OP_ADD;
                case "-": return TokenType.OP_SUBTRACT;
                case "*": return TokenType.OP_MULTIPLY;
                case "/": return TokenType.OP_DIVIDE;
                default: throw new KeyNotFoundException("Unknown single symbol: " + value);
            }
        }
    }
}

