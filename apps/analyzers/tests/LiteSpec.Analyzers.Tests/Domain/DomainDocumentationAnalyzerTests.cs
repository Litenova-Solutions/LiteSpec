using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1011, documentation of public Domain contracts.</summary>
public sealed class DomainDocumentationAnalyzerTests
{
    /// <summary>LS1011 accepts a documented type and member.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 accepts a documented type and member")]
    public Task AcceptsDocumentedTypeAndMember() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
                /// <summary>Gets the number of lines.</summary>
                public int LineCount { get; set; }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reads /// comments in a build without a documentation file.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reads /// comments in a build without a documentation file")]
    public Task ReadsCommentsWithoutADocumentationFile() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
                /// <inheritdoc />
                public override string ToString() => "order";

                /// <summary>Gets the number of lines.</summary>
                public int LineCount { get; init; }

                /// <summary></summary>
                public int {|LS1011:Total|} { get; init; }

                public int {|LS1011:Weight|} { get; init; }
            }
            """, path: "Shop.Spending/Domain/Source.cs", documentationFile: false);

    /// <summary>LS1011 reports an undocumented type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented type")]
    public Task ReportsUndocumentedType() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            public sealed class {|LS1011:Order|}
            {
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reports an undocumented member.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented member")]
    public Task ReportsUndocumentedMember() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
                public void {|LS1011:Cancel|}()
                {
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reports an empty summary.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an empty summary")]
    public Task ReportsEmptySummary() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
                /// <summary></summary>
                public void {|LS1011:Cancel|}()
                {
                }

                /// <summary>   </summary>
                public void {|LS1011:Refund|}()
                {
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 accepts inherited and block documentation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 accepts inherited and block documentation")]
    public Task AcceptsInheritedAndBlockDocumentation() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
                /// <inheritdoc/>
                public string Label() => "order";

                /** <summary>Gets the tag.</summary> */
                public string Tag() => "tag";
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 ignores an internal class.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 ignores an internal class")]
    public Task IgnoresInternalClass() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            internal sealed class Order
            {
                public void Cancel()
                {
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 ignores an override and an explicit implementation.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 ignores an override and an explicit implementation")]
    public Task IgnoresOverrideAndExplicitImplementation() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            using System;

            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order : IDisposable
            {
                public override string ToString() => "order";

                void IDisposable.Dispose()
                {
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reports an undocumented field, property, event and constructor.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented field, property, event and constructor")]
    public Task ReportsUndocumentedFieldPropertyEventAndConstructor() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            using System;

            namespace Shop.Orders;

            /// <summary>An order placed by a customer.</summary>
            public class Order
            {
                public const int {|LS1011:MaxLines|} = 10;

                public int {|LS1011:LineCount|} = 0;

                public decimal {|LS1011:Total|} { get; set; }

                public event EventHandler? {|LS1011:Changed|};

                protected void {|LS1011:Validate|}()
                {
                }

                public {|LS1011:Order|}()
                {
                }
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reports an undocumented record with a primary constructor.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented record with a primary constructor")]
    public Task ReportsUndocumentedRecordWithPrimaryConstructor() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            using System;

            namespace Shop.Orders;

            public sealed record {|LS1011:Order|}(Guid Id);
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 reports an undocumented interface member.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented interface member")]
    public Task ReportsUndocumentedInterfaceMember() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            /// <summary>Loads orders.</summary>
            public interface IOrderRepository
            {
                void {|LS1011:Store|}(Order order);
            }

            /// <summary>An order placed by a customer.</summary>
            public sealed class Order
            {
            }
            """, path: "Shop.Spending/Domain/Source.cs");

    /// <summary>LS1011 ignores undocumented contracts outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 ignores undocumented contracts outside Domain")]
    public Task IgnoresUndocumentedContractsOutsideDomain() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Order
            {
                public void Cancel()
                {
                }
            }
            """, assemblyName: "Shop.WebApi");

    /// <summary>LS1011 reports an undocumented member in a knowledge model.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/backend/domain#document-every-public-domain-contract</proves>
    [Fact(DisplayName = "LS1011 reports an undocumented member in a knowledge model")]
    public Task ReportsAnUndocumentedMemberInAKnowledgeModel() =>
        Verify.AnalyzerAsync<DomainDocumentationAnalyzer>("""
            namespace Claimwick.Travel;

            /// <summary>Travel rates by country.</summary>
            public sealed class TravelRates
            {
                public decimal {|LS1011:Amount|} { get; }
            }
            """, assemblyName: "Claimwick.Travel", path: "src/Knowledge/Claimwick.Travel/TravelRates.cs");

}
