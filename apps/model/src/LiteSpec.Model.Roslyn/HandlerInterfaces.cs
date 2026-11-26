using System;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     The handler shape of a type symbol: the message it handles and the result it
///     returns, read from the command or query handler interface it implements, wherever
///     that interface comes from. The syntax reader finds the same shape from the Handle
///     method and the base list; the symbol shape resolves it through base types, so an
///     inherited Handle and an aliased message name read what the compiler knows.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class HandlerInterfaces
{
    /// <summary>One handler shape: the message handled with the result returned.</summary>
    public sealed class HandlerShape
    {
        /// <summary>Initializes a new instance of the <see cref="HandlerShape" /> class.</summary>
        /// <param name="message">The simple message type name.</param>
        /// <param name="result">The simple result type name, or null when it names no type.</param>
        /// <param name="isQuery">Whether the type handles a query.</param>
        /// <param name="isCommand">Whether the type handles a command.</param>
        public HandlerShape(string message, string? result, bool isQuery, bool isCommand)
        {
            Message = message;
            Result = result;
            IsQuery = isQuery;
            IsCommand = isCommand;
        }

        /// <summary>Gets the simple message type name.</summary>
        public string Message { get; }

        /// <summary>Gets the simple result type name, or null when it names no type.</summary>
        public string? Result { get; }

        /// <summary>Gets a value indicating whether the type handles a query.</summary>
        public bool IsQuery { get; }

        /// <summary>Gets a value indicating whether the type handles a command.</summary>
        public bool IsCommand { get; }
    }

    /// <summary>
    ///     Reads the handler shape of the type: the message and result its command or query
    ///     handler interface names. A command wins over a query when the type implements
    ///     both, as the syntax kind does. Null when the type implements neither with a
    ///     resolvable message.
    /// </summary>
    /// <param name="type">The type symbol to read.</param>
    /// <returns>The handler shape, or null when the type handles nothing.</returns>
    /// <exception cref="ArgumentNullException">The type is null.</exception>
    public static HandlerShape? Of(INamedTypeSymbol type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        string? commandMessage = null;
        string? commandResult = null;
        string? queryMessage = null;
        string? queryResult = null;

        foreach (var face in type.AllInterfaces)
        {
            if (face.TypeArguments.Length != 2)
            {
                continue;
            }

            if (NameOf(face.TypeArguments[0]) is not { } message)
            {
                continue;
            }

            if (face.Name == "ICommandHandler" && commandMessage is null)
            {
                commandMessage = message;
                commandResult = NameOf(face.TypeArguments[1]);
            }
            else if (face.Name == "IQueryHandler" && queryMessage is null)
            {
                queryMessage = message;
                queryResult = NameOf(face.TypeArguments[1]);
            }
        }

        if (commandMessage is not null)
        {
            return new HandlerShape(commandMessage, commandResult, queryMessage is not null, true);
        }

        if (queryMessage is not null)
        {
            return new HandlerShape(queryMessage, queryResult, true, false);
        }

        return null;
    }

    /// <summary>Whether the type implements the interface, through its own bases or an inherited one.</summary>
    /// <param name="type">The type symbol to read.</param>
    /// <param name="face">The interface simple name.</param>
    /// <returns>True when the type implements the interface.</returns>
    /// <exception cref="ArgumentNullException">The type or the face is null.</exception>
    public static bool Implements(INamedTypeSymbol type, string face)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (face is null)
        {
            throw new ArgumentNullException(nameof(face));
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (implemented.Name == face)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The event a handler subscribes to: the first type argument of its event-handler interface.</summary>
    /// <param name="type">The type symbol to read.</param>
    /// <returns>The simple event type name, or null when the type subscribes to none.</returns>
    /// <exception cref="ArgumentNullException">The type is null.</exception>
    public static string? EventOf(INamedTypeSymbol type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        foreach (var face in type.AllInterfaces)
        {
            if (face.Name is "IEventHandler" or "IDomainEventReaction"
                && face.TypeArguments.Length > 0
                && NameOf(face.TypeArguments[0]) is { } name)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>The simple name of a type, or null when the type failed to resolve.</summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>The simple name, or null for an error type.</returns>
    private static string? NameOf(ITypeSymbol type) =>
        type is IErrorTypeSymbol ? null : type.Name;
}
