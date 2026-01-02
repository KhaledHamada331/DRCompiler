using System;
using System.Collections.Generic;
using System.Linq;

namespace DR_GUI
{
    // ---------- Symbol Table and Semantic helper classes ----------
    public class SymbolInfo
    {
        public string Name { get; }
        public string DataType { get; set; }
        public int DeclaredLine { get; }

        public SymbolInfo(string name, string dataType, int line)
        {
            Name = name;
            DataType = dataType;
            DeclaredLine = line;
        }

        public override string ToString() => $"{Name} : {DataType} (line {DeclaredLine})";
    }

    public class SymbolTable
    {
        private readonly Stack<Dictionary<string, SymbolInfo>> scopes = new Stack<Dictionary<string, SymbolInfo>>();
        private readonly List<SymbolInfo> allSymbols = new List<SymbolInfo>(); // Keep track of all symbols ever declared

        public SymbolTable()
        {
            EnterScope();
        }

        public void EnterScope() => scopes.Push(new Dictionary<string, SymbolInfo>());

        public void ExitScope()
        {
            if (scopes.Count > 0) scopes.Pop();
        }

        public bool Declare(string name, string dataType, int line, out string err)
        {
            var current = scopes.Peek();
            if (current.ContainsKey(name))
            {
                err = $"Redeclaration of '{name}' at line {line}. Previously declared at line {current[name].DeclaredLine}.";
                return false;
            }
            var symbol = new SymbolInfo(name, dataType, line);
            current[name] = symbol;
            allSymbols.Add(symbol); // Add to the all symbols list
            err = null;
            return true;
        }

        public SymbolInfo Lookup(string name)
        {
            foreach (var scope in scopes)
            {
                if (scope.TryGetValue(name, out var sym)) return sym;
            }
            return null;
        }

        public IEnumerable<SymbolInfo> GetAllSymbols()
        {
            // Return all symbols that were ever declared
            return allSymbols;
        }
    }

    public class SemanticError
    {
        public int Line { get; }
        public string Message { get; }
        public SemanticError(int line, string msg) { Line = line; Message = msg; }
        public override string ToString() => $"Semantic Error (line {Line}): {Message}";
    }

    public class ParseNode
    {
        public string Label { get; set; }
        public string DataType { get; set; }
        public int Line { get; set; }
        public List<ParseNode> Children { get; } = new List<ParseNode>();

        public ParseNode(string label, int line = -1)
        {
            Label = label;
            Line = line;
            DataType = "unknown";
        }

        public void Add(params ParseNode[] nodes) => Children.AddRange(nodes);

        public override string ToString() => $"{Label} : {DataType} (line {Line})";

        public string Pretty(int indent = 0)
        {
            var pad = new string(' ', indent * 2);
            var s = $"{pad}{ToString()}\n";
            foreach (var c in Children) s += c.Pretty(indent + 1);
            return s;
        }
    }
    
    public class DRParserSemantic
    {
        private readonly List<Token> tokens;
        private int currentTokenIndex = 0;

        private readonly TokenType[] dataTypes = {
            TokenType.TYPE_FILE, TokenType.TYPE_DURATION,
            TokenType.TYPE_NOTE, TokenType.TYPE_STATUS
        };

        private readonly TokenType[] relOps = {
            TokenType.OP_EQUAL, TokenType.OP_NOT_EQUAL,
            TokenType.OP_LESS, TokenType.OP_LESS_EQUAL,
            TokenType.OP_GREATER, TokenType.OP_GREATER_EQUAL
        };

        private readonly SymbolTable symtab = new SymbolTable();
        private readonly List<SemanticError> errors = new List<SemanticError>();

        public List<SemanticError> Errors => errors;
        public SymbolTable SymTab => symtab;
        public ParseNode Root { get; private set; }

