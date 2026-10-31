using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the effective status inherits draft through decisions, requirements and use cases.</summary>
public sealed class AuthorityTests
{
    /// <summary>A requirement drafted by a draft decision makes its use case draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
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
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
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

    /// <summary>A topic takes its own status, like every other Intent record.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Topic_Takes_Its_Own_Status()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("topic", "topic/borrowing", "approved"),
            new FakeRecord("topic", "topic/holds", "draft"),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("approved", effective["topic/borrowing"]);
        Assert.Equal("draft", effective["topic/holds"]);
    }

    /// <summary>An exception takes its own status, like every other Governance record.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Exception_Takes_Its_Own_Status()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("exception", "exception/list-loans-unimplemented", "approved"),
            new FakeRecord("exception", "exception/export-review-bypass", "draft"),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("approved", effective["exception/list-loans-unimplemented"]);
        Assert.Equal("draft", effective["exception/export-review-bypass"]);
    }

    /// <summary>A draft screen is draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Draft_Screen_Is_Draft()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("entry-point", "entry-point/claimwick-app", "reviewed"),
            new FakeRecord("use-case", "use-case/spending.list-expenses", "reviewed"),
            new FakeRecord("journey", "journey/submit-and-reimburse"),
            new FakeRecord(
                "screen",
                "screen/claimwick-app.expense-list",
                "draft",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["entryPoint"] = new[] { "entry-point/claimwick-app" },
                    ["offers"] = new[] { "use-case/spending.list-expenses" },
                    ["serves"] = new[] { "journey/submit-and-reimburse#s1" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("draft", effective["screen/claimwick-app.expense-list"]);
    }

    /// <summary>A reviewed screen offering a draft use case is draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Draft_Use_Case_Makes_Screen_Draft()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("entry-point", "entry-point/claimwick-app", "reviewed"),
            new FakeRecord("use-case", "use-case/spending.list-expenses", "draft"),
            new FakeRecord("journey", "journey/submit-and-reimburse"),
            new FakeRecord(
                "screen",
                "screen/claimwick-app.expense-list",
                "reviewed",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["entryPoint"] = new[] { "entry-point/claimwick-app" },
                    ["offers"] = new[] { "use-case/spending.list-expenses" },
                    ["serves"] = new[] { "journey/submit-and-reimburse#s1" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("draft", effective["screen/claimwick-app.expense-list"]);
    }

    /// <summary>A reviewed screen serving a draft journey is draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Draft_Journey_Makes_Screen_Draft()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("entry-point", "entry-point/claimwick-app", "reviewed"),
            new FakeRecord("use-case", "use-case/spending.list-expenses", "reviewed"),
            new FakeRecord("journey", "journey/submit-and-reimburse", "draft"),
            new FakeRecord(
                "screen",
                "screen/claimwick-app.expense-list",
                "reviewed",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["entryPoint"] = new[] { "entry-point/claimwick-app" },
                    ["offers"] = new[] { "use-case/spending.list-expenses" },
                    ["serves"] = new[] { "journey/submit-and-reimburse#s1" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("draft", effective["screen/claimwick-app.expense-list"]);
    }

    /// <summary>A reviewed screen on a draft entry point is draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Draft_Entry_Point_Makes_Screen_Draft()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("entry-point", "entry-point/claimwick-app", "draft"),
            new FakeRecord("use-case", "use-case/spending.list-expenses", "reviewed"),
            new FakeRecord("journey", "journey/submit-and-reimburse"),
            new FakeRecord(
                "screen",
                "screen/claimwick-app.expense-list",
                "reviewed",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["entryPoint"] = new[] { "entry-point/claimwick-app" },
                    ["offers"] = new[] { "use-case/spending.list-expenses" },
                    ["serves"] = new[] { "journey/submit-and-reimburse#s1" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("draft", effective["screen/claimwick-app.expense-list"]);
    }

    /// <summary>A reviewed screen with live parents stays reviewed.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Live_Parents_Keep_Screen_Live()
    {
        IReadOnlyList<IRecordFacts> records = new IRecordFacts[]
        {
            new FakeRecord("entry-point", "entry-point/claimwick-app", "reviewed"),
            new FakeRecord("use-case", "use-case/spending.list-expenses", "reviewed"),
            new FakeRecord("journey", "journey/submit-and-reimburse"),
            new FakeRecord(
                "screen",
                "screen/claimwick-app.expense-list",
                "reviewed",
                lists: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["entryPoint"] = new[] { "entry-point/claimwick-app" },
                    ["offers"] = new[] { "use-case/spending.list-expenses" },
                    ["serves"] = new[] { "journey/submit-and-reimburse#s1" },
                }),
        };

        var effective = Authority.EffectiveStatus(records, _ => null);

        Assert.Equal("reviewed", effective["screen/claimwick-app.expense-list"]);
    }
}
