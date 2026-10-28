using Library.Domain.Lending.Copies;
using Library.Domain.Lending.Copies.Exceptions;

namespace Library.Application.Lending.Copies.RegisterCopy;

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
