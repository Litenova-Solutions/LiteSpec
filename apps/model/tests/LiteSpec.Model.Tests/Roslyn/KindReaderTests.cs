using System.Linq;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for recognizing each code kind from syntax, with a near miss for each.</summary>
public sealed class KindReaderTests
{
    /// <summary>A class deriving AggregateRoot is an aggregate root.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class deriving AggregateRoot is an aggregate root")]
    public void AggregateRootDerivingIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>;
            """);

        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class whose base the context names is an aggregate root.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class whose base the context names is an aggregate root")]
    public void AggregateRootFromContextIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class Expense : SpendingRoot;
            """);
        var context = new KindContext(["SpendingRoot"], [], []);

        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(type, context));
    }

    /// <summary>A plain class is not an aggregate root.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A plain class is not an aggregate root")]
    public void PlainClassIsNotAnAggregateRoot()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class Money;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class deriving a non-generic AggregateRoot is not an aggregate root.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class deriving a non-generic AggregateRoot is not an aggregate root")]
    public void NonGenericAggregateBaseIsNotAnAggregateRoot()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A public aggregate method raising an event is recognized.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A public aggregate method raising an event is recognized")]
    public void AggregateMethodRaisingIsRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>An aggregate method raising through a private helper is recognized.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An aggregate method raising through a private helper is recognized")]
    public void AggregateMethodThroughPrivateHelperIsRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public void Submit() => ApplySubmission();

                private void ApplySubmission()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>An unmarked public aggregate method raising nothing is not an aggregate method.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An unmarked public aggregate method raising nothing is not an aggregate method")]
    public void UnmarkedAggregateMethodRaisingNothingIsNotRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public Money Total() => Money.Zero;
            }
            """);

        Assert.Null(KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>A marked public aggregate method raising nothing is recognized.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked public aggregate method raising nothing is recognized")]
    public void MarkedAggregateMethodRaisingNothingIsRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                /// <implements>use-case/spending.erase-personal-data</implements>
                public void Erase()
                {
                }
            }
            """);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>An unmarked public aggregate method assigning state is recognized.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An unmarked public aggregate method assigning state is recognized")]
    public void UnmarkedStateChangingAggregateMethodIsRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private bool erased;

                public void Erase()
                {
                    erased = true;
                }
            }
            """);

        Assert.Equal(CodeKind.AggregateMethod, KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>An unmarked public aggregate method assigning only locals is not an aggregate method.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An unmarked public aggregate method assigning only locals is not an aggregate method")]
    public void UnmarkedLocalAssigningAggregateMethodIsNotRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public bool TryErase(bool allowed)
                {
                    var erased = false;
                    erased = allowed;
                    return erased;
                }
            }
            """);

        Assert.Null(KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>A marked private aggregate method is not an aggregate method.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked private aggregate method is not an aggregate method")]
    public void MarkedPrivateAggregateMethodIsNotRecognized()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                /// <implements>use-case/spending.submit-expense</implements>
                private void RecordSubmission()
                {
                    RaiseDomainEvent(new ExpenseSubmittedEvent());
                }
            }
            """);

        Assert.Null(KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>A record implementing IDomainEvent is a domain event.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A record implementing IDomainEvent is a domain event")]
    public void DomainEventIsRecognized()
    {
        var type = TestTrees.First<RecordDeclarationSyntax>("""
            public sealed record ExpenseSubmittedEvent : IDomainEvent;
            """);

        Assert.Equal(CodeKind.DomainEvent, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A record implementing nothing is not a domain event.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A record implementing nothing is not a domain event")]
    public void PlainRecordIsNotADomainEvent()
    {
        var type = TestTrees.First<RecordDeclarationSyntax>("""
            public sealed record Money(decimal Amount);
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>An interface extending IDomainEvent is not a domain event.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An interface extending IDomainEvent is not a domain event")]
    public void EventInterfaceIsNotADomainEvent()
    {
        var type = TestTrees.First<InterfaceDeclarationSyntax>("""
            public interface IExpenseEvent : IDomainEvent;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class deriving DomainException is a rule exception.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class deriving DomainException is a rule exception")]
    public void RuleExceptionIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed class ApproverIsSubmitterException : DomainException;
            """);

        Assert.Equal(CodeKind.RuleException, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>An abstract exception is not a rule exception.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An abstract exception is not a rule exception")]
    public void AbstractExceptionIsNotARuleException()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            public abstract class SpendingException : DomainException;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class implementing a command handler interface is a handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing a command handler interface is a handler")]
    public void CommandHandlerIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand>;
            """);

        Assert.Equal(CodeKind.Handler, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class implementing a query handler interface is a handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing a query handler interface is a handler")]
    public void QueryHandlerIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class OpenExpensesQueryHandler : IQueryHandler<OpenExpensesQuery, IReadOnlyList<Expense>>;
            """);

        Assert.Equal(CodeKind.Handler, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class named Handler implementing nothing is not a handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class named Handler implementing nothing is not a handler")]
    public void HandlerNameAloneIsNotAHandler()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseCommandHandler;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class implementing an event handler interface is an event handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing an event handler interface is an event handler")]
    public void EventHandlerIsRecognized()
    {
        var first = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class NotifyOnExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedEvent>;
            """);
        var second = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class NotifyOnExpenseSubmittedHandler : IDomainEventReaction<ExpenseSubmittedEvent>;
            """);

        Assert.Equal(CodeKind.EventHandler, KindReader.KindOf(first, KindContext.Empty));
        Assert.Equal(CodeKind.EventHandler, KindReader.KindOf(second, KindContext.Empty));
    }

    /// <summary>An event handler name without the interface is not an event handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An event handler name without the interface is not an event handler")]
    public void EventHandlerNameAloneIsNotAnEventHandler()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class NotifyOnExpenseSubmittedHandler;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A hosted service named Job is a job.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A hosted service named Job is a job")]
    public void HostedJobIsRecognized()
    {
        var first = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class ClearStuckReimbursementsJob : BackgroundService;
            """);
        var second = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class ClearStuckReimbursementsWorker : IHostedService;
            """);

        Assert.Equal(CodeKind.Job, KindReader.KindOf(first, KindContext.Empty));
        Assert.Equal(CodeKind.Job, KindReader.KindOf(second, KindContext.Empty));
    }

    /// <summary>A Job name without a hosted service base is not a job.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A Job name without a hosted service base is not a job")]
    public void JobNameAloneIsNotAJob()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class ClearStuckReimbursementsJob;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A hosted service without a job name is not a job.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A hosted service without a job name is not a job")]
    public void HostedServiceWithoutJobNameIsNotAJob()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class StartupProbe : BackgroundService;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A marked interface in Application is a port.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked interface in Application is a port")]
    public void MarkedInterfaceIsAPort()
    {
        var type = TestTrees.First<InterfaceDeclarationSyntax>("""
            /// <implements>external-system/payout-bank</implements>
            public interface IPayoutPort;
            """);

        Assert.Equal(CodeKind.Port, KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Port, KindReader.KindOf(type, KindContext.Empty, "Application.Abstractions"));
        Assert.Equal(CodeKind.Port, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>An interface port with no mark is not a port.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "An interface port with no mark is not a port")]
    public void UnmarkedInterfaceIsNotAPort()
    {
        var type = TestTrees.First<InterfaceDeclarationSyntax>("""
            public interface IPayoutPort;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A marked interface outside Application is not a port.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A marked interface outside Application is not a port")]
    public void MarkedInterfaceOutsideApplicationIsNotAPort()
    {
        var type = TestTrees.First<InterfaceDeclarationSyntax>("""
            /// <implements>external-system/payout-bank</implements>
            public interface IPayoutPort;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Shop.WebApi"));
    }

    /// <summary>A nested type sharing a root's name is not a root, and a partial part of the root is.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A nested type sharing a root's name is not a root, and a partial part of the root is")]
    public void OnlyAPartialPartTakesItsKindFromItsName()
    {
        var context = new KindContext(["OrganizerInvitation"], [], []);
        var nestedRecord = TestTrees.Parse("""
            public abstract record OutboundMessagePurpose
            {
                public sealed record OrganizerInvitation(string Code) : OutboundMessagePurpose;
            }
            """).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<RecordDeclarationSyntax>().Single(record => record.Identifier.ValueText == "OrganizerInvitation");
        var part = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed partial class OrganizerInvitation
            {
            }
            """);

        Assert.Null(KindReader.KindOf(nestedRecord, context));
        Assert.Equal(CodeKind.AggregateRoot, KindReader.KindOf(part, context));
    }

    /// <summary>A Domain class marked with its rule is a domain service.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A Domain class marked with its rule is a domain service")]
    public void MarkedDomainClassIsADomainService()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>rule/spending.receipt-threshold</implements>
            public sealed class ReceiptThresholdPolicy;
            """);

        Assert.Equal(CodeKind.DomainService, KindReader.KindOf(type, KindContext.Empty, "Claimwick.Domain"));
    }

    /// <summary>A Domain class with no rule mark is not a domain service.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A Domain class with no rule mark is not a domain service")]
    public void UnmarkedDomainClassIsNotADomainService()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            /// <implements>use-case/spending.submit-expense</implements>
            public sealed class ReceiptThresholdPolicy;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Claimwick.Domain"));
        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Claimwick.Application"));
    }

    /// <summary>A class implementing a marked port is an adapter.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing a marked port is an adapter")]
    public void AdapterIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class PayoutBankAdapter : IPayoutPort;
            """);
        var context = new KindContext([], [], ["IPayoutPort"]);

        Assert.Equal(CodeKind.Adapter, KindReader.KindOf(type, context));
    }

    /// <summary>A class implementing an unmarked interface is not an adapter.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing an unmarked interface is not an adapter")]
    public void UnmarkedInterfaceImplementationIsNotAnAdapter()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class PayoutBankAdapter : IPayoutPort;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class implementing IEndpoint is an endpoint.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class implementing IEndpoint is an endpoint")]
    public void EndpointInterfaceIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseEndpoint : IEndpoint;
            """);

        Assert.Equal(CodeKind.Endpoint, KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A class named Endpoint in WebApi is an endpoint.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class named Endpoint in WebApi is an endpoint")]
    public void EndpointNameInWebApiIsRecognized()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseEndpoint;
            """);

        Assert.Equal(CodeKind.Endpoint, KindReader.KindOf(type, KindContext.Empty, "Shop.WebApi"));
        Assert.Equal(CodeKind.Endpoint, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class named Endpoint outside WebApi is not an endpoint.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class named Endpoint outside WebApi is not an endpoint")]
    public void EndpointNameOutsideWebApiIsNotAnEndpoint()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseEndpoint;
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A method with a test attribute is a test.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method with a test attribute is a test")]
    public void TestMethodIsRecognized()
    {
        foreach (var attribute in new[]
        {
            "Fact", "Theory", "Scenario", "Test", "TestCase", "TestCaseSource", "TestMethod", "DataTestMethod", "FactAttribute",
        })
        {
            var method = TestTrees.First<MethodDeclarationSyntax>($$"""
                internal sealed class SubmitExpenseTests
                {
                    [{{attribute}}]
                    public void SubmittingSucceeds() { }
                }
                """);

            Assert.Equal(CodeKind.Test, KindReader.KindOf(method, KindContext.Empty));
        }
    }

    /// <summary>A method without a test attribute is not a test.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method without a test attribute is not a test")]
    public void PlainMethodIsNotATest()
    {
        var method = TestTrees.First<MethodDeclarationSyntax>("""
            internal sealed class SubmitExpenseTests
            {
                public void SubmittingSucceeds() { }
            }
            """);

        Assert.Null(KindReader.KindOf(method, KindContext.Empty));
    }

    /// <summary>A handler that also handles events reads as a handler.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A handler that also handles events reads as a handler")]
    public void FirstMatchingKindWins()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseCommandHandler
                : ICommandHandler<SubmitExpenseCommand>, IEventHandler<ExpenseSubmittedEvent>;
            """);

        Assert.Equal(CodeKind.Handler, KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A top-level class with a method in an outer layer is technical.</summary>
    /// <param name="project">The project name that names the layer.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "A top-level class with a method in an outer layer is technical")]
    [InlineData("Application")]
    [InlineData("Application.Abstractions")]
    [InlineData("Infrastructure")]
    [InlineData("WebApi")]
    [InlineData("Worker")]
    [InlineData("Shop.Application")]
    [InlineData("Shop.WebApi")]
    [InlineData("Shop.Worker")]
    public void ClassWithMethodInOuterLayerIsTechnical(string project)
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(type, KindContext.Empty, project));
    }

    /// <summary>A class outside the outer layers is not technical.</summary>
    /// <param name="project">The project name that names the layer.</param>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory(DisplayName = "A class outside the outer layers is not technical")]
    [InlineData("Domain")]
    [InlineData("Shop.Domain")]
    [InlineData("Shop.Application.Tests")]
    public void ClassOutsideOuterLayersIsNotTechnical(string project)
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty, project));
    }

    /// <summary>A class with no known project is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class with no known project is not technical")]
    public void ClassWithNoProjectIsNotTechnical()
    {
        var type = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class CommitStep
            {
                public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """);

        Assert.Null(KindReader.KindOf(type, KindContext.Empty));
    }

    /// <summary>A class with only properties, fields, or a constructor is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class with only properties, fields, or a constructor is not technical")]
    public void ClassWithoutOrdinaryMethodIsNotTechnical()
    {
        foreach (var code in new[]
        {
            """
            internal sealed class ExpenseSettings
            {
                public int ReceiptThreshold { get; set; }
            }
            """,
            """
            internal sealed class ExpenseSettings
            {
                private readonly int threshold;

                public ExpenseSettings(int threshold)
                {
                    this.threshold = threshold;
                }
            }
            """,
            """
            internal sealed class ExpenseSettings;
            """,
        })
        {
            var type = TestTrees.First<ClassDeclarationSyntax>(code);

            Assert.Null(KindReader.KindOf(type, KindContext.Empty, "Shop.Application"));
        }
    }

    /// <summary>A record, struct, interface, or enum is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A record, struct, interface, or enum is not technical")]
    public void NonClassIsNotTechnical()
    {
        var record = TestTrees.Parse("""
            public sealed record ExpenseSummary(Guid ExpenseId);
            """).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>().First();
        var structure = TestTrees.Parse("""
            public struct Money
            {
                public decimal Amount() => 0m;
            }
            """).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>().First();
        var face = TestTrees.First<InterfaceDeclarationSyntax>("""
            public interface IClock
            {
                DateTimeOffset UtcNow();
            }
            """);

        Assert.Null(KindReader.KindOf(record, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(structure, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(face, KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A nested class is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A nested class is not technical")]
    public void NestedClassIsNotTechnical()
    {
        var nested = TestTrees.Parse("""
            public sealed class CommitPipeline
            {
                public sealed class CommitStep
                {
                    public void Commit() { }
                }
            }
            """).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(type => type.Identifier.ValueText == "CommitStep");

        Assert.Null(KindReader.KindOf(nested, KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A generated class is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A generated class is not technical")]
    public void GeneratedClassIsNotTechnical()
    {
        var generatedFile = CSharpSyntaxTree.ParseText("""
            internal sealed class CommitStep
            {
                public void Commit() { }
            }
            """, path: "CommitStep.g.cs", cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var markedFile = CSharpSyntaxTree.ParseText("""
            // <auto-generated/>
            internal sealed class CommitStep
            {
                public void Commit() { }
            }
            """, path: "CommitStep.cs", cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var attribute = TestTrees.First<ClassDeclarationSyntax>("""
            [System.CodeDom.Compiler.GeneratedCode("tool", "1.0")]
            internal sealed class CommitStep
            {
                public void Commit() { }
            }
            """);

        Assert.Null(KindReader.KindOf(generatedFile, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(markedFile, KindContext.Empty, "Shop.Application"));
        Assert.Null(KindReader.KindOf(attribute, KindContext.Empty, "Shop.Application"));
    }

    /// <summary>A migration or model snapshot is not technical.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A migration or model snapshot is not technical")]
    public void MigrationIsNotTechnical()
    {
        var migration = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class AddExpenses : Migration
            {
                public void Up() { }
            }
            """);
        var snapshot = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class ShopModelSnapshot : ModelSnapshot
            {
                public void Build() { }
            }
            """);

        Assert.Null(KindReader.KindOf(migration, KindContext.Empty, "Shop.Infrastructure"));
        Assert.Null(KindReader.KindOf(snapshot, KindContext.Empty, "Shop.Infrastructure"));
    }

    /// <summary>A class taking another kind keeps it.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A class taking another kind keeps it")]
    public void OtherKindWinsOverTechnical()
    {
        var handler = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand>
            {
                public void Handle() { }
            }
            """);
        var endpoint = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class SubmitExpenseEndpoint
            {
                public void Map() { }
            }
            """);
        var adapter = TestTrees.First<ClassDeclarationSyntax>("""
            internal sealed class PayoutBankAdapter : IPayoutPort
            {
                public void Send() { }
            }
            """);
        var context = new KindContext([], [], ["IPayoutPort"]);

        Assert.Equal(CodeKind.Handler, KindReader.KindOf(handler, KindContext.Empty, "Shop.Application"));
        Assert.Equal(CodeKind.Endpoint, KindReader.KindOf(endpoint, KindContext.Empty, "Shop.WebApi"));
        Assert.Equal(CodeKind.Adapter, KindReader.KindOf(adapter, context, "Shop.Infrastructure"));
    }

    /// <summary>A partial part without a method reads through the context names.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A partial part without a method reads through the context names")]
    public void PartialPartReadsThroughContext()
    {
        var part = TestTrees.First<ClassDeclarationSyntax>("""
            public sealed partial class CommitStep
            {
            }
            """);
        var context = new KindContext([], [], [], technicalClasses: ["CommitStep"]);

        Assert.Equal(CodeKind.Technical, KindReader.KindOf(part, context, "Shop.Application"));
        Assert.Null(KindReader.KindOf(part, KindContext.Empty, "Shop.Application"));
    }
}
