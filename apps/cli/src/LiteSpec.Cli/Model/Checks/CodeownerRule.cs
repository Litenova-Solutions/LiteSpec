using System.Text.RegularExpressions;

namespace LiteSpec.Model.Checks;

/// <summary>One CODEOWNERS line: the paths it owns and the owners it names.</summary>
internal sealed record CodeownerRule(Regex Matcher, IReadOnlyList<string> Owners);
