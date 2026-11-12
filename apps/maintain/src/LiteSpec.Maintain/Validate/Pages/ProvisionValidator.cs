using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Maintain.Validate.Repository.ManifestValidator;
using static LiteSpec.Maintain.Validate.RepositoryWalk;

namespace LiteSpec.Maintain.Validate.Pages;

/// <summary>
///     The provision contract: labels, assertions, and inline verification.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class ProvisionValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProvisionValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public ProvisionValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    /// <summary>Gets the shipped litespec command names.</summary>
    private static readonly HashSet<string> LiteSpecCommands = new(
        ["model", "book", "init", "new", "rename", "rules"],
        StringComparer.Ordinal);

    /// <summary>Gets the shipped litespec-maintain command names.</summary>
    private static readonly HashSet<string> MaintainCommands = new(
        ["validate", "provisions"],
        StringComparer.Ordinal);

    /// <summary>Gets the provision field labels.</summary>
    public static readonly HashSet<string> ProvisionLabels = new(
        ["Statement", "Deviation", "Rationale", "Example", "Default", "Replacement"],
        StringComparer.Ordinal);

    /// <summary>Gets the lazy set of stable diagnostic codes.</summary>
    private static readonly Lazy<HashSet<string>> StableCodeSetLazy = new(() => new HashSet<string>(StandardsValidator.StableDiagnosticCodes, StringComparer.Ordinal));

    /// <summary>Gets the set of stable diagnostic codes.</summary>
    private static HashSet<string> StableCodeSet => StableCodeSetLazy.Value;

    // A claim is the comparable core of a heading, a Statement, a Default, or a
    // summary bullet: lowercase words with punctuation, code marks, and any leading
    // actor and normative modal removed.
    /// <summary>Computes the comparable claim core of a heading or assertion.</summary>
    /// <param name="text">The heading or assertion text.</param>
    /// <returns>The normalized claim.</returns>
    private static string Claim(string text)
    {
        var normalized = ClaimCleanPattern().Replace(text.ToLowerInvariant().Replace("`", "", StringComparison.Ordinal), " ").Trim();
        return ClaimModalPattern().Replace(normalized, "").Trim();
    }

    /// <summary>Checks the labeled bodies of every provision on a page.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckProvisionBodies(string relative, PageReader reader, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        foreach (var provision in provisions)
        {
            var nonblank = provision.Body.Where(item => item.Line.Trim().Length > 0).ToList();
            string[] allowedLabels = provision.Section == "Standards"
                ? ["Statement", "Deviation", "Rationale", "Example"]
                : ["Default", "Replacement", "Rationale", "Example"];
            var labels = new List<(string Name, int Line)>();
            var activeLabel = "";
            var fenceReported = false;
            foreach (var item in provision.Body)
            {
                // The ID, Strength, Applies to, and verification lines belong to the
                // provision frame, not to its labeled fields; the identity, scope, and
                // verification checks own them.
                if (PageReader.TryParseIdLine(item.Line, out _)
                    || PageReader.TryParseVerificationLine(item.Line, out _, out _)
                    || PageReader.TryParseStrengthLine(item.Line, out _)
                    || PageReader.TryParseAppliesToLine(item.Line, out _))
                {
                    continue;
                }

                if (reader.LabelAtLine.TryGetValue(item.Number, out var name) && ProvisionLabels.Contains(name))
                {
                    labels.Add((name, item.Number));
                    activeLabel = name;
                    if (!allowedLabels.Contains(name))
                    {
                        add(relative, item.Number, MaintainerDiagnostics.Codes.StandardLabelInvalid, $"label '{name}' is invalid in {provision.Section}");
                    }

                    if (name is "Rationale" or "Example")
                    {
                        if (HasNormativeVocabulary(PageReader.VisibleText(item.Line)))
                        {
                            add(relative, item.Number, MaintainerDiagnostics.Codes.StandardInformativeNormative, $"informative content in '{provision.Id}' contains normative vocabulary");
                        }
                    }
                    else if (name == "Deviation" && provision.Section == "Standards")
                    {
                        // The modal lives in the Statement; the Deviation states the
                        // bounded reason that permits a departure, in plain words.
                        if (HasNormativeVocabulary(PageReader.VisibleText(item.Line)))
                        {
                            add(relative, item.Number, MaintainerDiagnostics.Codes.StandardDeviationNormative, $"Deviation for '{provision.Id}' contains normative vocabulary");
                        }
                    }

                    continue;
                }

                if (item.Fenced)
                {
                    if (activeLabel != "Example" && !fenceReported)
                    {
                        add(relative, item.Number, MaintainerDiagnostics.Codes.StandardExampleLabel, $"fenced example in '{provision.Id}' must follow an Example label");
                        fenceReported = true;
                    }

                    continue;
                }

                if (item.Line.Trim().Length == 0)
                {
                    continue;
                }

                if (activeLabel == "" || activeLabel is "Statement" or "Deviation" or "Default" or "Replacement")
                {
                    add(relative, item.Number, MaintainerDiagnostics.Codes.StandardUnlabeledContent, $"content in '{provision.Id}' must follow a Rationale or Example label");
                }

                if (activeLabel is "Rationale" or "Example")
                {
                    if (HasNormativeVocabulary(PageReader.VisibleText(item.Line)))
                    {
                        add(relative, item.Number, MaintainerDiagnostics.Codes.StandardInformativeNormative, $"informative content in '{provision.Id}' contains normative vocabulary");
                    }
                }
            }

            foreach (var name in labels.Select(label => label.Name).Distinct().ToList())
            {
                var count = labels.Count(label => label.Name == name);
                if (count > 1)
                {
                    add(relative, labels.First(label => label.Name == name).Line, MaintainerDiagnostics.Codes.StandardLabelDuplicate, $"provision '{provision.Id}' has {count} {name} labels");
                }
            }

            var previousLabel = -1;
            foreach (var label in labels)
            {
                var position = Array.IndexOf(allowedLabels, label.Name);
                if (position >= 0 && position < previousLabel)
                {
                    add(relative, label.Line, MaintainerDiagnostics.Codes.StandardLabelOrder, $"label '{label.Name}' is out of order in '{provision.Id}'");
                    break;
                }

                if (position >= 0)
                {
                    previousLabel = position;
                }
            }

            if (provision.Section == "Standards")
            {
                CheckStandardAssertion(relative, provision, nonblank);
            }
            else
            {
                CheckConventionAssertion(relative, provision, nonblank);
            }
        }
    }

    /// <summary>Reports whether visible text holds normative vocabulary.</summary>
    /// <param name="visible">The visible text.</param>
    /// <returns>True when the text holds normative vocabulary.</returns>
    private static bool HasNormativeVocabulary(string visible)
    {
        return ModalsPattern().Count(visible) > 0 || OtherNormativePattern().IsMatch(visible);
    }

    /// <summary>Reports whether a Statement holds one modal, or one MUST beside one folded MAY.</summary>
    /// <param name="modals">The modals the Statement holds, in order.</param>
    /// <returns>True when the Statement holds one modal or a folded permission.</returns>
    private static bool HasSingleModal(List<string> modals) =>
        modals.Count == 1
        || (modals.Count == 2
            && modals.Count(modal => modal is "MUST" or "MUST NOT") == 1
            && modals.Count(modal => modal == "MAY") == 1);

    /// <summary>Checks the Statement assertion of one Standard provision.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provision">The provision to check.</param>
    /// <param name="nonblank">The nonblank body lines.</param>
    private void CheckStandardAssertion(string relative, ProvisionSection provision, List<PageLine> nonblank)
    {
        var add = Context.Add;
        var statements = nonblank.Where(item => item.Line.StartsWith("**Statement:**", StringComparison.Ordinal)).ToList();
        if (statements.Count == 0)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.StandardMissingRequirement, $"Standard '{provision.Id}' has no Statement");
            return;
        }

        if (statements.Count > 1)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.StandardRequirementCount, $"Standard '{provision.Id}' has {statements.Count} Statements");
        }

        var assertion = statements[0];
        var statement = assertion.Line["**Statement:**".Length..].Trim();
        var visibleStatement = PageReader.VisibleText(statement);
        // A permission is not a provision on its own: it is folded into the
        // provision it relaxes, so one MAY may stand beside one MUST or MUST NOT.
        var modals = ModalsPattern().Matches(visibleStatement).Cast<Match>().Select(match => match.Value).ToList();
        if (!HasSingleModal(modals))
        {
            add(relative, assertion.Number, MaintainerDiagnostics.Codes.StandardModalCount, $"Statement for '{provision.Id}' has {modals.Count} normative modals");
        }

        if (OtherNormativePattern().IsMatch(visibleStatement))
        {
            add(relative, assertion.Number, MaintainerDiagnostics.Codes.StandardModalVocabulary, $"Statement for '{provision.Id}' uses an unsupported normative term");
        }

        if (Prose.Sentences(visibleStatement).Count != 1)
        {
            add(relative, assertion.Number, MaintainerDiagnostics.Codes.StandardSentenceCount, $"Statement for '{provision.Id}' must contain one sentence");
        }

        var should = ShouldPattern().IsMatch(visibleStatement);
        var deviations = nonblank.Where(item => item.Line.StartsWith("**Deviation:**", StringComparison.Ordinal)).ToList();
        if (should && deviations.Count == 0)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.StandardMissingDeviation, $"recommendation '{provision.Id}' has no Deviation statement");
        }

        if (!should && deviations.Count > 0)
        {
            add(relative, deviations[0].Number, MaintainerDiagnostics.Codes.StandardUnexpectedDeviation, $"non-recommendation '{provision.Id}' cannot contain a Deviation statement");
        }

        if (deviations.Count > 0 && Prose.Sentences(PageReader.VisibleText(deviations[0].Line["**Deviation:**".Length..].Trim())).Count != 1)
        {
            add(relative, deviations[0].Number, MaintainerDiagnostics.Codes.StandardDeviationSentence, $"Deviation for '{provision.Id}' must contain one sentence");
        }
    }

    /// <summary>Checks the Default and Replacement assertions of one convention provision.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provision">The provision to check.</param>
    /// <param name="nonblank">The nonblank body lines.</param>
    private void CheckConventionAssertion(string relative, ProvisionSection provision, List<PageLine> nonblank)
    {
        var add = Context.Add;
        var defaults = nonblank.Where(item => item.Line.StartsWith("**Default:**", StringComparison.Ordinal)).ToList();
        var replacements = nonblank.Where(item => item.Line.StartsWith("**Replacement:**", StringComparison.Ordinal)).ToList();
        if (defaults.Count == 0)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.ConventionMissingDefault, $"convention '{provision.Id}' has no Default statement");
        }

        if (replacements.Count == 0)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.ConventionMissingReplacement, $"convention '{provision.Id}' has no Replacement statement");
        }

        if (defaults.Count > 0 && Prose.Sentences(PageReader.VisibleText(defaults[0].Line["**Default:**".Length..].Trim())).Count != 1)
        {
            add(relative, defaults[0].Number, MaintainerDiagnostics.Codes.ConventionDefaultSentence, $"Default for '{provision.Id}' must contain one sentence");
        }

        if (replacements.Count > 0 && Prose.Sentences(PageReader.VisibleText(replacements[0].Line["**Replacement:**".Length..].Trim())).Count != 1)
        {
            add(relative, replacements[0].Number, MaintainerDiagnostics.Codes.ConventionReplacementSentence, $"Replacement for '{provision.Id}' must contain one sentence");
        }

        // A Replacement names the local convention that takes the default's
        // place. A departure instrument belongs to a Standard, never here.
        if (replacements.Count > 0 && DeviationMentionPattern().IsMatch(PageReader.VisibleText(replacements[0].Line["**Replacement:**".Length..])))
        {
            add(relative, replacements[0].Number, MaintainerDiagnostics.Codes.ConventionReplacementDeviation, $"Replacement for '{provision.Id}' names an exception or deviation; name the local convention instead");
        }
    }

    // Strength grades how binding a provision is, and the wording carries the
    // grade: a required Statement obliges with MUST or MUST NOT, an advisory
    // Statement holds neither, and a default holds a Default line.
    /// <summary>Checks that the Strength line of every provision agrees with its wording.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckStrengthAgreement(string relative, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        foreach (var provision in provisions)
        {
            var strengths = provision.Body.Where(item => PageReader.TryParseStrengthLine(item.Line, out _)).ToList();
            if (strengths.Count == 0
                || !PageReader.TryParseStrengthLine(strengths[0].Line, out var named)
                || !Scopes.StrengthValues.Contains(Scopes.Unquote(named)))
            {
                // A missing or invalid Strength line is the Strength line check's own defect.
                continue;
            }

            var strength = Scopes.Unquote(named);
            if (strength == "default")
            {
                if (!provision.Body.Any(item => item.Line.StartsWith("**Default:**", StringComparison.Ordinal)))
                {
                    add(relative, provision.Line, MaintainerDiagnostics.Codes.StandardStrengthModal, $"provision '{provision.Id}' carries Strength default but holds no Default line");
                }

                continue;
            }

            var statements = provision.Body.Where(item => item.Line.StartsWith("**Statement:**", StringComparison.Ordinal)).ToList();
            if (statements.Count == 0)
            {
                // A missing Statement is the provision contract's own defect.
                continue;
            }

            var holdsMust = MustPattern().IsMatch(PageReader.VisibleText(statements[0].Line["**Statement:**".Length..].Trim()));
            if (strength == "required" && !holdsMust)
            {
                add(relative, statements[0].Number, MaintainerDiagnostics.Codes.StandardStrengthModal, $"Statement for '{provision.Id}' carries Strength required but holds no MUST or MUST NOT");
            }
            else if (strength == "advisory" && holdsMust)
            {
                add(relative, statements[0].Number, MaintainerDiagnostics.Codes.StandardStrengthModal, $"Statement for '{provision.Id}' carries Strength advisory but holds MUST or MUST NOT");
            }
        }
    }

    /// <summary>Checks that no provision assertion only restates its heading.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckProvisionClaims(string relative, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        // A provision whose Statement or Default only repeats its own heading
        // states no obligation. The heading is a label; the assertion belongs in the
        // labeled block. (litespec/core/authoring#write-atomic-standards-provisions, litespec/core/authoring#identify-actionable-conventions)
        foreach (var provision in provisions)
        {
            var label = provision.Section == "Standards" ? "Statement" : "Default";
            var prefix = $"**{label}:**";
            var assertion = provision.Body.FirstOrDefault(item => item.Line.StartsWith(prefix, StringComparison.Ordinal));
            if (assertion is null)
            {
                continue;
            }

            provision.Claim = Claim(assertion.Line[prefix.Length..]);
            if (provision.Claim.Length > 0 && provision.Claim == Claim(provision.Title))
            {
                add(relative, provision.Line, MaintainerDiagnostics.Codes.ProvisionRestatesHeading, $"{label} for '{provision.Id}' only restates its heading");
            }
        }
    }

    /// <summary>Checks that normative vocabulary sits only inside a provision assertion.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="clean">The fence-stripped page lines.</param>
    /// <param name="provisions">The provisions on the page.</param>
    internal void CheckNormativeLocation(string relative, List<PageLine> clean, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        var provisionLines = new HashSet<int>(provisions.SelectMany(provision => new[] { provision.Line }.Concat(provision.Body.Select(item => item.Number))));
        foreach (var item in clean)
        {
            if (provisionLines.Contains(item.Number))
            {
                continue;
            }

            if (HasNormativeVocabulary(PageReader.VisibleText(item.Line)))
            {
                add(relative, item.Number, MaintainerDiagnostics.Codes.ProseNormativeLocation, "normative vocabulary is outside a Statement, Default, or Replacement");
            }
        }
    }

    /// <summary>Checks the ID line of one provision.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provision">The provision to check.</param>
    /// <param name="fields">The provision field lines.</param>
    private void CheckProvisionIdentity(string relative, ProvisionSection provision, List<PageLine> fields)
    {
        var add = Context.Add;
        // The ID line is the first line after the heading. It holds the
        // provision's identity, so a reader copies the identifier without
        // composing it. The heading may read differently from the ID slug,
        // but the ID still names its owning page.
        var ids = fields.Where(item => PageReader.TryParseIdLine(item.Line, out _)).ToList();
        if (ids.Count == 0)
        {
            add(relative, provision.Line, MaintainerDiagnostics.Codes.IdLineMissing, $"provision '{provision.Id}' has no ID line; the first line after the heading states its ID");
            return;
        }

        var first = fields.First(item => item.Line.Trim().Length > 0);
        if (!PageReader.TryParseIdLine(first.Line, out _))
        {
            add(relative, ids[0].Number, MaintainerDiagnostics.Codes.IdLineMissing, $"ID line for '{provision.Id}' must be the first line after the heading");
        }

        if (!PageReader.TryParseIdLine(ids[0].Line, out var stated) || !Provisions.IsProvisionId(stated))
        // The combined condition above also covers a stated id that is no provision identifier.
        {
            add(relative, ids[0].Number, MaintainerDiagnostics.Codes.IdLineMismatch, $"ID line states '{stated}', which is not a provision identifier");
        }
        else if (Provisions.Parse(stated)!.Page != Provisions.PagePath(relative))
        {
            add(relative, ids[0].Number, MaintainerDiagnostics.Codes.IdLineMismatch, $"ID line states '{stated}' but the provision sits on page '{Provisions.PagePath(relative)}'");
        }

        if (ids.Count > 1)
        {
            add(relative, ids[1].Number, MaintainerDiagnostics.Codes.IdLineMismatch, $"provision '{provision.Id}' has {ids.Count} ID lines");
        }
    }

    /// <summary>Reports whether a line is a Strength or Applies to frame line.</summary>
    /// <param name="line">The line to read.</param>
    /// <returns>True when the line is a scope frame line.</returns>
    private static bool IsScopeFrameLine(string line) =>
        PageReader.TryParseStrengthLine(line, out _) || PageReader.TryParseAppliesToLine(line, out _);

    /// <summary>Reads the values of one scope field from parsed fields.</summary>
    /// <param name="scope">The parsed scope fields.</param>
    /// <param name="field">The field name.</param>
    /// <returns>The field values in order.</returns>
    private static List<string> ScopeValues(IReadOnlyList<Scopes.ScopeField> scope, string field) =>
        scope.Where(entry => entry.Name == field).SelectMany(entry => entry.Values).ToList();

    // Every provision carries its Strength line, and an Applies to line only when it narrows
    // its page scope, states a record layer or trace that differs from its page, or marks its
    // layer-wide scope. Strength values are the three the scope page fixes; Scope and Applies
    // to values come from the scope tables, so an unreadable vocabulary skips value checks
    // while presence and narrowing still run. ArchitectureLayer names are checked against the scope page's
    // layer table, and trace is yes or no in code. One line reports one scope defect: a line
    // with a value defect reports no narrowing defect beside it.
    /// <summary>Checks the Strength and Applies to lines of every provision on a page.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckProvisionScopes(string relative, PageReader reader, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        var vocabulary = Context.Vocabulary;
        var page = Scopes.ReadPageScope(reader);
        var pageStacks = ScopeValues(page, "stack");
        var pageLayer = Provisions.PageLayer(relative, ScopeValues(page, "concern"));

        // On a page where one provision names the kind of file it governs, every provision
        // names one, so litespec rules can match each provision to its files.
        var artifactNamed = provisions
            .Select(provision => (provision, named: provision.Body
                .Where(item => PageReader.TryParseAppliesToLine(item.Line, out _))
                .Take(1)
                .Any(item => PageReader.TryParseAppliesToLine(item.Line, out var line)
                    && Scopes.ParseScopeFields(line).Any(field => field.Name == "artifact" && field.Values.Count > 0))))
            .ToList();
        if (artifactNamed.Any(entry => entry.named))
        {
            foreach (var (provision, named) in artifactNamed.Where(entry => !entry.named))
            {
                add(relative, provision.Line, MaintainerDiagnostics.Codes.ScopeArtifact, $"provision '{provision.Id}' names no artifact on a page where another provision names one; name the kinds of file it governs, or `artifact: any` when it governs every file of its layer");
            }
        }

        foreach (var provision in provisions)
        {
            // A pattern's fields end at its first option; everything below that
            // is drawn options, not the pattern's own fields.
            var cutoff = reader.Headings
                .Where(heading => heading.Level == 4 && heading.Line > provision.Line)
                .Select(heading => heading.Line)
                .Append(int.MaxValue)
                .Min();
            var fields = provision.Section == "Patterns"
                ? provision.Body.TakeWhile(item => item.Number < cutoff).ToList()
                : provision.Body;

            var strengths = fields.Where(item => PageReader.TryParseStrengthLine(item.Line, out _)).ToList();
            if (strengths.Count == 0)
            {
                add(relative, provision.Line, MaintainerDiagnostics.Codes.StrengthLine, $"provision '{provision.Id}' carries no Strength line; write `**Strength:** rule`, `default`, or `advice` directly after its ID line");
            }
            else if (PageReader.TryParseStrengthLine(strengths[0].Line, out var named) && !Scopes.StrengthValues.Contains(Scopes.Unquote(named)))
            {
                add(relative, strengths[0].Number, MaintainerDiagnostics.Codes.StrengthLine, $"Strength line for '{provision.Id}' names '{Scopes.Unquote(named)}', which is outside `required`, `default`, and `advisory`");
            }

            var applies = fields.Where(item => PageReader.TryParseAppliesToLine(item.Line, out _)).ToList();
            if (applies.Count == 0)
            {
                continue;
            }

            if (!PageReader.TryParseAppliesToLine(applies[0].Line, out var rest))
            {
                continue;
            }

            var narrowed = Scopes.ParseScopeFields(rest);
            if (narrowed.Count == 0)
            {
                add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names no field; write `field: value` naming stack, artifact, layer, or trace");
                continue;
            }

            var unknown = narrowed.Select(field => field.Name).Distinct().FirstOrDefault(name => name is not ("stack" or "artifact" or "layer" or "trace" or "when"));
            if (unknown is not null)
            {
                add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names unknown field '{unknown}'; name stack, artifact, layer, trace, or when");
                continue;
            }

            var clean = true;
            foreach (var field in narrowed)
            {
                if (field.Values.Count == 0)
                {
                    add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `{field.Name}` with no value");
                    clean = false;
                    continue;
                }

                if (field.Name is "layer" or "trace" or "when")
                {
                    if (field.Values.Count != 1)
                    {
                        add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `{field.Name}` with {field.Values.Count} values; name exactly one value");
                        clean = false;
                        continue;
                    }

                    if (field.Name == "when" && !Scopes.WhenValues.Contains(field.Values[0]))
                    {
                        add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `when` value '{field.Values[0]}', which is outside {string.Join(", ", Scopes.WhenValues.Order(StringComparer.Ordinal).Select(value => $"`{value}`"))}");
                        clean = false;
                    }
                    else if (field.Name == "trace" && !Scopes.TraceValues.Contains(field.Values[0]))
                    {
                        add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `trace` value '{field.Values[0]}', which is outside `yes` and `no`");
                        clean = false;
                    }
                    else if (field.Name == "layer" && vocabulary.Readable && !vocabulary.Layers.Contains(field.Values[0]))
                    {
                        add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `layer` value '{field.Values[0]}', which is outside the docs/core/scopes.md vocabulary");
                        clean = false;
                    }

                    continue;
                }

                if (!vocabulary.Readable)
                {
                    continue;
                }

                foreach (var value in field.Values)
                {
                    var known = field.Name == "stack"
                        ? vocabulary.Stacks.Contains(value)
                        : vocabulary.IsArtifact(value);
                    if (!known)
                    {
                        add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeValue, $"Applies to line for '{provision.Id}' names `{field.Name}` value '{value}', which is outside the docs/core/scopes.md vocabulary");
                        clean = false;
                    }
                }
            }

            if (!clean)
            {
                continue;
            }

            // A layer or trace that matches its page repeats the page, which the page already states.
            var layers = ScopeValues(narrowed, "layer");
            var traces = ScopeValues(narrowed, "trace");
            var layerDiffers = layers.Count == 1 && layers[0] != pageLayer;
            var traceDiffers = traces.Count == 1 && traces[0] != "no";
            var repeated = new List<string>();
            if (layers.Count == 1 && layers[0] == pageLayer)
            {
                repeated.Add($"`layer: {pageLayer}`");
            }

            if (traces.Count == 1 && traces[0] == "no")
            {
                repeated.Add("`trace: no`");
            }

            var repeats = $"Applies to line for '{provision.Id}' states {string.Join(" and ", repeated)}, which its page already gives; state a layer only when it differs from its page, and trace only when it is yes";
            if (page.Count == 0)
            {
                if (repeated.Count > 0
                    && ScopeValues(narrowed, "stack").Count == 0
                    && ScopeValues(narrowed, "artifact").Count == 0
                    && ScopeValues(narrowed, "when").Count == 0)
                {
                    add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeNarrow, repeats);
                }

                continue;
            }

            var stacks = ScopeValues(narrowed, "stack");
            var widened = false;
            // A when condition always narrows: it limits the provision to the records that meet it.
            var strictlyNarrower = ScopeValues(narrowed, "artifact").Count > 0 || ScopeValues(narrowed, "when").Count > 0 || layerDiffers || traceDiffers;
            if (stacks.Count > 0 && pageStacks.Count > 0)
            {
                if (pageStacks.Contains("any"))
                {
                    // Naming `any` repeats the page, which reports as wide; any stack set
                    // without it narrows the page to those stacks.
                    if (!stacks.Contains("any"))
                    {
                        strictlyNarrower = true;
                    }
                }
                else if (!stacks.All(pageStacks.Contains))
                {
                    widened = true;
                }
                else if (!pageStacks.All(stacks.Contains))
                {
                    strictlyNarrower = true;
                }
            }

            if (widened)
            {
                add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeNarrow, $"Applies to line for '{provision.Id}' widens its page scope");
            }
            else if (repeated.Count > 0)
            {
                add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeNarrow, repeats);
            }
            else if (!strictlyNarrower)
            {
                add(relative, applies[0].Number, MaintainerDiagnostics.Codes.ScopeNarrow, $"Applies to line for '{provision.Id}' is as wide as its page scope; narrow it or remove the line");
            }
        }
    }


    /// <summary>Checks the inline verification line of every provision on a page.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckInlineVerification(string relative, PageReader reader, List<ProvisionSection> provisions)
    {
        var add = Context.Add;
        var root = Context.Root;
        // Every provision carries its verification beside its heading: exactly one
        // verification line, last among its fields. The label names the mark:
        // "Checked by" is checkable with the command that checks it, and
        // "Reviewed for" is guidance with the question a reviewer answers. The
        // provision index reads the marks from the same lines this gate checks.
        if (!Provisions.IsProvisionPage(relative))
        {
            return;
        }

        // A leftover Verification table: the old header or one of its rows.
        foreach (var table in reader.Tables)
        {
            if (table.Header.Count == 3 && table.Header[0] == "ID" && table.Header[1] == "Mark" && table.Header[2] == "Evidence")
            {
                add(relative, table.HeaderLine, MaintainerDiagnostics.Codes.VerifyHeader, "standards pages carry verification beside each provision; remove the Verification table");
                foreach (var row in table.Rows)
                {
                    if (row.Cells.Count == 3 && Provisions.IsProvisionId(row.Cells[0].Trim()))
                    {
                        add(relative, row.Line, MaintainerDiagnostics.Codes.VerifyHeader, "standards pages carry verification beside each provision; remove the Verification table");
                    }
                }
            }
        }

        // Evidence shared word for word across provisions is one check written by
        // hand N times. The owning tool generates such lines instead.
        var seenEvidence = new HashSet<string>(StringComparer.Ordinal);
        foreach (var provision in provisions)
        {
            // A pattern's fields end at its first option; everything below that
            // is drawn options, not the pattern's own fields.
            var cutoff = reader.Headings
                .Where(heading => heading.Level == 4 && heading.Line > provision.Line)
                .Select(heading => heading.Line)
                .Append(int.MaxValue)
                .Min();
            var fields = provision.Section == "Patterns"
                ? provision.Body.TakeWhile(item => item.Number < cutoff).ToList()
                : provision.Body;

            CheckProvisionIdentity(relative, provision, fields);

            var lines = fields
                .Select((item, index) => (Item: item, Index: index))
                .Where(pair => PageReader.TryParseVerificationLine(pair.Item.Line, out _, out _))
                .ToList();
            if (lines.Count == 0)
            {
                add(relative, provision.Line, MaintainerDiagnostics.Codes.VerifyMissingId, $"provision '{provision.Id}' has no verification line");
                continue;
            }

            if (lines.Count > 1)
            {
                add(relative, lines[1].Item.Number, MaintainerDiagnostics.Codes.VerifyDuplicateId, $"provision '{provision.Id}' has {lines.Count} verification lines");
            }

            var (found, position) = lines[0];
            var after = fields.Skip(position + 1).ToList();
            if (after.Any(item => !IsScopeFrameLine(item.Line)
                && ((reader.LabelAtLine.TryGetValue(item.Number, out var label) && ProvisionLabels.Contains(label))
                    || PageReader.TryParseVerificationLine(item.Line, out _, out _)
                    || item.Fenced
                    || item.Line.Trim().Length > 0)))
            {
                add(relative, found.Number, MaintainerDiagnostics.Codes.VerifyOrder, $"verification line for '{provision.Id}' must be last among its fields");
            }

            var parsedVerification = PageReader.TryParseVerificationLine(found.Line, out var labelName, out var evidence);
            var mark = parsedVerification && labelName == "Checked by" ? "checkable" : "guidance";
            if (HasNormativeVocabulary(PageReader.VisibleText(found.Line)))
            {
                add(relative, found.Number, MaintainerDiagnostics.Codes.StandardInformativeNormative, $"informative content in '{provision.Id}' contains normative vocabulary");
            }

            if (GenericEvidencePattern().IsMatch(PageReader.VisibleText(evidence)))
            {
                add(relative, found.Number, MaintainerDiagnostics.Codes.VerifyGenericEvidence, $"Verification for '{provision.Id}' uses generic evidence");
            }

            // Evidence that echoes its own provision heading names no artifact. It
            // tells a reviewer to confirm that a rule says what it says.
            var ownerClaim = Claim(provision.Title);
            // A tool's finding tag such as `[litespec/page#slug]` names the
            // provision on purpose, so it does not count as a restatement.
            var withoutTags = ProvisionTagPattern().Replace(evidence, " ");
            if (ownerClaim.Length > 0 && Claim(withoutTags).Contains(ownerClaim, StringComparison.Ordinal))
            {
                add(relative, found.Number, MaintainerDiagnostics.Codes.VerifyTemplatedEvidence, $"Verification for '{provision.Id}' repeats its provision heading instead of naming evidence");
            }

            var normalized = evidence.Trim();
            if (normalized.Length > 0 && !seenEvidence.Add(normalized))
            {
                add(relative, found.Number, MaintainerDiagnostics.Codes.VerifyDuplicateEvidence, $"Verification for '{provision.Id}' repeats evidence used by another provision on this page");
            }

            if (mark == "checkable")
            {
                CheckCheckedByReferences(relative, found.Number, provision.Id, evidence, root);
            }
        }
    }

    // A checkable line's evidence starts with the command that checks it, in
    // backticks, then the finding code the tool reports where the tool emits
    // one. A shipped tool is cited in its consumer-side form, so a leading
    // `litespec/` resolves against the repository root with the prefix removed. A
    // path outside the shipped trees is a consumer command the page itself defines,
    // such as the consumer's route suite, which exists in consumers rather than
    // here, so only a shipped tool path that does not exist fails.
    /// <summary>Checks every reference in one checkable verification line.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="line">The 1-based line of the verification line.</param>
    /// <param name="id">The provision identifier.</param>
    /// <param name="evidence">The verification evidence text.</param>
    /// <param name="root">The repository root.</param>
    private void CheckCheckedByReferences(string relative, int line, string id, string evidence, string root)
    {
        var add = Context.Add;
        var command = EvidenceCommandPattern().Match(evidence);
        if (!command.Success)
        {
            add(relative, line, MaintainerDiagnostics.Codes.VerifyCommand, $"Verification for '{id}' is checkable but names no command");
            return;
        }

        var tokens = TokenSplitPattern().Split(command.Groups[1].Value);
        CheckLiteSpecCommandName(relative, line, id, tokens);

        var candidates = new List<string>();
        var projectIndex = Array.IndexOf(tokens, "--project");
        if (projectIndex >= 0 && projectIndex + 1 < tokens.Length && tokens[projectIndex + 1].Length > 0)
        {
            candidates.Add(tokens[projectIndex + 1]);
        }

        foreach (var token in tokens)
        {
            var cleaned = TokenCleanPattern().Replace(token, "").Split('#')[0];
            if (PathShapedPattern().IsMatch(cleaned))
            {
                candidates.Add(cleaned);
            }
        }

        foreach (var rawCandidate in candidates)
        {
            var candidate = rawCandidate.StartsWith("litespec/", StringComparison.Ordinal) ? rawCandidate["litespec/".Length..] : rawCandidate;
            if (!candidate.StartsWith("tools/", StringComparison.Ordinal)
                && !candidate.StartsWith("apps/", StringComparison.Ordinal)
                && !candidate.StartsWith("packages/", StringComparison.Ordinal))
            {
                continue;
            }

            var target = JoinRoot(root, candidate);
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names '{candidate}', which does not exist in the repository");
                return;
            }
        }

        // Every other backticked span names a finding code, an analyzer rule, or a
        // maintainer diagnostic. A kebab-case span ending in a problem word is read
        // as a finding code; an all-caps span with an underscore is read as a
        // maintainer diagnostic, so acronyms and single words stay prose.
        string? pin = null;
        HashSet<string>? shipped = null;
        var shippedRead = false;
        foreach (var span in InlineCodes(evidence).Distinct(StringComparer.Ordinal))
        {
            if (AnalyzerCodePattern().IsMatch(span))
            {
                if (!shippedRead)
                {
                    pin = Context.Manifest.ReadAnalyzerPin();
                    shipped = pin is null ? null : Context.Manifest.ReadShippedAnalyzerRules(pin);
                    shippedRead = true;
                }

                if (shipped is not null && !shipped.Contains(span))
                {
                    add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names '{span}', but the pinned analyzer release '{pin}' does not ship it");
                }
            }
            else if (MaintainCodePattern().IsMatch(span))
            {
                if (!StableCodeSet.Contains(span))
                {
                    add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names '{span}', which is not a litespec-maintain diagnostic code");
                }
            }
            else if (FindingCodePattern().IsMatch(span) && !FindingCodes.IsKnown(span))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names '{span}', which is not a finding code");
            }
        }

        // The model check reports findings, so its Checked by line names the finding code it
        // reports. A line that names the command with no code states no check that a reader can read.
        if (command.Groups[1].Value.Contains("litespec model check", StringComparison.Ordinal)
            && !InlineCodes(evidence).Any(span => FindingCodes.IsKnown(span)))
        {
            add(relative, line, MaintainerDiagnostics.Codes.CheckedByFindingMissing, $"Verification for '{id}' names 'litespec model check' but no finding code");
        }
    }

    /// <summary>Checks the command name in one checkable verification line.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="line">The 1-based line of the verification line.</param>
    /// <param name="id">The provision identifier.</param>
    /// <param name="tokens">The command tokens.</param>
    private void CheckLiteSpecCommandName(string relative, int line, string id, string[] tokens)
    {
        var add = Context.Add;
        if (tokens.Length > 0 && tokens[0] == "litespec")
        {
            var subcommand = tokens.Length > 1 ? tokens[1] : "";
            if (!LiteSpecCommands.Contains(subcommand))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names 'litespec {subcommand}', which is not a litespec command");
            }

            return;
        }

        if (tokens.Length > 0 && tokens[0] == "litespec-maintain")
        {
            var subcommand = tokens.Length > 1 ? tokens[1] : "";
            if (!MaintainCommands.Contains(subcommand))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names 'litespec-maintain {subcommand}', which is not a litespec-maintain command");
            }

            return;
        }

        if (tokens.Length > 5 && tokens[0] == "dotnet" && tokens[1] == "run" && tokens[2] == "--project" && tokens[4] == "--")
        {
            if (tokens[3].EndsWith("LiteSpec.Cli", StringComparison.Ordinal) && !LiteSpecCommands.Contains(tokens[5]))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names 'litespec {tokens[5]}', which is not a litespec command");
            }

            if (tokens[3].EndsWith("LiteSpec.Maintain", StringComparison.Ordinal) && !MaintainCommands.Contains(tokens[5]))
            {
                add(relative, line, MaintainerDiagnostics.Codes.CheckedByUnknown, $"Verification for '{id}' names 'litespec-maintain {tokens[5]}', which is not a litespec-maintain command");
            }
        }
    }

    /// <summary>Gets the pattern that matches normative modal words.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\b(?:MUST NOT|SHOULD NOT|MUST|SHOULD|MAY)\b")]
    private static partial Regex ModalsPattern();

    /// <summary>Gets the pattern that matches other normative words.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\b(?:REQUIRED|FORBIDDEN|SHALL)\b")]
    private static partial Regex OtherNormativePattern();

    /// <summary>Gets the pattern that matches a deviation mention.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\b(?:exceptions?|deviations?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DeviationMentionPattern();

    /// <summary>Gets the pattern that matches generic evidence wording.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\b(?:verify compliance|check compliance|inspect evidence|verify evidence)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GenericEvidencePattern();

    /// <summary>Gets the pattern that cleans a claim to comparable words.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex ClaimCleanPattern();

    /// <summary>Gets the pattern that removes the leading modal from a claim.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^.*?\b(must not|must|should not|should|may)\b\s*")]
    private static partial Regex ClaimModalPattern();

    /// <summary>Gets the pattern that matches a SHOULD modal.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\bSHOULD(?: NOT)?\b")]
    private static partial Regex ShouldPattern();

    /// <summary>Gets the pattern that matches a MUST modal.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\bMUST(?: NOT)?\b")]
    private static partial Regex MustPattern();

    /// <summary>Gets the pattern that captures the command in verification evidence.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex EvidenceCommandPattern();

    /// <summary>Gets the pattern that splits command tokens.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex TokenSplitPattern();

    /// <summary>Gets the pattern that cleans a command token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^['""]|['"",;]+$")]
    private static partial Regex TokenCleanPattern();

    /// <summary>Gets the pattern that matches a path-shaped token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+$")]
    private static partial Regex PathShapedPattern();

    /// <summary>Gets the pattern that matches a provision tag.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\[litespec/[^\]\s]+\]")]
    private static partial Regex ProvisionTagPattern();

    /// <summary>Gets the pattern that matches an analyzer rule code.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^LS[0-9]+$")]
    private static partial Regex AnalyzerCodePattern();

    /// <summary>Gets the pattern that matches a maintainer diagnostic code.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[A-Z][A-Z0-9_]*_[A-Z0-9_]+$")]
    private static partial Regex MaintainCodePattern();

    /// <summary>Gets the pattern that matches a consumer finding code.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*-(?:missing|unknown|undeclared|unrecorded|unimplemented|unreached|drift|invalid|duplicate|clash|expired|reopened)$")]
    internal static partial Regex FindingCodePattern();
}
