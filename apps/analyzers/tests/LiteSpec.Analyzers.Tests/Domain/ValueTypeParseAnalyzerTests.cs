using LiteSpec.Analyzers.Rules.Domain;
using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Domain;

/// <summary>Tests for LS1017, the non-throwing parse rule.</summary>
public sealed class ValueTypeParseAnalyzerTests
{
    /// <summary>LS1017 reports a value type whose constructor throws and which offers no parse.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 reports a value type whose constructor throws and which offers no parse")]
    public Task ReportsAThrowingValueTypeWithoutParse() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct {|LS1017:ExpenseTitle|}
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }
            }
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 accepts a throwing value type that offers TryParse.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 accepts a throwing value type that offers TryParse")]
    public Task AcceptsAThrowingValueTypeWithTryParse() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct ExpenseTitle
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }

                public static bool TryParse(string? value, out ExpenseTitle result)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result = default;
                        return false;
                    }

                    result = new ExpenseTitle(value);
                    return true;
                }
            }
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 accepts a throwing value type that offers TryCreate.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 accepts a throwing value type that offers TryCreate")]
    public Task AcceptsAThrowingValueTypeWithTryCreate() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct ExpenseTitle
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }

                public static bool TryCreate(string? value, out ExpenseTitle result)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        result = default;
                        return false;
                    }

                    result = new ExpenseTitle(value);
                    return true;
                }
            }
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 reports a throwing value type whose TryParse has no out parameter of the type.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 reports a throwing value type whose TryParse has no out parameter of the type")]
    public Task ReportsAThrowingValueTypeWithAMisshapenTryParse() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct {|LS1017:ExpenseTitle|}
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }

                public static bool TryParse(string? value) => !string.IsNullOrWhiteSpace(value);
            }
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 accepts a struct whose constructor never throws.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 accepts a struct whose constructor never throws")]
    public Task AcceptsAStructWithoutThrowingConstructor() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct ExpenseId(Guid Value);
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 never reports a class, however its constructor validates.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 never reports a class")]
    public Task NeverReportsAClass() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public sealed class ExpenseTitle
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }
            }
            """, path: "Claimwick.Spending/Domain/Source.cs");

    /// <summary>LS1017 stays quiet outside Domain.</summary>
    /// <returns>A task that completes when the assertion finishes.</returns>
    /// <proves>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</proves>
    [Fact(DisplayName = "LS1017 stays quiet outside Domain")]
    public Task StaysQuietOutsideDomain() =>
        Verify.AnalyzerAsync<ValueTypeParseAnalyzer>("""
            using System;

            namespace Claimwick.Spending;

            public readonly record struct ExpenseTitle
            {
                public ExpenseTitle(string value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        throw new ArgumentException("A title is required.", nameof(value));
                    }

                    Value = value;
                }

                public string Value { get; }
            }
            """, path: "Claimwick.Spending/Application/Source.cs");
}
