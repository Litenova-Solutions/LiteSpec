using System.Text.Json;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>Proves a proof target is tested only by a passing test that asserts something.</summary>
public sealed class TestedAssertionTests
{
    const string Requirement = "requirement/performance.borrow-latency";

    /// <summary>A passing test with an assertion marks its quality requirement tested.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passing_Test_With_An_Assertion_Tests_Its_Requirement()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementPath, RequirementPage);
        Write(temp.Path, ProofPath, AssertingFile);
        WriteTrx(temp.Path, "Library.Integration.Tests.Tested.TestedProofTests.Borrowing_stays_fast");

        using var document = Build(temp.Path);

        Assert.True(document.RootElement.GetProperty("tested").GetProperty(Requirement).GetBoolean());
    }

    /// <summary>A passing test without an assertion leaves its quality requirement untested.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Passing_Test_Without_An_Assertion_Leaves_Its_Requirement_Untested()
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, RequirementPath, RequirementPage);
        Write(temp.Path, ProofPath, BareFile);
        WriteTrx(temp.Path, "Library.Integration.Tests.Tested.TestedProofTests.Borrowing_runs");

        using var document = Build(temp.Path);

        Assert.False(document.RootElement.GetProperty("tested").GetProperty(Requirement).GetBoolean());
    }

    const string RequirementPath = "docs/intent/lending/performance.md";
    const string ProofPath = "apps/api/tests/Library.Integration.Tests/Tested/TestedProofTests.cs";

    static void WriteTrx(string root, string testName)
    {
        var folder = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "results.trx"),
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<TestRun name=\"tested run\" xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">\n"
            + "  <Results>\n"
            + $"    <UnitTestResult testName=\"{testName}\" outcome=\"Passed\" />\n"
            + "  </Results>\n"
            + "</TestRun>\n");
    }

    static JsonDocument Build(string root)
    {
        var output = Path.Combine(root, ".model", "model.json");
        var result = Cli.Run("model", "build", "--root", root, "--out", output);
        Assert.Equal(0, result.Status);

        return JsonDocument.Parse(File.ReadAllText(output));
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string RequirementPage = """
        ---
        {
          "type": "topic",
          "id": "topic/performance",
          "title": "Borrowing performance",
          "owner": "lending-team",
          "status": "approved",
          "reviewBy": "2027-04-01",
          "records": [
            {
              "type": "requirement",
              "id": "requirement/performance.borrow-latency",
              "title": "Borrow latency",
              "owner": "lending-team",
              "status": "approved",
              "kind": "quality",
              "statement": "Borrowing answers fast.",
              "rationale": "Readers leave when borrowing is slow.",
              "cites": ["goal/more-borrowing"]
            }
          ]
        }
        ---

        # Borrowing performance

        Borrowing stays fast.
        """;

    const string AssertingFile = """
        namespace Library.Integration.Tests.Tested;

        public sealed class TestedProofTests
        {
            /// <proves>requirement/performance.borrow-latency</proves>
            [Fact]
            public async Task Borrowing_stays_fast()
            {
                var outcome = await gateway.BorrowAsync();
                Assert.NotNull(outcome);
            }
        }
        """;

    const string BareFile = """
        namespace Library.Integration.Tests.Tested;

        public sealed class TestedProofTests
        {
            /// <proves>requirement/performance.borrow-latency</proves>
            [Fact]
            public async Task Borrowing_runs()
            {
                await gateway.BorrowAsync();
            }
        }
        """;
}
