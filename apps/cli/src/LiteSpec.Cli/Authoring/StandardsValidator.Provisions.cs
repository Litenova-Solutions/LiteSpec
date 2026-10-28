using System.Text.RegularExpressions;

namespace LiteSpec.Authoring;

/// <summary>
///     The provision contract: labels, assertions, and inline verification.
/// </summary>
static partial class StandardsValidator
{
    static readonly HashSet<string> LiteSpecCommands = new(
        ["validate", "provisions", "consumer", "ui", "model", "book", "patterns", "init", "new", "rename"],
        StringComparer.Ordinal);

    // A claim is the comparable core of a heading, a Statement, a Default, or a
    // summary bullet: lowercase words with punctuation, code marks, and any leading
    // actor and normative modal removed.
    static string Claim(string text)
    {
        var normalized = ClaimCleanPattern().Replace(text.ToLowerInvariant().Replace("`", "", StringComparison.Ordinal), " ").Trim();
        return ClaimModalPattern().Replace(normalized, "").Trim();
    }

    static void CheckProvisionBodies(string relative, List<Provision> provisions, Action<string, int, string, string> add)
    {
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
                // The ID line and the verification line belong to the provision
                // frame, not to its labeled fields; the identity and verification
                // checks own them.
                if (IdLinePattern().IsMatch(item.Line) || VerificationLinePattern().IsMatch(item.Line))
                {
                    continue;
                }

                var label = ProvisionLabelPattern().Match(item.Line);
                if (label.Success)
                {
                    var name = label.Groups[1].Value;
                    labels.Add((name, item.Number));
                    activeLabel = name;
                    if (!allowedLabels.Contains(name))
                    {
                        add(relative, item.Number, "STANDARD_LABEL_INVALID", $"label '{name}' is invalid in {provision.Section}");
                    }

                    if (name is "Rationale" or "Example")
                    {
                        if (HasNormativeVocabulary(item.Line))
                        {
                            add(relative, item.Number, "STANDARD_INFORMATIVE_NORMATIVE", $"informative content in '{provision.Id}' contains normative vocabulary");
                        }
                    }
                    else if (name == "Deviation" && provision.Section == "Standards")
                    {
                        // The modal lives in the Statement; the Deviation states the
                        // bounded reason that permits a departure, in plain words.
                        if (HasNormativeVocabulary(item.Line))
                        {
                            add(relative, item.Number, "STANDARD_DEVIATION_NORMATIVE", $"Deviation for '{provision.Id}' contains normative vocabulary");
                        }
                    }

                    continue;
                }

                if (item.Fenced)
                {
                    if (activeLabel != "Example" && !fenceReported)
                    {
                        add(relative, item.Number, "STANDARD_EXAMPLE_LABEL", $"fenced example in '{provision.Id}' must follow an Example label");
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
                    add(relative, item.Number, "STANDARD_UNLABELED_CONTENT", $"content in '{provision.Id}' must follow a Rationale or Example label");
                }

                if (activeLabel is "Rationale" or "Example")
                {
                    if (HasNormativeVocabulary(item.Line))
                    {
                        add(relative, item.Number, "STANDARD_INFORMATIVE_NORMATIVE", $"informative content in '{provision.Id}' contains normative vocabulary");
                    }
                }
            }

            foreach (var name in labels.Select(label => label.Name).Distinct().ToList())
            {
                var count = labels.Count(label => label.Name == name);
                if (count > 1)
                {
                    add(relative, labels.First(label => label.Name == name).Line, "STANDARD_LABEL_DUPLICATE", $"provision '{provision.Id}' has {count} {name} labels");
                }
            }

            var previousLabel = -1;
            foreach (var label in labels)
            {
                var position = Array.IndexOf(allowedLabels, label.Name);
                if (position >= 0 && position < previousLabel)
                {
                    add(relative, label.Line, "STANDARD_LABEL_ORDER", $"label '{label.Name}' is out of order in '{provision.Id}'");
                    break;
                }

                if (position >= 0)
                {
                    previousLabel = position;
                }
            }

            if (provision.Section == "Standards")
            {
                CheckStandardAssertion(relative, provision, nonblank, add);
            }
            else
            {
                CheckConventionAssertion(relative, provision, nonblank, add);
            }
        }
    }

    static bool HasNormativeVocabulary(string line)
    {
        var visible = Prose.VisibleText(line);
        return ModalsPattern().Matches(visible).Count > 0 || OtherNormativePattern().IsMatch(visible);
    }

    static void CheckStandardAssertion(string relative, Provision provision, List<LineItem> nonblank, Action<string, int, string, string> add)
    {
        var statements = nonblank.Where(item => item.Line.StartsWith("**Statement:**", StringComparison.Ordinal)).ToList();
        if (statements.Count == 0)
        {
            add(relative, provision.Line, "STANDARD_MISSING_REQUIREMENT", $"Standard '{provision.Id}' has no Statement");
            return;
        }

        if (statements.Count > 1)
        {
            add(relative, provision.Line, "STANDARD_REQUIREMENT_COUNT", $"Standard '{provision.Id}' has {statements.Count} Statements");
        }

        var assertion = statements[0];
        var statement = assertion.Line["**Statement:**".Length..].Trim();
        var modalCount = ModalsPattern().Matches(Prose.VisibleText(statement)).Count;
        if (modalCount != 1)
        {
            add(relative, assertion.Number, "STANDARD_MODAL_COUNT", $"Statement for '{provision.Id}' has {modalCount} normative modals");
        }

        if (OtherNormativePattern().IsMatch(Prose.VisibleText(statement)))
        {
            add(relative, assertion.Number, "STANDARD_MODAL_VOCABULARY", $"Statement for '{provision.Id}' uses an unsupported normative term");
        }

        if (Prose.Sentences(statement).Count != 1)
        {
            add(relative, assertion.Number, "STANDARD_SENTENCE_COUNT", $"Statement for '{provision.Id}' must contain one sentence");
        }

        var should = ShouldPattern().IsMatch(Prose.VisibleText(statement));
        var deviations = nonblank.Where(item => item.Line.StartsWith("**Deviation:**", StringComparison.Ordinal)).ToList();
        if (should && deviations.Count == 0)
        {
            add(relative, provision.Line, "STANDARD_MISSING_DEVIATION", $"recommendation '{provision.Id}' has no Deviation statement");
        }

        if (!should && deviations.Count > 0)
        {
            add(relative, deviations[0].Number, "STANDARD_UNEXPECTED_DEVIATION", $"non-recommendation '{provision.Id}' cannot contain a Deviation statement");
        }

        if (deviations.Count > 0 && Prose.Sentences(deviations[0].Line["**Deviation:**".Length..].Trim()).Count != 1)
        {
            add(relative, deviations[0].Number, "STANDARD_DEVIATION_SENTENCE", $"Deviation for '{provision.Id}' must contain one sentence");
        }
    }

    static void CheckConventionAssertion(string relative, Provision provision, List<LineItem> nonblank, Action<string, int, string, string> add)
    {
        var defaults = nonblank.Where(item => item.Line.StartsWith("**Default:**", StringComparison.Ordinal)).ToList();
        var replacements = nonblank.Where(item => item.Line.StartsWith("**Replacement:**", StringComparison.Ordinal)).ToList();
        if (defaults.Count == 0)
        {
            add(relative, provision.Line, "CONVENTION_MISSING_DEFAULT", $"convention '{provision.Id}' has no Default statement");
        }

        if (replacements.Count == 0)
        {
            add(relative, provision.Line, "CONVENTION_MISSING_REPLACEMENT", $"convention '{provision.Id}' has no Replacement statement");
        }

        if (defaults.Count > 0 && Prose.Sentences(defaults[0].Line["**Default:**".Length..].Trim()).Count != 1)
        {
            add(relative, defaults[0].Number, "CONVENTION_DEFAULT_SENTENCE", $"Default for '{provision.Id}' must contain one sentence");
        }

        if (replacements.Count > 0 && Prose.Sentences(replacements[0].Line["**Replacement:**".Length..].Trim()).Count != 1)
        {
            add(relative, replacements[0].Number, "CONVENTION_REPLACEMENT_SENTENCE", $"Replacement for '{provision.Id}' must contain one sentence");
        }

        // A Replacement names the local convention that takes the default's
        // place. A departure instrument belongs to a Standard, never here.
        if (replacements.Count > 0 && DeviationMentionPattern().IsMatch(Prose.VisibleText(replacements[0].Line["**Replacement:**".Length..])))
        {
            add(relative, replacements[0].Number, "CONVENTION_REPLACEMENT_DEVIATION", $"Replacement for '{provision.Id}' names an exception or deviation; name the local convention instead");
        }
    }

    static void CheckProvisionClaims(string relative, List<Provision> provisions, Action<string, int, string, string> add)
    {
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
                add(relative, provision.Line, "PROVISION_RESTATES_HEADING", $"{label} for '{provision.Id}' only restates its heading");
            }
        }
    }

    static void CheckNormativeLocation(string relative, List<LineItem> clean, List<Provision> provisions, Action<string, int, string, string> add)
    {
        var provisionLines = new HashSet<int>(provisions.SelectMany(provision => new[] { provision.Line }.Concat(provision.Body.Select(item => item.Number))));
        foreach (var item in clean)
        {
            if (provisionLines.Contains(item.Number))
            {
                continue;
            }

            var text = Prose.VisibleText(item.Line);
            if (ModalsPattern().Matches(text).Count > 0 || OtherNormativePattern().IsMatch(text))
            {
                add(relative, item.Number, "PROSE_NORMATIVE_LOCATION", "normative vocabulary is outside a Statement, Default, or Replacement");
            }
        }
    }

    static void CheckProvisionIdentity(string relative, Provision provision, List<LineItem> fields, Action<string, int, string, string> add)
    {
        // The ID line is the first line after the heading. It states the derived
        // ID, so a reader copies the identifier without composing it.
        var ids = fields.Where(item => IdLinePattern().IsMatch(item.Line)).ToList();
        if (ids.Count == 0)
        {
            add(relative, provision.Line, "ID_LINE_MISSING", $"provision '{provision.Id}' has no ID line; the first line after the heading states its ID");
            return;
        }

        var first = fields.First(item => item.Line.Trim().Length > 0);
        if (!IdLinePattern().IsMatch(first.Line))
        {
            add(relative, ids[0].Number, "ID_LINE_MISSING", $"ID line for '{provision.Id}' must be the first line after the heading");
        }

        var stated = IdLinePattern().Match(ids[0].Line).Groups[1].Value;
        if (stated != provision.Id)
        {
            add(relative, ids[0].Number, "ID_LINE_MISMATCH", $"ID line states '{stated}' but the heading derives '{provision.Id}'");
        }

        if (ids.Count > 1)
        {
            add(relative, ids[1].Number, "ID_LINE_MISMATCH", $"provision '{provision.Id}' has {ids.Count} ID lines");
        }
    }

    static void CheckInlineVerification(string relative, List<LineItem> clean, List<Provision> provisions, string root, Action<string, int, string, string> add)
    {
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
        foreach (var item in clean)
        {
            if (VerifyHeaderPattern().IsMatch(item.Line) || VerificationRowPattern.IsMatch(item.Line))
            {
                add(relative, item.Number, "VERIFY_HEADER", "standards pages carry verification beside each provision; remove the Verification table");
            }
        }

        // Evidence shared word for word across provisions is one check written by
        // hand N times. The owning tool generates such lines instead.
        var seenEvidence = new HashSet<string>(StringComparer.Ordinal);
        foreach (var provision in provisions)
        {
            // A pattern's fields end at its first option; everything below that
            // is drawn options, not the pattern's own fields.
            var fields = provision.Section == "Patterns"
                ? provision.Body.TakeWhile(item => !item.Line.StartsWith("#### ", StringComparison.Ordinal)).ToList()
                : provision.Body;

            CheckProvisionIdentity(relative, provision, fields, add);

            var lines = fields
                .Select((item, index) => (Item: item, Index: index))
                .Where(pair => VerificationLinePattern().IsMatch(pair.Item.Line))
                .ToList();
            if (lines.Count == 0)
            {
                add(relative, provision.Line, "VERIFY_MISSING_ID", $"provision '{provision.Id}' has no verification line");
                continue;
            }

            if (lines.Count > 1)
            {
                add(relative, lines[1].Item.Number, "VERIFY_DUPLICATE_ID", $"provision '{provision.Id}' has {lines.Count} verification lines");
            }

            var (found, position) = lines[0];
            var after = fields.Skip(position + 1).ToList();
            if (after.Any(item => ProvisionLabelPattern().IsMatch(item.Line)
                || VerificationLinePattern().IsMatch(item.Line)
                || item.Fenced
                || item.Line.Trim().Length > 0))
            {
                add(relative, found.Number, "VERIFY_ORDER", $"verification line for '{provision.Id}' must be last among its fields");
            }

            var match = VerificationLinePattern().Match(found.Line);
            var mark = match.Groups[1].Value == "Checked by" ? "checkable" : "guidance";
            var evidence = match.Groups[2].Value.Trim();
            if (HasNormativeVocabulary(found.Line))
            {
                add(relative, found.Number, "STANDARD_INFORMATIVE_NORMATIVE", $"informative content in '{provision.Id}' contains normative vocabulary");
            }

            if (GenericEvidencePattern().IsMatch(Prose.VisibleText(evidence)))
            {
                add(relative, found.Number, "VERIFY_GENERIC_EVIDENCE", $"Verification for '{provision.Id}' uses generic evidence");
            }

            // Evidence that echoes its own provision heading names no artifact. It
            // tells a reviewer to confirm that a rule says what it says.
            var ownerClaim = Claim(provision.Title);
            // A tool's finding tag such as `[litespec/page#slug]` names the
            // provision on purpose, so it does not count as a restatement.
            var withoutTags = ProvisionTagPattern().Replace(evidence, " ");
            if (ownerClaim.Length > 0 && Claim(withoutTags).Contains(ownerClaim, StringComparison.Ordinal))
            {
                add(relative, found.Number, "VERIFY_TEMPLATED_EVIDENCE", $"Verification for '{provision.Id}' repeats its provision heading instead of naming evidence");
            }

            var normalized = evidence.Trim();
            if (normalized.Length > 0 && !seenEvidence.Add(normalized))
            {
                add(relative, found.Number, "VERIFY_DUPLICATE_EVIDENCE", $"Verification for '{provision.Id}' repeats evidence used by another provision on this page");
            }

            if (mark == "checkable")
            {
                CheckVerificationCommand(relative, found.Number, provision.Id, evidence, root, add);
            }
        }
    }

    // A checkable row's Evidence starts with the command that checks it, in
    // backticks. A shipped tool is cited in its consumer-side form, so a leading
    // `litespec/` resolves against the repository root with the prefix removed. A
    // path outside the shipped trees is a consumer command the page itself defines,
    // such as the consumer's route suite, which exists in consumers rather than
    // here, so only a shipped tool path that does not exist fails.
    static void CheckVerificationCommand(string relative, int line, string id, string evidence, string root, Action<string, int, string, string> add)
    {
        var command = EvidenceCommandPattern().Match(evidence);
        if (!command.Success)
        {
            add(relative, line, "VERIFY_COMMAND", $"Verification for '{id}' is checkable but names no command");
            return;
        }

        var tokens = TokenSplitPattern().Split(command.Groups[1].Value);
        CheckLiteSpecCommandName(relative, line, id, tokens, add);

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
                add(relative, line, "VERIFY_COMMAND", $"Verification for '{id}' names '{candidate}', which does not exist in the repository");
                return;
            }
        }
    }

    static void CheckLiteSpecCommandName(string relative, int line, string id, string[] tokens, Action<string, int, string, string> add)
    {
        if (tokens.Length > 0 && tokens[0] == "litespec")
        {
            var subcommand = tokens.Length > 1 ? tokens[1] : "";
            if (!LiteSpecCommands.Contains(subcommand))
            {
                add(relative, line, "VERIFY_COMMAND", $"Verification for '{id}' names 'litespec {subcommand}', which is not a litespec command");
            }

            return;
        }

        if (tokens.Length > 5 && tokens[0] == "dotnet" && tokens[1] == "run" && tokens[2] == "--project" && tokens[4] == "--"
            && tokens[3].EndsWith("LiteSpec.Cli", StringComparison.Ordinal) && !LiteSpecCommands.Contains(tokens[5]))
        {
            add(relative, line, "VERIFY_COMMAND", $"Verification for '{id}' names 'litespec {tokens[5]}', which is not a litespec command");
        }
    }

    [GeneratedRegex(@"\b(?:MUST NOT|SHOULD NOT|MUST|SHOULD|MAY)\b")]
    private static partial Regex ModalsPattern();

    [GeneratedRegex(@"\b(?:REQUIRED|FORBIDDEN|SHALL)\b")]
    private static partial Regex OtherNormativePattern();

    [GeneratedRegex(@"^\*\*(Statement|Deviation|Rationale|Example|Default|Replacement):\*\*")]
    private static partial Regex ProvisionLabelPattern();

    [GeneratedRegex(@"^\*\*ID:\*\*\s*`([^`]+)`\s*$")]
    private static partial Regex IdLinePattern();

    [GeneratedRegex(@"^\*\*(Checked by|Reviewed for):\*\*\s*(.*)$")]
    private static partial Regex VerificationLinePattern();

    [GeneratedRegex(@"\b(?:exceptions?|deviations?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DeviationMentionPattern();

    [GeneratedRegex(@"\b(?:verify compliance|check compliance|inspect evidence|verify evidence)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GenericEvidencePattern();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex ClaimCleanPattern();

    [GeneratedRegex(@"^.*?\b(must not|must|should not|should|may)\b\s*")]
    private static partial Regex ClaimModalPattern();

    [GeneratedRegex(@"\bSHOULD(?: NOT)?\b")]
    private static partial Regex ShouldPattern();

    [GeneratedRegex(@"^\|\s*ID\s*\|\s*Mark\s*\|\s*Evidence\s*\|\s*$")]
    private static partial Regex VerifyHeaderPattern();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex EvidenceCommandPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex TokenSplitPattern();

    [GeneratedRegex(@"^['""]|['"",;]+$")]
    private static partial Regex TokenCleanPattern();

    [GeneratedRegex(@"^(?:[A-Za-z0-9_.-]+/)+[A-Za-z0-9_.-]+$")]
    private static partial Regex PathShapedPattern();

    [GeneratedRegex(@"\[litespec/[^\]\s]+\]")]
    private static partial Regex ProvisionTagPattern();
}
