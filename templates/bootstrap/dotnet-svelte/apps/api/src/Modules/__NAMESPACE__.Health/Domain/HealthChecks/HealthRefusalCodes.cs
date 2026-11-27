namespace __NAMESPACE__.Health.Domain.HealthChecks;

/// <summary>
/// The refusal codes the Health module answers.
/// </summary>
internal static class HealthRefusalCodes
{
    /// <summary>The check-in the caller named is unknown. Mapped to 404.</summary>
    public const string CheckInUnknown = "health.check-in-unknown";
}
