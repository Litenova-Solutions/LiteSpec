using System.Globalization;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Readers.Frontend;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>
///     Proves the frontend facts protocol version is one number in two languages. The TypeScript
///     reader states it in <c>protocol.ts</c>, and the CLI checks the reader's handshake against
///     its own copy, so a change made in one language and missed in the other fails here instead
///     of as a runtime mismatch.
/// </summary>
public sealed class ProtocolVersionTests
{
    /// <summary>The Protocol Version The Reader States Is The One The CLI Checks.</summary>
    /// <proves>litespec/core/release#qualify-the-reader-with-known-good-and-known-bad-fixtures</proves>
    [Fact]
    public void TheReaderStatesTheProtocolVersionTheCLIChecks()
    {
        var source = FileText.ReadText(Path.Combine(TestCli.Repository, "apps", "cli", "fe-reader", "src", "protocol.ts"));
        var match = Regex.Match(source, @"export const PROTOCOL_VERSION = (\d+);");

        Assert.True(match.Success, "apps/cli/fe-reader/src/protocol.ts declares no PROTOCOL_VERSION number.");
        Assert.Equal(
            NodeFrontendReader.Protocol,
            int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
    }
}
