#pragma warning disable LS1002
using System.Diagnostics.CodeAnalysis;

namespace AnalyzerLoweringFixture;

/// <summary>A widget whose file lowers one rule and suppresses another.</summary>
internal sealed class Widget
{
    /// <summary>Describes the widget.</summary>
    /// <returns>The widget description.</returns>
    [SuppressMessage("LiteSpec", "LS2012", Justification = "The fixture states a suppression with no exception record.")]
    public string Describe() => "Widget";
}
