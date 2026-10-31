using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves a result takes its holding use case's effective status through the name index.</summary>
public sealed class RecordIndexTests
{
    /// <summary>A result held by a draft use case is effectively a draft.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Result_Of_Draft_Use_Case_Is_Draft()
    {
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord("use-case", "use-case/lending.borrow-copy", "draft"),
            new FakeRecord("result", "result/borrow-copy.borrowed"),
        ];

        var index = new RecordIndex(records);

        Assert.Equal("draft", index.EffectiveStatus["result/borrow-copy.borrowed"]);
    }

    /// <summary>A result with no holding use case keeps its own status.</summary>
    /// <proves>litespec/core/model#hold-approval-behind-draft-decisions</proves>
    [Fact]
    public void Result_Without_Holder_Keeps_Own_Status()
    {
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord("use-case", "use-case/lending.borrow-copy", "draft"),
            new FakeRecord("result", "result/renew-copy.renewed"),
        ];

        var index = new RecordIndex(records);

        Assert.Equal("approved", index.EffectiveStatus["result/renew-copy.renewed"]);
    }
}
