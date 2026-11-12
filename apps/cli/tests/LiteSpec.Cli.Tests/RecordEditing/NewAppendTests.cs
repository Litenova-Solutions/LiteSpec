using System.Text.Json.Nodes;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.RecordEditing;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new result</c> and <c>litespec new scenario</c>.</summary>
public sealed class NewAppendTests
{
    const string UseCase = "use-case/lending.borrow-copy";

    const string OrderedJson = """
        {
          "type": "use-case",
          "id": "use-case/lending.borrow-copy",
          "title": "Borrow copy",
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
              "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
              "title": "Borrowing an available copy succeeds",
              "given": "A copy sits available.",
              "when": "The reader borrows the copy.",
              "then": "The loan is active.",
              "result": "result/borrow-copy.borrowed"
            }
          ]
        }
        """;

    /// <summary>New result appends a refusal after the existing refusals.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new result appends a refusal after the existing refusals")]
    public void AppendsRefusalAfterExistingRefusals()
    {
        using var fixture = ModelFixture.Prepare("large");
        var page = UseCasePage(fixture.Path);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "result", UseCase, "copy-on-loan",
            "--outcome", "refusal", "--code", "lending.copy-on-loan",
            "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended result/borrow-copy.copy-on-loan to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "id: result/borrow-copy.copy-on-loan");
        Assert.Contains("outcome: refusal", inserted);
        Assert.Contains("code: lending.copy-on-loan", inserted);

        var success = after.IndexOf("result/borrow-copy.borrowed", StringComparison.Ordinal);
        var oldRefusal = after.IndexOf("result/borrow-copy.copy-unavailable", StringComparison.Ordinal);
        var added = after.IndexOf("result/borrow-copy.copy-on-loan", StringComparison.Ordinal);
        var scenario = after.IndexOf(
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds", StringComparison.Ordinal);
        Assert.True(0 <= success && success < oldRefusal && oldRefusal < added && added < scenario);
    }

    /// <summary>New result appends a success before the refusals.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new result appends a success before the refusals")]
    public void AppendsSuccessBeforeRefusals()
    {
        using var fixture = ModelFixture.Prepare("small");
        var page = UseCasePage(fixture.Path);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "result", UseCase, "renewed", "--outcome", "success", "--root", fixture.Path);

        Assert.Equal(0, result.Status);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(before, after, "id: result/borrow-copy.renewed");
        Assert.Contains("outcome: success", inserted);
        Assert.DoesNotContain("code:", inserted);
        Assert.DoesNotContain("codes:", inserted);

        var success = after.IndexOf("result/borrow-copy.borrowed", StringComparison.Ordinal);
        var added = after.IndexOf("result/borrow-copy.renewed", StringComparison.Ordinal);
        var refusal = after.IndexOf("result/borrow-copy.copy-unavailable", StringComparison.Ordinal);
        Assert.True(0 <= success && success < added && added < refusal);
    }

