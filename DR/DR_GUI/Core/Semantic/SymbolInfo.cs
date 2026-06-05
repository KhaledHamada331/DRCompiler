using DR_GUI.Core.AST;

namespace DR_GUI.Core.Semantic
{
    public class SymbolInfo
    {
        public string Name { get; }
        public DataType DataType { get; set; }
        public int DeclaredLine { get; }

        public SymbolInfo(string name, DataType dataType, int line)
        {
            Name = name;
            DataType = dataType;
            DeclaredLine = line;
        }

        public override string ToString() => $"{Name} : {DataType} (line {DeclaredLine})";
    }
}
