using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2023, one message definition contract per class.</summary>
public sealed class MessageDefinitionAnalyzerTests
{
    private const string Stubs = """
        namespace LiteBus.Messaging.Abstractions;

        public interface IMessageDefinition<TMessage, TValue>;

        public sealed class AuthorizationDeclaration;

        public sealed class AuditDeclaration;

        public sealed class IdempotencyDeclaration;

        public interface IAuthorizationDefinition<TMessage> : IMessageDefinition<TMessage, AuthorizationDeclaration>;

        public interface IAuditDefinition<TMessage> : IMessageDefinition<TMessage, AuditDeclaration>;

        public interface IIdempotencyDefinition<TMessage> : IMessageDefinition<TMessage, IdempotencyDeclaration>;

        public interface IUseCaseOwned;
        """;

    private const string Command = """
        namespace Claimwick.Spending.Application.SubmitExpense;

        public sealed class SubmitExpenseCommand;
        """;

    /// <summary>LS2023 reports a class implementing two message definition contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</proves>
    [Fact(DisplayName = "LS2023 reports a class implementing two message definition contracts")]
    public Task ReportsAClassImplementingTwoDefinitions() =>
        Verify.AnalyzerAsync<MessageDefinitionAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", Command),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandDefinition.cs", """
                    using LiteBus.Messaging.Abstractions;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class {|LS2023:SubmitExpenseCommandDefinition|}
                        : IAuthorizationDefinition<SubmitExpenseCommand>, IAuditDefinition<SubmitExpenseCommand>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2023 reports a class implementing three message definition contracts.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</proves>
    [Fact(DisplayName = "LS2023 reports a class implementing three message definition contracts")]
    public Task ReportsAClassImplementingThreeDefinitions() =>
        Verify.AnalyzerAsync<MessageDefinitionAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", Command),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandDefinition.cs", """
                    using LiteBus.Messaging.Abstractions;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class {|LS2023:SubmitExpenseCommandDefinition|}
                        : IAuthorizationDefinition<SubmitExpenseCommand>,
                            IAuditDefinition<SubmitExpenseCommand>,
                            IIdempotencyDefinition<SubmitExpenseCommand>
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2023 accepts a class implementing one definition contract beside another interface.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class</proves>
    [Fact(DisplayName = "LS2023 accepts a class implementing one definition contract beside another interface")]
    public Task AcceptsAClassImplementingOneDefinition() =>
        Verify.AnalyzerAsync<MessageDefinitionAnalyzer>(
            [
                ("Claimwick.Spending.Contracts/Stubs.cs", Stubs),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommand.cs", Command),
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandAuthorizationDefinition.cs", """
                    using LiteBus.Messaging.Abstractions;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandAuthorizationDefinition
                        : IAuthorizationDefinition<SubmitExpenseCommand>, IUseCaseOwned
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");
}
