using System.Linq;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Tests.Roslyn;

/// <summary>Tests for recognizing the methods that assign the state of their type.</summary>
public sealed class StateChangesTests
{
    /// <summary>A method assigning a field changes state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method assigning a field changes state")]
    public void AssigningAFieldChangesState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private bool erased;

                public void Erase()
                {
                    erased = true;
                }
            }
            """, "Erase");

        Assert.True(StateChanges.Changes(method, owner));
    }

    /// <summary>A method assigning a property through this changes state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method assigning a property through this changes state")]
    public void AssigningThroughThisChangesState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public string Status { get; private set; } = "open";

                public void Abandon(string reason)
                {
                    this.Status = "abandoned";
                }
            }
            """, "Abandon");

        Assert.True(StateChanges.Changes(method, owner));
    }

    /// <summary>A method incrementing a counter changes state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method incrementing a counter changes state")]
    public void IncrementingChangesState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private int attempts;

                public void Retry()
                {
                    attempts++;
                }
            }
            """, "Retry");

        Assert.True(StateChanges.Changes(method, owner));
    }

    /// <summary>A method assigning through a private helper changes state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method assigning through a private helper changes state")]
    public void AssigningThroughAPrivateHelperChangesState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private bool erased;

                public void Erase() => ApplyErasure();

                private void ApplyErasure()
                {
                    erased = true;
                }
            }
            """, "Erase");

        Assert.True(StateChanges.Changes(method, owner));
    }

    /// <summary>A method assigning only locals changes no state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method assigning only locals changes no state")]
    public void AssigningLocalsChangesNoState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private bool erased;

                public bool TryErase(bool allowed)
                {
                    var erased = false;
                    erased = allowed;
                    return erased;
                }
            }
            """, "TryErase");

        Assert.False(StateChanges.Changes(method, owner));
    }

    /// <summary>A reading method changes no state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A reading method changes no state")]
    public void ReadingChangesNoState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                public Money Total() => Money.Zero;
            }
            """, "Total");

        Assert.False(StateChanges.Changes(method, owner));
    }

    /// <summary>A method assigning a parameter of a field name changes no state.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact(DisplayName = "A method assigning a parameter of a field name changes no state")]
    public void AssigningAShadowedParameterChangesNoState()
    {
        var (method, owner) = MethodOf("""
            public sealed class Expense : AggregateRoot<ExpenseState>
            {
                private string status = "open";

                public void Check(string status)
                {
                    status = status.Trim();
                }
            }
            """, "Check");

        Assert.False(StateChanges.Changes(method, owner));
    }

    static (MethodDeclarationSyntax Method, TypeDeclarationSyntax Owner) MethodOf(string code, string name)
    {
        var tree = TestTrees.Parse(code);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .First(candidate => candidate.Identifier.ValueText == name);
        var owner = (TypeDeclarationSyntax)method.Parent!;

        return (method, owner);
    }
}
