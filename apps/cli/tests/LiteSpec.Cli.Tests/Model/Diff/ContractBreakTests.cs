using System.Diagnostics;
using System.Text.Json;
using LiteSpec.Cli.Model.Diff;
using LiteSpec.Cli.Tests.Model.Checks;
using LiteSpec.Cli.Tests.Support;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Diff;

/// <summary>Proves <c>model check --since</c> reports breaking contract changes as findings.</summary>
public sealed class ContractBreakTests
{
    /// <summary>A result the head removed reports contract-break on its use case.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RemovedResultReportsContractBreak()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var useCase = TestModel.Record("use-case", "use-case/lending.borrow-copy", file, """{"kind": "command"}""");
        var result = TestModel.Record(
            "result", "result/borrow-copy.borrowed", file,
            """{"outcome": "success", "meaning": "The copy is lent."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, useCase, result),
            TestModel.Model(config, useCase));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Equal("result/borrow-copy.borrowed", finding.Subject);
        Assert.Equal(file, finding.Path);
        Assert.Contains("no longer exists", finding.Message);
    }

    /// <summary>A refusal code the head removed reports contract-break naming the code.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RemovedRefusalCodeReportsContractBreak()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var before = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "code": "lending.copy-not-available", "meaning": "The copy is on loan."}""");
        var after = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "meaning": "The copy is on loan."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config, after));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Equal("result/borrow-copy.copy-unavailable", finding.Subject);
        Assert.Contains("lending.copy-not-available", finding.Message);
        Assert.Contains("no longer exists", finding.Message);
    }

    /// <summary>A refusal code the head changed reports contract-break naming both codes.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void ChangedRefusalCodeReportsContractBreak()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var before = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "code": "lending.copy-not-available", "meaning": "The copy is on loan."}""");
        var after = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "code": "lending.copy-on-loan", "meaning": "The copy is on loan."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config, after));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Contains("lending.copy-not-available", finding.Message);
        Assert.Contains("lending.copy-on-loan", finding.Message);
    }

    /// <summary>A refusal code the head added reports nothing: only removals break clients.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void AddedRefusalCodeReportsNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var before = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "meaning": "The copy is on loan."}""");
        var after = TestModel.Record(
            "result", "result/borrow-copy.copy-unavailable", file,
            """{"outcome": "refusal", "code": "lending.copy-not-available", "meaning": "The copy is on loan."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config, after));

        Assert.Empty(findings);
    }

    /// <summary>An integration event whose payload changed reports contract-break.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void ChangedIntegrationEventReportsContractBreak()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "loan-shipped.yaml");
        var before = TestModel.Record(
            "integration-event", "integration-event/loan-shipped", file,
            """{"direction": "out", "eventType": "library.loan.shipped", "payload": [{"name": "loanId"}]}""");
        var after = TestModel.Record(
            "integration-event", "integration-event/loan-shipped", file,
            """{"direction": "out", "eventType": "library.loan.shipped", "payload": [{"name": "loanId"}, {"name": "copyId"}]}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config, after));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Equal("integration-event/loan-shipped", finding.Subject);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>An integration event with only its title changed reports nothing.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RetitledIntegrationEventReportsNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "loan-shipped.yaml");
        var before = TestModel.Record(
            "integration-event", "integration-event/loan-shipped", file,
            """{"title": "Loan shipped", "direction": "out", "eventType": "library.loan.shipped"}""");
        var after = TestModel.Record(
            "integration-event", "integration-event/loan-shipped", file,
            """{"title": "Loan is shipped", "direction": "out", "eventType": "library.loan.shipped"}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config, after));

        Assert.Empty(findings);
    }

    /// <summary>An integration event the head removed reports contract-break.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RemovedIntegrationEventReportsContractBreak()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "loan-shipped.yaml");
        var before = TestModel.Record(
            "integration-event", "integration-event/loan-shipped", file,
            """{"direction": "out", "eventType": "library.loan.shipped"}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Equal("integration-event/loan-shipped", finding.Subject);
        Assert.Contains("no longer exists", finding.Message);
    }

    /// <summary>Identical models report nothing.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void UnchangedModelsReportNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var useCase = TestModel.Record("use-case", "use-case/lending.borrow-copy", file, """{"kind": "command"}""");
        var result = TestModel.Record(
            "result", "result/borrow-copy.borrowed", file,
            """{"outcome": "success", "meaning": "The copy is lent."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, useCase, result),
            TestModel.Model(config, useCase, result));

        Assert.Empty(findings);
    }

    /// <summary>An added result reports nothing: only removals break clients.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void AddedResultReportsNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var useCase = TestModel.Record("use-case", "use-case/lending.borrow-copy", file, """{"kind": "command"}""");
        var result = TestModel.Record(
            "result", "result/borrow-copy.borrowed", file,
            """{"outcome": "success", "meaning": "The copy is lent."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, useCase),
            TestModel.Model(config, useCase, result));

        Assert.Empty(findings);
    }

    /// <summary>A removed record outside the client contract reports nothing.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RemovedRequirementReportsNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrowing.yaml");
        var requirement = TestModel.Record(
            "requirement", "requirement/borrowing.max-active-loans", file,
            """{"kind": "functional", "statement": "A reader holds at most five active loans."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, requirement),
            TestModel.Model(config));

        Assert.Empty(findings);
    }

    /// <summary>A result whose use case is gone too anchors on the project file.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void RemovedUseCaseAnchorsOnTheProjectFile()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var file = Path.Combine(temp.Path, "borrow-copy.yaml");
        var useCase = TestModel.Record("use-case", "use-case/lending.borrow-copy", file, """{"kind": "command"}""");
        var result = TestModel.Record(
            "result", "result/borrow-copy.borrowed", file,
            """{"outcome": "success", "meaning": "The copy is lent."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, useCase, result),
            TestModel.Model(config));

        var finding = Assert.Single(findings);
        Assert.Equal("contract-break", finding.Code);
        Assert.Equal(Path.Combine(temp.Path, ".litespec", "project.yaml"), finding.Path);
        Assert.Equal(1, finding.Line);
    }

    /// <summary>A derived result the head removed reports nothing: it follows its use case.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void DerivedResultReportsNothing()
    {
        using var temp = TestCli.Temp("contract-break");
        var config = TestModel.Config(temp.Path);
        var before = Derived(
            "result", "result/borrow-copy.borrowed", Path.Combine(temp.Path, "borrow-copy.yaml"),
            """{"outcome": "success", "meaning": "The copy is lent."}""");

        var findings = ContractBreaks.Find(
            TestModel.Model(config, before),
            TestModel.Model(config));

        Assert.Empty(findings);
    }

    /// <summary>A removed result blocks <c>model check --since</c> with a level S finding.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void CheckSinceReportsARemovedResultAsBlocking()
    {
        using var folder = TestCli.Temp("contract-break");
        CopyBase(folder.Path);
        CommitAll(folder.Path);
        RemoveRecordBlock(
            Path.Combine(folder.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
            "result/borrow-copy.copy-unavailable");

        var json = TestCli.Run("model", "check", "--root", folder.Path, "--since", "HEAD", "--format", "json");
        var breaks = BreaksOf(json.Stdout);

        Assert.Equal(1, json.Status);
        var breaking = Assert.Single(breaks, finding => finding.Subject == "result/borrow-copy.copy-unavailable");
        Assert.Equal("S", breaking.Level);
        Assert.Equal("blocking", breaking.Outcome);

        var text = TestCli.Run("model", "check", "--root", folder.Path, "--since", "HEAD");

        Assert.Equal(1, text.Status);
        Assert.Contains("[contract-break]", text.Output);
        Assert.Contains("no longer exists", text.Output);
    }

    /// <summary>A changed integration event blocks <c>model check --since</c> with a level S finding.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void CheckSinceReportsAChangedIntegrationEventAsBlocking()
    {
        using var folder = TestCli.Temp("contract-break");
        CopyBase(folder.Path);
        CommitAll(folder.Path);
        var file = Path.Combine(folder.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "loan-shipped", "loan-shipped.yaml");
        File.WriteAllText(file, File.ReadAllText(file).Replace("The lent loan.", "The shipped loan.", StringComparison.Ordinal));

        var json = TestCli.Run("model", "check", "--root", folder.Path, "--since", "HEAD", "--format", "json");
        var breaks = BreaksOf(json.Stdout);

        Assert.Equal(1, json.Status);
        var breaking = Assert.Single(breaks, finding => finding.Subject == "integration-event/loan-shipped");
        Assert.Equal("S", breaking.Level);
        Assert.Equal("blocking", breaking.Outcome);
    }

    /// <summary>An unchanged tree reports no contract-break finding.</summary>
    /// <proves>litespec/core/language#keep-wire-codes-stable</proves>
    [Fact]
    public void CheckSinceWithoutContractChangesReportsNone()
    {
        using var folder = TestCli.Temp("contract-break");
        CopyBase(folder.Path);
        CommitAll(folder.Path);

        var json = TestCli.Run("model", "check", "--root", folder.Path, "--since", "HEAD", "--format", "json");

        Assert.Empty(BreaksOf(json.Stdout));
    }

    /// <summary>A derived record of the type and id, with the JSON fields.</summary>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record id.</param>
    /// <param name="file">The file the record sits in.</param>
    /// <param name="json">The record fields as JSON.</param>
    /// <returns>The derived record at line 1 of the file.</returns>
    static Record Derived(string type, string id, string file, string json)
    {
        using var document = JsonDocument.Parse(json);

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = 1,
            Raw = document.RootElement.Clone(),
            Derived = true,
        };
    }

    /// <summary>Every contract-break finding in the check JSON output.</summary>
    /// <param name="stdout">The check command JSON output.</param>
    /// <returns>The contract-break findings with their level, outcome and subject.</returns>
    static IReadOnlyList<(string Level, string Outcome, string? Subject)> BreaksOf(string stdout)
    {
        using var document = JsonDocument.Parse(stdout);
        var found = new List<(string Level, string Outcome, string? Subject)>();

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            if (finding.GetProperty("code").GetString() == "contract-break")
            {
                found.Add((
                    finding.GetProperty("level").GetString()!,
                    finding.GetProperty("outcome").GetString()!,
                    finding.GetProperty("subject").ValueKind == JsonValueKind.Null
                        ? null
                        : finding.GetProperty("subject").GetString()));
            }
        }

        return found;
    }

    /// <summary>Copies the diff base fixture into the folder.</summary>
    /// <param name="target">The folder to copy the fixture into.</param>
    static void CopyBase(string target)
    {
        var source = TestCli.Fixture("Diff/base");

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }

    /// <summary>Removes the nested record block carrying the id from the YAML file.</summary>
    /// <param name="file">The record file holding the block.</param>
    /// <param name="id">The record id of the block to remove.</param>
    static void RemoveRecordBlock(string file, string id)
    {
        var lines = File.ReadAllLines(file).ToList();
        var at = lines.FindIndex(line => line.Contains(id, StringComparison.Ordinal));

        if (at < 0)
        {
            throw new InvalidOperationException($"'{file}' holds no '{id}'.");
        }

        var start = at;

        while (start > 0 && !lines[start].StartsWith("  - type:", StringComparison.Ordinal))
        {
            start--;
        }

        var end = at + 1;

        while (end < lines.Count && lines[end].StartsWith("    ", StringComparison.Ordinal))
        {
            end++;
        }

        lines.RemoveRange(start, end - start);
        File.WriteAllLines(file, lines);
    }

    /// <summary>Commits the whole folder so the index starts clean.</summary>
    /// <param name="path">The folder to commit.</param>
    static void CommitAll(string path)
    {
        RunGit(path, "-c", "init.defaultBranch=main", "init");
        RunGit(path, "add", "-A");
        RunGit(
            path, "-c", "user.name=Test", "-c", "user.email=test@example.com",
            "-c", "commit.gpgsign=false", "commit", "-m", "init");
    }

    /// <summary>Runs git in the folder and returns its standard output, throwing when it fails.</summary>
    /// <param name="workdir">The folder to run git in.</param>
    /// <param name="args">The git arguments.</param>
    /// <returns>The git standard output.</returns>
    static string RunGit(string workdir, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"git {string.Join(' ', args)} did not finish in two minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr.Result}");
        }

        return stdout.Result;
    }
}
