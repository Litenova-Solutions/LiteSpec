using System.Text.Json.Nodes;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Book;

/// <summary>Proves the book shards carry the reviewable specification: bodies, lifecycles, sentences, traces and views.</summary>
public sealed class BookViewsTests
{
    /// <summary>Every record type renders: each type is counted, sharded, searchable and placed once in the outline.</summary>
    /// <proves>litespec/core/model#organize-the-book-by-the-record-layers</proves>
    [Fact]
    public void Every_Record_Type_Renders_In_Shards_Search_And_Outline()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);

        var data = Path.Combine(temp.Path, "data");
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var totals = manifest["totals"]!.AsObject();
        foreach (var type in RecordTypes.All)
        {
            Assert.True((totals[type]?.GetValue<int>() ?? 0) > 0, $"No {type} record reached the book.");
        }

        var details = ReadRecords(data);
        Assert.Equal(
            details.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            manifest["shardOf"]!.AsObject().Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal).ToList());
        foreach (var pair in details)
        {
            Assert.Equal(pair.Key.Split('/')[0], Str(pair.Value, "type"));
            Assert.True((Str(pair.Value, "title") ?? "").Length > 0);
            Assert.NotNull(pair.Value["fields"]);
            Assert.NotNull(pair.Value["src"]);
            Assert.NotNull(pair.Value["out"]);
            Assert.NotNull(pair.Value["in"]);
        }

        var searchIds = ReadSearchIds(data);
        var tops = ReadOutlineTops(data, manifest);
        var ancestors = AncestorsOf(tops);
        foreach (var id in details.Keys)
        {
            Assert.Contains(id, searchIds);
            Assert.True(ancestors.ContainsKey(id), $"{id} is missing from the outline.");
        }

        Assert.Equal(details.Count, ancestors.Count);

        var unplaced = manifest["unplaced"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "journey/borrowing" }, unplaced);

        // Each detail parent agrees with the outline nesting: under its parent record,
        // or under a group when the product parents it outside Intent or no rule places it.
        foreach (var pair in details)
        {
            var parent = Str(pair.Value, "parent");
            var ancestor = ancestors[pair.Key];
            if (parent is null)
            {
                Assert.Null(ancestor);
                Assert.True(
                    pair.Key == "product/library" || unplaced.Contains(pair.Key),
                    $"{pair.Key} has no parent but is not unplaced.");
            }
            else if (parent == "product/library" && TopsItsLayer(Str(pair.Value, "type")))
            {
                Assert.Null(ancestor);
            }
            else
            {
                Assert.Equal(parent, ancestor);
            }
        }

        // Spot placements across the parent rules.
        Assert.Equal("problem/slow-desk-queues", Str(details["feature/self-checkout"], "parent"));
        Assert.Equal("feature/self-checkout", Str(details["requirement/borrowing.max-wait"], "parent"));
        Assert.Equal("aggregate/lending.loan", Str(details["use-case/lending.borrow-copy"], "parent"));
        Assert.Equal("aggregate/lending.loan", Str(details["use-case/lending.return-copy"], "parent"));
        Assert.Equal("use-case/lending.borrow-copy", Str(details["result/borrow-copy.borrowed"], "parent"));
        Assert.Equal("result/borrow-copy.borrowed", Str(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "parent"));
        Assert.Equal("aggregate/lending.loan", Str(details["rule/loan.one-loan-per-copy"], "parent"));
        Assert.Equal("aggregate/lending.loan", Str(details["event/loan.loan-due"], "parent"));
        Assert.Equal("module/lending", Str(details["job/lending.send-overdue-notices"], "parent"));
        Assert.Equal("entry-point/library-app", Str(details["screen/library-app.search"], "parent"));
        Assert.Equal("product/library", Str(details["integration-event/loan-borrowed"], "parent"));

        // Group nodes carry their layer and type; record nodes carry their records shard.
        var journeyGroup = FindNode(tops, "intent/journey");
        Assert.Equal("group", Str(journeyGroup, "kind"));
        Assert.Equal("journey", Str(journeyGroup, "type"));
        var roleGroup = FindNode(tops, "behavior/role");
        Assert.Equal("group", Str(roleGroup, "kind"));
        foreach (var top in tops)
        {
            foreach (var node in RecordsUnder(top))
            {
                var shard = Str(node, "shard");
                Assert.True(shard is { Length: > 0 }, $"{Str(node, "id")} names no shard.");
                Assert.True(File.Exists(Path.Combine(data, shard + ".js")), $"Shard {shard} is missing.");
            }
        }
    }

    /// <summary>A record detail renders its prose body as text: headings, spans, lists, tables, code and quotes.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Record_Body_Renders_As_Escaped_Html_With_Sections()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var loan = ReadRecords(Path.Combine(temp.Path, "data"))["aggregate/lending.loan"];
        var html = Str(loan, "bodyHtml") ?? "";
        Assert.Contains("<h3 id=\"body-bounds\">Bounds</h3>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>one copy</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<em>one member</em>", html, StringComparison.Ordinal);
        Assert.Contains("<code>three weeks</code>", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://example.com/lending\">lending guide</a>", html, StringComparison.Ordinal);
        Assert.Contains("<a href=\"https://example.com/desks\">https://example.com/desks</a>", html, StringComparison.Ordinal);
        Assert.Contains("<th>Week</th>", html, StringComparison.Ordinal);
        Assert.Contains("<td class=\"num\">120</td>", html, StringComparison.Ordinal);
        Assert.Contains("<pre class=\"codeblock\"><code>scan member, scan copy, print slip</code></pre>", html, StringComparison.Ordinal);
        Assert.Contains("<blockquote>", html, StringComparison.Ordinal);
        Assert.Contains("<hr>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#use-case/lending.borrow-copy\"", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"reviewer-only\">Borrow Copy</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"dev-only mono\">use-case/lending.borrow-copy</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("## Bounds", html, StringComparison.Ordinal);

        var sections = Obj(loan, "sections");
        Assert.Equal("", Str(sections, "intro"));
        var list = sections["list"]!.AsArray();
        Assert.Single(list);
        Assert.Equal("Bounds", Str(list[0]!.AsObject(), "h"));
        Assert.Contains("one copy", Str(list[0]!.AsObject(), "html") ?? "", StringComparison.Ordinal);
    }

    /// <summary>A body can never inject markup: angle brackets escape inside code spans.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Record_Body_Escapes_Markup()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var html = Str(ReadRecords(Path.Combine(temp.Path, "data"))["runbook/overdue-notice"], "bodyHtml") ?? "";
        Assert.Contains("<code>&lt;code&gt;LOAN-1&lt;/code&gt;</code>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<code>LOAN-1</code>", html, StringComparison.Ordinal);
    }

    /// <summary>No shard falls back to raw JSON: no object dumps and no raw JSON blocks anywhere.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void No_Shard_Holds_Raw_Json_Or_Object_Dumps()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        foreach (var shard in Directory.GetFiles(Path.Combine(temp.Path, "data"), "*.js"))
        {
            var text = File.ReadAllText(shard);
            Assert.DoesNotContain("[object Object]", text, StringComparison.Ordinal);
            Assert.DoesNotContain("<pre class=\"json\">", text, StringComparison.Ordinal);
        }
    }

    /// <summary>An aggregate detail carries its lifecycle fields and its reachable states.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Aggregate_Detail_Carries_Lifecycle_Fields_And_Reachable_States()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var loan = ReadRecords(Path.Combine(temp.Path, "data"))["aggregate/lending.loan"];
        var fields = Obj(loan, "fields");
        Assert.Equal(3, Arr(fields, "states").Count);
        Assert.Equal(2, Arr(fields, "transitions").Count);
        Assert.Equal(3, Arr(fields, "data").Count);
        Assert.Equal(
            new[] { "OnLoan", "OnShelf" },
            Arr(loan, "reachable").Select(node => node!.GetValue<string>()).ToList());
    }

    /// <summary>A scenario detail carries its Given, When and Then sentences with examples and proof.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Scenario_Detail_Carries_Sentences_Examples_And_Proof()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var passing = details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"];
        var fields = Obj(passing, "fields");
        Assert.Equal("A copy sits available on its shelf.", Str(fields, "given"));
        Assert.Equal("The member borrows the copy.", Str(fields, "when"));
        Assert.Equal("The loan is active.", Str(fields, "then"));
        Assert.Equal("result/borrow-copy.borrowed", Str(fields, "result"));
        Assert.Equal(2, Arr(fields, "examples").Count);
        var proof = Obj(passing, "proof");
        Assert.Equal("passing", Str(proof, "state"));
        Assert.Equal(1, proof["tests"]?.GetValue<int>());
        var test = Obj(proof, "test");
        Assert.Equal("Borrowing An Available Copy Succeeds", Str(test, "name"));
        Assert.Equal("tests/BorrowTests.cs", Str(test, "path"));
        Assert.Equal(41, test["line"]?.GetValue<int>());
        Assert.Empty(Arr(passing, "lacks"));

        var refused = details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"];
        Assert.Equal("unproved", Str(Obj(refused, "proof"), "state"));
        Assert.True(HasString(Arr(refused, "lacks"), "no test"));
    }

    /// <summary>A scenario traces to its goal through edges: result, use case, rules, requirement, feature and goal.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Scenario_Traces_To_Its_Goal_Through_Edges()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        Assert.True(HasEdge(
            Arr(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "out"),
            "result/borrow-copy.borrowed",
            "scenario.result"));
        Assert.True(HasEdge(
            Arr(details["result/borrow-copy.borrowed"], "in"),
            "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
            "scenario.result"));
        Assert.True(HasEdge(
            Arr(details["use-case/lending.borrow-copy"], "out"),
            "requirement/borrowing.max-wait",
            "use-case.fulfills"));
        Assert.True(HasEdge(
            Arr(details["rule/loan.one-loan-per-copy"], "out"),
            "use-case/lending.borrow-copy",
            "rule.governs"));
        Assert.True(HasEdge(
            Arr(details["rule/loan.late-fee"], "out"),
            "use-case/lending.borrow-copy",
            "rule.governs"));
        Assert.True(HasEdge(
            Arr(details["requirement/borrowing.max-wait"], "out"),
            "feature/self-checkout",
            "requirement.features"));
        Assert.True(HasEdge(
            Arr(details["feature/self-checkout"], "out"),
            "goal/faster-borrowing",
            "feature.advances"));
        Assert.True(HasEdge(
            Arr(details["goal/faster-borrowing"], "in"),
            "feature/self-checkout",
            "feature.advances"));
    }

    /// <summary>Every edge key resolves to a Links table phrase in the lexicon, in both directions.</summary>
    /// <proves>litespec/core/model#name-book-labels-with-litespec-terms</proves>
    [Fact]
    public void Connection_Sentences_Resolve_From_Lexicon_Phrases()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var data = Path.Combine(temp.Path, "data");
        var details = ReadRecords(data);
        var links = ReadShard(Path.Combine(data, "lexicon.js"), "lexicon")["links"]!.AsArray();

        Assert.Equal(("serves", "is served by"), Phrases(links, "serves", "use-case"));
        Assert.Equal(("serves", "is served by"), Phrases(links, "serves", "journey"));
        Assert.Equal(("advances", "is advanced by"), Phrases(links, "advances", "feature"));
        Assert.Equal(("is carried by", "carries"), Phrases(links, "features", "requirement"));
        Assert.Equal(("fulfills", "is fulfilled by"), Phrases(links, "fulfills", "use-case"));
        Assert.Equal(("solves", "is solved by"), Phrases(links, "solves", "feature"));
        Assert.Equal(("is walked by", "walks"), Phrases(links, "actor", "journey"));
        Assert.Equal(("acts as", "is acted as by"), Phrases(links, "actor", "external-system"));

        // Each edge key below joins to its row by the field after the carrier prefix.
        Assert.True(HasEdge(Arr(details["use-case/lending.borrow-copy"], "out"), "journey/borrowing", "use-case.serves"));
        Assert.True(HasEdge(Arr(details["feature/self-checkout"], "out"), "goal/faster-borrowing", "feature.advances"));
        Assert.True(HasEdge(Arr(details["requirement/borrowing.max-wait"], "out"), "feature/self-checkout", "requirement.features"));
        Assert.True(HasEdge(Arr(details["rule/loan.one-loan-per-copy"], "out"), "use-case/lending.borrow-copy", "rule.governs"));
        Assert.True(HasEdge(Arr(details["decision/self-checkout-kiosk"], "out"), "feature/self-checkout", "decision.decides"));
        Assert.True(HasEdge(Arr(details["reaction/lending.send-due-reminder"], "out"), "event/loan.loan-due", "reaction.on"));
        Assert.True(HasEdge(Arr(details["journey/borrowing"], "out"), "actor/member", "journey.actor"));
        Assert.True(HasEdge(Arr(details["external-system/fine-payments"], "out"), "actor/member", "external-system.actor"));
        foreach (var (key, carrier) in new[]
        {
            ("use-case.serves", "use-case"),
            ("feature.advances", "feature"),
            ("requirement.features", "requirement"),
            ("rule.governs", "rule"),
            ("decision.decides", "decision"),
            ("reaction.on", "reaction"),
            ("journey.actor", "journey"),
            ("external-system.actor", "external-system"),
        })
        {
            var bare = key[(carrier.Length + 1)..];
            var row = links.OfType<JsonObject>().FirstOrDefault(link =>
                Str(link, "field") == bare
                && link["carriers"]!.AsArray().Any(node => node!.GetValue<string>() == carrier));
            Assert.True(row is not null, $"No lexicon row joins edge key {key}.");
            Assert.True((Str(row, "forward") ?? "").Length > 0);
            Assert.True((Str(row, "inverse") ?? "").Length > 0);
        }
    }

    /// <summary>A goal traces through features and requirements to the verified state; a goal no feature serves has no incoming advances.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Goal_Trace_Reaches_Verified_Requirements()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        Assert.True(HasEdge(
            Arr(details["goal/faster-borrowing"], "in"), "feature/self-checkout", "feature.advances"));
        foreach (var id in new[]
        {
            "requirement/borrowing.max-wait",
            "requirement/borrowing.copy-shelf-home",
            "requirement/borrowing.late-fee",
        })
        {
            Assert.True(HasEdge(Arr(details["feature/self-checkout"], "in"), id, "requirement.features"));
        }

        Assert.True(BoolOf(Obj(details["requirement/borrowing.copy-shelf-home"], "machine")["verified"]));
        Assert.False(BoolOf(Obj(details["requirement/borrowing.max-wait"], "machine")["verified"]));

        var quiet = Arr(details["goal/quiet-study"], "in").OfType<JsonObject>().ToList();
        Assert.DoesNotContain(quiet, edge => Str(edge, "key") == "feature.advances");
    }

    /// <summary>The queue shard carries the review batches and ballots, and findings sit on their records.</summary>
    /// <proves>litespec/core/release#compute-the-review-queue-from-the-model</proves>
    [Fact]
    public void Review_Queue_Batches_Ballots_And_Findings()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run(
            "book",
            "--model", BookTruthTests.ModelWithEmbeddedFindings(temp),
            "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var data = Path.Combine(temp.Path, "data");
        var queue = ReadShard(Path.Combine(data, "queue.js"), "queue");
        var order = queue["order"]!.AsArray().Select(node => node!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "decision/decision/self-checkout-kiosk" }, order);
        var batch = queue["batches"]!.AsObject()["decision/decision/self-checkout-kiosk"]!.AsObject();
        Assert.Equal("decision", Str(batch, "kind"));
        Assert.Equal(3, batch["tier"]?.GetValue<int>());
        Assert.Equal(1, batch["n"]?.GetValue<int>());
        Assert.Equal(new[] { "feature/self-checkout" }, batch["records"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());
        Assert.Equal("2026-11-15", Str(batch, "decideBy"));
        Assert.Equal(new[] { "Ada" }, batch["deciders"]!.AsArray().Select(node => node!.GetValue<string>()).ToList());

        Assert.Equal(2, queue["ballots"]!.AsArray().Count);

        var details = ReadRecords(data);
        var open = Arr(details["use-case/lending.return-copy"], "findings");
        Assert.Single(open);
        Assert.Equal("use-case-unimplemented", Str(open[0]!.AsObject(), "code"));
        Assert.False(BoolOf(open[0]!.AsObject()["known"]));
        var known = Arr(details["journey/borrowing"], "findings");
        Assert.Single(known);
        Assert.True(BoolOf(known[0]!.AsObject()["known"]));

        Assert.Equal("2026-01-01", Str(Obj(details["known-gap/offline-kiosk"], "fields"), "endsBy"));
    }

    /// <summary>A rule detail states its kind, what it governs and refuses, and its proof.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Rule_Detail_Carries_Kind_Governs_And_Refusal()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var invariant = details["rule/loan.one-loan-per-copy"];
        Assert.Equal("invariant", Str(Obj(invariant, "fields"), "kind"));
        Assert.Equal("unproved", Str(Obj(invariant, "proof"), "state"));
        Assert.True(HasEdge(Arr(invariant, "out"), "use-case/lending.borrow-copy", "rule.governs"));
        Assert.True(HasEdge(Arr(invariant, "out"), "requirement/borrowing.copy-shelf-home", "rule.fulfills"));
        Assert.True(HasEdge(Arr(invariant, "out"), "result/borrow-copy.copy-unavailable", "rule.refusal"));

        var calculation = details["rule/loan.late-fee"];
        Assert.Equal("calculation", Str(Obj(calculation, "fields"), "kind"));
        Assert.Equal("unproved", Str(Obj(calculation, "proof"), "state"));
        Assert.True(HasEdge(Arr(calculation, "out"), "use-case/lending.borrow-copy", "rule.governs"));

        var governed = Arr(details["use-case/lending.borrow-copy"], "in").OfType<JsonObject>().ToList();
        Assert.Contains(governed, edge => Str(edge, "id") == "rule/loan.one-loan-per-copy" && Str(edge, "key") == "rule.governs");
        Assert.Contains(governed, edge => Str(edge, "id") == "rule/loan.late-fee" && Str(edge, "key") == "rule.governs");
        var examples = Arr(Obj(details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"], "fields"), "examples");
        Assert.Equal(2, examples.Count);
    }

    /// <summary>A requirement detail states its fit criterion and its verification.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Requirement_Detail_Carries_Fit_Criterion_And_Verification()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var data = Path.Combine(temp.Path, "data");
        var details = ReadRecords(data);
        var measured = details["requirement/borrowing.max-wait"];
        var fields = Obj(measured, "fields");
        Assert.Equal("p95 desk wait", Str(fields, "measure"));
        Assert.Equal("under 2 minutes", Str(fields, "target"));
        Assert.Equal("2026-12-31", Str(fields, "dueBy"));
        Assert.Equal("k6 desk-wait p(95)<120", Str(fields, "check"));
        var machine = Obj(measured, "machine");
        Assert.False(BoolOf(machine["verified"]));
        Assert.Equal(false, machine["measurePassed"]?.GetValue<bool>());
        var kept = Obj(measured, "keptBy");
        Assert.Equal(4, kept.Count);
        Assert.Equal(1, kept["journey"]?.GetValue<int>());

        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        Assert.Equal("2026-10-20", manifest["testRun"]!.AsObject()["date"]!.GetValue<string>());

        var fulfilled = details["requirement/borrowing.copy-shelf-home"];
        Assert.True(BoolOf(Obj(fulfilled, "machine")["verified"]));
    }

    /// <summary>A reaction detail carries its trigger events and what it sends; a notification carries its audience and channel.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Reaction_And_Notification_Details_Carry_Trigger_And_Audience()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var reaction = details["reaction/lending.send-due-reminder"];
        Assert.True(HasEdge(Arr(reaction, "out"), "event/loan.loan-due", "reaction.on"));
        Assert.True(HasEdge(Arr(reaction, "out"), "notification/lending.due-reminder", "reaction.sends"));
        Assert.True(HasEdge(Arr(reaction, "out"), "requirement/borrowing.max-wait", "reaction.fulfills"));

        var notification = details["notification/lending.due-reminder"];
        var fields = Obj(notification, "fields");
        Assert.Equal("email", Str(fields, "channel"));
        Assert.True(HasString(Arr(fields, "audience"), "actor/member"));
        Assert.True(HasEdge(Arr(notification, "in"), "reaction/lending.send-due-reminder", "reaction.sends"));
    }

    /// <summary>A journey detail carries its chapter: aim, steps, serving, promises and outcomes.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Journey_Detail_Carries_Its_Chapter()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var journey = details["journey/borrowing"];
        var fields = Obj(journey, "fields");
        Assert.Equal("Borrow before closing", Str(fields, "aim"));
        Assert.Equal("actor/member", Str(fields, "actor"));
        Assert.Equal("The member leaves the desk with the copy before closing.", Str(fields, "finishedWhen"));
        var steps = Arr(fields, "steps");
        Assert.Equal(2, steps.Count);
        Assert.Equal("The member borrows a copy before closing.", Str(steps[0]!.AsObject(), "says"));
        Assert.Equal("The desk renews the loan.", Str(steps[1]!.AsObject(), "ifItGoesWrong"));

        var servers = Obj(journey, "stepServers");
        Assert.True(HasString(servers["s1"] as JsonArray ?? new JsonArray(), "use-case/lending.borrow-copy"));
        Assert.Empty(servers["s2"] as JsonArray ?? new JsonArray());
        Assert.Empty(Obj(journey, "stepScreens")["s1"] as JsonArray ?? new JsonArray());
        Assert.Empty(Arr(journey, "exercised"));

        Assert.True(HasEdge(Arr(journey, "out"), "requirement/borrowing.max-wait", "journey.fulfills"));
        Assert.True(HasEdge(Arr(journey, "out"), "actor/member", "journey.actor"));
        Assert.True(HasEdge(Arr(journey, "in"), "use-case/lending.borrow-copy", "use-case.serves"));

        var questions = Arr(Obj(details["requirement/borrowing.max-wait"], "fields"), "questions");
        Assert.Single(questions);
        Assert.Equal("Do kiosk users count toward the wait?", Str(questions[0]!.AsObject(), "question"));
        Assert.Equal("Count kiosk users separately.", Str(questions[0]!.AsObject(), "recommended"));
    }

    /// <summary>Ballots carry the tier, decide-by date and ballot each ordering needs, tier 1 first.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Ballots_Carry_Tier_DecideBy_And_Ballot_For_Ordering()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var queue = ReadShard(Path.Combine(temp.Path, "data", "queue.js"), "queue");
        var ballots = queue["ballots"]!.AsArray().OfType<JsonObject>().ToList();
        Assert.Equal(2, ballots.Count);

        var tiered = ballots.Single(entry => Str(Obj(entry, "q"), "question") == "Must the kiosk serve members without cards?");
        Assert.Equal("feature/self-checkout", Str(tiered, "id"));
        Assert.Equal(0, tiered["i"]?.GetValue<int>());
        Assert.Equal(1, Obj(tiered, "q")["tier"]?.GetValue<int>());
        Assert.Equal("Ada", Str(Obj(tiered, "q"), "decider"));
        Assert.Equal("2026-10-30", Str(Obj(tiered, "q"), "decideBy"));
        Assert.Equal("desk-queue", Str(Obj(tiered, "q"), "ballot"));
        Assert.Null(Obj(tiered, "q")["recommended"]);

        var sampled = ballots.Single(entry => Str(Obj(entry, "q"), "question") == "Do kiosk users count toward the wait?");
        Assert.Equal("requirement/borrowing.max-wait", Str(sampled, "id"));
        Assert.Equal(3, Obj(sampled, "q")["tier"]?.GetValue<int>());
        Assert.Equal("2026-11-01", Str(Obj(sampled, "q"), "decideBy"));
        Assert.Equal("desk-queue", Str(Obj(sampled, "q"), "ballot"));
        Assert.Equal("Count kiosk users separately.", Str(Obj(sampled, "q"), "recommended"));

        Assert.Equal(7, queue["review"]!.AsObject()["vetoWindowDays"]!.GetValue<int>());
    }

    /// <summary>A setting detail carries its meaning and default; a use-case detail carries its access grant and what it serves.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Setting_And_Use_Case_Details_Carry_Meaning_Access_And_Serves()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var setting = details["setting/lending.max-active-loans"];
        Assert.Equal("Active loans per member.", Str(Obj(setting, "fields"), "meaning"));
        Assert.Equal(5, Obj(setting, "fields")["default"]?.GetValue<int>());

        var useCase = details["use-case/lending.borrow-copy"];
        var access = Obj(Obj(useCase, "fields"), "access");
        Assert.Equal("permission/borrow-copy", Str(access, "grant"));
        Assert.Equal("own", Str(access, "tenant"));
        Assert.True(HasString(Arr(Obj(useCase, "fields"), "serves"), "journey/borrowing#s1"));
        Assert.True(HasString(Arr(Obj(useCase, "fields"), "reachedThrough"), "entry-point/library-app"));
        var keys = Arr(useCase, "out").OfType<JsonObject>().Select(edge => Str(edge, "key")).ToList();
        Assert.Contains("use-case.serves", keys);
        Assert.Contains("use-case.fulfills", keys);
        Assert.Contains("use-case.raises", keys);
        Assert.Contains("use-case.reachedThrough", keys);
        Assert.Contains("use-case.access.grant", keys);
        Assert.True(HasEdge(Arr(details["journey/borrowing"], "in"), "use-case/lending.borrow-copy", "use-case.serves"));
    }

    /// <summary>The search shards cover statements, summaries, aims and body heads for every record.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Search_Shards_Cover_Statements_And_Body_Heads()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var data = Path.Combine(temp.Path, "data");
        var rows = ReadSearchRows(data);
        var manifest = ReadShard(Path.Combine(data, "manifest.js"), "manifest");
        var total = manifest["totals"]!.AsObject().Sum(pair => pair.Value!.GetValue<int>());
        Assert.Equal(total, rows.Count);
        Assert.Equal(
            rows.Select(row => row[0]).OrderBy(id => id, StringComparer.Ordinal).ToList(),
            rows.Select(row => row[0]).Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList());

        var wait = rows.Single(row => row[0] == "requirement/borrowing.max-wait");
        Assert.Equal("Max Wait", wait[1]);
        Assert.Equal("requirement", wait[2]);
        Assert.Equal("A member waits less than two minutes at the desk.", wait[3]);
        Assert.Equal("One copy lent to one member.", rows.Single(row => row[0] == "aggregate/lending.loan")[3]);
        Assert.Equal("Borrow before closing", rows.Single(row => row[0] == "journey/borrowing")[3]);
        Assert.Contains(
            "Members scan and go",
            rows.Single(row => row[0] == "feature/self-checkout")[3],
            StringComparison.Ordinal);
        Assert.Equal("", rows.Single(row => row[0] == "scenario/borrow-copy.borrowing-an-available-copy-succeeds")[3]);
    }

    /// <summary>Facts carry their status reason inputs: deciders, decide-by dates, open questions and held-by drafts.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Facts_Carry_Status_Reason_Inputs()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var draft = details["feature/self-checkout"];
        Assert.Equal("draft", Str(draft, "status"));
        Assert.Equal("Ada", Str(draft, "decider"));
        Assert.Equal("2026-11-15", Str(draft, "decideBy"));
        Assert.Single(Arr(Obj(draft, "fields"), "questions"));
        Assert.Empty(Arr(draft, "heldBy"));

        var inherited = details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"];
        Assert.Equal("reviewed", Str(inherited, "status"));
        Assert.Equal("draft", Str(inherited, "effective"));
        Assert.Empty(Arr(inherited, "heldBy"));

        var settled = details["use-case/lending.borrow-copy"];
        Assert.Equal("reviewed", Str(settled, "status"));
        Assert.Equal("approved", Str(settled, "effective"));
        Assert.Empty(Arr(settled, "heldBy"));
    }

    /// <summary>Implementation and proof rail sections read from computed code, tests, expectations and lacks.</summary>
    /// <proves>litespec/core/model#show-only-computed-judgements-in-the-book</proves>
    [Fact]
    public void Implementation_And_Proof_Rail_Sections()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var borrow = details["use-case/lending.borrow-copy"];
        Assert.True(BoolOf(Obj(borrow, "expect")["impl"]));
        Assert.True(BoolOf(Obj(borrow, "expect")["proof"]));
        var impl = Arr(borrow, "impl");
        Assert.Single(impl);
        Assert.Equal("handler", Str(impl[0]!.AsObject(), "kind"));
        Assert.Equal("src/Application/Lending/BorrowCopyHandler.cs", Str(impl[0]!.AsObject(), "path"));
        Assert.Null(impl[0]!.AsObject()["line"]);
        Assert.True(HasString(Arr(borrow, "lacks"), "scenarios without a test"));

        var returned = details["use-case/lending.return-copy"];
        Assert.False(BoolOf(Obj(returned, "expect")["impl"]));
        Assert.Empty(Arr(returned, "impl"));
        Assert.True(HasString(Arr(returned, "lacks"), "scenarios without a test"));

        var passing = details["scenario/borrow-copy.borrowing-an-available-copy-succeeds"];
        var tests = Arr(passing, "tests");
        Assert.Single(tests);
        Assert.Equal("Borrowing An Available Copy Succeeds", Str(tests[0]!.AsObject(), "name"));
        Assert.Equal("tests/BorrowTests.cs", Str(tests[0]!.AsObject(), "path"));
        Assert.Equal(41, tests[0]!.AsObject()["line"]?.GetValue<int>());
        Assert.True(BoolOf(tests[0]!.AsObject()["confirmed"]));
        Assert.Empty(Arr(passing, "lacks"));

        Assert.Empty(Arr(details["scenario/borrow-copy.borrowing-a-loaned-copy-is-refused"], "tests"));
        Assert.True(HasString(Arr(details["aggregate/lending.loan"], "lacks"), "no code"));
    }

    /// <summary>The Why block inputs split from the body: the intro stays prose and Why sections list by heading.</summary>
    /// <proves>litespec/core/model#explain-the-book-from-litespec-definitions</proves>
    [Fact]
    public void Why_Block_Placement()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        var goal = Obj(details["goal/faster-borrowing"], "sections");
        Assert.Equal("", Str(goal, "intro"));
        var why = goal["list"]!.AsArray();
        Assert.Single(why);
        Assert.Equal("Why It Matters", Str(why[0]!.AsObject(), "h"));
        Assert.Contains("Short waits bring members back.", Str(why[0]!.AsObject(), "html") ?? "", StringComparison.Ordinal);

        var useCase = Obj(details["use-case/lending.borrow-copy"], "sections");
        Assert.Equal("", Str(useCase, "intro"));
        Assert.Equal(
            new[] { "Goal", "What Happens" },
            useCase["list"]!.AsArray().Select(node => Str(node!.AsObject(), "h")).ToList());
    }

    /// <summary>Numbered reference lists read linked records with their statement, meaning or summary.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Numbered_Reference_Lists_Read_From_Fields_And_Edges()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        Assert.True(HasEdge(Arr(details["feature/self-checkout"], "out"), "problem/slow-desk-queues", "feature.solves"));
        Assert.True(HasEdge(Arr(details["feature/self-checkout"], "out"), "goal/faster-borrowing", "feature.advances"));
        Assert.Equal(
            "A member waits less than two minutes at the desk.",
            Str(Obj(details["requirement/borrowing.max-wait"], "fields"), "statement"));
        Assert.Equal(
            "The copy stays unavailable and the member is told it is on loan.",
            Str(Obj(details["result/borrow-copy.copy-unavailable"], "fields"), "meaning"));
        Assert.Equal(
            "One copy lent to one member.",
            Str(Obj(details["aggregate/lending.loan"], "fields"), "summary"));
        Assert.Equal(
            "lending.copy-not-available",
            Str(Obj(details["result/borrow-copy.copy-unavailable"], "fields"), "code"));
    }

    /// <summary>Trace bands walk goal to behavior through incoming advances, carried features, kept-by counts and fulfilling edges.</summary>
    /// <proves>litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots</proves>
    [Fact]
    public void Trace_Bands_Read_From_Edges_And_KeptBy()
    {
        using var temp = Cli.Temp("book-views");
        var result = Cli.Run("book", "--model", Cli.Fixture("Book/review.json"), "--out", temp.Path);
        Assert.Equal(0, result.Status);
        var details = ReadRecords(Path.Combine(temp.Path, "data"));
        Assert.True(HasEdge(Arr(details["goal/faster-borrowing"], "in"), "feature/self-checkout", "feature.advances"));
        Assert.True(HasEdge(Arr(details["problem/slow-desk-queues"], "in"), "feature/self-checkout", "feature.solves"));
        foreach (var id in new[]
        {
            "requirement/borrowing.max-wait",
            "requirement/borrowing.copy-shelf-home",
            "requirement/borrowing.late-fee",
        })
        {
            Assert.True(HasEdge(Arr(details["feature/self-checkout"], "in"), id, "requirement.features"));
        }

        var kept = Obj(details["requirement/borrowing.max-wait"], "keptBy");
        Assert.Equal(1, kept["journey"]?.GetValue<int>());
        Assert.Equal(1, kept["use-case"]?.GetValue<int>());
        Assert.Equal(1, kept["reaction"]?.GetValue<int>());
        Assert.Equal(1, kept["notification"]?.GetValue<int>());
        var fulfilling = Arr(details["requirement/borrowing.max-wait"], "in")
            .OfType<JsonObject>()
            .Where(edge => (Str(edge, "key") ?? "").EndsWith("fulfills", StringComparison.Ordinal))
            .Select(edge => Str(edge, "id"))
            .ToList();
        Assert.Equal(4, fulfilling.Count);
        Assert.True(HasEdge(Arr(details["journey/borrowing"], "in"), "use-case/lending.borrow-copy", "use-case.serves"));
    }

    /// <summary>Whether a product-parented type tops its own layer instead of nesting under the product.</summary>
    static bool TopsItsLayer(string? type) =>
        type is "module" or "role" or "permission" or "entry-point" or "external-system" or "runbook"
            or "integration-event" or "deviation" or "known-gap" or "term";

    /// <summary>The forward and inverse phrases of one link row for one carrier type.</summary>
    static (string? Forward, string? Inverse) Phrases(JsonArray links, string field, string carrier)
    {
        var row = links.OfType<JsonObject>().First(link =>
            Str(link, "field") == field
            && link["carriers"]!.AsArray().Any(node => node!.GetValue<string>() == carrier));
        return (Str(row, "forward"), Str(row, "inverse"));
    }

    static JsonObject ReadShard(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsObject();
    }

    static JsonArray ReadShardArray(string path, string key)
    {
        var text = File.ReadAllText(path);
        var prefix = $"BookData.put(\"{key}\", ";
        Assert.StartsWith(prefix, text, StringComparison.Ordinal);
        Assert.EndsWith(");\n", text, StringComparison.Ordinal);
        return JsonNode.Parse(text[prefix.Length..^3])!.AsArray();
    }

    static Dictionary<string, JsonObject> ReadRecords(string dataDir)
    {
        var details = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(dataDir, "records-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            var shard = ReadShard(file, Path.GetFileNameWithoutExtension(file));
            foreach (var pair in shard)
            {
                details[pair.Key] = pair.Value!.AsObject();
            }
        }

        return details;
    }

    static List<string> ReadSearchIds(string dataDir) =>
        ReadSearchRows(dataDir).Select(row => row[0]).ToList();

    static List<string[]> ReadSearchRows(string dataDir)
    {
        var rows = new List<string[]>();
        foreach (var file in Directory.GetFiles(dataDir, "search-*.js").OrderBy(path => path, StringComparer.Ordinal))
        {
            foreach (var row in ReadShardArray(file, Path.GetFileNameWithoutExtension(file)).OfType<JsonArray>())
            {
                rows.Add(row.Select(cell => cell!.GetValue<string>()).ToArray());
            }
        }

        return rows;
    }

    static List<JsonObject> ReadOutlineTops(string dataDir, JsonObject manifest)
    {
        var tops = new List<JsonObject>();
        foreach (var layer in manifest["layers"]!.AsArray().OfType<JsonObject>())
        {
            foreach (var key in layer["outline"]!.AsArray().Select(node => node!.GetValue<string>()))
            {
                tops.AddRange(ReadShardArray(Path.Combine(dataDir, key + ".js"), key).OfType<JsonObject>());
            }
        }

        return tops;
    }

    /// <summary>The nearest enclosing record ancestor per record id, walking through groups.</summary>
    static Dictionary<string, string?> AncestorsOf(List<JsonObject> tops)
    {
        var ancestors = new Dictionary<string, string?>(StringComparer.Ordinal);
        void Walk(JsonObject node, string? ancestor)
        {
            var id = Str(node, "id") ?? "";
            var next = Str(node, "kind") == "record" ? id : ancestor;
            if (Str(node, "kind") == "record")
            {
                Assert.False(ancestors.ContainsKey(id), $"{id} is placed twice in the outline.");
                ancestors[id] = ancestor;
            }

            foreach (var child in node["children"]!.AsArray().OfType<JsonObject>())
            {
                Walk(child, next);
            }
        }

        foreach (var top in tops)
        {
            Walk(top, null);
        }

        return ancestors;
    }

    static JsonObject FindNode(List<JsonObject> tops, string id)
    {
        JsonObject? found = null;
        void Walk(JsonObject node)
        {
            if (Str(node, "id") == id)
            {
                found = node;
            }

            foreach (var child in node["children"]!.AsArray().OfType<JsonObject>())
            {
                Walk(child);
            }
        }

        foreach (var top in tops)
        {
            Walk(top);
        }

        Assert.True(found is not null, $"No outline node for {id}.");
        return found!;
    }

    static List<JsonObject> RecordsUnder(JsonObject node)
    {
        var records = new List<JsonObject>();
        void Walk(JsonObject current)
        {
            if (Str(current, "kind") == "record")
            {
                records.Add(current);
            }

            foreach (var child in current["children"]!.AsArray().OfType<JsonObject>())
            {
                Walk(child);
            }
        }

        Walk(node);
        return records;
    }

    static string? Str(JsonObject? obj, string name) =>
        obj?[name] is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : null;

    static bool BoolOf(JsonNode? node) =>
        node is JsonValue scalar && scalar.TryGetValue<bool>(out var flag) && flag;

    static JsonObject Obj(JsonObject obj, string name) => obj[name] as JsonObject ?? new JsonObject();

    static JsonArray Arr(JsonObject obj, string name) => obj[name] as JsonArray ?? new JsonArray();

    static bool HasString(JsonArray list, string value)
    {
        foreach (var item in list)
        {
            if (item is JsonValue scalar && scalar.TryGetValue<string>(out var text) && text == value)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasEdge(JsonArray edges, string id, string key)
    {
        foreach (var edge in edges.OfType<JsonObject>())
        {
            if (Str(edge, "id") == id && Str(edge, "key") == key)
            {
                return true;
            }
        }

        return false;
    }
}
