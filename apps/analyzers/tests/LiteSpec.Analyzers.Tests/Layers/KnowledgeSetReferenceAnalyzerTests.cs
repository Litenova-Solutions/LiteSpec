using LiteSpec.Analyzers.Rules.Layers.Knowledge;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2038, a module naming a type of a knowledge set.</summary>
public sealed class KnowledgeSetReferenceAnalyzerTests
{
    /// <summary>LS2038 reports a module naming a type of a knowledge set.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 reports a module naming a type of a knowledge set")]
    public Task ReportsAModuleNamingASetType() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.France.Rates;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private {|LS2038:FranceRates|}? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 accepts a module naming a type of a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 accepts a module naming a type of a knowledge model")]
    public Task AcceptsAModuleNamingAModelType() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.Rates;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private TravelRates? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel/Rates/TravelRates.cs", """
                    namespace Claimwick.Travel.Rates;

                    public sealed class TravelRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 stays quiet in a host, which references a set to register it.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet in a host, which references a set to register it")]
    public Task StaysQuietInAHost() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Api/TravelRegistration.cs", """
                    using Claimwick.Travel.France.Rates;

                    namespace Claimwick.Api;

                    public sealed class TravelRegistration
                    {
                        private FranceRates? _rates;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Api");

    /// <summary>LS2038 stays quiet in the set's own compilation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet in the set's own compilation")]
    public Task StaysQuietInTheSetsOwnCompilation() =>
        Verify.AnalyzerAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceRates
                    {
                        private FranceMileage? _mileage;
                    }
                    """),
                ("src/Knowledge/Claimwick.Travel.France/Rates/FranceMileage.cs", """
                    namespace Claimwick.Travel.France.Rates;

                    public sealed class FranceMileage
                    {
                    }
                    """),
            ],
            assemblyName: "Claimwick.Travel.France");

    /// <summary>LS2038 stays quiet for a third-party assembly shaped like a set that references no knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet for a third-party assembly shaped like a set")]
    public Task StaysQuietForAThirdPartyAssemblyShapedLikeASet() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Vendor.Messaging.Abstractions;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private ICommand? _command;
                    }
                    """),
            ],
            [
                new Verify.ReferencedAssembly(
                    "Vendor.Messaging",
                    [
                        ("Vendor/Messaging/Message.cs", """
                            namespace Vendor.Messaging;

                            public sealed class Message
                            {
                                public string Name { get; } = string.Empty;
                            }
                            """),
                    ]),
                new Verify.ReferencedAssembly(
                    "Vendor.Messaging.Abstractions",
                    [
                        ("Vendor/Messaging/Abstractions/Command.cs", """
                            namespace Vendor.Messaging.Abstractions;

                            public interface ICommand
                            {
                                void Execute();
                            }
                            """),
                    ]),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 stays quiet for another product's assembly that references its parent, as a vendor's extension package does.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 stays quiet for another product's assembly that references its parent")]
    public Task StaysQuietForAnotherProductsAssemblyReferencingItsParent() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Vendor.Commands.Recurring;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private RecurringSchedule? _schedule;
                    }
                    """),
            ],
            [
                new Verify.ReferencedAssembly(
                    "Vendor.Commands",
                    [
                        ("Vendor/Commands/Command.cs", """
                            namespace Vendor.Commands;

                            public class Command
                            {
                            }
                            """),
                    ]),
                new Verify.ReferencedAssembly(
                    "Vendor.Commands.Recurring",
                    [
                        ("Vendor/Commands/Recurring/RecurringSchedule.cs", """
                            namespace Vendor.Commands.Recurring;

                            public sealed class RecurringSchedule : Vendor.Commands.Command
                            {
                            }
                            """),
                    ],
                    ["Vendor.Commands"]),
            ],
            assemblyName: "Claimwick.Spending");

    /// <summary>LS2038 reports a module naming a set type referenced as metadata.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/knowledge#read-knowledge-through-its-model</proves>
    [Fact(DisplayName = "LS2038 reports a module naming a set type referenced as metadata")]
    public Task ReportsAModuleNamingASetTypeReferencedAsMetadata() =>
        Verify.AnalyzerWithMetadataAsync<KnowledgeSetReferenceAnalyzer>(
            [
                ("Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs", """
                    using Claimwick.Travel.France.Rates;

                    namespace Claimwick.Spending.Application.SubmitExpense;

                    internal sealed class SubmitExpenseCommandHandler
                    {
                        private {|LS2038:FranceRates|}? _rates;
                    }
                    """),
            ],
            [
                new Verify.ReferencedAssembly(
                    "Claimwick.Travel",
                    [
                        ("src/Knowledge/Claimwick.Travel/ITravelSet.cs", """
                            namespace Claimwick.Travel;

                            public interface ITravelSet
                            {
                                string Country { get; }
                            }
                            """),
                    ]),
                new Verify.ReferencedAssembly(
                    "Claimwick.Travel.France",
                    [
                        ("src/Knowledge/Claimwick.Travel.France/Rates/FranceRates.cs", """
                            namespace Claimwick.Travel.France.Rates;

                            public sealed class FranceRates : Claimwick.Travel.ITravelSet
                            {
                                public string Country => "France";
                            }
                            """),
                    ],
                    ["Claimwick.Travel"]),
            ],
            assemblyName: "Claimwick.Spending");
}
