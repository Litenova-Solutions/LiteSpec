using System.Collections.Generic;
using LiteSpec.Model;

namespace LiteSpec.Model.Tests;

/// <summary>Proves the record index reads records by id and by type.</summary>
public sealed class RecordIndexTests
{
    /// <summary>ById reads the first record carrying the id, or null when none carries it.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ById_Reads_The_First_Record_With_The_Id()
    {
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord("use-case", "use-case/lending.borrow-copy"),
            new FakeRecord("result", "result/borrow-copy.borrowed"),
        ];

        var index = new RecordIndex(records);

        Assert.Same(records[1], index.ById("result/borrow-copy.borrowed"));
        Assert.Null(index.ById("result/borrow-copy.ghost"));
    }

    /// <summary>ByType reads every record of the type, in read order.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void ByType_Reads_Every_Record_Of_The_Type()
    {
        IReadOnlyList<IRecordFacts> records =
        [
            new FakeRecord("use-case", "use-case/lending.borrow-copy"),
            new FakeRecord("result", "result/borrow-copy.borrowed"),
            new FakeRecord("result", "result/borrow-copy.copy-unavailable"),
        ];

        var index = new RecordIndex(records);
        var results = index.ByType("result");

        Assert.Equal(2, results.Count);
        Assert.Same(records[1], results[0]);
        Assert.Same(records[2], results[1]);
        Assert.Empty(index.ByType("scenario"));
    }
}