        public DRParserSemantic(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token peek()
        {
            if (currentTokenIndex >= tokens.Count) return tokens.Last();
            return tokens[currentTokenIndex];
        }

        private bool check(TokenType type)
        {
            return peek().Type == type;
        }

        private Token consume(TokenType expectedType)
        {
            Token token = peek();
            if (token.Type == expectedType)
            {
                currentTokenIndex++;
                return token;
            }
            throw new Exception($" Syntax Error at line {token.Line}: Expected {expectedType} but found {token.Type} ('{token.Value}').");
        }

        public void Parse()
        {
            Root = new ParseNode("Program", 0);
            parseStatementList(Root);
            consume(TokenType.EOF);
        }

        private string BaseType(string t)
        {
            switch (t)
            {
                case "file": return "int";
                case "duration": return "double";
                case "note": return "string";
                case "status": return "bool";
                default: return t;
            }
        }

        private void parseStatementList(ParseNode parent)
        {
            while (!check(TokenType.EOF) && !check(TokenType.SEP_RBRACE))
            {
                if (check(TokenType.KEYWORD_INCLUDE))
                {
                    var incNode = new ParseNode("Include", peek().Line);
                    consume(TokenType.KEYWORD_INCLUDE);
                    var id = consume(TokenType.IDENTIFIER);
                    var idNode = new ParseNode($"#ATTACH {id.Value}", id.Line) { DataType = "include" };
                    incNode.Add(idNode);
                    parent.Add(incNode);
                }
                else if (check(TokenType.KEYWORD_PLAN))
                {
                    var fnNode = parseFunctionDefinition();
                    parent.Add(fnNode);
                }
                else
                {
                    var st = parseStatement();
                    parent.Add(st);
                }
            }
        }

        private ParseNode parseFunctionDefinition()
        {
            var node = new ParseNode("PlanMain", peek().Line);
            consume(TokenType.KEYWORD_PLAN);
            consume(TokenType.KEYWORD_MAIN);
            consume(TokenType.SEP_LPAREN);
            consume(TokenType.SEP_RPAREN);
            symtab.EnterScope(); // Enter function scope
            var block = parseBlock();
            node.Add(block);
            symtab.ExitScope(); // Exit function scope
            return node;
        }

        private ParseNode parseStatement()
        {
            TokenType currentType = peek().Type;

            if (dataTypes.Contains(currentType))
                return parseDeclaration();
            else if (currentType == TokenType.IDENTIFIER)
                return parseAssignmentOrFunctionCall();
            else if (currentType == TokenType.KEYWORD_IF)
                return parseIfStatement();
            else if (currentType == TokenType.KEYWORD_WHILE)
                return parseWhileLoop();
            else if (currentType == TokenType.KEYWORD_FOR)
                return parseForLoop();
            else if (currentType == TokenType.KEYWORD_OUTPUT)
                return parseOutputStatement();
            else if (currentType == TokenType.KEYWORD_RETURN)
                return parseReturnStmt();
            else
                throw new Exception($"Syntax Error at line {peek().Line}: Expected statement start but found {peek().Type}.");
        }

        private ParseNode parseBlock()
        {
            var node = new ParseNode("Block", peek().Line);
            consume(TokenType.SEP_LBRACE);
            symtab.EnterScope();
            parseStatementList(node);
            consume(TokenType.SEP_RBRACE);
            symtab.ExitScope();
            return node;
        }

        private string TokenTypeToDataType(TokenType tt)
        {
            switch (tt)
            {
                case TokenType.TYPE_FILE: return "file";
                case TokenType.TYPE_DURATION: return "duration";
                case TokenType.TYPE_NOTE: return "note";
                case TokenType.TYPE_STATUS: return "status";
                default: return "unknown";
            }
        }

        private ParseNode parseDeclaration()
        {
            var typeToken = consume(peek().Type);
            var declaredType = TokenTypeToDataType(typeToken.Type);
            var id = consume(TokenType.IDENTIFIER);
            var node = new ParseNode("Declaration", id.Line);
            node.Add(new ParseNode(id.Value, id.Line) { DataType = declaredType });

            if (!symtab.Declare(id.Value, declaredType, id.Line, out var err))
                errors.Add(new SemanticError(id.Line, err));

            if (check(TokenType.SEP_DOLLAR) || check(TokenType.OP_ASSIGN))
            {
                if (check(TokenType.SEP_DOLLAR)) consume(TokenType.SEP_DOLLAR);
                consume(TokenType.OP_ASSIGN);
                var (exprNode, exprType) = parseExpression();
                node.Add(exprNode);

                if (!IsAssignable(declaredType, exprType))
                    errors.Add(new SemanticError(id.Line, $"Type mismatch in initialization of '{id.Value}': cannot assign '{exprType}' to '{declaredType}'."));
            }

            consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private ParseNode parseAssignmentOrFunctionCall()
        {
            var idToken = consume(TokenType.IDENTIFIER);
            var node = new ParseNode("Stmt", idToken.Line);
            var sym = symtab.Lookup(idToken.Value);
            if (sym == null)
                errors.Add(new SemanticError(idToken.Line, $"Undefined identifier '{idToken.Value}'."));

            if (check(TokenType.SEP_DOLLAR) || check(TokenType.OP_ASSIGN))
            {
                if (check(TokenType.SEP_DOLLAR)) consume(TokenType.SEP_DOLLAR);
                consume(TokenType.OP_ASSIGN);
                var (exprNode, exprType) = parseExpression();
                node.Add(new ParseNode(idToken.Value, idToken.Line) { DataType = sym?.DataType ?? "unknown" });
                node.Add(exprNode);

                if (sym != null && !IsAssignable(sym.DataType, exprType))
                    errors.Add(new SemanticError(idToken.Line, $"Type mismatch in assignment to '{idToken.Value}': cannot assign '{exprType}' to '{sym.DataType}'."));

                consume(TokenType.SEP_SEMICOLON);
                return node;
            }
            else if (check(TokenType.OP_INCREMENT) || check(TokenType.OP_DECREMENT))
            {
                var op = consume(peek().Type);
                node.Add(new ParseNode($"{idToken.Value} {op.Value}", idToken.Line));
                consume(TokenType.SEP_SEMICOLON);
                return node;
            }
            else if (check(TokenType.SEP_LPAREN))
            {
                var call = parseFunctionCallWithIdentifier(idToken.Value, idToken.Line);
                consume(TokenType.SEP_SEMICOLON);
                return call;
            }
            else
                throw new Exception($"Syntax Error at line {peek().Line}: Expected assignment, increment, or function call after identifier.");
        }

        private ParseNode parseIfStatement()
        {
            var node = new ParseNode("IF", peek().Line);
            consume(TokenType.KEYWORD_IF);
            consume(TokenType.SEP_LPAREN);
            var (condNode, condType) = parseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType))
                errors.Add(new SemanticError(condNode.Line, $"Condition expression should be boolean but found '{condType}'."));
            consume(TokenType.SEP_RPAREN);
            var blk = parseBlock();
            node.Add(blk);

            if (check(TokenType.KEYWORD_ELSE))
            {
                consume(TokenType.KEYWORD_ELSE);
                var elseBlk = parseBlock();
                node.Add(elseBlk);
            }
            return node;
        }

