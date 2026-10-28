namespace LiteSpec.Model;

/// <summary>The verdict of the mark-count rule for one element's marks.</summary>
public sealed class MarkCount
{
    /// <summary>Creates the verdict.</summary>
    /// <param name="violated">Whether the marks violate the kind's count rule.</param>
    /// <param name="duplicate">The twice-named record id when the violation is a duplicate, else null.</param>
    public MarkCount(bool violated, string? duplicate)
    {
        Violated = violated;
        Duplicate = duplicate;
    }

    /// <summary>Whether the marks violate the kind's count rule.</summary>
    public bool Violated { get; }

    /// <summary>The twice-named record id when the violation is a duplicate, else null.</summary>
    public string? Duplicate { get; }
}
