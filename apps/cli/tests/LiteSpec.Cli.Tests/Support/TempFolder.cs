namespace LiteSpec.Tests.Support;

/// <summary>A temporary folder deleted on dispose, unless KEEP_FIXTURE is set.</summary>
sealed class TempFolder : IDisposable
{
    public TempFolder(string prefix)
    {
        Path = Directory.CreateTempSubdirectory($"litespec-{prefix}-").FullName;
    }

    /// <summary>Gets the folder's full path.</summary>
    public string Path { get; }

    public void Dispose()
    {
        if (Environment.GetEnvironmentVariable("KEEP_FIXTURE") is null)
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
