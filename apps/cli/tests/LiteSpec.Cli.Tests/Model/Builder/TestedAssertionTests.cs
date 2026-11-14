using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Builder;

/// <summary>Proves a proof target is tested only by a passing test that asserts something.</summary>
public sealed class TestedAssertionTests
{
    const string Requirement = "requirement/performance.borrow-latency";

    /// <summary>A passing test with an assertion marks its quality requirement tested.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingTestWithAnAssertionTestsItsRequirement()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, RequirementPath, RequirementJson);
        Write(temp.Path, ProofPath, AssertingFile);
        WriteTrx(temp.Path, "Library.Integration.Tests.Tested.TestedProofTests.BorrowingStaysFast");

        using var document = Build(temp.Path);
        var link = ProvesLink(document.RootElement, Requirement);

        Assert.True(link.GetProperty("confirmed").GetBoolean());
        Assert.True(link.GetProperty("passing").GetBoolean());
    }

    /// <summary>A passing test without an assertion leaves its quality requirement untested.</summary>
    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void PassingTestWithoutAnAssertionLeavesItsRequirementUntested()
    {
        using var temp = ModelFixture.Prepare("small");
        TestRecords.Write(temp.Path, RequirementPath, RequirementJson);
        Write(temp.Path, ProofPath, BareFile);
        WriteTrx(temp.Path, "Library.Integration.Tests.Tested.TestedProofTests.BorrowingRuns");

        using var document = Build(temp.Path);
        var link = ProvesLink(document.RootElement, Requirement);

        Assert.False(link.GetProperty("confirmed").GetBoolean());
    }

    const string RequirementPath = ".litespec/intent/problems/slow-desk-loans/features/performance/requirements/borrow-latency/borrow-latency.yaml";
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
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return JsonDocument.Parse(model.ToJson());
    }

    static JsonElement ProvesLink(JsonElement root, string claim)
    {
        foreach (var link in root.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link;
            }
        }

        throw new InvalidOperationException($"No proves link claims '{claim}'.");
    }

    static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    const string RequirementJson = """
        {
          "type": "requirement",
          "id": "requirement/performance.borrow-latency",
          "title": "Borrow latency",
          "kind": "quality",
          "statement": "Borrowing answers fast.",
          "rationale": "Readers leave when borrowing is slow.",
          "cites": ["goal/more-borrowing"]
        }
        """;

    const string AssertingFile = """
        namespace Library.Integration.Tests.Tested;

        public sealed class TestedProofTests
        {
            /// <proves>requirement/performance.borrow-latency</proves>
            [Fact]
            public async Task BorrowingStaysFast()
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
            public async Task BorrowingRuns()
            {
                await gateway.BorrowAsync();
            }
        }
        """;
}
