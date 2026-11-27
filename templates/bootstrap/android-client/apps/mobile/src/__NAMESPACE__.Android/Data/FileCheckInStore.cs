using System.Text.Json;
using Android.Content;
using __NAMESPACE__.Application.Health;
using __NAMESPACE__.Domain.Values;
using __NAMESPACE__.Infrastructure.Persistence;

namespace __NAMESPACE__.Android.Data;

/// <summary>The file store behind the Health screens. It keeps the persisted check-ins in app-private storage, writing each update atomically so a write stopped midway leaves the prior complete file.</summary>
public sealed class FileCheckInStore : ICheckInStore
{
    /// <summary>The cache file name under app-private storage.</summary>
    private const string FileName = "check-ins.json";

    /// <summary>The context giving the app-private files directory.</summary>
    private readonly Context context;

    /// <summary>The API client the store loads through.</summary>
    private readonly CheckInApiClient client;

    /// <summary>Initializes a new instance of the <see cref="FileCheckInStore"/> class.</summary>
    /// <param name="context">The context giving the app-private files directory.</param>
    /// <param name="client">The API client the store loads through.</param>
    public FileCheckInStore(Context context, CheckInApiClient client)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(client);
        this.context = context;
        this.client = client;
    }

    /// <summary>Gets the check-ins the last load read, or an empty list before the first load.</summary>
    public IReadOnlyList<CheckIn> Current { get; private set; } = [];

    /// <summary>Gets whether the store finished its first load.</summary>
    public bool IsReady { get; private set; }

    /// <summary>Loads the check-ins once: fresh from the backend sample, falling back to the cached file, keeping the prior list when both fail.</summary>
    /// <param name="cancellationToken">The token cancelling the load.</param>
    /// <returns>A task that completes when the load finishes.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (await client.ListAsync(cancellationToken).ConfigureAwait(false) is CheckInListResult.Loaded fresh)
        {
            Current = fresh.CheckIns;
            await SaveAsync(fresh.CheckIns).ConfigureAwait(false);
        }
        else if (ReadCache() is { } cached)
        {
            Current = cached;
        }

        IsReady = true;
    }

    /// <summary>Reads the cached check-ins from app-private storage.</summary>
    /// <returns>The cached check-ins, or null when no complete cache exists.</returns>
    private IReadOnlyList<CheckIn>? ReadCache()
    {
        var path = Path(context);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var text = File.ReadAllText(path);
            var stored = JsonSerializer.Deserialize<StoredCheckIn[]>(text);

            if (stored is null)
            {
                return null;
            }

            return stored.Select(entry => new CheckIn(entry.Id, CheckInState.FromCode(entry.State))).ToList();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Saves the check-ins atomically: the new file replaces the prior copy only after it turns whole.</summary>
    /// <param name="checkIns">The check-ins to persist.</param>
    /// <returns>A task that completes when the write lands.</returns>
    private Task SaveAsync(IReadOnlyList<CheckIn> checkIns)
    {
        var stored = checkIns.Select(entry => new StoredCheckIn(entry.Id, entry.State.Code)).ToArray();
        var text = JsonSerializer.Serialize(stored);
        var path = Path(context);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, text);
        File.Move(temporary, path, overwrite: true);
        return Task.CompletedTask;
    }

    /// <summary>The cache file path under app-private storage.</summary>
    /// <param name="context">The context giving the app-private files directory.</param>
    /// <returns>The cache file path.</returns>
    private static string Path(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var directory = context.FilesDir;

        if (directory is null)
        {
            throw new InvalidOperationException("The app-private files directory is unavailable.");
        }

        return System.IO.Path.Combine(directory.AbsolutePath, FileName);
    }
}
