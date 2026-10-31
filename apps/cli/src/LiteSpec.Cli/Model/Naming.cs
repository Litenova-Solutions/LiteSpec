using System.Text;

namespace LiteSpec.Model;

/// <summary>
///     The casing conversions between code names and record identifiers.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class Naming
{
    /// <summary>
    ///     Turns a PascalCase name into the kebab-case form identifiers use.
    /// </summary>
    /// <param name="name">The PascalCase name to convert.</param>
    /// <returns>The kebab-case form, with a hyphen before each interior capital and all letters lowered.</returns>
    public static string Kebab(string name)
    {
        var built = new StringBuilder(name.Length + 8);

        for (var index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]))
            {
                built.Append('-');
            }

            built.Append(char.ToLowerInvariant(name[index]));
        }

        return built.ToString();
    }

    /// <summary>
    ///     The key two operation names match under: the PascalCase form of a
    ///     kebab-case, snake_case or camelCase name, so a generated-client
    ///     export and an endpoint's <c>WithName</c> meet on either spelling.
    /// </summary>
    /// <param name="name">The operation name in kebab-case, snake_case or camelCase spelling.</param>
    /// <returns>The PascalCase key the spellings meet on.</returns>
    public static string OperationKey(string name) => Pascal(name.Replace('_', '-'));

    /// <summary>
    ///     Turns a kebab-case name into the PascalCase form code uses.
    /// </summary>
    /// <param name="kebab">The kebab-case name to convert.</param>
    /// <returns>The PascalCase form, with each hyphen-separated part capitalized and the hyphens removed.</returns>
    public static string Pascal(string kebab)
    {
        var built = new StringBuilder(kebab.Length);

        foreach (var part in kebab.Split('-', StringSplitOptions.RemoveEmptyEntries))
        {
            built.Append(char.ToUpperInvariant(part[0]));
            built.Append(part[1..]);
        }

        return built.ToString();
    }
}
