using LiteSpec.Analyzers.Tests.Support;

namespace LiteSpec.Analyzers.Tests.Generation;

/// <summary>Tests for the generated rule exception Code.</summary>
public sealed class ExceptionValueTests
{
    [Fact(DisplayName = "An exception overrides Code when its base declares a virtual Code")]
    public void VirtualBaseCodeGeneratesAnOverride()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed partial class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "result/submit-expense.receipt-missing"),
                RecordPages.ResultEntry("result/submit-expense.receipt-missing", "refusal", "expense.receipt-missing")))));

        Assert.True(run.Sources.TryGetValue("LiteSpecException.Shop.Orders.ReceiptRequiredException.g.cs", out var part), "The exception part is missing.");
        Assert.Contains("public override string Code => \"expense.receipt-missing\";", part, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "An exception declares a plain Code when its base declares none")]
    public void MissingBaseCodeGeneratesAPlainProperty()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed partial class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "result/submit-expense.receipt-missing"),
                RecordPages.ResultEntry("result/submit-expense.receipt-missing", "refusal", "expense.receipt-missing")))));

        Assert.True(run.Sources.TryGetValue("LiteSpecException.Shop.Orders.ReceiptRequiredException.g.cs", out var part), "The exception part is missing.");
        Assert.Contains("public string Code => \"expense.receipt-missing\";", part, StringComparison.Ordinal);
        Assert.DoesNotContain("override", part, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A rule with a bare refusal code generates that code")]
    public void BareRefusalCodeGeneratesDirectly()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed partial class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "expense.receipt-missing")))));

        Assert.True(run.Sources.TryGetValue("LiteSpecException.Shop.Orders.ReceiptRequiredException.g.cs", out var part), "The exception part is missing.");
        Assert.Contains("public override string Code => \"expense.receipt-missing\";", part, StringComparison.Ordinal);
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A rule with no refusal takes no generated code")]
    public void RuleWithoutRefusalTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed partial class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", null)))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecException.", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "A non-partial exception takes no generated code")]
    public void NonPartialExceptionTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }

                    public override string Code => "expense.receipt-missing";
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "expense.receipt-missing")))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecException.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A hand-written code takes no generated value")]
    public void HandWrittenCodeTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.receipt-required</implements>
                public sealed partial class ReceiptRequiredException : DomainException
                {
                    public ReceiptRequiredException()
                        : base("A receipt is required.")
                    {
                    }

                    public override string Code => "expense.receipt-missing";
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "expense.receipt-missing")))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecException.", StringComparison.Ordinal));
        Assert.True(run.CompileErrors.Count == 0, "The generated code does not compile:\n" + string.Join("\n", run.CompileErrors));
    }

    [Fact(DisplayName = "A mark to an unknown rule takes no generated value")]
    public void UnknownRuleTakesNothing()
    {
        var run = GeneratorVerify.Run(
            RecordPages.Sources("""
                namespace Shop.Orders;

                public abstract class DomainException : System.Exception
                {
                    protected DomainException(string message)
                        : base(message)
                    {
                    }

                    public abstract string Code { get; }
                }

                /// <implements>rule/expense.unknown</implements>
                public sealed partial class UnknownException : DomainException
                {
                    public UnknownException()
                        : base("An unknown rule.")
                    {
                    }
                }
                """),
            RecordPages.Files("xml", ("rules.md", RecordPages.RuleSetPage(
                RecordPages.RuleEntry("rule/expense.receipt-required", "expense.receipt-missing")))));

        Assert.DoesNotContain(run.Sources.Keys, key => key.StartsWith("LiteSpecException.", StringComparison.Ordinal));
    }
}
