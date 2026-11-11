using LiteSpec.Model;

namespace LiteSpec.Cli.Ui;

/// <summary>The collected result of one <see cref="UiValidator.Validate"/> run.</summary>
/// <param name="ExitCode">What the UI pass returns: 0 pass, 1 problems, 2 unusable root.</param>
/// <param name="Problems">The findings the UI pass reported.</param>
/// <param name="Consumer">The consumer root the run checked.</param>
/// <param name="SharedPackage">The declared shared package, or null when none resolved.</param>
/// <param name="ControlledFrontends">How many controlled frontends were checked.</param>
/// <param name="SkippedFrontends">The declared frontends outside the controlled contract.</param>
/// <param name="HasPackage">Whether a shared UI package is declared.</param>
/// <param name="Frontends">Every declared frontend, for the skipped scope line.</param>
sealed record UiReport(
    int ExitCode,
    IReadOnlyList<Finding> Problems,
    string Consumer,
    UiSharedPackage? SharedPackage,
    int ControlledFrontends,
    IReadOnlyList<UiFrontend> SkippedFrontends,
    bool HasPackage,
    IReadOnlyList<UiFrontend> Frontends);
