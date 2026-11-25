using LiteSpec.Cli.Generate;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Generate;

/// <summary>
///     The golden-file helper every generator test reuses. Expected output files live under
///     <c>Generate/Goldens/</c>, one per renderer and stack, and compare byte for byte against
///     what the renderer writes for the small fixture's records. A golden holds
///     <c>__LITESPEC_VERSION__</c> where the header stamps the LiteSpec version, because the
///     version varies by build; the helper replaces it with the carried release before comparing.
///     A new renderer adds its goldens beside the existing ones and asserts them through
///     <see cref="AssertEqual" />. See the test project's README for the steps.
/// </summary>
static class GenerateGoldens
{
    /// <summary>The placeholder a golden file holds where the header stamps the LiteSpec version.</summary>
    public const string VersionPlaceholder = "__LITESPEC_VERSION__";

    /// <summary>Reads a golden file with the version placeholder replaced by the carried release.</summary>
    /// <param name="name">The golden file name under <c>Generate/Goldens/</c>.</param>
    /// <returns>The expected text with LF line endings.</returns>
    public static string Read(string name)
    {
        var path = Path.Combine(
            TestCli.Repository, "apps", "cli", "tests", "LiteSpec.Cli.Tests", "Generate", "Goldens", name);
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);

        return text.Replace(VersionPlaceholder, GenerateNaming.CurrentVersion(), StringComparison.Ordinal);
    }

    /// <summary>Asserts the rendered text matches the golden file byte for byte.</summary>
    /// <param name="name">The golden file name under <c>Generate/Goldens/</c>.</param>
    /// <param name="actual">The rendered text.</param>
    public static void AssertEqual(string name, string actual) =>
        Assert.Equal(Read(name), actual);
}
