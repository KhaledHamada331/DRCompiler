using System;
using System.Collections.Generic;
using System.Linq;
using DR_GUI.Core.AST;
using DR_GUI.Core.Lexer;
using DR_GUI.Core.Semantic;

namespace DR_GUI.Core.Parser
{
    public class DRParserSemantic
    {
        private readonly List<Token> tokens;
        private int _position = 0;

        private readonly TokenType[] dataTypes = {
            TokenType.TYPE_FILE, TokenType.TYPE_DURATION,
            TokenType.TYPE_NOTE, TokenType.TYPE_STATUS
        };

        private readonly TokenType[] relOps = {
            TokenType.OP_EQUAL, TokenType.OP_NOT_EQUAL,
            TokenType.OP_LESS, TokenType.OP_LESS_EQUAL,
            TokenType.OP_GREATER, TokenType.OP_GREATER_EQUAL
        };

        private readonly SymbolTable _symbolTable = new SymbolTable();
        private readonly List<SemanticError> _errors = new List<SemanticError>();

        public List<SemanticError> Errors => _errors;
        public SymbolTable SymTab => _symbolTable;
        public ParseNode Root { get; private set; }

        public DRParserSemantic(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token Peek()
        {
            if (_position >= tokens.Count) return tokens.Last();
            return tokens[_position];
        }

        private bool Check(TokenType type)
        {
            return Peek().Type == type;
        }

        private Token Consume(TokenType expectedType)
        {
            Token token = Peek();
            if (token.Type == expectedType)
            {
                _position++;
                return token;
            }
            throw new Exception($" Syntax Error at line {token.Line}: Expected {expectedType} but found {token.Type} ('{token.Value}').");
        }

        public void Parse()
        {
            Root = new ParseNode("Program", 0);
            ParseStatementList(Root);
            Consume(TokenType.EOF);
        }

        private void ParseStatementList(ParseNode parent)
        {
            while (!Check(TokenType.EOF) && !Check(TokenType.SEP_RBRACE))
            {
                if (Check(TokenType.KEYWORD_INCLUDE))
                {
                    var incNode = new ParseNode("Include", Peek().Line);
                    Consume(TokenType.KEYWORD_INCLUDE);
                    var id = Consume(TokenType.IDENTIFIER);
                    var idNode = new ParseNode($"#ATTACH {id.Value}", id.Line) { DataType = DataType.Include };
                    incNode.Add(idNode);
                    parent.Add(incNode);
                }
                else if (Check(TokenType.KEYWORD_PLAN))
                {
                    var fnNode = ParseFunctionDefinition();
                    parent.Add(fnNode);
                }
                else
                {
                    var st = ParseStatement();
                    parent.Add(st);
                }
            }
        }

        private ParseNode ParseFunctionDefinition()
        {
            var node = new ParseNode("PlanMain", Peek().Line);
            Consume(TokenType.KEYWORD_PLAN);
            Consume(TokenType.KEYWORD_MAIN);
            Consume(TokenType.SEP_LPAREN);
            Consume(TokenType.SEP_RPAREN);
            var block = ParseBlock();
            node.Add(block);
            return node;
        }

        private ParseNode ParseStatement()
        {
            TokenType currentType = Peek().Type;

            if (dataTypes.Contains(currentType))
                return ParseDeclaration();
            else if (currentType == TokenType.IDENTIFIER)
                return ParseAssignmentOrFunctionCall();
            else if (currentType == TokenType.KEYWORD_IF)
                return ParseIfStatement();
            else if (currentType == TokenType.KEYWORD_WHILE)
                return ParseWhileLoop();
            else if (currentType == TokenType.KEYWORD_FOR)
                return ParseForLoop();
            else if (currentType == TokenType.KEYWORD_OUTPUT)
                return ParseOutputStatement();
            else if (currentType == TokenType.KEYWORD_RETURN)
                return ParseReturnStatement();
            else
                throw new Exception($"Syntax Error at line {Peek().Line}: Expected statement start but found {Peek().Type}.");
        }

        private ParseNode ParseBlock()
        {
            var node = new ParseNode("Block", Peek().Line);
            Consume(TokenType.SEP_LBRACE);
            _symbolTable.EnterScope();
            ParseStatementList(node);
            Consume(TokenType.SEP_RBRACE);
            _symbolTable.ExitScope();
            return node;
        }

        private DataType TokenTypeToDataType(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.TYPE_FILE: return DataType.File;
                case TokenType.TYPE_DURATION: return DataType.Duration;
                case TokenType.TYPE_NOTE: return DataType.Note;
                case TokenType.TYPE_STATUS: return DataType.Status;
                default: return DataType.Unknown;
            }
        }

