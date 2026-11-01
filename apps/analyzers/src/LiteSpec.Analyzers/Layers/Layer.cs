namespace LiteSpec.Analyzers.Layers;

/// <summary>
///     The application project a compilation belongs to, as
///     litespec/backend/dotnet/architecture#use-five-application-projects names them.
/// </summary>
internal enum Layer
{
    /// <summary>Not one of the known projects, or opted out with <c>LiteSpecLayer=None</c>.</summary>
    None,
    /// <summary>The Domain application project, which holds the aggregates and domain services.</summary>
    Domain,
    /// <summary>The Application.Abstractions application project, which holds the messages, results and ports.</summary>
    ApplicationAbstractions,
    /// <summary>The Application application project, which holds the handlers and validators.</summary>
    Application,
    /// <summary>The Infrastructure application project, which holds the persistence and external adapters.</summary>
    Infrastructure,
    /// <summary>The WebApi application project, which holds the endpoints.</summary>
    WebApi,
    /// <summary>The Worker application project, which runs jobs and reactions in the background.</summary>
    Worker,
    /// <summary>A test project, named <c>*.Tests</c> or declared with <c>LiteSpecLayer=Test</c>.</summary>
    Test,
}
