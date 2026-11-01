namespace LiteSpec.Model.Diff;

/// <summary>Which records a diff shows: Intent, Behavior, or both.</summary>
enum DiffView
{
    /// <summary>Both Intent and Behavior records.</summary>
    Both,

    /// <summary>Intent records.</summary>
    Intent,

    /// <summary>Behavior records.</summary>
    Behavior,
}
