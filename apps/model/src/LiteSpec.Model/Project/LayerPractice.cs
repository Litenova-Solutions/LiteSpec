namespace LiteSpec.Model;

/// <summary>The practice a project follows in a claimed Implementation or Proof layer.</summary>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
public enum LayerPractice
{
    /// <summary>LiteSpec's practice for the layer: its stack, workspace and testing pages apply.</summary>
    LiteSpec = 0,

    /// <summary>The project's own practice: only the provisions that tie the layer to the records apply.</summary>
    Own = 1,
}
