namespace LiteSpec.Standards;

/// <summary>The kind of one wireframe problem: the language itself, or one composition rule.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public enum WireframeProblemKind
{
    /// <summary>The block breaks the wireframe language: shape, names or name resolution.</summary>
    Language,

    /// <summary>A card holds a card or a region, or a region holds a card.</summary>
    Nesting,

    /// <summary>A field sits outside every region, card, dialog and drawer.</summary>
    Surface,

    /// <summary>The page stacks past the section limit with no tabs or navigation.</summary>
    Tabs,

    /// <summary>The form holds past the field limit in fewer than two regions.</summary>
    Group,
}
