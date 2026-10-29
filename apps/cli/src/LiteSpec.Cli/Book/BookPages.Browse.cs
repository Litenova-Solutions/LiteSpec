using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Book;

/// <summary>The book generator's index pages: type indexes, lenses, review, glossary and search.</summary>
static partial class BookPages
{
    /// <summary>Renders one record type's index page.</summary>
    public static string RenderTypeIndex(BookIndex index, string rel, string type)
    {
        var list = index.ByType.TryGetValue(type, out var found) ? found : [];
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader("Index", BookModel.TypeLabel(type, true), $"{list.Count} records."));
        var rows = string.Concat(list.Select(record =>
        {
            var status = BookRender.OrStr(record["status"], "");
            var extra = BookRender.StrProp(record, "type") == "use-case"
                ? StatusCells(BookModel.UseCaseStatus(index, BookRender.StrProp(record, "id")))
                : "";
            return $"<tr><td><a href=\"{rel}{BookModel.RecordUrl(BookRender.StrProp(record, "id") ?? "")}\">{BookRender.Esc(record["title"])}</a>"
                + $"<div class=\"mono dim dev-only\">{BookRender.Esc(record["id"])}</div></td>"
                + $"<td data-value=\"{BookRender.Esc(status)}\">{BookRender.StatusChip(status)}</td>"
                + $"<td>{BookRender.EscOr(record["owner"], "")}</td>{extra}</tr>";
        }));
        var extraHead = type == "use-case" ? "<th>Implemented</th><th>Verified</th><th>Stale</th>" : "";
        parts.Add("<div class=\"tablewrap\"><table class=\"tbl sortable\">"
            + "<thead><tr><th><button type=\"button\" data-sort=\"0\">Title</button></th>"
            + "<th><button type=\"button\" data-sort=\"1\">Status</button></th>"
            + $"<th><button type=\"button\" data-sort=\"2\">Owner</button></th>{extraHead}</tr></thead>"
            + $"<tbody>{rows}</tbody></table></div>");
        return string.Join("\n", parts);
    }

    static string StatusCells(UseCaseState state) =>
        $"<td>{(state.Implemented ? "Yes" : "No")}</td><td>{(state.Verified ? "Yes" : "No")}</td><td>{(state.Stale ? "Yes" : "No")}</td>";

    /// <summary>Renders the lens index: one entry per actor.</summary>
    public static string RenderLensIndex(BookIndex index, string rel)
    {
        var actors = index.ByType.TryGetValue("actor", out var found) ? found : [];
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader("Lenses", "Lenses", "One lens per actor: what each actor does, holds and may reach."));
        var items = string.Concat(actors.Select(actor =>
        {
            var lens = BookModel.ActorLens(index, BookRender.StrProp(actor, "id") ?? "");
            return $"<li><a href=\"{rel}{BookModel.LensUrl(BookRender.StrProp(actor, "id") ?? "")}\">{BookRender.Esc(actor["title"])}</a> "
                + $"<span class=\"dim\">{lens.Journeys.Count} journeys, {lens.Roles.Count} roles, "
                + $"{lens.Permissions.Count} permissions, {lens.UseCases.Count} use cases</span></li>";
        }));
        parts.Add($"<ul class=\"links\">{items}</ul>");
        return string.Join("\n", parts);
    }

    /// <summary>Renders one actor's lens page.</summary>
    public static string RenderLens(BookIndex index, string rel, JsonObject actor)
    {
        var lens = BookModel.ActorLens(index, BookRender.StrProp(actor, "id") ?? "");
        var kind = BookRender.Fields(actor)?["kind"];
        var kindFacts = new List<(string Term, string Value)>();
        if (BookRender.IsTruthy(kind))
        {
            kindFacts.Add(("Kind", BookRender.Esc(kind)));
        }

        var parts = new List<string>();
        parts.Add(BookRender.PageHeader($"Lens · {BookRender.PropText(actor, "id")}", BookRender.Str(actor["title"]), "", kindFacts));
        parts.Add("<h2 id=\"journeys\">Journeys</h2>");
        if (lens.Journeys.Count > 0)
        {
            parts.Add($"<ul class=\"links\">{string.Concat(lens.Journeys.Select(journey =>
                $"<li>{BookRender.RecordLink(index, rel, BookRender.StrProp(journey, "id") ?? "")}</li>"))}</ul>");
        }
        else
        {
            parts.Add("<p>This actor takes no journey.</p>");
        }

        parts.Add("<h2 id=\"roles\">Roles</h2>");
        if (lens.Roles.Count > 0)
        {
            var rows = string.Concat(lens.Roles.Select(hold =>
            {
                var name = hold.Role is not null
                    ? $"<a href=\"{rel}{BookModel.RecordUrl(BookRender.StrProp(hold.Role, "id") ?? "")}\">{BookRender.Esc(hold.Role["title"])}</a>"
                    : $"<span>Unknown role</span> <span class=\"mono dim dev-only\">{BookRender.Esc(hold.Hold["role"])}</span>";
                return $"<tr><td>{name}</td><td>{BookRender.EscOr(hold.Hold["scope"], "")}</td></tr>";
            }));
            parts.Add($"<div class=\"tablewrap\"><table class=\"tbl\"><thead><tr><th>Role</th><th>Scope</th></tr></thead><tbody>{rows}</tbody></table></div>");
        }
        else
        {
            parts.Add("<p>This actor holds no role.</p>");
        }

        parts.Add("<h2 id=\"permissions\">Permissions</h2>");
        if (lens.Permissions.Count > 0)
        {
            var items = string.Concat(lens.Permissions.Select(permission =>
            {
                var meaning = BookRender.Fields(permission)?["meaning"];
                return $"<li>{BookRender.RecordLink(index, rel, BookRender.StrProp(permission, "id") ?? "")}"
                    + (BookRender.IsTruthy(meaning) ? $" <span class=\"dim\">{BookRender.Esc(meaning)}</span>" : "") + "</li>";
            }));
            parts.Add($"<ul class=\"links\">{items}</ul>");
        }
        else
        {
            parts.Add("<p>Its roles allow no permission.</p>");
        }

        parts.Add("<h2 id=\"use-cases\">Use cases</h2>");
        if (lens.UseCases.Count > 0)
        {
            parts.Add($"<ul class=\"links\">{string.Concat(lens.UseCases.Select(useCase =>
                $"<li>{BookRender.RecordLink(index, rel, BookRender.StrProp(useCase, "id") ?? "")}</li>"))}</ul>");
        }
        else
        {
            parts.Add("<p>Its permissions open no use case.</p>");
        }

        return string.Join("\n", parts);
    }

    /// <summary>Renders the review page: known gaps, drafts, questions, findings and covered findings.</summary>
    public static string RenderReview(BookIndex index, string rel, ReviewBundle review, string generatedDate)
    {
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader(
            "Review",
            "Review",
            $"Known gaps, drafts awaiting a decision, open questions, and check findings, as of {(generatedDate.Length >= 10 ? generatedDate[..10] : generatedDate)}.",
            [
                ("Known gaps", $"<b>{review.Gaps.Count}</b>"),
                ("Drafts", $"<b>{review.Batches.Sum(batch => batch.Records.Count)}</b>"),
                ("Open questions", $"<b>{review.Questions.Count}</b>"),
                ("Findings", $"<b>{review.Findings.Count}</b>"),
            ]));
        parts.Add("<h2 id=\"gaps\">Known Gaps</h2>");
        if (review.Gaps.Count > 0)
        {
            var items = string.Concat(review.Gaps.Select(gap =>
            {
                var fields = BookRender.Fields(gap.Record) ?? new JsonObject();
                var subjects = BookRender.Links(gap.Record)?["subject"] as JsonArray ?? [];
                var subjectLinks = string.Join(", ", subjects.Select(id => BookRender.RecordLink(index, rel, id)));
                return $"<div class=\"fitem\"><span class=\"sev{(gap.Expired ? " high" : "")}\"></span><div>"
                    + $"<div class=\"t\">{(gap.Expired ? "Expired: " : "")}<a href=\"{rel}{BookModel.RecordUrl(BookRender.StrProp(gap.Record, "id") ?? "")}\">{BookRender.Esc(gap.Record["title"])}</a></div>"
                    + $"<div class=\"m\"><span>Owner {BookRender.EscOr(gap.Record["owner"], "none")}</span>"
                    + $"<span>Ends {BookRender.EscOr(fields["endsBy"], "no date")}</span>"
                    + $"<span class=\"dev-only\">Check <code>{BookRender.EscOr(fields["check"], "")}</code></span></div>"
                    + (BookRender.IsTruthy(fields["reason"]) ? $"<p class=\"d\">{BookRender.Esc(fields["reason"])}</p>" : "")
                    + (subjectLinks.Length > 0 ? $"<p class=\"d\">About {subjectLinks}</p>" : "")
                    + "</div></div>";
            }));
            parts.Add(items);
        }
        else
        {
            parts.Add("<p>No known gap is recorded.</p>");
        }

        parts.Add("<h2 id=\"drafts\">Drafts Awaiting a Decision</h2>");
        if (review.Batches.Count == 0)
        {
            parts.Add("<p>No draft awaits a decision.</p>");
        }
        else
        {
            foreach (var batch in review.Batches)
            {
                parts.Add(RenderBatch(index, rel, batch));
            }
        }

        parts.Add("<h2 id=\"questions\">Open Questions</h2>");
        if (review.Questions.Count > 0)
        {
            var rows = string.Concat(review.Questions.Select(question =>
            {
                var id = BookRender.StrProp(question.Record, "id") ?? "";
                return $"<tr><td><a href=\"{rel}{BookModel.RecordUrl(id)}\">{BookRender.Esc(question.Record["title"])}</a></td>"
                    + $"<td>{BookRender.Esc(question.Question)}</td>"
                    + $"<td>{BookRender.Esc(question.Decider ?? "no decider")}</td>"
                    + $"<td>{BookRender.Esc(question.DecideBy ?? "no date")}</td>"
                    + $"<td>{BookRender.Esc(question.Ballot ?? "")}</td>"
                    + $"<td class=\"num\">{question.Tier}</td></tr>";
            }));
            parts.Add($"<div class=\"tablewrap\"><table class=\"tbl\"><thead><tr><th>Record</th><th>Question</th><th>Decider</th><th>Decide By</th><th>Ballot</th><th>Tier</th></tr></thead><tbody>{rows}</tbody></table></div>");
        }
        else
        {
            parts.Add("<p>No open question is recorded.</p>");
        }

        parts.Add("<h2 id=\"findings\">Findings</h2>");
        if (review.Findings.Count > 0)
        {
            var items = string.Concat(review.Findings.Select(finding =>
            {
                var subject = BookRender.IsTruthy(finding?["subject"])
                    && BookRender.StrProp(finding, "subject") is { } id && index.ById.ContainsKey(id)
                    ? $"<p class=\"d\">About {BookRender.RecordLink(index, rel, id)}</p>" : "";
                return "<div class=\"fitem\"><span class=\"sev high\"></span><div>"
                    + $"<div class=\"t\"><span class=\"dev-only\"><code>{BookRender.Esc(finding?["code"])}</code> </span>{BookRender.Esc(finding?["message"])}</div>"
                    + $"<div class=\"m\"><span class=\"mono dev-only\">{BookRender.EscOr(finding?["path"], "")}{(BookRender.IsTruthy(finding?["line"]) ? $":{BookRender.Str(finding?["line"])}" : "")}</span></div>"
                    + subject + "</div></div>";
            }));
            parts.Add(items);
        }
        else
        {
            parts.Add("<p>No findings file was supplied. Run the generator with <code>--findings</code> "
                + "pointing at the JSON output of the model check to list them here.</p>");
        }

        if (review.Known.Count > 0)
        {
            parts.Add("<h2 id=\"known\">Findings Covered by a Known Gap</h2>");
            var items = string.Concat(review.Known.Select(finding =>
                $"<li><span class=\"dev-only\"><code>{BookRender.Esc(finding?["code"])}</code> </span>{BookRender.Esc(finding?["message"])} "
                + $"<span class=\"mono dim dev-only\">{BookRender.EscOr(finding?["path"], "")}{(BookRender.IsTruthy(finding?["line"]) ? $":{BookRender.Str(finding?["line"])}" : "")}</span></li>"));
            parts.Add($"<details><summary>{review.Known.Count} covered findings</summary><ul class=\"links\">{items}</ul></details>");
        }

        return string.Join("\n", parts);
    }

    /// <summary>Renders one review batch: its facts, its approve command, and its records.</summary>
    static string RenderBatch(BookIndex index, string rel, ReviewBatch batch)
    {
        var parts = new List<string>();
        var records = batch.Records.Count == 1 ? "1 record" : $"{batch.Records.Count} records";
        parts.Add($"<h3 id=\"batch-{BallotSlug(batch.Selector)}\">{BookRender.Esc(batch.Selector)} <span class=\"dim\">tier {batch.Tier}, {records}</span></h3>");
        var deciders = batch.Deciders.Count > 0
            ? string.Join(", ", batch.Deciders.Select(decider => BookRender.Esc(decider)))
            : "no decider";
        var waiting = batch.Records
            .SelectMany(entry => entry.Awaiting)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var awaiting = $"<b>{batch.Awaiting}</b>"
            + (waiting.Count > 0 ? " " + string.Join(", ", waiting.Select(id => BookRender.RecordLink(index, rel, id))) : "");
        parts.Add("<dl class=\"facts\">"
            + $"<div><dt>Deciders</dt><dd>{deciders}</dd></div>"
            + $"<div><dt>Decide by</dt><dd>{(batch.DecideBy is { Length: > 0 } decideBy ? BookRender.Esc(decideBy) : "no date")}</dd></div>"
            + $"<div><dt>Awaiting</dt><dd>{awaiting}</dd></div></dl>");
        parts.Add($"<p><code>litespec model approve --batch {BookRender.Esc(batch.Selector)} --by {BookRender.Esc(ApproveOwner(batch))}</code></p>");
        var rows = string.Concat(batch.Records.Select(entry =>
        {
            var id = entry.Record.Id;
            var title = index.ById.TryGetValue(id, out var found) ? BookRender.Esc(found["title"]) : BookRender.Esc(id);
            return $"<tr><td><a href=\"{rel}{BookModel.RecordUrl(id)}\">{title}</a>"
                + $"<div class=\"mono dim dev-only\">{BookRender.Esc(id)}</div></td>"
                + $"<td>{BookRender.Esc(BookModel.TypeLabel(entry.Record.Type))}</td>"
                + $"<td>{BookRender.StatusChip(entry.Record.Status)}</td>"
                + $"<td>{BookRender.Esc(FlagWords(entry))}</td>"
                + $"<td>{BookRender.Esc(ScenarioWords(entry.Scenarios))}</td></tr>";
        }));
        parts.Add("<div class=\"tablewrap\"><table class=\"tbl\">"
            + "<thead><tr><th>Record</th><th>Type</th><th>Status</th><th>Flags</th><th>Scenarios</th></tr></thead>"
            + $"<tbody>{rows}</tbody></table></div>");
        return string.Join("\n", parts);
    }

    /// <summary>The owner the batch approve command names: its first decider, else its first record owner, else the owner placeholder.</summary>
    static string ApproveOwner(ReviewBatch batch) =>
        batch.Deciders.FirstOrDefault()
        ?? batch.Records.Select(entry => entry.Record.Owner).FirstOrDefault(owner => owner is { Length: > 0 })
        ?? "<owner>";

    /// <summary>One queued record's flags: reopened, full re-read, and open questions.</summary>
    static string FlagWords(ReviewEntry entry)
    {
        var flags = new List<string>();
        if (entry.Reopened)
        {
            flags.Add("reopened");
        }

        if (entry.FullReRead)
        {
            flags.Add("full re-read");
        }

        if (entry.OpenQuestions > 0)
        {
            flags.Add(entry.OpenQuestions == 1 ? "1 open question" : $"{entry.OpenQuestions} open questions");
        }

        return flags.Count > 0 ? string.Join(", ", flags) : "none";
    }

    /// <summary>One queued record's scenario proof state in words.</summary>
    static string ScenarioWords(ReviewScenarios scenarios)
    {
        if (scenarios.Total == 0)
        {
            return "No scenarios";
        }

        var words = scenarios.Total == 1
            ? $"1 scenario, {scenarios.Confirmed} confirmed, {scenarios.Passing} passing"
            : $"{scenarios.Total} scenarios, {scenarios.Confirmed} confirmed, {scenarios.Passing} passing";
        return scenarios.Stale > 0 ? $"{words}, {scenarios.Stale} stale" : words;
    }

    /// <summary>Renders the ballots page: every open question grouped by ballot, tier 1 first.</summary>
    public static string RenderBallots(BookIndex index, string rel)
    {
        var ballots = BookModel.BallotsOf(index);
        var total = ballots.Sum(ballot => ballot.Questions.Count);
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader(
            "Ballots",
            "Ballots",
            $"Every open question grouped by ballot, tier 1 first. A tier-3 question with a recommendation auto-approves {index.VetoWindowDays} days after its decide-by date.",
            [
                ("Ballots", $"<b>{ballots.Count}</b>"),
                ("Open questions", $"<b>{total}</b>"),
            ]));
        if (total == 0)
        {
            parts.Add("<p>No open question is recorded.</p>");
            return string.Join("\n", parts);
        }

        foreach (var ballot in ballots)
        {
            var title = ballot.Name ?? "No ballot";
            var slug = BallotSlug(title);
            var tier1 = ballot.Questions.Count(question => question.Tier == 1);
            parts.Add($"<h2 id=\"ballot-{slug}\">{BookRender.Esc(title)} <span class=\"dim\">{ballot.Questions.Count} questions{(tier1 > 0 ? $", {tier1} tier 1" : "")}</span></h2>");
            var rows = string.Concat(ballot.Questions.Select((question, position) =>
            {
                var id = BookRender.StrProp(question.Record, "id") ?? "";
                var auto = question.Tier == 3 && question.Recommended is { Length: > 0 }
                    ? BookModel.AutoApprovalDate(index, question.DecideBy)
                    : "";
                var approval = question.Tier == 1
                    ? "Needs explicit approval"
                    : auto.Length > 0 ? $"Auto-approves {auto}" : "No recommendation yet";
                return $"<tr><td class=\"num\">{position + 1}</td>"
                    + $"<td>{BookRender.Esc(question.Question)}<div class=\"dim\">On {BookRender.RecordLink(index, rel, id)}</div></td>"
                    + $"<td>{BookRender.Esc(question.Recommended ?? "")}</td>"
                    + $"<td>{BookRender.Esc(question.Decider ?? "no decider")}</td>"
                    + $"<td>{BookRender.Esc(question.DecideBy ?? "no date")}</td>"
                    + $"<td>{BookRender.Esc(approval)}</td>"
                    + $"<td class=\"num\">{question.Tier}</td></tr>";
            }));
            parts.Add("<div class=\"tablewrap\"><table class=\"tbl\">"
                + "<thead><tr><th></th><th>Question</th><th>Recommended</th><th>Decider</th><th>Decide By</th><th>Approval</th><th>Tier</th></tr></thead>"
                + $"<tbody>{rows}</tbody></table></div>");
        }

        return string.Join("\n", parts);
    }

    static string BallotSlug(string title)
    {
        var built = new System.Text.StringBuilder();
        foreach (var ch in title.ToLowerInvariant())
        {
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                built.Append(ch);
            }
            else if (ch is ' ' or '-' or '_' or '/' or '.')
            {
                built.Append('-');
            }
        }

        var slug = built.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return slug.Length > 0 ? slug : "ballot";
    }

    /// <summary>Renders the glossary: one card per term.</summary>
    public static string RenderGlossary(BookIndex index, string rel)
    {
        var terms = index.ByType.TryGetValue("term", out var found) ? found : [];
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader("Glossary", "Glossary", $"{terms.Count} terms."));
        foreach (var term in terms)
        {
            var fields = BookRender.Fields(term) ?? new JsonObject();
            var labels = fields["labels"] switch
            {
                JsonObject table => string.Join(", ", table.Select(pair => $"{pair.Key}: {Template(pair.Value)}")),
                JsonArray table => string.Join(", ", table.Select((value, position) => $"{position}: {Template(value)}")),
                _ => "",
            };
            var rejected = new List<string>();
            foreach (var entry in fields["rejected"] as JsonArray ?? [])
            {
                JsonNode? word = entry switch
                {
                    JsonObject obj => obj["word"],
                    JsonArray => null,
                    _ => entry,
                };
                if (BookRender.IsTruthy(word))
                {
                    rejected.Add(BookRender.Str(word));
                }
            }

            var aliases = fields["aliases"] is JsonArray names
                ? string.Join(", ", names.Select(BookRender.Str).Where(part => part.Length > 0))
                : "";
            parts.Add($"<div class=\"card term\"><div class=\"h\"><b><a href=\"{rel}{BookModel.RecordUrl(BookRender.StrProp(term, "id") ?? "")}\">{BookRender.Esc(term["title"])}</a></b>"
                + (labels.Length > 0 ? $"<span>{BookRender.Esc(labels)}</span>" : "")
                + (BookRender.IsTruthy(fields["layer"]) ? $"<span class=\"chip\">{BookRender.Esc(fields["layer"])}</span>" : "") + "</div>"
                + (BookRender.IsTruthy(fields["meaning"]) ? $"<p>{BookRender.Esc(fields["meaning"])}</p>" : "")
                + (aliases.Length > 0 ? $"<p class=\"dim\">Also called: {BookRender.Esc(aliases)}.</p>" : "")
                + (rejected.Count > 0 ? $"<p class=\"dim\">Not: {BookRender.Esc(string.Join(", ", rejected))}.</p>" : "") + "</div>");
        }

        if (terms.Count == 0)
        {
            parts.Add("<p>No term is recorded.</p>");
        }

        return string.Join("\n", parts);
    }

    static string Template(JsonNode? value) => value is null ? "null" : BookRender.Str(value);

    /// <summary>Renders the search page with the index embedded, so search works when the site opens as files.</summary>
    public static string RenderSearch(string rel, JsonArray entries)
    {
        // The index is embedded in the page so search works when the site opens
        // as files, where fetch cannot load search.json. The container id is
        // unique to this page: no section heading on any page may reuse it.
        _ = rel;
        var embedded = BookRender.JsonCompact(entries).Replace("<", "\\u003c", StringComparison.Ordinal);
        return BookRender.PageHeader("Search", "Search", "Filter every record by its title, statement, meaning or scenario.")
            + "\n<div id=\"search-results\" role=\"status\" aria-live=\"polite\"><p>Type above to search.</p></div>"
            + $"\n<script>window.bookSearchIndex = {embedded};</script>";
    }

    /// <summary>Renders the goal index: one card per goal with its verified share.</summary>
    public static string RenderGoalIndex(BookIndex index, string rel)
    {
        var goals = index.ByType.TryGetValue("goal", out var found) ? found : [];
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader("Goals", "Goals", "One lens per goal: from the goal through its features and requirements to the verified state."));
        foreach (var goal in goals)
        {
            var lens = BookModel.GoalLensOf(index, BookRender.StrProp(goal, "id") ?? "");
            var fields = BookRender.Fields(goal) ?? new JsonObject();
            parts.Add($"<div class=\"card\"><div class=\"h\"><b><a href=\"{rel}{BookModel.GoalUrl(BookRender.StrProp(goal, "id") ?? "")}\">{BookRender.Esc(goal["title"])}</a></b>"
                + $"<span>{lens?.Verified ?? 0} of {lens?.Total ?? 0} requirements verified</span></div>"
                + (BookRender.IsTruthy(fields["statement"]) ? $"<p>{BookRender.Esc(fields["statement"])}</p>" : "") + "</div>");
        }

        if (goals.Count == 0)
        {
            parts.Add("<p>No goal is recorded.</p>");
        }

        return string.Join("\n", parts);
    }

    /// <summary>Renders one goal's lens: its features with their requirements and proof state.</summary>
    public static string RenderGoalLens(BookIndex index, string rel, JsonObject goal)
    {
        var goalId = BookRender.StrProp(goal, "id") ?? "";
        var lens = BookModel.GoalLensOf(index, goalId);
        var parts = new List<string>();
        parts.Add(BookRender.PageHeader($"Lens · {BookModel.TypeLabel("goal")}", BookRender.Str(goal["title"]), "",
            [
                ("Features", $"<b>{lens?.Features.Count ?? 0}</b>"),
                ("Requirements", $"<b>{lens?.Total ?? 0}</b>"),
                ("Verified", $"<b>{lens?.Verified ?? 0}</b>"),
            ]));
        var about = RenderAbout(index, rel, goal);
        if (about != "")
        {
            parts.Add(about);
        }

        var fieldsHtml = RenderFields(index, rel, goal);
        if (fieldsHtml != "")
        {
            parts.Add(fieldsHtml);
        }

        if (lens is null || lens.Features.Count == 0)
        {
            parts.Add("<p>No feature serves this goal yet.</p>");
            return string.Join("\n", parts);
        }

        foreach (var feature in lens.Features)
        {
            var featureId = BookRender.StrProp(feature.Feature, "id") ?? "";
            parts.Add($"<h2>{BookRender.Esc(feature.Feature["title"])}</h2>");
            parts.Add($"<p>{BookRender.RecordLink(index, rel, featureId)}</p>");
            if (feature.Requirements.Count == 0)
            {
                parts.Add("<p>This feature carries no requirement yet.</p>");
                continue;
            }

            var rows = string.Concat(feature.Requirements.Select(requirement =>
            {
                var state = BookModel.RequirementStateOf(index, BookRender.StrProp(requirement, "id"));
                var chip = state.Stale
                    ? "<span class=\"chip risk\">Stale</span>"
                    : state.Verified ? "<span class=\"chip ok\">Verified</span>" : "<span class=\"chip warn\">Awaiting proof</span>";
                return $"<tr><td>{BookRender.RecordLink(index, rel, BookRender.StrProp(requirement, "id") ?? "")}</td>"
                    + $"<td>{BookRender.EscOr(BookRender.Fields(requirement)?["kind"], "")}</td>"
                    + $"<td>{chip}</td></tr>";
            }));
            parts.Add("<div class=\"tablewrap\"><table class=\"tbl\">"
                + "<thead><tr><th>Requirement</th><th>Kind</th><th>State</th></tr></thead>"
                + $"<tbody>{rows}</tbody></table></div>");
        }

        return string.Join("\n", parts);
    }
}
