namespace DR_GUI.Core.Semantic
{
    public class SemanticError
    {
        public int Line { get; }
        public string Message { get; }
        public SemanticError(int line, string msg) { Line = line; Message = msg; }
        public override string ToString() => $"Semantic Error (line {Line}): {Message}";
    }
}
