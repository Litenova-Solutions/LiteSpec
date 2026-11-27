using System.Text.Json;
using __NAMESPACE__.Domain.Values;

namespace __NAMESPACE__.Android.Data;

/// <summary>The typed result of loading the check-in list: the loaded check-ins, or the failure the list shows instead.</summary>
public abstract record CheckInListResult
{
    /// <summary>The list loaded.</summary>
    /// <param name="CheckIns">The check-ins the backend sample returned.</param>
    public sealed record Loaded(IReadOnlyList<CheckIn> CheckIns) : CheckInListResult;

    /// <summary>The list is unavailable: the backend sample could not be reached.</summary>
    public sealed record Unavailable : CheckInListResult;

    /// <summary>Prevents external inheritance: the result is either loaded or unavailable.</summary>
    private CheckInListResult()
    {
    }
}

/// <summary>The typed result of reading one check-in: the check-in, or the failure the detail shows instead.</summary>
public abstract record CheckInDetailResult
{
    /// <summary>The check-in was read.</summary>
    /// <param name="CheckIn">The check-in the backend sample returned.</param>
    public sealed record Loaded(CheckIn CheckIn) : CheckInDetailResult;

    /// <summary>The check-in is unavailable: the backend sample could not be reached, or it holds no such check-in.</summary>
    public sealed record Unavailable : CheckInDetailResult;

    /// <summary>Prevents external inheritance: the result is either loaded or unavailable.</summary>
    private CheckInDetailResult()
    {
    }
}

/// <summary>The generated-client-shaped reader the Health screens call over HTTP. It owns the base address and maps every failure to a typed result the screens map to their messages.</summary>
public sealed class CheckInApiClient
{
    /// <summary>The base address reaching the backend sample from the emulator: the host loopback.</summary>
    public const string EmulatorBaseAddress = "http://10.0.2.2:5005";

    /// <summary>The list path on the backend sample.</summary>
    private const string ListPath = "/api/health/check-ins";

    /// <summary>The detail path prefix on the backend sample, followed by the check-in identifier.</summary>
    private const string DetailPrefix = "/api/health/check-ins/";

    /// <summary>The HTTP client carrying the base address.</summary>
    private readonly HttpClient http;

    /// <summary>Initializes a new instance of the <see cref="CheckInApiClient"/> class.</summary>
    /// <param name="http">The HTTP client carrying the base address.</param>
    public CheckInApiClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        this.http = http;
    }

    /// <summary>Initializes a new instance of the <see cref="CheckInApiClient"/> class with its own client.</summary>
    /// <param name="baseAddress">The base address of the backend sample.</param>
    public CheckInApiClient(string baseAddress)
        : this(new HttpClient { BaseAddress = new Uri(baseAddress, UriKind.Absolute) })
    {
    }

    /// <summary>Lists the check-ins of the backend sample.</summary>
    /// <param name="cancellationToken">The token cancelling the read.</param>
    /// <returns>The loaded check-ins, or unavailable when the backend sample could not be reached.</returns>
    public async Task<CheckInListResult> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(ListPath, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new CheckInListResult.Unavailable();
            }

            using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new CheckInListResult.Unavailable();
            }

            var found = new List<CheckIn>();

            foreach (var row in document.RootElement.EnumerateArray())
            {
                if (ReadCheckIn(row) is { } checkIn)
                {
                    found.Add(checkIn);
                }
            }

            return new CheckInListResult.Loaded(found);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException or System.OperationCanceledException)
        {
            return new CheckInListResult.Unavailable();
        }
    }

    /// <summary>Reads one check-in of the backend sample.</summary>
    /// <param name="checkInId">The check-in identifier to read.</param>
    /// <param name="cancellationToken">The token cancelling the read.</param>
    /// <returns>The check-in, or unavailable when the backend sample could not be reached.</returns>
    public async Task<CheckInDetailResult> ReadAsync(string checkInId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(checkInId);

        try
        {
            using var response = await http.GetAsync(DetailPrefix + Uri.EscapeDataString(checkInId), cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new CheckInDetailResult.Unavailable();
            }

            using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);

            return ReadCheckIn(document.RootElement) is { } checkIn
                ? new CheckInDetailResult.Loaded(checkIn)
                : new CheckInDetailResult.Unavailable();
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or IOException or System.OperationCanceledException)
        {
            return new CheckInDetailResult.Unavailable();
        }
    }

    /// <summary>Reads one check-in row of the backend sample.</summary>
    /// <param name="row">The JSON row.</param>
    /// <returns>The check-in, or null when the row carries no identifier.</returns>
    private static CheckIn? ReadCheckIn(JsonElement row)
    {
        if (row.ValueKind != JsonValueKind.Object
            || !row.TryGetProperty("checkInId", out var id)
            || id.ValueKind != JsonValueKind.String
            || id.GetString() is not { Length: > 0 } checkInId)
        {
            return null;
        }

        return new CheckIn(checkInId, new RecordedState());
    }
}
