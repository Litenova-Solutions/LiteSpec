namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     The contract document and generated folder for one frontend's API client.
/// </summary>
/// <param name="Contract">The absolute path of the contract document the client is generated from.</param>
/// <param name="Generated">The absolute path of the generated client folder.</param>
sealed record FrontendClientConfig(string Contract, string Generated);
