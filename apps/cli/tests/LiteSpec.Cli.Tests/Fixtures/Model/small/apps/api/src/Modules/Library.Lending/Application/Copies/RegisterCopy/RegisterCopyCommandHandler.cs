using Library.Lending.Domain.Copies;
using Library.Lending.Domain.Copies.Exceptions;

namespace Library.Lending.Application.Copies.RegisterCopy;

/// <implements>use-case/lending.register-copy</implements>
public sealed class RegisterCopyCommandHandler : ICommandHandler<RegisterCopyCommand>
{
    public Task Handle(RegisterCopyCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.CopyId))
            throw new CopyAlreadyRegisteredException();
        var copy = new Copy();
        copy.Register();
        return Task.CompletedTask;
    }
}
