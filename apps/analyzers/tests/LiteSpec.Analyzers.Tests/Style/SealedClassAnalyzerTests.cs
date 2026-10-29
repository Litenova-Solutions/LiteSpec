using LiteSpec.Analyzers.Rules.Style;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Style;

/// <summary>Tests for LS3004, sealed concrete classes.</summary>
public sealed class SealedClassAnalyzerTests
{
    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a sealed class")]
    public Task AcceptsASealedClass() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public sealed class Order { }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 reports an unsealed leaf class")]
    public Task ReportsAnUnsealedLeafClass() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public class {|LS3004:Order|} { }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a base with a derived class in another file")]
    public Task AcceptsABaseWithADerivedClassInAnotherFile() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>(
            [
                ("Order.cs", """
                    namespace Shop.Orders;

                    public class Order { }
                    """),
                ("PriorityOrder.cs", """
                    namespace Shop.Orders;

                    public class {|LS3004:PriorityOrder|} : Order { }
                    """),
            ]);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts an abstract class")]
    public Task AcceptsAnAbstractClass() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public abstract class OrderBase { }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a static class")]
    public Task AcceptsAStaticClass() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public static class OrderRules { }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a class with a virtual member")]
    public Task AcceptsAClassWithAVirtualMember() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public class Recorder
            {
                public virtual void Save() { }
            }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a class with a protected member")]
    public Task AcceptsAClassWithAProtectedMember() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public class Recorder
            {
                protected void Save() { }
            }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 ignores records")]
    public Task IgnoresRecords() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public record Order(int Id);

            public record struct Money(decimal Amount);
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 reports an unsealed attribute class")]
    public Task ReportsAnUnsealedAttributeClass() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            using System;

            namespace Shop.Orders;

            public class {|LS3004:ValidateAttribute|} : Attribute { }
            """);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 accepts a partial class sealed in any part")]
    public Task AcceptsAPartialClassSealedInAnyPart() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>(
            [
                ("Order.cs", """
                    namespace Shop.Orders;

                    public sealed partial class Order { }
                    """),
                ("Order.State.cs", """
                    namespace Shop.Orders;

                    public partial class Order { }
                    """),
            ]);

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 ignores the Program composition marker")]
    public Task IgnoresTheProgramCompositionMarker() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Api;

            public class Program { }
            """, assemblyName: "Shop.WebApi");

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 ignores a breach outside the five projects")]
    public Task IgnoresABreachOutsideTheFiveProjects() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Orders;

            public class Order { }
            """, assemblyName: "Shop.Unknown");

    /// <proves>litespec/backend/dotnet/naming#keep-implementation-style-consistent</proves>
    [Fact(DisplayName = "LS3004 follows the LiteSpecLayer property over the assembly name")]
    public Task FollowsTheDeclaredLayer() =>
        Verify.AnalyzerAsync<SealedClassAnalyzer>("""
            namespace Shop.Core;

            public class {|LS3004:Order|} { }
            """, assemblyName: "Shop.Core", layer: "Infrastructure");
}
