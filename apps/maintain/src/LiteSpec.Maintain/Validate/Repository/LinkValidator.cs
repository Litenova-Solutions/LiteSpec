using System.Text.RegularExpressions;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Validate.Repository;

/// <summary>The link check: every relative link in a page resolves to a file, and every record link names a record that exists.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class LinkValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LinkValidator"/> class.
    /// </summary>
    /// <param name="context">The shared validation context.</param>
    public LinkValidator(ValidationContext context)
    {
        Context = context;
    }

    /// <summary>Gets the shared validation context.</summary>
    private ValidationContext Context { get; }

    // A record citation as a link target: a record type, a slash, and the record
    // name. The book resolves it to the record file.
    /// <summary>The pattern of a record identifier link target.</summary>
    internal static readonly Regex RecordLinkTarget = new(
        $"^(?:{string.Join("|", RecordTypes.All.OrderBy(kind => kind, StringComparer.Ordinal).Select(Regex.Escape))})/[A-Za-z0-9][A-Za-z0-9._-]*$",
        RegexOptions.Compiled);

    /// <summary>Checks that every link and anchor in the given files resolves.</summary>
    /// <param name="files">The files to check.</param>
    internal void CheckLinks(List<string> files)
    {
        var root = Context.Root;
        var add = Context.Add;
        var known = RecordsValidator.OwnRecordIds(root);
        known.UnionWith(RecordsValidator.ExampleRecordIds(root));
        var readers = new Dictionary<string, PageReader>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            readers[file] = PageReader.Parse(FileText.ReadText(file));
        }

        foreach (var file in files)
        {
            var reader = readers[file];
            var relative = PathText.Relative(root, file);
            foreach (var link in reader.Links)
            {
                var href = link.Target.Trim();
                if (AbsoluteUriPattern().IsMatch(href))
                {
                    continue;
                }

                var parts = href.Split('#');
                var targetPart = parts[0];
                var anchor = parts.Length > 1 ? parts[1] : "";

                // A link whose target is a known record identifier cites that record;
                // the book resolves it, so the file check below does not apply. An
                // unknown identifier falls through and reports as a broken link.
                if (targetPart.Length > 0 && RecordLinkTarget.IsMatch(targetPart) && known.Contains(targetPart))
                {
                    continue;
                }

                if (!TryDecodeURIComponent(targetPart, out var decoded))
                {
                    add(relative, link.Line, MaintainerDiagnostics.Codes.LinkBroken, $"link target '{href}' holds a malformed percent escape");
                    continue;
                }

                var target = targetPart.Length > 0
                    ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded))
                    : file;
                if (targetPart.Length > 0 && !File.Exists(target) && !Directory.Exists(target))
                {
                    add(relative, link.Line, MaintainerDiagnostics.Codes.LinkBroken, $"link target '{href}' does not exist");
                    continue;
                }

                if (anchor.Length > 0 && (File.Exists(target) || Directory.Exists(target)) && target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    if (!readers.TryGetValue(target, out var targetReader))
                    {
                        targetReader = PageReader.Parse(FileText.ReadText(target));
                    }

                    if (!targetReader.Anchors.Contains(anchor))
                    {
                        add(relative, link.Line, MaintainerDiagnostics.Codes.AnchorBroken, $"anchor '#{anchor}' does not exist in '{PathText.Relative(root, target)}'");
                    }
                }
            }
        }
    }

    /// <summary>Tries to decode a percent-escaped link target.</summary>
    /// <param name="value">The escaped target.</param>
    /// <param name="decoded">The decoded target.</param>
    /// <returns>True when the target decodes.</returns>
    internal static bool TryDecodeURIComponent(string value, out string decoded)
    {
        try
        {
            decoded = DecodeURIComponent(value);
            return true;
        }
        catch (InvalidOperationException)
        {
            decoded = value;
            return false;
        }
    }

    /// <summary>Decodes a percent-escaped link target.</summary>
    /// <param name="value">The escaped target.</param>
    /// <returns>The decoded target.</returns>
    private static string DecodeURIComponent(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '%' && (index + 2 > value.Length - 1 || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2])))
            {
                throw new InvalidOperationException($"Malformed escape in link target '{value}'.");
            }
        }

        return Uri.UnescapeDataString(value);
    }

    /// <summary>Gets the pattern that matches an absolute link target.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^(?:https?:|mailto:)")] internal static partial Regex AbsoluteUriPattern();
}
