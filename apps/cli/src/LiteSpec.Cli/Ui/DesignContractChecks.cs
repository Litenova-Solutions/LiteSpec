using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Ui;

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
        "Brand", "Tokens", "Patterns", "Do", "Do Not", "Motion", "Voice",
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
    /// <param name="projectFile">The consumer project file as a full path.</param>
    public void ValidateDesignContract(JsonNode? package, string platform, string projectFile)
    {
        _ = platform;
        var contract = JsonRead.Child(package, "designContract");
        if (contract.Node is not JsonValue contractValue || !contractValue.TryGetValue<string>(out var relative))
        {
            session.Add("ui-design-contract-invalid", $"[litespec/frontend/ui#publish-a-design-contract] shared package '{JsonRead.Show(JsonRead.Child(package, "name"))}': no design contract at '{JsonRead.Show(contract)}'", projectFile);
            return;
        }

        var file = session.FilePath(relative);
        var label = PathText.Relative(session.Root, file);
        if (!FileWalk.Exists(file))
        {
            session.Add("ui-design-contract-invalid", $"[litespec/frontend/ui#publish-a-design-contract] shared package '{JsonRead.Show(JsonRead.Child(package, "name"))}': no design contract at '{relative}'", file);
            return;
        }

        var metadata = session.ParseMetadata(file);
        if (metadata is null)
        {
            session.Add("ui-design-contract-invalid", "[litespec/frontend/ui#publish-a-design-contract] design contract has no metadata block", file);
            return;
        }

        session.SchemaCheck("design-contract.schema.yaml", metadata, label, "litespec/frontend/ui#publish-a-design-contract", "ui-design-contract-invalid");
        var name = JsonRead.Child(package, "name");
        if (JsonRead.Truthy(name) && !JsonRead.StrictEqual(JsonRead.Child(metadata, "package"), name))
        {
            session.Add("ui-design-contract-invalid", $"[litespec/frontend/ui#publish-a-design-contract] package must be '{JsonRead.Show(name)}'", file);
        }

        var present = SectionNames(File.ReadAllText(file)).ToHashSet(StringComparer.Ordinal);
        foreach (var section in DesignSections)
        {
            if (!present.Contains(section))
            {
                session.Add("ui-design-contract-invalid", $"[litespec/frontend/ui#publish-a-design-contract] missing required section '{section}'", file);
            }
        }
    }
}
