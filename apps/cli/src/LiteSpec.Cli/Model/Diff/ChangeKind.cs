namespace LiteSpec.Model.Diff;

/// <summary>How one record changed between the base model and the head model.</summary>
enum ChangeKind
{
    /// <summary>The record was removed.</summary>
    Removed,

    /// <summary>The record was modified.</summary>
    Modified,

    /// <summary>The record was added.</summary>
    Added,
}
