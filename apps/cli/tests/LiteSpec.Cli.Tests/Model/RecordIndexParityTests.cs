using LiteSpec.Model;
using LiteSpec.Tests.Model.Diff;

namespace LiteSpec.Tests.Model;

/// <summary>Proves the record index reads every fixture page exactly like the CLI reader.</summary>
public sealed class RecordIndexParityTests
{
    /// <summary>Every non-derived record from a Markdown page appears in the index unchanged.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Index_Matches_Cli_Reader_On_Every_Consumer_Fixture()
    {
        foreach (var fixture in new[] { "small", "large" })
        {
            using var temp = ModelFixture.Prepare(fixture);
            CheckRoot(temp.Path);
        }

        var pair = DiffFixture.Prepare();
        using (pair.Base)
        using (pair.Head)
        {
            CheckRoot(pair.Base.Path);
        }
    }

    static void CheckRoot(string root)
    {
        var config = ConsumerConfig.Load(root);
        var read = RecordReader.Read(config);
        var pages = new List<IRecordFacts>();

        foreach (var recordRoot in config.RecordRoots)
        {
            foreach (var file in SourceTree.Files(recordRoot, ".md"))
            {
                pages.AddRange(RecordFiles.Read(file, SourceTree.Read(file)));
            }
        }

        var index = new RecordIndex(pages);
        var written = read.Records
            .Where(record => !record.Derived && record.File.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(written.Count, index.Records.Count);

        foreach (var record in written)
        {
            var match = index.Records.Single(candidate =>
                candidate.File == record.File && candidate.Line == record.Line && candidate.Id == record.Id);

            Assert.Equal(record.Type, match.Type);
            Assert.Equal(record.Id, match.Id);
            Assert.Equal(record.Status, match.Status);
            Assert.Equal(record.File, match.File);
            Assert.Equal(record.Line, match.Line);
        }
    }
}
