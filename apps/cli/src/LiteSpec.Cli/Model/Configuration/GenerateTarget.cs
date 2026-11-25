namespace LiteSpec.Cli.Model.Configuration;

/// <summary>
///     One file set <c>litespec generate</c> writes from the records, from an entry of the
///     <c>generate</c> list in the consumer declaration: the kind of facts, the stack to
///     write for, and the folder the files go in.
/// </summary>
/// <param name="Kind">The record facts the target writes: refusal-codes, permissions, event-decisions, closed-sets, knowledge-sets, exceptions, domain-events, module-events, declarations, validators, messages, results or endpoints.</param>
/// <param name="Stack">The stack the target writes for: dotnet, blazor or android write C#, react or svelte write TypeScript.</param>
/// <param name="Path">The folder the target writes into, relative to the consumer root with forward slashes, with {module} or {Module} for the module name on a .NET stack.</param>
/// <param name="Namespace">The C# namespace of the generated files, with {module} or {Module} for the module name; empty when the declaration names none.</param>
/// <param name="BaseType">The C# base type an exceptions target derives from, such as DomainException; empty when the declaration names none.</param>
/// <param name="MarkerType">The C# marker interface an event target implements, such as IDomainEvent; empty when the declaration names none.</param>
/// <param name="Usings">The C# namespaces an event, messages or results target imports for referenced identifier and value types, or a validators target for messages outside their use case folder; null when the declaration names none.</param>
/// <param name="PermissionPolicy">How a declarations target turns a permission grant into a policy name, with {permission} for the permission id or {name} for the name after the prefix; empty for the default.</param>
/// <param name="SignedInPolicy">The policy name a declarations target writes for a signed-in grant; empty for the default.</param>
/// <param name="AnonymousPolicy">The policy name a declarations target writes for an anonymous grant; empty for the default.</param>
/// <param name="SystemPolicy">The policy name a declarations target writes for a system grant; empty for the default.</param>
/// <param name="SelfPolicy">How a declarations target wraps a policy for a self grant or a self scope, with {policy} for the wrapped policy; empty for the default.</param>
/// <param name="RecentSignInPolicy">How a declarations target wraps a policy for a recent sign-in, with {policy} for the wrapped policy and {window} for the window; empty for the default.</param>
/// <param name="RunsAsPolicy">How a declarations target wraps a policy for a use case the system also runs, with {policy} for the wrapped policy; empty for the default.</param>
/// <param name="CrossPolicy">How a declarations target wraps a policy for a use case reaching across tenants, with {policy} for the grant's policy; empty for the grant's policy unwrapped.</param>
/// <param name="PublicPolicy">How a declarations target wraps a policy for a use case working beyond tenant boundaries, with {policy} for the grant's policy; empty for the grant's policy unwrapped.</param>
/// <param name="NonePolicy">How a declarations target wraps a policy for a use case touching no tenant data, with {policy} for the grant's policy; empty for the grant's policy unwrapped.</param>
/// <param name="CommandInterface">The C# message interface a messages target implements on commands, such as ICommand; empty when the declaration names none.</param>
/// <param name="QueryInterface">The C# message interface a messages target implements on queries, such as IQuery; empty when the declaration names none.</param>
/// <param name="Visibility">The use cases a messages or results target renders: public for the use cases another module reaches, anything else for the module's own.</param>
/// <param name="ClaimReader">The qualified static method an endpoints target reads an actor identity with, as Namespace.Type.Method; empty when the declaration names none.</param>
/// <param name="TenantReader">The qualified static method an endpoints target reads the tenant identity with, as Namespace.Type.Method; empty when the declaration names none.</param>
/// <param name="ClaimValueReader">The qualified generic static method an endpoints target reads a non-identity claim value with, as Namespace.Type.Method, taking the caller and the claim name; empty when the declaration names none.</param>
/// <param name="CommandValidatorInterface">The C# validator interface a validators target implements on commands, such as ICommandValidator; empty for the LiteBus default.</param>
/// <param name="QueryValidatorInterface">The C# validator interface a validators target implements on queries, such as IQueryValidator; empty for the LiteBus default.</param>
sealed record GenerateTarget(
    string Kind,
    string Stack,
    string Path,
    string Namespace,
    string BaseType = "",
    string MarkerType = "",
    IReadOnlyList<string>? Usings = null,
    string PermissionPolicy = "",
    string SignedInPolicy = "",
    string AnonymousPolicy = "",
    string SystemPolicy = "",
    string SelfPolicy = "",
    string RecentSignInPolicy = "",
    string RunsAsPolicy = "",
    string CrossPolicy = "",
    string PublicPolicy = "",
    string NonePolicy = "",
    string CommandInterface = "",
    string QueryInterface = "",
    string Visibility = "",
    string ClaimReader = "",
    string TenantReader = "",
    string ClaimValueReader = "",
    string CommandValidatorInterface = "",
    string QueryValidatorInterface = "");
