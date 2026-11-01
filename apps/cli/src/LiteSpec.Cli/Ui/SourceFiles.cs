using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The patterns that classify a frontend's source files for the UI checks:
///     authored source, tests, visual files, imports and inline styles.
/// </summary>
static class SourceFiles
{
    /// <summary>An authored source file.</summary>
    public static readonly Regex SourceFile = new(@"\.(?:ts|tsx|js|jsx|svelte)$", RegexOptions.Compiled);

    /// <summary>A test directory.</summary>
    public static readonly Regex TestDir = new(@"(?:^|[\\/])(?:tests?|__tests__)(?:[\\/])", RegexOptions.Compiled);

    /// <summary>A test file.</summary>
    public static readonly Regex TestFile = new(@"\.(?:test|spec)\.[^.]+$", RegexOptions.Compiled);

    /// <summary>A visual file.</summary>
    public static readonly Regex VisualFile = new(@"\.visual\.(?:tsx?|svelte)$", RegexOptions.Compiled);

    /// <summary>An import or export statement.</summary>
    public static readonly Regex ImportStatement = new(@"(?:^|\n)\s*(?:import|export)[^;\n]*['""][^'""]+['""]", RegexOptions.Compiled);

    /// <summary>A Svelte inline style.</summary>
    public static readonly Regex SvelteInlineStyle = new(@"\sstyle(?::|=|\{)", RegexOptions.Compiled);

    /// <summary>A React inline style.</summary>
    public static readonly Regex ReactInlineStyle = new(@"style=\{\{", RegexOptions.Compiled);
}
