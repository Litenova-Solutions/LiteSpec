using LiteSpec.Analyzers.Rules.Layers;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Layers;

/// <summary>Tests for LS2010, repository variable names.</summary>
public sealed class RepositoryVariableNamingAnalyzerTests
{
    [Fact(DisplayName = "LS2010 accepts parameters named after their interfaces")]
    public Task AcceptsParametersNamedAfterTheirInterfaces() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler(IExpenseRepository expenseRepository)
            {
            }

            internal sealed class SubmitExpenseCommandHandler
            {
                public void Handle(IExpenseRepository expenseRepository)
                {
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 accepts fields with or without a leading underscore")]
    public Task AcceptsFieldsWithOrWithoutALeadingUnderscore() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            public interface IPolicyRepository { }

            internal sealed class ApproveExpenseCommandHandler
            {
                private readonly IExpenseRepository _expenseRepository = null!;

                private readonly IPolicyRepository policyRepository = null!;
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 accepts a local named after its interface")]
    public Task AcceptsALocalNamedAfterItsInterface() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler
            {
                private readonly IExpenseRepository _expenseRepository = null!;

                public void Handle()
                {
                    IExpenseRepository expenseRepository = _expenseRepository;
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 reports a parameter not named after its interface")]
    public Task ReportsAParameterNotNamedAfterItsInterface() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler(IExpenseRepository {|LS2010:expenses|})
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 reports an underscored parameter")]
    public Task ReportsAnUnderscoredParameter() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler(IExpenseRepository {|LS2010:_expenseRepository|})
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 reports a field not named after its interface")]
    public Task ReportsAFieldNotNamedAfterItsInterface() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler
            {
                private readonly IExpenseRepository {|LS2010:expenses|} = null!;
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 reports a local not named after its interface")]
    public Task ReportsALocalNotNamedAfterItsInterface() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler
            {
                public void Handle()
                {
                    IExpenseRepository {|LS2010:expenses|} = null!;
                }
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 ignores a non-repository interface")]
    public Task IgnoresANonRepositoryInterface() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseService { }

            internal sealed class ApproveExpenseCommandHandler(IExpenseService expenses)
            {
            }
            """, assemblyName: "Shop.Application");

    [Fact(DisplayName = "LS2010 ignores a repository variable outside Application and Infrastructure")]
    public Task IgnoresARepositoryVariableOutsideItsLayers() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Spending;

            public interface IExpenseRepository { }

            public sealed class ApproveExpenseCommandHandler(IExpenseRepository expenses)
            {
            }
            """, assemblyName: "Shop.Domain");

    [Fact(DisplayName = "LS2010 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<RepositoryVariableNamingAnalyzer>("""
            namespace Shop.Core;

            public interface IExpenseRepository { }

            internal sealed class ApproveExpenseCommandHandler(IExpenseRepository {|LS2010:expenses|})
            {
            }
            """, assemblyName: "Shop.Core", layer: "Application");
}