    /// <summary>New scenario appends last.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new scenario appends last")]
    public void AppendsScenarioLast()
    {
        using var fixture = ModelFixture.Prepare("large");
        var page = UseCasePage(fixture.Path);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "scenario", UseCase, "borrowing-a-loaned-copy-is-refused",
            "--result", "result/borrow-copy.copy-unavailable",
            "--root", fixture.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Appended scenario/borrow-copy.borrowing-a-loaned-copy-is-refused to ", result.Stdout);

        var after = File.ReadAllText(page);
        var inserted = AssertSingleInsertion(
            before, after, "id: scenario/borrow-copy.borrowing-a-loaned-copy-is-refused");
        Assert.Contains("result: result/borrow-copy.copy-unavailable", inserted);

        var oldScenario = after.IndexOf(
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds", StringComparison.Ordinal);
        var added = after.IndexOf(
            "scenario/borrow-copy.borrowing-a-loaned-copy-is-refused", StringComparison.Ordinal);
        Assert.True(0 <= oldScenario && oldScenario < added);
    }

    /// <summary>New result refuses a duplicate id and leaves the page alone.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new result refuses a duplicate id and leaves the page alone")]
    public void RefusesDuplicateId()
    {
        using var fixture = ModelFixture.Prepare("small");
        var page = UseCasePage(fixture.Path);
        var before = File.ReadAllText(page);

        var result = TestCli.Run(
            "new", "result", UseCase, "borrowed", "--outcome", "success", "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(page));
    }

    /// <summary>New scenario refuses an unknown use case.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new scenario refuses an unknown use case")]
    public void RefusesUnknownUseCase()
    {
        using var fixture = ModelFixture.Prepare("small");

        var result = TestCli.Run(
            "new", "scenario", "use-case/lending.no-such-case", "something-happens",
            "--result", "result/borrow-copy.borrowed",
            "--root", fixture.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("No use case holds 'use-case/lending.no-such-case'", result.Output);
    }

    /// <summary>New help documents the append forms.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new help documents the append forms")]
    public void HelpDocumentsAppendForms()
    {
        var result = TestCli.Run("new", "--help");

        Assert.Equal(0, result.Status);
        Assert.Contains("new result <use-case-id> <name>", result.Stdout);
        Assert.Contains("new scenario <use-case-id> <name>", result.Stdout);
    }

    /// <summary>New on a page type refuses the append options.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new on a page type refuses the append options")]
    public void ScaffoldRefusesAppendOptions()
    {
        var result = TestCli.Run("new", "use-case", "lending.borrow-copy", "--code", "lending.x");

        Assert.Equal(2, result.Status);
        Assert.Contains("Option --code applies only to new result and new scenario", result.Output);
    }

    /// <summary>Titles derive from kebab names.</summary>
    /// <param name="name">The kebab-case entry name to convert.</param>
    /// <param name="title">The title the name converts to.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "titles derive from kebab names")]
    [InlineData("borrowed", "Borrowed")]
    [InlineData("copy-unavailable", "Copy unavailable")]
    [InlineData("borrowing-an-available-copy-succeeds", "Borrowing an available copy succeeds")]
    public void TitlesDeriveFromNames(string name, string title) =>
        Assert.Equal(title, AppendEntry.TitleFrom(name));

    /// <summary>Insert places a refusal after the existing refusals.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert places a refusal after the existing refusals")]
    public void InsertPlacesRefusalAfterRefusals()
    {
        var entry = new JsonObject
        {
            ["type"] = "result",
            ["id"] = "result/borrow-copy.copy-on-loan",
            ["title"] = "Copy on loan",
            ["outcome"] = "refusal",
            ["code"] = "lending.copy-on-loan",
            ["meaning"] = "The copy is on loan.",
        };
        var before = RecordFile.Write(JsonNode.Parse(OrderedJson)!);

        var updated = AppendEntry.Insert(before, AppendEntry.Placement.Refusal, entry, out var error);

        AssertInserts(before, updated, error, entry,
        [
            "result/borrow-copy.borrowed",
            "result/borrow-copy.copy-unavailable",
            "result/borrow-copy.copy-on-loan",
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
        ]);
    }

    /// <summary>Insert places a success before the refusals.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert places a success before the refusals")]
    public void InsertPlacesSuccessBeforeRefusals()
    {
        var entry = new JsonObject
        {
            ["type"] = "result",
            ["id"] = "result/borrow-copy.renewed",
            ["title"] = "Renewed",
            ["outcome"] = "success",
            ["meaning"] = "The loan is renewed.",
        };
        var before = RecordFile.Write(JsonNode.Parse(OrderedJson)!);

        var updated = AppendEntry.Insert(before, AppendEntry.Placement.Success, entry, out var error);

        AssertInserts(before, updated, error, entry,
        [
            "result/borrow-copy.borrowed",
            "result/borrow-copy.renewed",
            "result/borrow-copy.copy-unavailable",
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
        ]);
    }

    /// <summary>Insert appends a scenario last.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert appends a scenario last")]
    public void InsertAppendsScenarioLast()
    {
        var entry = new JsonObject
        {
            ["type"] = "scenario",
            ["id"] = "scenario/borrow-copy.borrowing-a-loaned-copy-is-refused",
            ["title"] = "Borrowing a loaned copy is refused",
            ["given"] = "The copy sits on loan.",
            ["when"] = "The reader borrows the copy.",
            ["then"] = "The borrow is refused.",
            ["result"] = "result/borrow-copy.copy-unavailable",
        };
        var before = RecordFile.Write(JsonNode.Parse(OrderedJson)!);

        var updated = AppendEntry.Insert(before, AppendEntry.Placement.Scenario, entry, out var error);

        AssertInserts(before, updated, error, entry,
        [
            "result/borrow-copy.borrowed",
            "result/borrow-copy.copy-unavailable",
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
            "scenario/borrow-copy.borrowing-a-loaned-copy-is-refused",
        ]);
    }

    /// <summary>Insert fills an empty records array.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert fills an empty records array")]
    public void InsertFillsEmptyRecords()
    {
        const string pageJson = """
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy",
              "records": []
            }
            """;
        var entry = new JsonObject
        {
            ["type"] = "result",
            ["id"] = "result/borrow-copy.borrowed",
            ["title"] = "Borrowed",
            ["outcome"] = "success",
            ["meaning"] = "The copy is lent.",
        };
        var before = RecordFile.Write(JsonNode.Parse(pageJson)!);

        var updated = AppendEntry.Insert(before, AppendEntry.Placement.Success, entry, out var error);

        AssertInserts(before, updated, error, entry, ["result/borrow-copy.borrowed"]);
    }

    /// <summary>Insert writes a missing records array.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert writes a missing records array")]
    public void InsertWritesMissingRecords()
    {
        const string pageJson = """
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy"
            }
            """;
        var entry = new JsonObject
        {
            ["type"] = "result",
            ["id"] = "result/borrow-copy.borrowed",
            ["title"] = "Borrowed",
            ["outcome"] = "success",
            ["meaning"] = "The copy is lent.",
        };
        var before = RecordFile.Write(JsonNode.Parse(pageJson)!);

        var updated = AppendEntry.Insert(before, AppendEntry.Placement.Success, entry, out var error);

        var expectedNode = (JsonObject)JsonNode.Parse(pageJson)!;
        expectedNode["records"] = new JsonArray((JsonNode)entry.DeepClone());

        Assert.True(error == "", error);
        Assert.Equal(RecordFile.Write(expectedNode), updated);
    }

    /// <summary>Insert refuses a records field that is not an array.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert refuses a records field that is not an array")]
    public void InsertRefusesNonArrayRecords()
    {
        var page = RecordFile.Write(JsonNode.Parse("""
            {
              "type": "use-case",
              "id": "use-case/lending.borrow-copy",
              "title": "Borrow copy",
              "records": {}
            }
            """)!);

        var updated = AppendEntry.Insert(page, AppendEntry.Placement.Success, new JsonObject(), out var error);

        Assert.Null(updated);
        Assert.Contains("not an array", error);
    }

    /// <summary>Insert refuses a file that holds no record.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "insert refuses a file that holds no record")]
    public void InsertRefusesFileWithoutRecord()
    {
        var updated = AppendEntry.Insert("# Just prose\n", AppendEntry.Placement.Success, new JsonObject(), out var error);

        Assert.Null(updated);
        Assert.Contains("holds no record", error);
    }

    /// <summary>Run writes a result through the model lookup.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "run writes a result through the model lookup")]
    public void RunWritesResult()
    {
        using var consumer = Consumer(OrderedJson);

        var status = Append(
            consumer,
            "result", UseCase, "copy-on-loan",
            "--outcome", "refusal", "--code", "lending.copy-on-loan");

        Assert.Equal(0, status);

        var after = File.ReadAllText(UseCasePage(consumer.Path));
        Assert.Contains("id: result/borrow-copy.copy-on-loan", after);
        Assert.Contains("title: Copy on loan", after);
    }

    /// <summary>Run accepts the use case as home and name.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "run accepts the use case as home and name")]
    public void RunAcceptsBareUseCaseName()
    {
        using var consumer = Consumer(OrderedJson);

        var status = Append(consumer, "result", "lending.borrow-copy", "renewed", "--outcome", "success");

        Assert.Equal(0, status);
        Assert.Contains("id: result/borrow-copy.renewed", File.ReadAllText(UseCasePage(consumer.Path)));
    }

    /// <summary>Run accepts a derived result reference.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "run accepts a derived result reference")]
    public void RunAcceptsDerivedResultRef()
    {
        using var consumer = Consumer(OrderedJson);

        var status = Append(
            consumer,
            "scenario", UseCase, "borrowing-without-a-grant-is-refused",
            "--result", "result/borrow-copy.forbidden");

        Assert.Equal(0, status);
        Assert.Contains(
            "result: result/borrow-copy.forbidden",
            File.ReadAllText(UseCasePage(consumer.Path)));
    }

    /// <summary>Run carries a success code as codes.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "run carries a success code as codes")]
    public void RunCarriesSuccessCodeAsCodes()
    {
        using var consumer = Consumer(OrderedJson);
        var before = File.ReadAllText(UseCasePage(consumer.Path));

        var status = Append(
            consumer, "result", UseCase, "renewed", "--outcome", "success", "--code", "lending.renewed");

        Assert.Equal(0, status);

        var after = File.ReadAllText(UseCasePage(consumer.Path));
        Assert.Contains("codes:", after);
        Assert.Contains("- lending.renewed", after);
        Assert.Equal(CountOccurrences(before, "code:"), CountOccurrences(after, "code:"));
    }

    public static TheoryData<string[]> RejectedArgs() => new()
    {
        { ["result", UseCase, "copy-on-loan"] },
        { ["result", UseCase, "copy-on-loan", "--outcome", "maybe", "--code", "lending.x"] },
        { ["result", UseCase, "copy-on-loan", "--outcome", "refusal"] },
        { ["result", UseCase, "copy-on-loan", "--outcome", "refusal", "--code", "lending"] },
        { ["result", UseCase, "copy-on-loan", "--outcome", "refusal", "--code", "Lending.x"] },
        { ["result", UseCase, "copy-on-loan", "--outcome", "success", "--result", "result/borrow-copy.borrowed"] },
        { ["scenario", UseCase, "something-happens"] },
        { ["scenario", UseCase, "something-happens", "--result", "banana"] },
        { ["scenario", UseCase, "something-happens", "--result", "scenario/borrow-copy.x"] },
        { ["scenario", UseCase, "something-happens", "--result", "result/borrow-copy.borrowed", "--code", "lending.x"] },
        { ["scenario", UseCase, "something-happens", "--result", "result/borrow-copy.borrowed", "--outcome", "success"] },
        { ["result", UseCase, "Bad_Name", "--outcome", "success"] },
        { ["result", UseCase, "123", "--outcome", "success"] },
        { ["result", UseCase, "a-b-c-d-e-f-g", "--outcome", "success"] },
        { ["result", "lending", "copy-on-loan", "--outcome", "refusal", "--code", "lending.x"] },
        { ["result", "reaction/lending.notify", "copy-on-loan", "--outcome", "refusal", "--code", "lending.x"] },
        { ["result", "use-case/lending.no-such-case", "copy-on-loan", "--outcome", "refusal", "--code", "lending.x"] },
        { ["result", UseCase, "borrowed", "--outcome", "success"] },
        { ["result", UseCase] },
    };

    /// <summary>Run refuses invalid appends and leaves the page alone.</summary>
    /// <param name="args">One rejected argument set from <see cref="RejectedArgs" />.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "run refuses invalid appends and leaves the page alone")]
    [MemberData(nameof(RejectedArgs))]
    public void RunRefusesInvalidAppends(string[] args)
    {
        using var consumer = Consumer(OrderedJson);
        var before = File.ReadAllText(UseCasePage(consumer.Path));

        Assert.Equal(2, Append(consumer, args));
        Assert.Equal(before, File.ReadAllText(UseCasePage(consumer.Path)));
    }

    /// <summary>The use-case record file of the fixture or consumer.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <returns>The full path to the borrow-copy use-case record file under the root.</returns>
    static string UseCasePage(string root) =>
        Path.Combine(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml");

    /// <summary>A minimal consumer holding one use-case record file.</summary>
    /// <param name="pageJson">The record file's fields as one JSON object.</param>
    /// <returns>The temporary consumer folder holding the record file.</returns>
    static TempFolder Consumer(string pageJson)
    {
        var temp = TestCli.Temp("new-append");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, ".litespec/behavior/modules/lending/loans/borrow-copy.yaml", pageJson);
        return temp;
    }

    /// <summary>Runs the append path in process against the consumer.</summary>
    /// <param name="consumer">The fixture consumer to append to.</param>
    /// <param name="args">The append arguments, without the root option the helper adds.</param>
    /// <returns>The append run exit code.</returns>
    static int Append(TempFolder consumer, params string[] args)
    {
        var parsed = CliArgs.Parse(
            [.. args, "--root", consumer.Path],
            [],
            ["--root", "--code", "--outcome", "--result"],
            maxPositionals: 3);
        Assert.Null(parsed.Error);
        return AppendEntry.Run(parsed);
    }

    /// <summary>Proves the page grew by one contiguous insertion holding the new entry.</summary>
    /// <param name="before">The page text before the insertion.</param>
    /// <param name="after">The page text after the insertion.</param>
    /// <param name="marker">The text the inserted block must contain.</param>
    /// <returns>The inserted text.</returns>
    static string AssertSingleInsertion(string before, string after, string marker)
    {
        // Lines, not characters: a new entry shares its opening lines with its neighbour,
        // and a character scan would cut the insertion mid-line.
        // A CRLF checkout gives the fixture CRLF line ends; the tool writes LF.
        // The tool writes canonical form, so a fixture that drifted is normalized first:
        // the proof is that the canonical file grows by one insertion, not that the
        // drifted file does.
        var normalized = RecordFile.Canonical(before) ?? before;
        var old = normalized.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var grown = after.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var prefix = 0;

        while (prefix < old.Length && prefix < grown.Length && old[prefix] == grown[prefix])
        {
            prefix++;
        }

        var suffix = 0;

        while (suffix < old.Length - prefix && suffix < grown.Length - prefix
            && old[old.Length - 1 - suffix] == grown[grown.Length - 1 - suffix])
        {
            suffix++;
        }

        Assert.Equal(old.Length, prefix + suffix);

        var inserted = string.Join('\n', grown[prefix..(grown.Length - suffix)]);
        Assert.Contains(marker, inserted);

        return inserted;
    }

    /// <summary>Proves the insert wrote the entry in canonical form, in house order, and changed nothing else.</summary>
    /// <param name="before">The record file text before the insertion, in canonical form.</param>
    /// <param name="updated">The record file text after the insertion, or null when the insert failed.</param>
    /// <param name="error">The insert error text, empty when the insert succeeded.</param>
    /// <param name="entry">The entry the insert must add with every field intact.</param>
    /// <param name="orderedIds">The record identifiers in the order the file must hold them after the insert.</param>
    static void AssertInserts(string before, string? updated, string error, JsonObject entry, string[] orderedIds)
    {
        Assert.True(error == "", error);
        Assert.NotNull(updated);
        Assert.Equal(updated, RecordFile.Canonical(updated!));
        Assert.True(RecordFile.TryParse(updated!, out var root, out _, out var parseError), parseError);

        var records = root.GetProperty("records");
        Assert.Equal(
            orderedIds,
            records.EnumerateArray().Select(item => item.GetProperty("id").GetString()!).ToArray());

        var id = entry["id"]!.GetValue<string>();
        var actual = records.EnumerateArray().First(item => item.GetProperty("id").GetString() == id);
        Assert.Equal(entry.Count, actual.EnumerateObject().Count());

        foreach (var (key, value) in entry)
        {
            Assert.Equal(value!.GetValue<string>(), actual.GetProperty(key).GetString());
        }

        var node = (JsonObject)JsonNode.Parse(root.GetRawText())!;
        var items = (JsonArray)node["records"]!;

        for (var index = items.Count - 1; index >= 0; index--)
        {
            if (items[index]?["id"]?.GetValue<string>() == id)
            {
                items.RemoveAt(index);
            }
        }

        Assert.Equal(before, RecordFile.Write(node));
    }

    /// <summary>Counts the needle's occurrences in the text.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="needle">The substring to count.</param>
    /// <returns>The number of non-overlapping occurrences.</returns>
    static int CountOccurrences(string text, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