        private static DataType LiteralToExpressionType(TokenType literalType)
        {
            switch (literalType)
            {
                case TokenType.LITERAL_INTEGER: return DataType.File;
                case TokenType.LITERAL_DOUBLE: return DataType.Duration;
                case TokenType.LITERAL_STRING: return DataType.Note;
                case TokenType.LITERAL_BOOLEAN: return DataType.Status;
                default: return DataType.Unknown;
            }
        }

        private static bool IsNumericType(DataType type)
        {
            return type == DataType.File || type == DataType.Duration;
        }

        private static bool IsTextType(DataType type)
        {
            return type == DataType.Note;
        }

        private static bool IsBooleanType(DataType type)
        {
            return type == DataType.Status;
        }

        private static bool IsIntegerLiteral(ParseNode node, DataType type)
        {
            return type == DataType.File && int.TryParse(node.Label, out _);
        }

        private static bool IsBooleanLike(DataType type, ParseNode node)
        {
            if (IsBooleanType(type)) return true;
            if (IsIntegerLiteral(node, type)) return true;
            return false;
        }

        private ParseNode ParseDeclaration()
        {
            var typeToken = Consume(Peek().Type);
            var declaredType = TokenTypeToDataType(typeToken.Type);
            var id = Consume(TokenType.IDENTIFIER);
            var node = new ParseNode("Declaration", id.Line);
            node.Add(new ParseNode(id.Value, id.Line) { DataType = declaredType });

            if (!_symbolTable.Declare(id.Value, declaredType, id.Line, out var err))
                _errors.Add(new SemanticError(id.Line, err));

            if (Check(TokenType.SEP_DOLLAR) || Check(TokenType.OP_ASSIGN))
            {
                if (Check(TokenType.SEP_DOLLAR)) Consume(TokenType.SEP_DOLLAR);
                Consume(TokenType.OP_ASSIGN);
                var (exprNode, exprType) = ParseExpression();
                node.Add(exprNode);

                if (!IsAssignable(declaredType, exprType))
                    _errors.Add(new SemanticError(id.Line, $"Type mismatch in initialization of '{id.Value}': cannot assign '{exprType}' to '{declaredType}'."));
            }

            Consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private ParseNode ParseAssignmentOrFunctionCall()
        {
            return ParseAssignmentOrIncrement(consumeSemicolon: true);
        }

        private ParseNode ParseAssignmentOrIncrement(bool consumeSemicolon)
        {
            var idToken = Consume(TokenType.IDENTIFIER);
            var nodeLabel = consumeSemicolon ? "Stmt" : "Update";
            var node = new ParseNode(nodeLabel, idToken.Line);
            var sym = _symbolTable.Lookup(idToken.Value);
            if (sym == null)
            {
                var undefinedMsg = consumeSemicolon
                    ? $"Undefined identifier '{idToken.Value}'."
                    : $"Undefined identifier '{idToken.Value}' in update expression.";
                _errors.Add(new SemanticError(idToken.Line, undefinedMsg));
            }

            if (Check(TokenType.SEP_DOLLAR) || Check(TokenType.OP_ASSIGN))
            {
                if (Check(TokenType.SEP_DOLLAR)) Consume(TokenType.SEP_DOLLAR);
                Consume(TokenType.OP_ASSIGN);
                var (exprNode, exprType) = ParseExpression();
                node.Add(new ParseNode(idToken.Value, idToken.Line) { DataType = sym?.DataType ?? DataType.Unknown });
                node.Add(exprNode);

                if (sym != null && !IsAssignable(sym.DataType, exprType))
                {
                    var mismatchMsg = consumeSemicolon
                        ? $"Type mismatch in assignment to '{idToken.Value}': cannot assign '{exprType}' to '{sym.DataType}'."
                        : $"Type mismatch in update: cannot assign '{exprType}' to '{sym.DataType}'.";
                    _errors.Add(new SemanticError(idToken.Line, mismatchMsg));
                }

                if (consumeSemicolon) Consume(TokenType.SEP_SEMICOLON);
                return node;
            }
            else if (Check(TokenType.OP_INCREMENT) || Check(TokenType.OP_DECREMENT))
            {
                var op = Consume(Peek().Type);
                node.Add(new ParseNode($"{idToken.Value} {op.Value}", idToken.Line));
                if (consumeSemicolon) Consume(TokenType.SEP_SEMICOLON);
                return node;
            }
            else if (Check(TokenType.SEP_LPAREN))
            {
                var call = ParseFunctionCall(idToken.Value, idToken.Line);
                if (consumeSemicolon) Consume(TokenType.SEP_SEMICOLON);
                return call;
            }
            else
            {
                var syntaxMsg = consumeSemicolon
                    ? $"Syntax Error at line {Peek().Line}: Expected assignment, increment, or function call after identifier."
                    : $" Syntax Error at line {Peek().Line}: Expected valid update expression in CHECKLIST.";
                throw new Exception(syntaxMsg);
            }
        }

        private ParseNode ParseIfStatement()
        {
            var node = new ParseNode("IF", Peek().Line);
            Consume(TokenType.KEYWORD_IF);
            Consume(TokenType.SEP_LPAREN);
            var (condNode, condType) = ParseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType, condNode))
                _errors.Add(new SemanticError(condNode.Line, $"Condition expression should be Status but found '{condType}'."));
            Consume(TokenType.SEP_RPAREN);
            var blk = ParseBlock();
            node.Add(blk);

