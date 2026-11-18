using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the plural aggregate folder names every layer uses.</summary>
public sealed class PluralNamingTests
{
    /// <summary>An aggregate name maps to its plural folder name in kebab case and PascalCase.</summary>
    /// <param name="name">The aggregate name part of the identifier.</param>
    /// <param name="kebab">The expected kebab-case plural.</param>
    /// <param name="pascal">The expected PascalCase plural.</param>
    /// <proves>litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree</proves>
    [Theory]
    [InlineData("expense", "expenses", "Expenses")]
    [InlineData("chart-of-accounts", "charts-of-accounts", "ChartsOfAccounts")]
    [InlineData("person", "people", "People")]
    [InlineData("counterparty", "counterparties", "Counterparties")]
    [InlineData("business", "businesses", "Businesses")]
    [InlineData("sales-invoice", "sales-invoices", "SalesInvoices")]
    [InlineData("icp-declaration", "icp-declarations", "IcpDeclarations")]
    [InlineData("day", "days", "Days")]
    [InlineData("x", "xes", "Xes")]
    public void PluralizesTheHeadWord(string name, string kebab, string pascal)
    {
        Assert.Equal(kebab, Naming.PluralKebab(name));
        Assert.Equal(pascal, Naming.PluralPascal(name));
    }

    /// <summary>An empty name has no plural.</summary>
    /// <proves>litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree</proves>
    [Fact]
    public void EmptyNameHasNoPlural()
    {
        Assert.Equal(string.Empty, Naming.PluralKebab(string.Empty));
    }
}
