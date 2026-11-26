using LiteSpec.Analyzers.Rules.Layers.Application;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2036, the production clock rule.</summary>
public sealed class ProductionClockAnalyzerTests
{
    /// <summary>LS2036 reports static clock reads in Application.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 reports static clock reads in Application")]
    public Task ReportsStaticClockReadsInApplication() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.Application.SubmitExpense;

            internal sealed class SubmitExpenseCommandHandler
            {
                public DateTime Stamp() => DateTime.{|LS2036:UtcNow|};

                public DateTime Day() => DateTime.{|LS2036:Today|};

                public DateTimeOffset Stamped() => DateTimeOffset.{|LS2036:Now|};
            }
            """, path: "Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs");

    /// <summary>LS2036 reports a static clock read in Infrastructure.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 reports a static clock read in Infrastructure")]
    public Task ReportsAStaticClockReadInInfrastructure() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.Infrastructure.Persistence.Expenses;

            internal sealed class ExpenseRepository
            {
                public DateTime Cutoff() => DateTime.{|LS2036:UtcNow|}.AddYears(-7);
            }
            """, path: "Claimwick.Spending/Infrastructure/Persistence/Expenses/ExpenseRepository.cs");

    /// <summary>LS2036 reports a static clock read in WebApi.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 reports a static clock read in WebApi")]
    public Task ReportsAStaticClockReadInWebApi() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.WebApi.SubmitExpense;

            internal sealed class SubmitExpenseEndpoint
            {
                public DateTimeOffset Stamp() => DateTimeOffset.{|LS2036:UtcNow|};
            }
            """, path: "Claimwick.Spending/WebApi/SubmitExpense/SubmitExpenseEndpoint.cs");

    /// <summary>LS2036 reports a static clock read in Worker.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 reports a static clock read in Worker")]
    public Task ReportsAStaticClockReadInWorker() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Worker;

            internal sealed class EraseExpiredPersonalDataJob
            {
                public DateTime Stamp() => DateTime.{|LS2036:Now|};
            }
            """, assemblyName: "Claimwick.Worker");

    /// <summary>LS2036 accepts time read from an injected clock.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 accepts time read from an injected clock")]
    public Task AcceptsTimeFromAnInjectedClock() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.Application.SubmitExpense;

            internal sealed class SubmitExpenseCommandHandler
            {
                private readonly TimeProvider _clock;

                public SubmitExpenseCommandHandler(TimeProvider clock)
                {
                    _clock = clock;
                }

                public DateTimeOffset Stamp() => _clock.GetUtcNow();

                public TimeProvider SystemClock() => TimeProvider.System;
            }
            """, path: "Claimwick.Spending/Application/SubmitExpense/SubmitExpenseCommandHandler.cs");

    /// <summary>LS2036 stays quiet in a type carrying a development-shortcut tag.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 stays quiet in a type carrying a development-shortcut tag")]
    public Task StaysQuietForADevelopmentShortcut() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.Application.Seeding;

            /// <development-shortcut>Seed the demo clock.</development-shortcut>
            internal sealed class DemoSeeder
            {
                public DateTime Stamp() => DateTime.UtcNow;
            }
            """, path: "Claimwick.Spending/Application/Seeding/DemoSeeder.cs");

    /// <summary>LS2036 stays quiet in Domain, which LS1009 keeps.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/core/backend-application#read-time-from-the-scoped-clock</proves>
    [Fact(DisplayName = "LS2036 stays quiet in Domain")]
    public Task StaysQuietInDomain() =>
        Verify.AnalyzerAsync<ProductionClockAnalyzer>("""
            using System;

            namespace Claimwick.Spending.Domain.Expenses;

            public sealed class ExpensePolicy
            {
                public DateTime Stamp() => DateTime.UtcNow;
            }
            """, path: "Claimwick.Spending/Domain/Expenses/ExpensePolicy.cs");
}
