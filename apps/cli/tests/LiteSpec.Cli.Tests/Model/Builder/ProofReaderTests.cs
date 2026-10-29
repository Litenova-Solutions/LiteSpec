using System.Text.Json;
using LiteSpec.Tests.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Builder;

/// <summary>
///     Proves the proof shapes a shared harness writes confirm their claim: a probe table, a step that requests
///     a route itself, a then step that asserts inside its class, a use case observed after it runs in a given
///     step, and a use case set off by a producer whose effects issue it.
/// </summary>
public sealed class ProofReaderTests
{
    const string Extend = "scenario/extend-loan.extending-an-active-loan-succeeds";
    const string Return = "scenario/return-copy.returning-a-borrowed-copy-succeeds";
    const string Refusal = "scenario/borrow-copy.borrowing-an-unavailable-copy-refuses";

    /// <summary>A probe step whose argument keys a route of the use case's endpoint confirms.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Probe_Step_Confirms_The_Use_Case_Whose_Route_Its_Argument_Keys()
    {
        Assert.True(Confirmed(Extend, ProbeScenarios("loan extension"), ("Probe/ProbeSteps.cs", ProbeSteps)));
    }

    /// <summary>A probe step whose argument keys no route in the table confirms nothing.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Probe_Step_With_An_Unknown_Key_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Extend, ProbeScenarios("loan renewal"), ("Probe/ProbeSteps.cs", ProbeSteps)));
    }

    /// <summary>A when step fetched inline through Steps&lt;T&gt;() runs on the type argument's steps class.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Inline_Steps_Call_Reaches_Its_Type_Argument()
    {
        Assert.True(Confirmed(Return, InlineScenarios("ReturnCopySteps")));
    }

    /// <summary>An inline steps call of another use case's class confirms nothing.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Inline_Steps_Call_Of_Another_Class_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Return, InlineScenarios("ListLoansSteps")));
    }

    /// <summary>A step that requests the use case's endpoint route itself confirms.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Step_Requesting_The_Endpoint_Route_Confirms()
    {
        Assert.True(Confirmed(Return, DeskScenarios, ("Desk/DeskSteps.cs", DeskSteps("return"))));
    }

    /// <summary>A step that requests another endpoint's route confirms nothing.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Step_Requesting_Another_Route_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Return, DeskScenarios, ("Desk/DeskSteps.cs", DeskSteps("archive"))));
    }

    /// <summary>A then step that asserts the refusal's mapped status inside its class confirms the refusal.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Then_Step_Asserting_The_Mapped_Status_Inside_Its_Class_Confirms()
    {
        Assert.True(Confirmed(Refusal, RefusedScenarios, ("Answers/AnswerSteps.cs", AnswerSteps("Conflict"))));
    }

    /// <summary>A then step that asserts another status inside its class does not confirm the refusal.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Then_Step_Asserting_Another_Status_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Refusal, RefusedScenarios, ("Answers/AnswerSteps.cs", AnswerSteps("Forbidden"))));
    }

    /// <summary>A use case run in a given step and observed by a later when and then step confirms.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Use_Case_Observed_After_A_Given_Step_Confirms()
    {
        Assert.True(Confirmed(Extend, ObservedScenarios("new ExtendLoanSteps().ExtendTheLoan(\"loan-1\")")));
    }

    /// <summary>A given step of another use case observed later confirms nothing for the claimed one.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Another_Use_Case_In_The_Given_Step_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Extend, ObservedScenarios("new RegisterCopySteps().RegisterACopy(\"copy-1\")")));
    }

    /// <summary>A when step driving a use case whose effects issue the claimed use case confirms on an effect assertion.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Producer_Whose_Effects_Issue_The_Use_Case_Confirms()
    {
        Assert.True(Confirmed(Extend, ProducerScenarios, effects: true));
    }

    /// <summary>A when step driving a use case that declares no edge to the claimed one confirms nothing.</summary>
    /// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>
    [Fact]
    public void Producer_Without_An_Edge_Does_Not_Confirm()
    {
        Assert.False(Confirmed(Extend, ProducerScenarios, effects: false));
    }

    static bool Confirmed(string claim, string scenarios, (string Path, string Text)? steps = null, bool effects = false)
    {
        using var temp = ModelFixture.Prepare("small");
        Write(temp.Path, "docs/behavior/modules/lending/loans/extend-loan-proof-scenario.md", ScenarioPage(Extend, "Extending an active loan succeeds", "result/extend-loan.extended"));
        Write(temp.Path, "docs/behavior/modules/lending/loans/return-copy-proof-scenario.md", ScenarioPage(Return, "Returning a borrowed copy succeeds", "result/return-copy.returned"));
        Write(temp.Path, "docs/behavior/modules/lending/loans/borrow-copy-refusal-scenario.md", ScenarioPage(Refusal, "Borrowing an unavailable copy refuses", "result/borrow-copy.copy-unavailable"));
        Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/Harness/HarnessScenarios.cs", scenarios);
        WriteRefusals(temp.Path, """{"lending.copy-not-available": 409}""");

        if (steps is { } file)
        {
            Write(temp.Path, "apps/api/tests/Library.Acceptance.Tests/" + file.Path, file.Text);
        }

        if (effects)
        {
            var page = Path.Combine(temp.Path, "docs", "behavior", "modules", "lending", "loans", "borrow-copy.md");
            var text = File.ReadAllText(page);

            File.WriteAllText(page, text.Replace(
                "\"raises\": [",
                "\"effects\": [{\"issues\": \"use-case/lending.extend-loan\", \"timing\": \"after-commit\", \"reason\": \"A borrowed copy starts its first extension.\"}],\n  \"raises\": [",
                StringComparison.Ordinal));
        }

        using var document = Build(temp.Path);

        foreach (var link in document.RootElement.GetProperty("links").GetProperty("proves").EnumerateArray())
        {
            if (link.GetProperty("claim").GetString() == claim)
            {
                return link.GetProperty("confirmed").GetBoolean();
            }
        }

        throw new InvalidOperationException($"No proves link claims '{claim}'.");
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

    static void WriteRefusals(string root, string statuses)
    {
        var path = Path.Combine(root, "litespec.project.json");
        var text = File.ReadAllText(path);
        var refusals = "\"refusals\": {\"forbidden\": \"authorization.forbidden\", \"invalidInput\": \"request.validation-failed\", \"statuses\": " + statuses + "}";

        File.WriteAllText(path, text.Replace(@"""selectedExtensions"": []", $@"""selectedExtensions"": [], {refusals}"));
    }

    static string ProbeScenarios(string key) => $$"""
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>{{Extend}}</proves>
            public Task Extending_an_active_loan_succeeds() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => new ProbeSteps().the_reader_asks_for_a_RECORD("{{key}}"),
                then => new LoanSteps().TheLoanIsDueLater());
        }
        """;

    const string ProbeSteps = """
        namespace Library.Acceptance.Tests.Probe;

        public sealed class ProbeSteps
        {
            private static readonly Dictionary<string, Request> Requests = new(StringComparer.Ordinal)
            {
                ["loan extension"] = new Request("POST", "/api/lending/loans/{}/extend"),
            };

            public Task the_reader_asks_for_a_RECORD(string record) => Send(Requests[record]);

            private Task Send(Request request) => Task.CompletedTask;
        }
        """;

    static string InlineScenarios(string steps) => $$"""
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>scenario/return-copy.returning-a-borrowed-copy-succeeds</proves>
            public Task Returning_a_borrowed_copy_succeeds() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => Steps<{{steps}}>().ReturnTheCopy("loan-1"),
                then => new LoanSteps().TheLoanIsClosed());
        }
        """;

    const string DeskScenarios = """
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>scenario/return-copy.returning-a-borrowed-copy-succeeds</proves>
            public Task Returning_a_borrowed_copy_succeeds() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => new DeskSteps().the_desk_hands_the_copy_back("loan-1"),
                then => new LoanSteps().TheLoanIsClosed());
        }
        """;

    static string DeskSteps(string verb) => $$"""
        namespace Library.Acceptance.Tests.Desk;

        public sealed class DeskSteps
        {
            public Task the_desk_hands_the_copy_back(string loanId) => Post($"/api/lending/loans/{loanId}/{{verb}}");

            private Task Post(string route) => Task.CompletedTask;
        }
        """;

    const string RefusedScenarios = """
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>scenario/borrow-copy.borrowing-an-unavailable-copy-refuses</proves>
            public Task Borrowing_an_unavailable_copy_refuses() => Runner.RunScenarioAsync(
                given => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-2", "copy-1"),
                then => new AnswerSteps().the_copy_is_refused());
        }
        """;

    static string AnswerSteps(string status) => $$"""
        namespace Library.Acceptance.Tests.Answers;

        public sealed class AnswerSteps
        {
            public HttpStatusCode Status { get; set; }

            public void the_copy_is_refused() => Assert.Equal(HttpStatusCode.{{status}}, Status);
        }
        """;

    static string ObservedScenarios(string given) => $$"""
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>{{Extend}}</proves>
            public Task Extending_an_active_loan_succeeds() => Runner.RunScenarioAsync(
                given => {{given}},
                when => new ListLoansSteps().ListTheLoans("reader-1"),
                then => new ListLoansSteps().TheLoanIsDueLater());
        }
        """;

    const string ProducerScenarios = """
        namespace Library.Acceptance.Tests.Harness;

        public sealed class HarnessScenarios
        {
            /// <proves>scenario/extend-loan.extending-an-active-loan-succeeds</proves>
            public Task Extending_an_active_loan_succeeds() => Runner.RunScenarioAsync(
                given => new RegisterCopySteps().RegisterACopy("copy-1"),
                when => new BorrowCopySteps().BorrowACopy("reader-1", "copy-1"),
                then => new LoanSteps().TheLoanIsDueLater());
        }
        """;

    static string ScenarioPage(string id, string title, string result) => $$"""
        ---
        {
          "type": "scenario",
          "id": "{{id}}",
          "title": "{{title}}",
          "owner": "lending-team",
          "status": "reviewed",
          "given": "The library holds the copy.",
          "when": "The reader acts on it.",
          "result": "{{result}}",
          "then": "The library answers."
        }
        ---

        # {{title}}

        A scenario a shared harness proves.
        """;
}