        private ParseNode parseWhileLoop()
        {
            var node = new ParseNode("WHILE", peek().Line);
            consume(TokenType.KEYWORD_WHILE);
            consume(TokenType.SEP_LPAREN);
            var (condNode, condType) = parseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType))
                errors.Add(new SemanticError(condNode.Line, $"Condition expression should be boolean but found '{condType}'."));
            consume(TokenType.SEP_RPAREN);
            var blk = parseBlock();
            node.Add(blk);
            return node;
        }

        private ParseNode parseForLoop()
        {
            var node = new ParseNode("FOR", peek().Line);
            consume(TokenType.KEYWORD_FOR);
            consume(TokenType.SEP_LPAREN);
            var decl = parseDeclaration();
            node.Add(decl);
            var (condNode, condType) = parseCondition();
            node.Add(condNode);
            if (!IsBooleanLike(condType))
                errors.Add(new SemanticError(condNode.Line, $"CHECKLIST condition should be boolean but found '{condType}'."));
            consume(TokenType.SEP_SEMICOLON);
            var update = parseAssignmentOrFunctionCallWithoutSemicolon();
            node.Add(update);
            consume(TokenType.SEP_RPAREN);
            var blk = parseBlock();
            node.Add(blk);
            return node;
        }

        private ParseNode parseAssignmentOrFunctionCallWithoutSemicolon()
        {
            var idToken = consume(TokenType.IDENTIFIER);
            var node = new ParseNode("Update", idToken.Line);
            var sym = symtab.Lookup(idToken.Value);
            if (sym == null) errors.Add(new SemanticError(idToken.Line, $"Undefined identifier '{idToken.Value}' in update expression."));

            if (check(TokenType.SEP_DOLLAR) || check(TokenType.OP_ASSIGN))
            {
                if (check(TokenType.SEP_DOLLAR)) consume(TokenType.SEP_DOLLAR);
                consume(TokenType.OP_ASSIGN);
                var (exprNode, exprType) = parseExpression();
                node.Add(new ParseNode(idToken.Value, idToken.Line) { DataType = sym?.DataType ?? "unknown" });
                node.Add(exprNode);
                if (sym != null && !IsAssignable(sym.DataType, exprType))
                    errors.Add(new SemanticError(idToken.Line, $"Type mismatch in update: cannot assign '{exprType}' to '{sym.DataType}'."));
                return node;
            }
            else if (check(TokenType.OP_INCREMENT) || check(TokenType.OP_DECREMENT))
            {
                var op = consume(peek().Type);
                node.Add(new ParseNode($"{idToken.Value} {op.Value}", idToken.Line));
                return node;
            }
            else if (check(TokenType.SEP_LPAREN))
            {
                var call = parseFunctionCallWithIdentifier(idToken.Value, idToken.Line);
                return call;
            }
            else
                throw new Exception($" Syntax Error at line {peek().Line}: Expected valid update expression in CHECKLIST.");
        }

        private ParseNode parseOutputStatement()
        {
            var node = new ParseNode("OUTPUT", peek().Line);
            consume(TokenType.KEYWORD_OUTPUT);
            var (exprNode, exprType) = parseExpression();
            node.Add(exprNode);
            consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private ParseNode parseReturnStmt()
        {
            var node = new ParseNode("RETURN", peek().Line);
            consume(TokenType.KEYWORD_RETURN);
            var (exprNode, exprType) = parseExpression();
            node.Add(exprNode);
            consume(TokenType.SEP_SEMICOLON);
            return node;
        }

        private (ParseNode, string) parseCondition()
        {
            var (leftNode, leftType) = parseExpression();
            var node = new ParseNode("Condition", leftNode.Line);
            node.Add(leftNode);

            if (relOps.Any(op => check(op)))
            {
                var opToken = consume(peek().Type);
                var (rightNode, rightType) = parseExpression();
                node.Add(new ParseNode(opToken.Value, opToken.Line));
                node.Add(rightNode);

                var resultType = TypeOfRelational(leftType, rightType, opToken.Type, opToken.Line);
                node.DataType = resultType;
                return (node, resultType);
            }
            node.DataType = leftType;
            return (node, leftType);
        }

        private (ParseNode, string) parseExpression()
        {
            var (leftNode, leftType) = parseTerm();
            var root = leftNode;
            var currentType = leftType;

            while (check(TokenType.OP_ADD) || check(TokenType.OP_SUBTRACT))
            {
                var op = consume(peek().Type);
                var (rightNode, rightType) = parseTerm();

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

        private (ParseNode, string) parseTerm()
        {
            var (leftNode, leftType) = parseFactor();
            var root = leftNode;
            var currentType = leftType;

            while (check(TokenType.OP_MULTIPLY) || check(TokenType.OP_DIVIDE))
            {
                var op = consume(peek().Type);
                var (rightNode, rightType) = parseFactor();

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

        private (ParseNode, string) parseFactor()
        {
            TokenType currentType = peek().Type;

            if (currentType == TokenType.LITERAL_INTEGER)
            {
                var t = consume(TokenType.LITERAL_INTEGER);
                var n = new ParseNode(t.Value, t.Line) { DataType = "int" };
                return (n, "int");
            }
            else if (currentType == TokenType.LITERAL_DOUBLE)
            {
                var t = consume(TokenType.LITERAL_DOUBLE);
                var n = new ParseNode(t.Value, t.Line) { DataType = "double" };
                return (n, "double");
            }
            else if (currentType == TokenType.LITERAL_STRING)
            {
                var t = consume(TokenType.LITERAL_STRING);
                var n = new ParseNode($"\"{t.Value}\"", t.Line) { DataType = "string" };
                return (n, "string");
            }
            else if (currentType == TokenType.LITERAL_BOOLEAN)
            {
                var t = consume(TokenType.LITERAL_BOOLEAN);
                var n = new ParseNode(t.Value, t.Line) { DataType = "bool" };
                return (n, "bool");
            }
            else if (currentType == TokenType.IDENTIFIER)
            {
                var id = consume(TokenType.IDENTIFIER);
                var sym = symtab.Lookup(id.Value);
                if (sym == null)
                {
                    errors.Add(new SemanticError(id.Line, $"Undefined identifier '{id.Value}'."));
                    var n = new ParseNode(id.Value, id.Line) { DataType = "unknown" };
                    return (n, "unknown");
                }
                var node = new ParseNode(id.Value, id.Line) { DataType = sym.DataType };
                return (node, sym.DataType);
            }
            else if (currentType == TokenType.SEP_LPAREN)
            {
                consume(TokenType.SEP_LPAREN);
                var (node, type) = parseExpression();
                consume(TokenType.SEP_RPAREN);
                var paren = new ParseNode("Paren", node.Line) { DataType = type };
                paren.Add(node);
                return (paren, type);
            }
            else
                throw new Exception($"Syntax Error at line {peek().Line}: Expected number, identifier, or expression.");
        }

        private ParseNode parseFunctionCallWithIdentifier(string funcName, int line)
        {
            var node = new ParseNode($"Call {funcName}", line);
            consume(TokenType.SEP_LPAREN);

            if (!check(TokenType.SEP_RPAREN))
            {
                var (argNode, argType) = parseExpression();
                node.Add(argNode);

                while (check(TokenType.SEP_COMMA))
                {
                    consume(TokenType.SEP_COMMA);
                    var (aN, aT) = parseExpression();
                    node.Add(aN);
                }
            }

            consume(TokenType.SEP_RPAREN);
            return node;
        }

        private static bool IsNumeric(string t) => t == "int" || t == "double";
        private static bool IsBooleanLike(string t) => t == "bool" || t == "int";

        private bool IsAssignable(string target, string source)
        {
            string t1 = BaseType(target);
            string t2 = BaseType(source);

            if (t1 == t2) return true;
            if (IsNumeric(t1) && IsNumeric(t2)) return true;
            return false;
        }

        private string TypeOfBinaryArithmetic(string left, string right, TokenType op, int line)
        {
            string l = BaseType(left);
            string r = BaseType(right);

            if (IsNumeric(l) && IsNumeric(r))
                return (l == "double" || r == "double") ? "double" : "int";

            if (op == TokenType.OP_ADD && l == "string" && r == "string")
                return "string";

            errors.Add(new SemanticError(line, $"Invalid operands for arithmetic '{left}' {op} '{right}'."));
            return "unknown";
        }

        private string TypeOfRelational(string left, string right, TokenType op, int line)
        {
            string l = BaseType(left);
            string r = BaseType(right);

            if (l == r) return "bool";
            if (IsNumeric(l) && IsNumeric(r)) return "bool";

            errors.Add(new SemanticError(line, $"Invalid operands for relational '{left}' {op} '{right}'."));
            return "unknown";
        }

        public void DumpSymbolTable()
        {
            Console.WriteLine("=== Symbol Table ===");
            foreach (var s in symtab.GetAllSymbols())
                Console.WriteLine(s.ToString());
        }

        public void DumpErrors()
        {
            if (errors.Count == 0) Console.WriteLine("No semantic errors.");
            else
            {
                Console.WriteLine("=== Semantic Errors ===");
                foreach (var e in errors) Console.WriteLine(e.ToString());
            }
        }
    }
}

