using System.Collections.Generic;
using DR_GUI.Core.AST;

namespace DR_GUI.Core.Semantic
{
    public class SymbolTable
    {
        private readonly Stack<Dictionary<string, SymbolInfo>> scopes = new Stack<Dictionary<string, SymbolInfo>>();
        private readonly List<SymbolInfo> allSymbols = new List<SymbolInfo>();

        public SymbolTable()
        {
            EnterScope();
        }

        public void EnterScope() => scopes.Push(new Dictionary<string, SymbolInfo>());

        public void ExitScope()
        {
            if (scopes.Count > 0) scopes.Pop();
        }

        public bool Declare(string name, DataType dataType, int line, out string err)
        {
            var current = scopes.Peek();
            if (current.ContainsKey(name))
            {
                err = $"Redeclaration of '{name}' at line {line}. Previously declared at line {current[name].DeclaredLine}.";
                return false;
            }
            var symbol = new SymbolInfo(name, dataType, line);
            current[name] = symbol;
            allSymbols.Add(symbol);
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
            return allSymbols;
        }
    }
}
