using LiteSpec.Analyzers.Tests.Support;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated mark attributes.</summary>
public sealed class AttributeGenerationTests
{
    [Fact(DisplayName = "The attribute style generates the Implements and Proves attributes")]
    public void AttributeStyleGeneratesTheAttributes()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecAttributes.g.cs", out var attributes), "The attributes source is missing.");
        Assert.Contains("class ImplementsAttribute", attributes, StringComparison.Ordinal);
        Assert.Contains("AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct | System.AttributeTargets.Method", attributes, StringComparison.Ordinal);
        Assert.Contains("class ProvesAttribute", attributes, StringComparison.Ordinal);
        Assert.Contains("AttributeTargets.Method", attributes, StringComparison.Ordinal);
        Assert.Contains("AllowMultiple = true", attributes, StringComparison.Ordinal);
        Assert.Contains("EmbeddedAttribute", attributes, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "The xml style generates no attribute types")]
    public void XmlStyleGeneratesNoAttributes()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            RecordPages.Files("xml", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.False(run.Sources.ContainsKey("LiteSpecAttributes.g.cs"), "The attributes source must not be generated.");
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "No project file generates nothing")]
    public void NoProjectFileGeneratesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            [("/project/docs/behavior/events.md", RecordPages.EventPage("event/expense.submitted"))]);

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
    }

    [Fact(DisplayName = "Proves implements the xUnit v3 trait attribute when xUnit v3 is referenced")]
    public void XunitReferenceAddsTheTrait()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("events.md", RecordPages.EventPage("event/expense.submitted"))),
            extraReferences: [MetadataReference.CreateFromFile(typeof(Xunit.v3.ITraitAttribute).Assembly.Location)]);

        Assert.True(run.Sources.TryGetValue("LiteSpecAttributes.g.cs", out var attributes), "The attributes source is missing.");
        Assert.Contains("Xunit.v3.ITraitAttribute", attributes, StringComparison.Ordinal);
        Assert.Contains("GetTraits", attributes, StringComparison.Ordinal);
        Assert.Contains("\"Scenario\"", attributes, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "Proves stays a plain attribute without xUnit v3")]
    public void XunitAbsentKeepsAPlainAttribute()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            RecordPages.Files("attribute", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecAttributes.g.cs", out var attributes), "The attributes source is missing.");
        Assert.DoesNotContain("ITraitAttribute", attributes, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "Attribute marks generate values from the generated attributes")]
    public void AttributeMarksGenerateValues()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                using LiteSpec;

                namespace Shop.Orders;

                public interface IDomainEvent;

                [Implements("event/expense.submitted")]
                public sealed partial record ExpenseSubmittedEvent(string ExpenseId) : IDomainEvent;
                """),
            RecordPages.Files("attribute", ("events.md", RecordPages.EventPage("event/expense.submitted"))));

        Assert.True(run.Sources.TryGetValue("LiteSpecEvent.Shop.Orders.ExpenseSubmittedEvent.g.cs", out var part), "The event part is missing.");
        Assert.Contains("public const string Classification = \"event/expense.submitted\";", part, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A project file with no record pages generates nothing")]
    public void NoRecordPagesGeneratesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("namespace Shop.Orders; internal sealed class Placeholder;"),
            RecordPages.Files("attribute"));

        Assert.Empty(run.Sources);
        Assert.Empty(run.ReportedDiagnostics);
    }
}
