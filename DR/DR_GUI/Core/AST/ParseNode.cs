using System.Collections.Generic;

namespace DR_GUI.Core.AST
{
    public class ParseNode
    {
        public string Label { get; set; }
        public DataType DataType { get; set; }
        public int Line { get; set; }
        public List<ParseNode> Children { get; } = new List<ParseNode>();

        public ParseNode(string label, int line = -1)
        {
            Label = label;
            Line = line;
            DataType = DataType.Unknown;
        }

        public void Add(params ParseNode[] nodes) => Children.AddRange(nodes);

        public override string ToString() => $"{Label} : {DataType} (line {Line})";
    }
}
