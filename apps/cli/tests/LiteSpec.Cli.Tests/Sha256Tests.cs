using System.Text;

namespace LiteSpec.Cli.Tests;

/// <summary>
///     Proves the managed SHA-256 the CLI fingerprints content with: the FIPS 180-4 test vectors,
///     and the same hash as the platform implementation for every message length across several
///     blocks, appended whole or in pieces.
/// </summary>
public sealed class Sha256Tests
{
    static string Hex(byte[] hash) => Convert.ToHexStringLower(hash);

    /// <summary>The FIPS 180-4 short messages hash to their published values.</summary>
    /// <param name="message">The ASCII message.</param>
    /// <param name="expected">The published hash in lowercase hex.</param>
    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
    [InlineData(
        "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu",
        "cf5b16a778af8380036ce59e7b0492370b249b11e8f07a51afac45037afee9d1")]
    public void Hashes_The_Published_Vectors(string message, string expected) =>
        Assert.Equal(expected, Hex(Sha256.Hash(Encoding.ASCII.GetBytes(message))));

    /// <summary>One million repetitions of 'a' hash to the published value.</summary>
    [Fact]
    public void HashesAMillionLetters()
    {
        var hasher = new Sha256();
        var thousand = Encoding.ASCII.GetBytes(new string('a', 1000));

        for (var index = 0; index < 1000; index++)
        {
            hasher.Append(thousand);
        }

        Assert.Equal("cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0", Hex(hasher.Finish()));
    }

    /// <summary>Every message length across five blocks hashes as the platform implementation does, appended whole or in pieces.</summary>
    [Fact]
    public void MatchesThePlatformHashAtEveryLength()
    {
        var random = new Random(4_1_0);

        for (var size = 0; size <= 320; size++)
        {
            var message = new byte[size];
            random.NextBytes(message);
            var expected = Hex(System.Security.Cryptography.SHA256.HashData(message));

            Assert.Equal(expected, Hex(Sha256.Hash(message)));

            var hasher = new Sha256();
            var offset = 0;

            while (offset < size)
            {
                var piece = Math.Min(random.Next(1, 80), size - offset);
                hasher.Append(message.AsSpan(offset, piece));
                offset += piece;
            }

            Assert.Equal(expected, Hex(hasher.Finish()));
        }
    }

    /// <summary>
    ///     The CLI and its shared library name no System.Security.Cryptography type: on Linux that
    ///     namespace runs through OpenSSL, which the native executable must not need.
    /// </summary>
    [Fact]
    public void TheCLIUsesNoPlatformCryptography()
    {
        var sources = new[] { Path.Combine("apps", "cli", "src"), Path.Combine("apps", "model", "src", "LiteSpec.Shared") }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(Support.TestCli.Repository, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains("System.Security.Cryptography", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(Support.TestCli.Repository, file))
            .ToList();

        Assert.Empty(sources);
    }

    /// <summary>Finishing a message starts the next one, so one hasher serves message after message.</summary>
    [Fact]
    public void StartsANewMessageAfterEachHash()
    {
        var hasher = new Sha256();
        hasher.Append(Encoding.ASCII.GetBytes("abc"));
        hasher.Finish();
        hasher.Append(Encoding.ASCII.GetBytes("abc"));

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", Hex(hasher.Finish()));
    }
}
