using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LiteSpec.Ui;

/// <summary>
///     The design-contract checks of the UI validator: the contract the shared
///     package publishes, against its schema and its required sections.
/// </summary>
/// <follows>litespec/frontend/ui#publish-a-design-contract</follows>
/// <follows>litespec/frontend/ui#declare-a-closed-floorplan-set</follows>
sealed class DesignContractChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>The sections a design contract carries.</summary>
    static readonly string[] DesignSections =
    [
        "Brand", "Tokens", "Vocabulary", "Patterns", "Do", "Do Not", "Motion", "Voice",
    ];

    /// <summary>An H2 heading of a Markdown page.</summary>
    static readonly Regex SectionHeading = new("^##\\s+(.+?)\\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    ///     Initializes a new instance of the <see cref="DesignContractChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public DesignContractChecks(UiSession session)
    {
        this.session = session;
    }

    /// <summary>The H2 names a Markdown page carries, in the order they appear.</summary>
    /// <param name="raw">The page text.</param>
    /// <returns>The section names.</returns>
    static List<string> SectionNames(string raw) =>
        SectionHeading.Matches(raw).Cast<Match>().Select(match => match.Groups[1].Value.Trim()).ToList();

    // The design contract travels with the shared package, so it is read once for the
    // workspace rather than once per application. A floorplan it names outside the
    // closed set is a design decision, not a route-level choice.
    // (litespec/frontend/ui#publish-a-design-contract,
    // litespec/frontend/ui#declare-a-closed-floorplan-set)
    /// <summary>
    ///     Validates the design contract the shared package publishes.
    /// </summary>
    /// <param name="package">The declared shared package.</param>
    /// <param name="platform">The package platform.</param>
    public void ValidateDesignContract(JsonNode? package, string platform)
    {
        _ = platform;
        var contract = JsonRead.Child(package, "designContract");
        if (contract.Node is not JsonValue contractValue || !contractValue.TryGetValue<string>(out var relative))
        {
            session.Errors.Add($"[litespec/frontend/ui#publish-a-design-contract] shared package '{JsonRead.Show(JsonRead.Child(package, "name"))}': no design contract at '{JsonRead.Show(contract)}'");
            return;
        }

        var file = session.FilePath(relative);
        var label = session.RelativeToRoot(file);
        if (!FileWalk.Exists(file))
        {
            session.Errors.Add($"[litespec/frontend/ui#publish-a-design-contract] shared package '{JsonRead.Show(JsonRead.Child(package, "name"))}': no design contract at '{relative}'");
            return;
        }

        var metadata = session.ParseMetadata(file);
        if (metadata is null)
        {
            session.Errors.Add($"[litespec/frontend/ui#publish-a-design-contract] {label}: design contract has no metadata block");
            return;
        }

        session.SchemaCheck("design-contract.schema.json", metadata, label, "litespec/frontend/ui#publish-a-design-contract");
        var name = JsonRead.Child(package, "name");
        if (JsonRead.Truthy(name) && !JsonRead.StrictEqual(JsonRead.Child(metadata, "package"), name))
        {
            session.Errors.Add($"[litespec/frontend/ui#publish-a-design-contract] {label}: package must be '{JsonRead.Show(name)}'");
        }

        var present = SectionNames(File.ReadAllText(file)).ToHashSet(StringComparer.Ordinal);
        foreach (var section in DesignSections)
        {
            if (!present.Contains(section))
            {
                session.Errors.Add($"[litespec/frontend/ui#publish-a-design-contract] {label}: missing required section '{section}'");
            }
        }
    }
}
