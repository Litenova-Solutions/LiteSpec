using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Maintain.Validate.Repository.ManifestValidator;

namespace LiteSpec.Maintain.Validate.Pages;

/// <summary>
///     The check-scope contract: a provision whose Checked by line cites finding
///     codes states the size and extension conditions those checks run under. A layer
///     is not checked: a check often enforces only the part of a wider provision
///     that one record layer holds.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed class CheckScopeValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CheckScopeValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public CheckScopeValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // A finding code runs under the conditions its table row carries: the size
    // it runs at and the extension that adds it. A provision that cites codes
    // without stating those conditions reads as if it binds every size and
    // selection. Only a Checked by line cites
    // codes; a Reviewed for line asks a reviewer a question instead.
    /// <summary>Checks that every provision citing finding codes states their check conditions.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="reader">The parsed page.</param>
    /// <param name="provisions">The provisions to check.</param>
    internal void CheckProvisionCheckScopes(string relative, PageReader reader, List<ProvisionSection> provisions)
    {
        foreach (var provision in provisions)
        {
            var verification = provision.Body
                .Where(item => PageReader.TryParseVerificationLine(item.Line, out var label, out _) && label == "Checked by")
                .ToList();
            if (verification.Count == 0)
            {
                continue;
            }

            if (!PageReader.TryParseVerificationLine(verification[0].Line, out _, out var evidence))
            {
                continue;
            }

            // An unknown span is the Checked by reference check's own defect.
            var codes = InlineCodes(evidence)
                .Where(FindingCodes.IsKnown)
                .Distinct(StringComparer.Ordinal)
                .Select(FindingCodes.Of)
                .ToList();
            if (codes.Count == 0)
            {
                continue;
            }

            CheckCodeSize(relative, provision, codes);
            CheckCodeExtension(relative, provision, codes);
        }
    }

    // A check that runs at large size only, or at large size or with one
    // extension, opens the assertive line with that condition, so a small
    // project reads the provision as not binding without it.
    /// <summary>Checks that a provision checked at large size opens its assertive line with the size condition.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provision">The provision to check.</param>
    /// <param name="codes">The finding codes the provision cites.</param>
    private void CheckCodeSize(string relative, ProvisionSection provision, List<FindingCode> codes)
    {
        if (codes.Any(code => code.Size != "large"))
        {
            return;
        }

        var extensions = codes.Select(code => code.OrExtension).Distinct().ToList();
        var shared = extensions.Count == 1 ? extensions[0] : null;
        var expected = shared is null ? "At large size" : $"At large size or with the {shared} extension";
        var assertion = AssertiveText(provision);
        if (assertion is not null && assertion.Replace("`", "", StringComparison.Ordinal).StartsWith(expected, StringComparison.Ordinal))
        {
            return;
        }

        var opening = shared is null ? "At large size" : $"At large size or with the `{shared}` extension";
        var condition = shared is null ? "at large size only" : $"at large size or with the `{shared}` extension";
        var label = provision.Section == "Standards" ? "Statement" : "Default";
        Context.Add(relative, provision.Line, MaintainerDiagnostics.Codes.ProvisionCheckScope, $"provision '{provision.Id}' is checked {condition} but its {label} opens without the condition; open it with '{opening}'");
    }

    // A check that an extension adds binds only the projects that select it,
    // so the provision sits on that extension's page or names the extension in
    // its assertive line.
    /// <summary>Checks that a provision checked only with an extension sits on its page or names it.</summary>
    /// <param name="relative">The repository-relative path.</param>
    /// <param name="provision">The provision to check.</param>
    /// <param name="codes">The finding codes the provision cites.</param>
    private void CheckCodeExtension(string relative, ProvisionSection provision, List<FindingCode> codes)
    {
        var extensions = codes.Select(code => code.Extension).Distinct().ToList();
        if (extensions.Count != 1 || extensions[0] is null)
        {
            return;
        }

        var extension = extensions[0];

        // An extension's path is its scope and the file name is the extension
        // id: docs/ext/<id>.md, or <stack folder>/ext/<id>.md.
        var segments = relative.Split('/');
        if (segments.Length >= 2 && segments[^2] == "ext" && segments[^1] == $"{extension}.md")
        {
            return;
        }

        var assertion = AssertiveText(provision);
        if (assertion is not null && InlineCodes(assertion).Contains(extension, StringComparer.Ordinal))
        {
            return;
        }

        var label = provision.Section == "Standards" ? "Statement" : "Default";
        Context.Add(relative, provision.Line, MaintainerDiagnostics.Codes.ProvisionCheckScope, $"provision '{provision.Id}' is checked only with the `{extension}` extension but sits on '{relative}' and states no extension; move it to the extension page or name `{extension}` in its {label}");
    }

    /// <summary>Reads the assertive line of one provision: its Statement, or its Default.</summary>
    /// <param name="provision">The provision to read.</param>
    /// <returns>The assertive text after its label, or null when the provision states none.</returns>
    private static string? AssertiveText(ProvisionSection provision)
    {
        var prefix = provision.Section == "Standards" ? "**Statement:**" : "**Default:**";
        var assertion = provision.Body.FirstOrDefault(item => item.Line.StartsWith(prefix, StringComparison.Ordinal));
        return assertion?.Line[prefix.Length..].Trim();
    }
}
