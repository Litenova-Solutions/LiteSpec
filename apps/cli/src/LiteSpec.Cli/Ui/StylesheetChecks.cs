using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The stylesheet checks of the UI validator: the token stylesheet, the
///     shared stylesheets beside it, and each application stylesheet.
/// </summary>
/// <follows>litespec/frontend/web/ui#restrict-css-decisions</follows>
/// <follows>litespec/frontend/web/ui#keep-tokens-in-one-stylesheet</follows>
sealed class StylesheetChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    // The shared package's entry imports Tailwind, its own token layer, and the
    // approved animation layer. Anything else is a project styling decision.
    /// <summary>The imports the global stylesheet may carry.</summary>
    static readonly HashSet<string> AllowedGlobalImports = new(StringComparer.Ordinal)
    {
        "tailwindcss", "tw-animate-css", "shadcn/tailwind.css",
    };

    /// <summary>The at-rules the global stylesheet may carry.</summary>
    static readonly HashSet<string> AllowedGlobalAtRules = new(StringComparer.Ordinal)
    {
        "import", "theme", "layer", "variant", "custom-variant", "plugin", "utility", "source",
        "keyframes", "font-face", "media", "supports", "container", "property", "charset", "page",
    };

    /// <summary>A stylesheet import.</summary>
    static readonly Regex GlobalImport = new(@"@import\s+([""'])([^""']+)\1", RegexOptions.Compiled);

    /// <summary>The generated base layer opener.</summary>
    static readonly Regex LayerBase = new(@"^@layer\s+base\b", RegexOptions.Compiled | RegexOptions.ECMAScript);

    /// <summary>A CSS block.</summary>
    static readonly Regex CssBlock = new(@"\{([^}]*)\}", RegexOptions.Compiled);

    /// <summary>A CSS property name.</summary>
    static readonly Regex CssProperty = new(@"^-?[a-z][a-z0-9-]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>A literal colour.</summary>
    static readonly Regex LiteralColour = new(@"\b(?:oklch|rgba?|hsla?)\(|#[0-9a-fA-F]{8}\b|#[0-9a-fA-F]{6}\b|#[0-9a-fA-F]{3,4}\b", RegexOptions.Compiled | RegexOptions.ECMAScript);

    /// <summary>
    ///     Initializes a new instance of the <see cref="StylesheetChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public StylesheetChecks(UiSession session)
    {
        this.session = session;
    }

    /// <summary>
    ///     The index of the closing quote of a CSS string, or -1 when it never closes.
    /// </summary>
    /// <param name="text">The stylesheet text.</param>
    /// <param name="start">The opening quote index.</param>
    /// <returns>The closing quote index, or -1.</returns>
    static int ReadCssString(string text, int start)
    {
        var quote = text[start];
        for (var index = start + 1; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\\')
            {
                index++;
                continue;
            }

            if (character == quote)
            {
                return index;
            }

            if (character == '\n')
            {
                return -1;
            }
        }

        return -1;
    }

    // A CSS string can carry a brace, a semicolon, or a comment opener, so counting
    // those characters without tracking the string that holds them misreads
    // `[data-state="{"]` as a block and `[data-kind="a,b"]` as two selectors. The
    // scanner removes comments and empties every string body, which leaves the
    // structure the brace check and the statement classifier read while keeping each
    // selector's shape intact.
    /// <summary>
    ///     The structure of a stylesheet: comments removed and string bodies emptied.
    /// </summary>
    /// <param name="text">The stylesheet text.</param>
    /// <param name="report">Collects one malformed reason.</param>
    /// <returns>The stylesheet structure.</returns>
    static string CssStructure(string text, Action<string> report)
    {
        var output = new StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length)
        {
            var character = text[index];
            if (character == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                var end = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    report("an unterminated comment");
                    return output.ToString();
                }

                index = end + 2;
                continue;
            }

            if (character == '"' || character == '\'')
            {
                var end = ReadCssString(text, index);
                if (end < 0)
                {
                    report("an unterminated string");
                    return output.ToString();
                }

                output.Append(character).Append(character);
                index = end + 1;
                continue;
            }

            output.Append(character);
            index++;
        }

        return output.ToString();
    }

    /// <summary>
    ///     Validates the shared package's token stylesheet.
    /// </summary>
    /// <param name="file">The token stylesheet.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateGlobalCss(string file, string platform)
    {
        _ = platform;
        var label = session.RelativeToRoot(file);
        var text = File.ReadAllText(file);
        var malformed = false;
        var structure = CssStructure(text, reason =>
        {
            malformed = true;
            session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: global CSS has {reason}");
        });
        var depth = 0;
        foreach (var character in structure)
        {
            if (character == '{')
            {
                depth++;
            }

            if (character == '}')
            {
                depth--;
                if (depth < 0)
                {
                    session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: closing brace has no matching opening brace");
                    malformed = true;
                    break;
                }
            }
        }

        if (!malformed && depth != 0)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: global CSS has an unclosed block");
        }

        // The packages the manifest already pins. A pinned font family is a token, so
        // importing one from the token sheet is the sheet doing its job rather than a
        // project reaching for a stylesheet the sheet cannot see.
        var pinned = JsonRead.NpmKeys(session.Manifest);
        foreach (Match match in GlobalImport.Matches(text))
        {
            // The token sheet may import the Tailwind layer, the approved animation
            // layer, and any package the manifest already pins.
            var specifier = match.Groups[2].Value;
            if (AllowedGlobalImports.Contains(specifier) || specifier.StartsWith("./", StringComparison.Ordinal))
            {
                continue;
            }

            string packageName;
            if (specifier.StartsWith('@'))
            {
                var parts = specifier.Split('/');
                packageName = parts.Length >= 2 ? parts[0] + "/" + parts[1] : specifier;
            }
            else
            {
                packageName = specifier.Split('/')[0];
            }

            if (!pinned.Contains(packageName))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: import '{specifier}' is outside the approved global CSS surface");
            }
        }

        ClassifyGlobalCss(label, structure);
    }

    // A top-level classifier, not a CSS parser. It reports the statements the
    // convention names as ungoverned: a class or id selector declared in the global
    // entry. It deliberately makes no claim about declarations inside a block, and
    // the repository adds no parser dependency to make one.
    /// <summary>
    ///     Reports the ungoverned statements of a global stylesheet.
    /// </summary>
    /// <param name="label">The stylesheet label the findings name.</param>
    /// <param name="stripped">The stylesheet structure.</param>
    public void ClassifyGlobalCss(string label, string stripped)
    {
        var depth = 0;
        var statement = new StringBuilder();

        // The generated entry expresses its documented browser base rules with
        // '@apply' inside '@layer base'. That block is accepted generated source, the
        // same way upstream utility classes in installed components are. '@apply'
        // anywhere else hides an unreviewed utility group.
        var topLevelOpener = "";
        var applyOutsideBase = false;
        foreach (var character in stripped)
        {
            if (character == '{')
            {
                if (depth == 0)
                {
                    topLevelOpener = statement.ToString().Trim();
                    ReportGlobalStatement(label, statement.ToString());
                }

                depth++;
                statement.Clear();
                continue;
            }

            if (character == '}')
            {
                depth = Math.Max(0, depth - 1);
                if (depth == 0)
                {
                    topLevelOpener = "";
                }

                statement.Clear();
                continue;
            }

            if (character == ';' && depth == 0)
            {
                ReportGlobalStatement(label, statement.ToString());
                statement.Clear();
                continue;
            }

            statement.Append(character);
            if (depth > 0 && statement.ToString().Contains("@apply", StringComparison.Ordinal) && !LayerBase.IsMatch(topLevelOpener))
            {
                applyOutsideBase = true;
                statement.Clear();
            }
        }

        if (applyOutsideBase)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: '@apply' outside the generated '@layer base' block hides an unreviewed utility group");
        }
    }

    /// <summary>
    ///     Reports one ungoverned statement of a global stylesheet.
    /// </summary>
    /// <param name="label">The stylesheet label the findings name.</param>
    /// <param name="statement">The statement text.</param>
    public void ReportGlobalStatement(string label, string statement)
    {
        var value = statement.Trim();
        if (value.Length == 0)
        {
            return;
        }

        if (value.StartsWith('@'))
        {
            var rest = value[1..];
            var end = rest.Length;
            for (var index = 0; index < rest.Length; index++)
            {
                if (char.IsWhiteSpace(rest[index]) || rest[index] == '(' || rest[index] == ';')
                {
                    end = index;
                    break;
                }
            }

            var name = rest[..end];
            if (!AllowedGlobalAtRules.Contains(name))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: at-rule '@{name}' is outside the approved global CSS surface");
            }

            return;
        }

        // The generated theme entry declares the color-scheme class itself.
        foreach (var selector in value.Split(','))
        {
            var trimmed = selector.Trim();
            if (trimmed is ".dark" or ".light")
            {
                continue;
            }

            if (trimmed.StartsWith('.') || trimmed.StartsWith('#'))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#restrict-css-decisions] {label}: selector '{trimmed}' is a feature style; use a component variant or semantic token");
            }
        }
    }

    // An application stylesheet imports the shared package's tokens and writes any
    // value from a token. A literal color or a literal size here is a second token
    // sheet beside the first, so every declaration is held to the custom-property
    // form. A frontend holding no shared composite still declares its focus ring and
    // its device rules here, from the same tokens.
    /// <summary>
    ///     Validates one application stylesheet against the shared package's tokens.
    /// </summary>
    /// <param name="file">The application stylesheet.</param>
    /// <param name="packageRoot">The shared package root, or null.</param>
    /// <param name="package">The declared shared package.</param>
    /// <param name="platform">The frontend platform.</param>
    public void ValidateFrontendStylesheet(string file, string? packageRoot, JsonNode? package, string platform)
    {
        _ = platform;
        var label = session.RelativeToRoot(file);
        var text = File.ReadAllText(file);
        var structure = CssStructure(text, _ => { });
        var imports = GlobalImport.Matches(text).Cast<Match>().Select(match => match.Groups[2].Value).ToList();
        if (imports.Count == 0)
        {
            session.Errors.Add($"[litespec/frontend/web/ui#keep-tokens-in-one-stylesheet] {label}: imports no stylesheet; an application stylesheet imports the shared package's token stylesheet");
            return;
        }

        var packageName = JsonRead.Child(package, "name");
        var prefix = JsonRead.Show(packageName) + "/";
        foreach (var value in imports)
        {
            if (value.StartsWith("./", StringComparison.Ordinal) || value.StartsWith("../", StringComparison.Ordinal))
            {
                continue;
            }

            if (!value.StartsWith(prefix, StringComparison.Ordinal))
            {
                session.Errors.Add($"[litespec/frontend/web/ui#keep-tokens-in-one-stylesheet] {label}: import '{value}' is outside the shared package '{JsonRead.Show(packageName)}'");
            }
        }

        foreach (Match match in CssBlock.Matches(structure))
        {
            foreach (var declaration in match.Groups[1].Value.Split(';'))
            {
                var property = declaration.Split(':')[0].Trim();

                // A custom property is a token. An at-rule is `@apply`, `@media`, or a
                // variant, none of which writes a value of its own here. Anything else
                // that reads as a CSS property is a literal the token sheet owns.
                if (property.Length == 0 || property.StartsWith("--", StringComparison.Ordinal) || property.StartsWith('@'))
                {
                    continue;
                }

                if (!CssProperty.IsMatch(property))
                {
                    continue;
                }

                session.Errors.Add($"[litespec/frontend/web/ui#keep-tokens-in-one-stylesheet] {label}: declares '{property}'; a value written in an application belongs in the shared package's tokens");
            }
        }

        if (packageRoot is not null && !FileWalk.Within(Path.GetFullPath(session.Root), packageRoot))
        {
            session.Errors.Add($"[litespec/frontend/web/ui#keep-tokens-in-one-stylesheet] {label}: the shared package root '{JsonRead.Show(JsonRead.Child(package, "path"))}' is outside the workspace");
        }
    }

    // A stylesheet the shared package ships beside the token sheet carries
    // utilities, device rules, and display helpers, written from tokens. A literal
    // color here is a second token sheet beside the first, so every stylesheet but
    // the token sheet is held to the same rule as an application stylesheet.
    // (litespec/frontend/web/ui#keep-tokens-in-one-stylesheet)
    /// <summary>
    ///     Validates the stylesheets the shared package ships beside the token sheet.
    /// </summary>
    /// <param name="packageRoot">The shared package root.</param>
    /// <param name="tokensFile">The token stylesheet.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateSharedStylesheets(string packageRoot, string tokensFile, string platform)
    {
        _ = platform;
        var tokenPath = Path.GetFullPath(tokensFile);
        foreach (var file in FileWalk.Walk(packageRoot, candidate => candidate.EndsWith(".css", StringComparison.Ordinal)))
        {
            if (Path.GetFullPath(file) == tokenPath)
            {
                continue;
            }

            if (SourceFiles.TestDir.IsMatch(file))
            {
                continue;
            }

            var label = session.RelativeToRoot(file);
            var structure = CssStructure(File.ReadAllText(file), _ => { });
            var reported = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in LiteralColour.Matches(structure))
            {
                if (!reported.Add(match.Value))
                {
                    continue;
                }

                session.Errors.Add($"[litespec/frontend/web/ui#keep-tokens-in-one-stylesheet] {label}: literal color '{match.Value}' belongs in the token stylesheet");
            }
        }
    }
}
