using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the effective status inherits draft through decisions, requirements and use cases.</summary>
public sealed class AuthorityTests
{
    /// <summary>A requirement drafted by a draft decision makes its use case draft.</summary>
    [Fact]
    public void Draft_Decision_Makes_Use_Case_Draft()
    {
        var useCase = new FakeRecord(
            "use-case",
            "use-case/lending.borrow-copy",
            lists: new Dictionary<string, IReadOnlyList<string>>
            {
                ["fulfills"] = new[] { "requirement/borrowing.max-active-loans" },
            });
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "decision",
                "decision/0001-loan-period",
                "draft",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["decides"] = new[] { "requirement/borrowing.max-active-loans" },
                }),
            new FakeRecord("requirement", "requirement/borrowing.max-active-loans"),
            useCase,
            new FakeRecord("result", "result/borrow-copy.borrowed"),
        };

        var effective = Authority.EffectiveStatus(records, holder => holder.Type == "result" ? useCase : null);

        Assert.Equal("draft", effective["decision/0001-loan-period"]);
        Assert.Equal("draft", effective["requirement/borrowing.max-active-loans"]);
        Assert.Equal("draft", effective["use-case/lending.borrow-copy"]);
        Assert.Equal("draft", effective["result/borrow-copy.borrowed"]);
    }

    /// <summary>An approved decision leaves the requirement and its use case live.</summary>
    [Fact]
    public void Approved_Decision_Leaves_Use_Case_Live()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord(
                "decision",
                "decision/0001-loan-period",
                "approved",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["decides"] = new[] { "requirement/borrowing.max-active-loans" },
                }),
            new FakeRecord("requirement", "requirement/borrowing.max-active-loans"),
            new FakeRecord(
                "use-case",
                "use-case/lending.borrow-copy",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["fulfills"] = new[] { "requirement/borrowing.max-active-loans" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("approved", effective["decision/0001-loan-period"]);
        Assert.Equal("approved", effective["requirement/borrowing.max-active-loans"]);
        Assert.Equal("approved", effective["use-case/lending.borrow-copy"]);
    }
}
