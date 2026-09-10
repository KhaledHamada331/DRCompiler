using System.Text;
using DR_GUI.Core.AST;
using DR_GUI.Core.Lexer;
using DR_GUI.Core.Parser;
using DR_GUI.Core.Semantic;

namespace DRCompiler.Web.Services;

// ── DTOs ────────────────────────────────────────────────────────────────────

public sealed record TokenView(string Type, string Value, int Line);

public sealed record SymbolView(string Name, string Type, string Scope, int DeclaredLine);

public sealed record DiagnosticView(string Stage, string Message, int? Line);

public sealed record CompilationResult(
    IReadOnlyList<TokenView> Tokens,
    string? ParseTreeText,
    string? IrText, // Reserved for future IR output — null today
    IReadOnlyList<SymbolView> Symbols,
    IReadOnlyList<DiagnosticView> Diagnostics,
    bool Success
);

// ── Service ─────────────────────────────────────────────────────────────────

public sealed class DrCompilationService
{
    /// <summary>
    /// Runs the full compilation pipeline (Lex → Parse → Semantic) and returns
    /// structured results. Exceptions at each stage are caught individually and
    /// converted to <see cref="DiagnosticView"/> entries so the UI never sees
    /// an unhandled exception.
    /// </summary>
    public CompilationResult Compile(string sourceCode)
    {
        var tokens = new List<TokenView>();
        var symbols = new List<SymbolView>();
        var diagnostics = new List<DiagnosticView>();
        string? parseTreeText = null;

        // ── Stage 1: Lexical Analysis ──────────────────────────────────────
        List<Token>? rawTokens = null;
        try
        {
            var scanner = new DRScanner();
            rawTokens = scanner.Scan(sourceCode);

            foreach (var t in rawTokens)
            {
                tokens.Add(new TokenView(t.Type.ToString(), t.Value, t.Line));
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new DiagnosticView(
                "Lexical",
                ex.Message,
                TryExtractLine(ex.Message)
            ));
            return BuildResult(tokens, null, symbols, diagnostics, success: false);
        }

        // ── Stage 2: Parsing + Semantic Analysis ───────────────────────────
        DRParserSemantic? parser = null;
        try
        {
            parser = new DRParserSemantic(rawTokens);
            parser.Parse();
        }
        catch (Exception ex)
        {
            diagnostics.Add(new DiagnosticView(
                "Syntax",
                ex.Message,
                TryExtractLine(ex.Message)
            ));

            // Even though parsing failed, we still have tokens to show
            return BuildResult(tokens, null, symbols, diagnostics, success: false);
        }

        // ── Stage 3: Collect Semantic Errors ───────────────────────────────
        try
        {
            if (parser.Errors != null)
            {
                foreach (var err in parser.Errors)
                {
                    diagnostics.Add(new DiagnosticView(
                        "Semantic",
                        err.Message,
                        err.Line > 0 ? err.Line : null
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new DiagnosticView(
                "Internal",
                $"Failed to collect semantic errors: {ex.Message}",
                null
            ));
        }

        // ── Build AST text ─────────────────────────────────────────────────
        try
        {
            if (parser.Root != null)
            {
                parseTreeText = RenderParseTree(parser.Root);
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new DiagnosticView(
                "Internal",
                $"Failed to render parse tree: {ex.Message}",
                null
            ));
        }

        // ── Collect symbols ────────────────────────────────────────────────
        try
        {
            if (parser.SymTab != null)
            {
                foreach (var sym in parser.SymTab.GetAllSymbols())
                {
                    symbols.Add(new SymbolView(
                        sym.Name,
                        MapDataType(sym.DataType),
                        "Global", // Scope tracking not yet in Core
                        sym.DeclaredLine
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            diagnostics.Add(new DiagnosticView(
                "Internal",
                $"Failed to collect symbols: {ex.Message}",
                null
            ));
        }

        bool success = diagnostics.Count == 0;
        return BuildResult(tokens, parseTreeText, symbols, diagnostics, success);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static CompilationResult BuildResult(
        List<TokenView> tokens,
        string? parseTreeText,
        List<SymbolView> symbols,
        List<DiagnosticView> diagnostics,
        bool success)
    {
        return new CompilationResult(
            Tokens: tokens,
            ParseTreeText: parseTreeText,
            IrText: null, // Reserved for future IR phase
            Symbols: symbols,
            Diagnostics: diagnostics,
            Success: success
        );
    }

    /// <summary>
    /// Renders a <see cref="ParseNode"/> tree as indented plain text.
    /// Each line is tagged with a depth CSS class for styling.
    /// Format: "depth|label" so the Razor component can split and style.
    /// </summary>
    private static string RenderParseTree(ParseNode root)
    {
        var sb = new StringBuilder();
        RenderNode(sb, root, 0);
        return sb.ToString();
    }

    private static void RenderNode(StringBuilder sb, ParseNode node, int depth)
    {
        // Encode: depth|indentedLabel
        sb.Append(depth);
        sb.Append('|');
        sb.Append(new string(' ', depth * 2));
        sb.Append(node.ToString()).Append('\n');

        foreach (var child in node.Children)
        {
            RenderNode(sb, child, depth + 1);
        }
    }

    private static string MapDataType(DataType dt) => dt switch
    {
        DataType.File => "file (int)",
        DataType.Duration => "duration (double)",
        DataType.Note => "note (string)",
        DataType.Status => "status (bool)",
        DataType.Include => "include",
        _ => "unknown"
    };

    /// <summary>
    /// Attempts to extract a line number from an exception message of the form
    /// "... at line N" or "... (line N) ...".
    /// </summary>
    private static int? TryExtractLine(string message)
    {
        // Pattern: "line <number>"
        var match = System.Text.RegularExpressions.Regex.Match(
            message,
            @"line\s+(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
        if (match.Success && int.TryParse(match.Groups[1].Value, out var line))
        {
            return line;
        }
        return null;
    }
}
