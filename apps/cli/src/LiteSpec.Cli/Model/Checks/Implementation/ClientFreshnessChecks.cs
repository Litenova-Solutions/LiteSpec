using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>Checks that every declared generated API client records the contract document it was generated from.</summary>
/// <follows>litespec/packs/svelte/data#add-the-stack-query-plugin-to-the-client-generator</follows>
internal static class ClientFreshnessChecks
{
    /// <summary>The file a generated client uses to record its source contract's hash.</summary>
    private const string HashFileName = "contract.sha256";

    /// <summary>Reports each declared generated client whose recorded contract hash is missing or stale.</summary>
    /// <param name="model">The loaded project model that holds the declared frontends.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var frontend in model.Config.Frontends)
        {
            if (frontend.Client is { } client)
            {
                CheckFrontend(model, frontend, client, findings);
            }
        }
    }

    /// <summary>Compares one generated client's recorded hash with its current contract document's hash.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <param name="frontend">The frontend that declares the client.</param>
    /// <param name="client">The contract document and generated folder the frontend declares.</param>
    /// <param name="findings">The list that receives the finding when the client cannot be proved current.</param>
    private static void CheckFrontend(
        ProjectModel model,
        FrontendConfig frontend,
        FrontendClientConfig client,
        List<Finding> findings)
    {
        var hashFile = Path.Combine(client.Generated, HashFileName);

        if (!File.Exists(hashFile))
        {
            findings.Add(new Finding(
                "client-drift",
                $"Frontend '{frontend.Name}' has no '{HashFileName}' in '{PathText.Relative(model.Config.Root, client.Generated)}'; regenerate its client from '{PathText.Relative(model.Config.Root, client.Contract)}'.",
                hashFile,
                1,
                null));
            return;
        }

        if (ReadRecordedHash(model, frontend, hashFile, findings) is not { } recorded
            || ReadCurrentHash(model, frontend, client.Contract, findings) is not { } current)
        {
            return;
        }

        if (!string.Equals(recorded, current, StringComparison.Ordinal))
        {
            findings.Add(new Finding(
                "client-drift",
                $"Frontend '{frontend.Name}' records contract hash '{recorded}' in '{PathText.Relative(model.Config.Root, hashFile)}', but '{PathText.Relative(model.Config.Root, client.Contract)}' hashes to '{current}' after LF normalization; regenerate the client.",
                hashFile,
                1,
                null));
        }
    }

    /// <summary>Reads the contract hash recorded by a generated client.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <param name="frontend">The frontend whose client is being checked.</param>
    /// <param name="file">The generated client's hash file.</param>
    /// <param name="findings">The list that receives a finding when the file cannot be read.</param>
    /// <returns>The recorded hash, or null when the file cannot be read.</returns>
    private static string? ReadRecordedHash(
        ProjectModel model,
        FrontendConfig frontend,
        string file,
        List<Finding> findings)
    {
        try
        {
            return File.ReadAllText(file).Trim();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReportUnreadable(model, frontend, file, exception, findings);
            return null;
        }
    }

    /// <summary>Reads and hashes the current contract document.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <param name="frontend">The frontend whose client is being checked.</param>
    /// <param name="contract">The current contract document.</param>
    /// <param name="findings">The list that receives a finding when the document cannot be read.</param>
    /// <returns>The current contract hash, or null when the document cannot be read.</returns>
    private static string? ReadCurrentHash(
        ProjectModel model,
        FrontendConfig frontend,
        string contract,
        List<Finding> findings)
    {
        try
        {
            return HashContract(contract);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReportUnreadable(model, frontend, contract, exception, findings);
            return null;
        }
    }

    /// <summary>Hashes a contract document's bytes after every CRLF or CR line ending becomes LF.</summary>
    /// <param name="contract">The contract document to hash.</param>
    /// <returns>The lowercase hexadecimal SHA-256 of the normalized bytes.</returns>
    private static string HashContract(string contract)
    {
        var bytes = File.ReadAllBytes(contract);
        var normalized = new List<byte>(bytes.Length);

        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\r')
            {
                normalized.Add(bytes[index]);
                continue;
            }

            if (index + 1 < bytes.Length && bytes[index + 1] == (byte)'\n')
            {
                index++;
            }

            normalized.Add((byte)'\n');
        }

        return Convert.ToHexString(Sha256.Hash(normalized.ToArray())).ToLowerInvariant();
    }

    /// <summary>Reports a contract or hash file that the check cannot read.</summary>
    /// <param name="model">The loaded project model that holds the consumer root.</param>
    /// <param name="frontend">The frontend whose client is being checked.</param>
    /// <param name="file">The unreadable contract document or hash file.</param>
    /// <param name="exception">The read failure that explains why the file is unreadable.</param>
    /// <param name="findings">The list that receives the finding.</param>
    private static void ReportUnreadable(
        ProjectModel model,
        FrontendConfig frontend,
        string file,
        Exception exception,
        List<Finding> findings)
    {
        var detail = exception.Message.Trim().TrimEnd('.');

        findings.Add(new Finding(
            "client-drift",
            $"Frontend '{frontend.Name}' cannot be checked because '{PathText.Relative(model.Config.Root, file)}' is unreadable: {(detail.Length > 0 ? detail : "an unknown error")}.",
            file,
            1,
            null));
    }
}
