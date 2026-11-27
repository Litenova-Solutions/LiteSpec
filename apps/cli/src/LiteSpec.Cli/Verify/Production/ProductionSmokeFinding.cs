using System.Text.Json.Nodes;

namespace LiteSpec.Cli.Verify.Production;

/// <summary>One production smoke finding: its code, its subject, and its message.</summary>
/// <param name="Code">The finding code: production-readiness-failed, path-failed, or stub-adapter-shown.</param>
/// <param name="Subject">The subject the finding reports on: the compose file, the migrate command, the health path, a smoke path, or a stub adapter.</param>
/// <param name="Message">The message a reader sees.</param>
sealed record ProductionSmokeFinding(string Code, string Subject, string Message)
{
    /// <summary>The printed line of the finding in the model check format.</summary>
    /// <param name="path">The project file path the line names, relative to the consumer root.</param>
    /// <returns>The line, such as .litespec/project.yaml:1 [S] [path-failed] followed by the message.</returns>
    public string Line(string path) => $"{path}:1 [S] [{Code}] {Message}";

    /// <summary>The finding as JSON for the verify report.</summary>
    /// <returns>The JSON object with the code, the subject, and the message.</returns>
    public JsonObject AsJson() => new()
    {
        ["code"] = Code,
        ["subject"] = Subject,
        ["message"] = Message,
    };
}
