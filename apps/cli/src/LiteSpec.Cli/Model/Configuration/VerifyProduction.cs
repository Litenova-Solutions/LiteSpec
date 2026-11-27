namespace LiteSpec.Cli.Model.Configuration;

/// <summary>How the production smoke boots the production containers and what it probes.</summary>
/// <param name="Compose">The compose file, relative to the consumer root.</param>
/// <param name="EnvFile">The environment file the compose run reads, relative to the consumer root.</param>
/// <param name="BaseUrl">The address the booted reverse proxy answers on.</param>
/// <param name="Migrate">The command that migrates the empty database before the health wait, or null for none.</param>
/// <param name="Health">The readiness path polled until it answers 2xx.</param>
/// <param name="Paths">The paths requested with a forwarded https scheme once the host is ready.</param>
sealed record VerifyProduction(string Compose, string EnvFile, string BaseUrl, string? Migrate, string Health, IReadOnlyList<string> Paths);