            if (Check(TokenType.KEYWORD_ELSE))
            {
                Consume(TokenType.KEYWORD_ELSE);
                var elseBlk = ParseBlock();
                node.Add(elseBlk);
            }
            return node;
        }

        private ParseNode ParseWhileLoop()
        {
            var node = new ParseNode("WHILE", Peek().Line);
            Consume(TokenType.KEYWORD_WHILE);
            Consume(TokenType.SEP_LPAREN);
            var (condNode, condType) = ParseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType, condNode))
                _errors.Add(new SemanticError(condNode.Line, $"Condition expression should be Status but found '{condType}'."));
            Consume(TokenType.SEP_RPAREN);
            var blk = ParseBlock();
            node.Add(blk);
            return node;
        }

        private ParseNode ParseForLoop()
        {
            var node = new ParseNode("FOR", Peek().Line);
            Consume(TokenType.KEYWORD_FOR);
            Consume(TokenType.SEP_LPAREN);
            var decl = ParseDeclaration();
            node.Add(decl);
            var (condNode, condType) = ParseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType, condNode))
                _errors.Add(new SemanticError(condNode.Line, $"CHECKLIST condition should be Status but found '{condType}'."));
            Consume(TokenType.SEP_SEMICOLON);
            var update = ParseAssignmentOrIncrement(consumeSemicolon: false);
            node.Add(update);
            Consume(TokenType.SEP_RPAREN);
            var blk = ParseBlock();
            node.Add(blk);
            return node;
        }

        private ParseNode ParseOutputStatement()
        {
            var node = new ParseNode("OUTPUT", Peek().Line);
            Consume(TokenType.KEYWORD_OUTPUT);
            var (exprNode, exprType) = ParseExpression();
            node.Add(exprNode);
            Consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private ParseNode ParseReturnStatement()
        {
            var node = new ParseNode("RETURN", Peek().Line);
            Consume(TokenType.KEYWORD_RETURN);
            var (exprNode, exprType) = ParseExpression();
            node.Add(exprNode);
            Consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private (ParseNode, DataType) ParseCondition()
        {
            var (leftNode, leftType) = ParseExpression();
            var node = new ParseNode("Condition", leftNode.Line);
            node.Add(leftNode);

            if (relOps.Any(op => Check(op)))
            {
                var opToken = Consume(Peek().Type);
                var (rightNode, rightType) = ParseExpression();
                node.Add(new ParseNode(opToken.Value, opToken.Line));
                node.Add(rightNode);

                var resultType = TypeOfRelational(leftType, rightType, opToken.Type, opToken.Line);
                node.DataType = resultType;
                return (node, resultType);
            }
            node.DataType = leftType;
            return (node, leftType);
        }

        private (ParseNode, DataType) ParseExpression()
        {
            var (leftNode, leftType) = ParseTerm();
            var root = leftNode;
            var currentType = leftType;

            while (Check(TokenType.OP_ADD) || Check(TokenType.OP_SUBTRACT))
            {
                var op = Consume(Peek().Type);
                var (rightNode, rightType) = ParseTerm();

                var parent = new ParseNode("BinaryExpr", op.Line);
                parent.Add(root);
                parent.Add(new ParseNode(op.Value, op.Line));
                parent.Add(rightNode);

                var resType = TypeOfBinaryArithmetic(currentType, rightType, op.Type, op.Line);
                parent.DataType = resType;

                root = parent;
                currentType = resType;
            }

            return (root, currentType);
        }

        private (ParseNode, DataType) ParseTerm()
        {
            var (leftNode, leftType) = ParseFactor();
            var root = leftNode;
            var currentType = leftType;

            while (Check(TokenType.OP_MULTIPLY) || Check(TokenType.OP_DIVIDE))
            {
                var op = Consume(Peek().Type);
                var (rightNode, rightType) = ParseFactor();

                var parent = new ParseNode("BinaryExpr", op.Line);
                parent.Add(root);
                parent.Add(new ParseNode(op.Value, op.Line));
                parent.Add(rightNode);

                var resType = TypeOfBinaryArithmetic(currentType, rightType, op.Type, op.Line);
                parent.DataType = resType;

                root = parent;
                currentType = resType;
            }

            return (root, currentType);
        }

        private (ParseNode, DataType) ParseFactor()
        {
            TokenType currentType = Peek().Type;

            if (currentType == TokenType.LITERAL_INTEGER)
            {
                var t = Consume(TokenType.LITERAL_INTEGER);
                var exprType = LiteralToExpressionType(TokenType.LITERAL_INTEGER);
                var n = new ParseNode(t.Value, t.Line) { DataType = exprType };
                return (n, exprType);
            }
            else if (currentType == TokenType.LITERAL_DOUBLE)
            {
                var t = Consume(TokenType.LITERAL_DOUBLE);
                var exprType = LiteralToExpressionType(TokenType.LITERAL_DOUBLE);
                var n = new ParseNode(t.Value, t.Line) { DataType = exprType };
                return (n, exprType);
            }
            else if (currentType == TokenType.LITERAL_STRING)
            {
                var t = Consume(TokenType.LITERAL_STRING);
                var exprType = LiteralToExpressionType(TokenType.LITERAL_STRING);
                var n = new ParseNode($"\"{t.Value}\"", t.Line) { DataType = exprType };
                return (n, exprType);
            }
            else if (currentType == TokenType.LITERAL_BOOLEAN)
            {
                var t = Consume(TokenType.LITERAL_BOOLEAN);
                var exprType = LiteralToExpressionType(TokenType.LITERAL_BOOLEAN);
                var n = new ParseNode(t.Value, t.Line) { DataType = exprType };
                return (n, exprType);
            }
            else if (currentType == TokenType.IDENTIFIER)
            {
                var id = Consume(TokenType.IDENTIFIER);
                var sym = _symbolTable.Lookup(id.Value);
                if (sym == null)
                {
                    _errors.Add(new SemanticError(id.Line, $"Undefined identifier '{id.Value}'."));
                    var n = new ParseNode(id.Value, id.Line) { DataType = DataType.Unknown };
                    return (n, DataType.Unknown);
                }
                var node = new ParseNode(id.Value, id.Line) { DataType = sym.DataType };
                return (node, sym.DataType);
            }
            else if (currentType == TokenType.SEP_LPAREN)
            {
                Consume(TokenType.SEP_LPAREN);
                var (node, type) = ParseExpression();
                Consume(TokenType.SEP_RPAREN);
                var paren = new ParseNode("Paren", node.Line) { DataType = type };
                paren.Add(node);
                return (paren, type);
            }
            else
                throw new Exception($"Syntax Error at line {Peek().Line}: Expected number, identifier, or expression.");
        }

        private ParseNode ParseFunctionCall(string funcName, int line)
        {
            var node = new ParseNode($"Call {funcName}", line);
            Consume(TokenType.SEP_LPAREN);

            if (!Check(TokenType.SEP_RPAREN))
            {
                var (argNode, argType) = ParseExpression();
                node.Add(argNode);

                while (Check(TokenType.SEP_COMMA))
                {
                    Consume(TokenType.SEP_COMMA);
                    var (aN, aT) = ParseExpression();
                    node.Add(aN);
                }
            }

            Consume(TokenType.SEP_RPAREN);
            return node;
        }

        private bool IsAssignable(DataType target, DataType source)
        {
            if (target == source) return true;
            if (IsNumericType(target) && IsNumericType(source)) return true;
            return false;
        }

        private DataType TypeOfBinaryArithmetic(DataType left, DataType right, TokenType op, int line)
        {
            if (IsNumericType(left) && IsNumericType(right))
                return (left == DataType.Duration || right == DataType.Duration) ? DataType.Duration : DataType.File;

            if (op == TokenType.OP_ADD && IsTextType(left) && IsTextType(right))
                return DataType.Note;

            _errors.Add(new SemanticError(line, $"Invalid operands for arithmetic '{left}' {op} '{right}'."));
            return DataType.Unknown;
        }

        private DataType TypeOfRelational(DataType left, DataType right, TokenType op, int line)
        {
            if (left == right) return DataType.Status;
            if (IsNumericType(left) && IsNumericType(right)) return DataType.Status;

            _errors.Add(new SemanticError(line, $"Invalid operands for relational '{left}' {op} '{right}'."));
            return DataType.Unknown;
        }
    }
}
