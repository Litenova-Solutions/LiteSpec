namespace __NAMESPACE__.Kernel.Domain;

/// <summary>
/// The stable reason codes the use case validators report. Each finding names
/// the input member, one of these codes, and a caller-safe message, with no
/// HTTP status: the refusal catalog maps the codes to statuses.
/// </summary>
public static class ValidationCodes
{
    /// <summary>A required member is missing or empty.</summary>
    public const string Required = "required";

    /// <summary>A member carries more text than the record allows.</summary>
    public const string TooLong = "too-long";
}
