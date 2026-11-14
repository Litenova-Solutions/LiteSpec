using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec rename</c> moving the records homed to the renamed record to the new prefix.</summary>
public sealed class RenameEntriesTests
{
    const string Loans = ".litespec/behavior/modules/lending/aggregates/loans/use-cases";

    /// <summary>Renaming a use case renames its results and scenarios to the new prefix, and every citation of them follows.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a use case's results and scenarios to the new prefix")]
    public void RenameMovesAUseCaseEntriesToTheNewPrefix()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "use-case/lending.borrow-copy", "use-case/lending.borrow-item", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, Loans, "borrow-item", "borrow-item.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, Loans, "borrow-copy")));
        var rewritten = File.ReadAllText(moved);
        Assert.Contains("id: result/borrow-item.borrowed", rewritten);
        Assert.Contains("id: result/borrow-item.copy-unavailable", rewritten);
        Assert.Contains("id: scenario/borrow-item.borrowing-succeeds", rewritten);
        Assert.Contains("result: result/borrow-item.borrowed", rewritten);
        Assert.Contains("result: result/borrow-item.forbidden", rewritten);
        Assert.DoesNotContain("use-case/lending.borrow-copy", rewritten);
        Assert.DoesNotContain("result/borrow-copy", rewritten);
        Assert.DoesNotContain("scenario/borrow-copy", rewritten);

        var explanation = File.ReadAllText(Path.Combine(consumer.Path, Loans, "borrow-item", "borrow-item.md"));
        Assert.Contains("result/borrow-item.borrowed", explanation);
        Assert.Contains("scenario/borrow-item.borrowing-succeeds", explanation);
        Assert.DoesNotContain("borrow-copy", explanation);

        var notes = File.ReadAllText(Path.Combine(consumer.Path, ".litespec", "behavior", "modules", "lending", "notes.md"));
        Assert.Contains("scenario/borrow-item.borrowing-succeeds", notes);
        Assert.DoesNotContain("borrow-copy", notes);

        var proof = File.ReadAllText(Path.Combine(consumer.Path, "tests", "BorrowTests.cs"));
        Assert.Contains("<proves>scenario/borrow-item.borrowing-succeeds</proves>", proof);
        Assert.DoesNotContain("borrow-copy", proof);
    }

    /// <summary>Renaming a use case leaves the entries of another use case whose name merely starts with the old name alone.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename leaves entries whose home merely starts with the old name alone")]
    public void RenameLeavesLongerHomedEntriesAlone()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("rename", "use-case/lending.post", "use-case/lending.article", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, Loans, "article", "article.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.Contains("id: result/article.placed", File.ReadAllText(moved));
        Assert.DoesNotContain("result/post.placed", File.ReadAllText(moved));

        var neighbor = File.ReadAllText(Path.Combine(consumer.Path, Loans, "post-entry", "post-entry.yaml"));
        Assert.Contains("id: result/post-entry.placed", neighbor);
    }

    /// <summary>A consumer holding one use case with results and scenarios, two use cases sharing a name prefix, and files citing the entries.</summary>
    /// <returns>The temporary consumer folder.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("rename-entries");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/aggregates/loans/aggregate.yaml", """
            {
              "type": "aggregate",
              "id": "aggregate/lending.loans",
              "title": "Loans"
            }
            """);
        TestRecords.Write(temp.Path, $"{Loans}/borrow-copy/borrow-copy.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy",
              "kind": "command",
              "access": {
                "grant": "permission/borrow-copy",
                "tenant": "own"
              },
              "records": [
                {
                  "type": "result",
                  "id": "result/borrow-copy.borrowed",
                  "title": "Borrowed",
                  "outcome": "success",
                  "meaning": "The copy is lent."
                },
                {
                  "type": "result",
                  "id": "result/borrow-copy.copy-unavailable",
                  "title": "Copy unavailable",
                  "outcome": "refusal",
                  "code": "lending.copy-not-available",
                  "meaning": "The copy is unavailable."
                },
                {
                  "type": "scenario",
                  "id": "scenario/borrow-copy.borrowing-succeeds",
                  "title": "Borrowing succeeds",
                  "result": "result/borrow-copy.borrowed"
                },
                {
                  "type": "scenario",
                  "id": "scenario/borrow-copy.borrowing-without-the-grant-is-refused",
                  "title": "Borrowing without the grant is refused",
                  "result": "result/borrow-copy.forbidden"
                }
              ]
            }
            """);
        TestRecords.Explain(temp.Path, $"{Loans}/borrow-copy/borrow-copy.md", """
            # Borrow Copy

            A reader borrows a copy, ending in [borrowed](result/borrow-copy.borrowed) as [borrowing succeeds](scenario/borrow-copy.borrowing-succeeds) shows.
            """);
        TestRecords.Explain(temp.Path, ".litespec/behavior/modules/lending/notes.md", """
            # Notes

            See [borrowing succeeds](scenario/borrow-copy.borrowing-succeeds).
            """);
        var proof = Path.Combine(temp.Path, "tests", "BorrowTests.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(proof)!);
        File.WriteAllText(proof, "/// <proves>scenario/borrow-copy.borrowing-succeeds</proves>\ninternal sealed class BorrowTests;\n");
        TestRecords.Write(temp.Path, $"{Loans}/post/post.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.post",
              "title": "Post",
              "kind": "command",
              "records": [
                {
                  "type": "result",
                  "id": "result/post.placed",
                  "title": "Placed",
                  "outcome": "success",
                  "meaning": "The entry is placed."
                }
              ]
            }
            """);
        TestRecords.Write(temp.Path, $"{Loans}/post-entry/post-entry.yaml", """
            {
              "type": "use-case",
              "id": "use-case/lending.post-entry",
              "title": "Post entry",
              "kind": "command",
              "records": [
                {
                  "type": "result",
                  "id": "result/post-entry.placed",
                  "title": "Placed",
                  "outcome": "success",
                  "meaning": "The entry is placed."
                }
              ]
            }
            """);
        return temp;
    }
}
