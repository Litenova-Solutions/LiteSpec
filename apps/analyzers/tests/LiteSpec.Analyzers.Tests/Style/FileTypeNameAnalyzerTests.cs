using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Style;

/// <summary>Tests for LS3003, one primary top-level type per file.</summary>
public sealed class FileTypeNameAnalyzerTests
{
    /// <summary>LS3003 accepts a file named after its type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 accepts a file named after its type")]
    public Task AcceptsAFileNamedAfterItsType() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/OrderId.cs", """
                using System;

                namespace Shop.Orders;

                public readonly record struct OrderId(Guid Value);
                """)]);

    /// <summary>LS3003 reports a type that differs from its file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 reports a type that differs from its file")]
    public Task ReportsATypeThatDiffersFromItsFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public sealed class {|LS3003:Customer|} { }
                """)]);

    /// <summary>LS3003 reports the second and later types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 reports the second and later types")]
    public Task ReportsTheSecondAndLaterTypes() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders
                {
                    public sealed class Order { }

                    public sealed class {|LS3003:AuditEntry|} { }

                    public sealed class {|LS3003:OrderHelper|} { }
                }
                """)]);

    /// <summary>LS3003 reports the first type too when it differs from its file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 reports the first type too when it differs from its file")]
    public Task ReportsTheFirstTypeTooWhenItDiffersFromItsFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public sealed class {|LS3003:Customer|} { }

                public sealed class {|LS3003:AuditEntry|} { }
                """)]);

    /// <summary>LS3003 ignores a file-scoped companion of the primary type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 ignores a file-scoped companion of the primary type")]
    public Task IgnoresAFileScopedCompanionOfThePrimaryType() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public sealed class Order { }

                file sealed class OrderHelper { }
                """)]);

    /// <summary>LS3003 accepts a partial class in a dotted file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 accepts a partial class in a dotted file")]
    public Task AcceptsAPartialClassInADottedFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.Validation.cs", """
                namespace Shop.Orders;

                public sealed partial class Order
                {
                    public void Validate() { }
                }
                """)]);

    /// <summary>LS3003 accepts partial declarations of one type across files.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 accepts partial declarations of one type across files")]
    public Task AcceptsPartialDeclarationsOfOneTypeAcrossFiles() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [
                ("Shop.Spending/Domain/Order.cs", """
                    namespace Shop.Orders;

                    public sealed partial class Order
                    {
                        public int Id { get; }
                    }
                    """),
                ("Shop.Spending/Domain/Order.Validation.cs", """
                    namespace Shop.Orders;

                    public sealed partial class Order
                    {
                        public void Validate() { }
                    }
                    """),
            ]);

    /// <summary>LS3003 ignores nested types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 ignores nested types")]
    public Task IgnoresNestedTypes() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public sealed class Order
                {
                    private sealed class Validator { }
                }
                """)]);

    /// <summary>LS3003 ignores a file with no types.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 ignores a file with no types")]
    public Task IgnoresAFileWithNoTypes() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Usings.cs", """
                global using System;
                global using System.Threading.Tasks;
                """)]);

    /// <summary>LS3003 ignores generic arity in the type name.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 ignores generic arity in the type name")]
    public Task IgnoresGenericArityInTheTypeName() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Cache.cs", """
                namespace Shop.Orders;

                public sealed class Cache<T> { }
                """)]);

    /// <summary>LS3003 reports a delegate that differs from its file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 reports a delegate that differs from its file")]
    public Task ReportsADelegateThatDiffersFromItsFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Shop.Spending/Domain/Order.cs", """
                namespace Shop.Orders;

                public delegate void {|LS3003:OrderPlaced|}(int orderId);
                """)]);

    /// <summary>LS3003 accepts one matching type per file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 accepts one matching type per file")]
    public Task AcceptsOneMatchingTypePerFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [
                ("Shop.Spending/Domain/Order.cs", """
                    namespace Shop.Orders;

                    public sealed class Order { }
                    """),
                ("Shop.Spending/Domain/Customer.cs", """
                    namespace Shop.Orders;

                    public sealed class Customer { }
                    """),
            ]);

    /// <summary>LS3003 ignores a breach in a file the layer rules cannot place.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/naming#match-c-files-and-primary-types</proves>
    [Fact(DisplayName = "LS3003 ignores a breach in a file the layer rules cannot place")]
    public Task IgnoresABreachInAnUnplacedFile() =>
        Verify.AnalyzerAsync<FileTypeNameAnalyzer>(
            [("Order.cs", """
                namespace Shop.Orders;

                public sealed class Customer { }
                """)], assemblyName: "Shop.Unknown");

}
