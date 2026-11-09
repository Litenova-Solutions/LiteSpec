using System.Buffers.Binary;
using System.Numerics;

namespace LiteSpec;

/// <summary>
///     SHA-256 as FIPS 180-4 defines it, computed in managed code. The CLI hashes content to
///     fingerprint records, test runs and UI sources. .NET computes SHA-256 through OpenSSL on
///     Linux, so a native executable that called it would start only where .NET can load an
///     OpenSSL release: neither a bare container image nor a distribution that ships OpenSSL 4
///     alone, such as Fedora 45, has one. Computing the hash here leaves the executable needing
///     only the C library. Tests check it against the FIPS 180-4 vectors and the platform hash.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
/// <follows>litespec/frontend/ui#pin-the-baseline-in-the-shared-package</follows>
sealed class Sha256
{
    /// <summary>The length of a hash in bytes.</summary>
    public const int HashLength = 32;

    /// <summary>The round constants: the first 32 bits of the fractional parts of the cube roots of the first 64 primes.</summary>
    static readonly uint[] RoundConstants =
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
    ];

    /// <summary>The initial hash value: the first 32 bits of the fractional parts of the square roots of the first eight primes.</summary>
    static readonly uint[] InitialState =
    [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
    ];

    /// <summary>The running hash value.</summary>
    readonly uint[] state = new uint[8];

    /// <summary>The message schedule of the block being compressed.</summary>
    readonly uint[] schedule = new uint[64];

    /// <summary>The bytes of the block not yet compressed.</summary>
    readonly byte[] block = new byte[64];

    /// <summary>How many bytes of <see cref="block"/> hold data.</summary>
    int filled;

    /// <summary>How many bytes the message holds so far.</summary>
    ulong length;

    /// <summary>Initializes a new instance of the <see cref="Sha256"/> class, ready for the first bytes of a message.</summary>
    public Sha256() => Reset();

    /// <summary>The SHA-256 hash of one message.</summary>
    /// <param name="data">The message bytes.</param>
    /// <returns>The 32-byte hash.</returns>
    public static byte[] Hash(ReadOnlySpan<byte> data)
    {
        var hasher = new Sha256();
        hasher.Append(data);
        return hasher.Finish();
    }

    /// <summary>Adds the next bytes of the message.</summary>
    /// <param name="data">The bytes to add.</param>
    public void Append(ReadOnlySpan<byte> data)
    {
        length += (ulong)data.Length;

        if (filled > 0)
        {
            var take = Math.Min(64 - filled, data.Length);
            data[..take].CopyTo(block.AsSpan(filled));
            filled += take;
            data = data[take..];

            if (filled < 64)
            {
                return;
            }

            Compress(block);
            filled = 0;
        }

        while (data.Length >= 64)
        {
            Compress(data[..64]);
            data = data[64..];
        }

        data.CopyTo(block);
        filled = data.Length;
    }

    /// <summary>Pads the message, returns its hash, and starts a new message.</summary>
    /// <returns>The 32-byte hash.</returns>
    public byte[] Finish()
    {
        var bits = length * 8;

        // The padding is one 1 bit, zeros up to 56 bytes into the last block, then the message length in bits.
        block[filled++] = 0x80;

        if (filled > 56)
        {
            block.AsSpan(filled).Clear();
            Compress(block);
            filled = 0;
        }

        block.AsSpan(filled, 56 - filled).Clear();
        BinaryPrimitives.WriteUInt64BigEndian(block.AsSpan(56), bits);
        Compress(block);

        var hash = new byte[HashLength];

        for (var index = 0; index < 8; index++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(hash.AsSpan(index * 4), state[index]);
        }

        Reset();
        return hash;
    }

    /// <summary>Starts a new message.</summary>
    void Reset()
    {
        InitialState.CopyTo(state, 0);
        filled = 0;
        length = 0;
    }

    /// <summary>Compresses one 64-byte block into the running hash value.</summary>
    /// <param name="chunk">The block to compress.</param>
    void Compress(ReadOnlySpan<byte> chunk)
    {
        for (var index = 0; index < 16; index++)
        {
            schedule[index] = BinaryPrimitives.ReadUInt32BigEndian(chunk[(index * 4)..]);
        }

        for (var index = 16; index < 64; index++)
        {
            var low = schedule[index - 15];
            var high = schedule[index - 2];
            var sigma0 = BitOperations.RotateRight(low, 7) ^ BitOperations.RotateRight(low, 18) ^ (low >> 3);
            var sigma1 = BitOperations.RotateRight(high, 17) ^ BitOperations.RotateRight(high, 19) ^ (high >> 10);
            schedule[index] = schedule[index - 16] + sigma0 + schedule[index - 7] + sigma1;
        }

        var a = state[0];
        var b = state[1];
        var c = state[2];
        var d = state[3];
        var e = state[4];
        var f = state[5];
        var g = state[6];
        var h = state[7];

        for (var index = 0; index < 64; index++)
        {
            var sum1 = BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25);
            var choice = (e & f) ^ (~e & g);
            var first = h + sum1 + choice + RoundConstants[index] + schedule[index];
            var sum0 = BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22);
            var majority = (a & b) ^ (a & c) ^ (b & c);
            var second = sum0 + majority;

            h = g;
            g = f;
            f = e;
            e = d + first;
            d = c;
            c = b;
            b = a;
            a = first + second;
        }

        state[0] += a;
        state[1] += b;
        state[2] += c;
        state[3] += d;
        state[4] += e;
        state[5] += f;
        state[6] += g;
        state[7] += h;
    }
}
