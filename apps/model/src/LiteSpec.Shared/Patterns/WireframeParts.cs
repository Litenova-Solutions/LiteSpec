namespace LiteSpec.Patterns;

/// <summary>The closed set of wireframe parts and what each one accepts.</summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public static class WireframeParts
{
    /// <summary>What a part accepts: its modifiers, quoted strings, number and children.</summary>
    /// <param name="Modifiers">The bare words it accepts.</param>
    /// <param name="MinStrings">The fewest quoted strings.</param>
    /// <param name="MaxStrings">The most quoted strings.</param>
    /// <param name="Number">"count" or "ratio" when the part requires one, otherwise null.</param>
    /// <param name="Children">"none", "any", "one" or a comma-separated list of part names.</param>
    public sealed record Spec(string[] Modifiers, int MinStrings, int MaxStrings, string? Number, string Children);

    /// <summary>The tones a status, message or toast carries.</summary>
    public static readonly string[] Tones = ["neutral", "positive", "caution", "critical", "info"];

    /// <summary>The kinds a field carries.</summary>
    public static readonly string[] FieldKinds = ["text", "number", "date", "select", "textarea", "checkbox", "radio", "toggle", "search", "file"];

    /// <summary>The parts that overlay the page.</summary>
    public static readonly string[] Overlays = ["dialog", "drawer", "popover", "toast"];

    /// <summary>Every part by name, with what it accepts.</summary>
    public static readonly Dictionary<string, Spec> All = new(StringComparer.Ordinal)
    {
        ["page"] = new([], 0, 0, null, "any"),
        ["header"] = new([], 0, 0, null, "any"),
        ["footer"] = new(["sticky"], 0, 0, null, "any"),
        ["region"] = new([], 0, 1, null, "any"),
        ["aside"] = new(["sticky"], 0, 1, null, "any"),
        ["card"] = new([], 0, 1, null, "any"),
        ["row"] = new([], 0, 0, null, "any"),
        ["split"] = new([], 0, 0, "ratio", "any"),
        ["tabs"] = new([], 1, 12, null, "any"),
        ["nav"] = new(["side"], 0, 0, null, "link"),
        ["breadcrumb"] = new([], 1, 8, null, "none"),
        ["toolbar"] = new(["sticky"], 0, 0, null, "action,field,menu,text,status"),
        ["heading"] = new(["small"], 1, 1, null, "none"),
        ["text"] = new(["muted"], 1, 1, null, "none"),
        ["fact"] = new([], 2, 2, null, "none"),
        ["figure"] = new([], 1, 3, null, "none"),
        ["status"] = new(Tones, 1, 1, null, "none"),
        ["message"] = new(Tones, 1, 1, null, "none"),
        ["avatar"] = new([], 1, 1, null, "none"),
        ["image"] = new([], 1, 1, null, "none"),
        ["icon"] = new([], 1, 1, null, "none"),
        ["link"] = new(["current"], 1, 1, null, "none"),
        ["divider"] = new([], 0, 0, null, "none"),
        ["action"] = new(["primary", "secondary", "destructive", "quiet", "disabled"], 1, 1, null, "none"),
        ["menu"] = new([], 1, 1, null, "action"),
        ["field"] = new(FieldKinds, 1, 1, null, "none"),
        ["list"] = new([], 0, 0, "count", "one"),
        ["table"] = new([], 1, 12, "count", "item"),
        ["grid"] = new([], 0, 0, "count", "one"),
        ["timeline"] = new([], 0, 0, "count", "one"),
        ["item"] = new([], 0, 0, null, "any"),
        ["dialog"] = new([], 0, 1, null, "any"),
        ["drawer"] = new(["right", "left", "bottom"], 0, 1, null, "any"),
        ["popover"] = new([], 0, 1, null, "any"),
        ["toast"] = new(Tones, 1, 1, null, "action"),
    };

    /// <summary>The parts whose single tone or kind modifier is exclusive.</summary>
    /// <param name="part">The part name.</param>
    /// <returns>True when the part takes one exclusive modifier.</returns>
    public static bool IsExclusiveModifierPart(string part) => part is "status" or "message" or "toast" or "field" or "drawer";
}
