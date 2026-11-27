using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using __NAMESPACE__.Kernel.Domain;

namespace __NAMESPACE__.Health.Application.HealthChecks.RecordCheckIn;

/// <summary>
/// Structural checks for recording a check-in: the identifier and the recorded
/// moment are present, and the note holds at most 500 characters. The unknown
/// identifier and the business rules stay in the repository and the domain.
/// </summary>
/// <implements>use-case/health.record-check-in</implements>
internal sealed class RecordCheckInCommandValidator : ICommandValidator<RecordCheckInCommand>
{
    /// <summary>The longest note the sample stores.</summary>
    private const int MaxNoteLength = 500;

    /// <inheritdoc />
    public Task<Validity> ValidateAsync(RecordCheckInCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var failures = new List<ValidationFailure>();
        if (command.CheckInId.Value == Guid.Empty)
        {
            failures.Add(new ValidationFailure(
                "The check-in needs an identifier.", nameof(command.CheckInId), ValidationCodes.Required));
        }

        if (command.RecordedAt == default)
        {
            failures.Add(new ValidationFailure(
                "The check-in needs the moment it was recorded.", nameof(command.RecordedAt), ValidationCodes.Required));
        }

        if (command.Note is { Length: > MaxNoteLength })
        {
            failures.Add(new ValidationFailure(
                "The note holds at most 500 characters.", nameof(command.Note), ValidationCodes.TooLong));
        }

        return Task.FromResult(failures.Count == 0 ? Validity.Valid : Validity.Invalid(failures));
    }
}
