using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using LiteSpec.Model;
using LiteSpec.Schemas;

namespace LiteSpec.Consumer;

/// <summary>
///     The glossary and prose checks of the consumer validator: the glossary
///     against its schema, and every page and scanned surface against the
///     rejected synonyms and mannered terms it records.
/// </summary>
/// <follows>litespec/core/authoring#record-the-project-vocabulary-as-data</follows>
/// <follows>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</follows>
/// <follows>litespec/core/authoring#state-mannered-terms-literally</follows>
/// <follows>litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets</follows>
static class GlossaryChecks
{
    /// <summary>
    ///     Checks the glossary, scans the pages and the extra language surfaces for
    ///     rejected synonyms and mannered terms, and summarizes the outcome.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckGlossaryAndProse(ConsumerRun run)
    {
        var pagesWatch = System.Diagnostics.Stopwatch.StartNew();
        var paths = (run.Project as JsonObject)?["paths"] as JsonObject;
        var glossaryPath = JsonRead.PropString(paths, "glossary");
        var glossaryFile = glossaryPath is not null ? Path.Join(run.Root, glossaryPath) : Path.Join(run.DocsRoot, "glossary.json");
        var glossaryRelative = run.Relative(glossaryFile);
        CompiledLanguage? compiled = null;

        if (File.Exists(glossaryFile))
        {
            JsonNode? record = null;

            try
            {
                record = run.ReadJson(glossaryFile);
            }
            catch (Exception cause)
            {
                run.Err($"{glossaryRelative}: not valid JSON ({cause.Message})");
            }

            if (JsonValues.IsTruthy(record))
            {
                var shape = JsonSchema.Problems(run.GlossarySchemaFile, record, glossaryRelative, run.ReadSchema);

                foreach (var problem in shape)
                {
                    run.Err(problem);
                }

                // A scope that does not compile silently rejects nothing, which is the
                // fail-open shape this validator exists to refuse. The record is not
                // compiled afterwards, because compiling it would throw the same error
                // as an unhandled crash and print no diagnostic at all.
                var scopesOk = true;
                var terms = (record as JsonObject)?["terms"] is JsonArray array ? array.ToList() : new List<JsonNode?>();

                foreach (var entry in terms)
                {
                    if (entry is not JsonObject term)
                    {
                        continue;
                    }

                    var rejected = term["rejected"] is JsonArray synonyms ? synonyms.ToList() : new List<JsonNode?>();
                    var scopes = new List<string?> { JsonRead.PropString(term, "scope") };

                    foreach (var synonym in rejected)
                    {
                        scopes.Add(JsonRead.PropString(synonym as JsonObject, "scope"));
                    }

                    foreach (var scope in scopes)
                    {
                        if (string.IsNullOrEmpty(scope))
                        {
                            continue;
                        }

                        try
                        {
                            _ = new Regex(scope);
                        }
                        catch (ArgumentException)
                        {
                            run.Err($"{glossaryRelative}: term '{JsonRead.PropText(term, "term")}' has a scope that is not a regular expression: '{scope}'");
                            scopesOk = false;
                        }
                    }
                }

                if (shape.Count == 0 && scopesOk)
                {
                    compiled = Prose.CompileLanguage(record);
                }
            }
        }
        else if (glossaryPath is not null)
        {
            run.Err($"litespec.project.json: paths.glossary '{glossaryPath}' does not exist; the vocabulary and mannered-term checks resolve nothing");
        }

        // AGENTS.md is scanned for language even though it carries no record and
        // sits outside the documentation root. It is the first file an agent reads,
        // so its register is the register that gets copied into every page written
        // afterwards, and a rule the exemplar breaks is a rule that does not hold.
        var agentsFile = Path.Join(run.Root, "AGENTS.md");
        var languageFiles = File.Exists(agentsFile) && !run.Files.Contains(agentsFile, StringComparer.OrdinalIgnoreCase) ? run.Files.Append(agentsFile).ToList() : run.Files.ToList();

        var languageFindings = new List<string>();

        foreach (var file in languageFiles)
        {
            var relative = run.Relative(file);
            var raw = run.ReadText(file);

            // A scope is written against the documentation root. AGENTS.md sits above
            // it, so it matches no scoped rule and is checked by the mannered list.
            var scopeRelative = file == agentsFile ? relative : ConsumerRun.Relative(run.DocsRoot, file);

            Prose.CheckLanguage(scopeRelative, raw, compiled, (_, line, code, message) =>
            {
                languageFindings.Add($"{relative}:{line}: {code} {message}");
            });
        }

        pagesWatch.Stop();
        Timing.Report($"[time] consumer-glossary-pages {pagesWatch.Elapsed.TotalSeconds:0.0}s");

        // The documentation tree is one of the surfaces a word reaches, and usually
        // the smallest. A project's source, its interface copy, its API contract and
        // its tests carry the same vocabulary to a developer, a customer and a
        // reviewer, and a check that reads only Markdown holds the vocabulary where
        // nobody reads it. `paths.glossaryScan` names those surfaces.
        //
        // A scope in the glossary is written against the documentation root
        // for a page under it, and against the repository root for one of these
        // files, because there is no other root the two have in common.
        var surfacesWatch = System.Diagnostics.Stopwatch.StartNew();
        var scannedSurfaces = 0;
        var glossaryScan = paths?["glossaryScan"] is JsonArray patterns ? patterns.ToList() : new List<JsonNode?>();

        foreach (var entry in glossaryScan)
        {
            var pattern = JsonRead.AsString(entry);

            if (pattern is null)
            {
                run.Err($"litespec.project.json: paths.glossaryScan '{JsonValues.Text(entry)}' could not be read (pattern is not a string)");
                continue;
            }

            List<string> matched;

            try
            {
                matched = GlobWalker.GlobFiles(run.Root, pattern);
            }
            catch (Exception cause)
            {
                run.Err($"litespec.project.json: paths.glossaryScan '{pattern}' could not be read ({cause.Message})");
                continue;
            }

            // A pattern that matches nothing switches a surface off in silence, which
            // is the shape this validator refuses everywhere else.
            if (matched.Count == 0)
            {
                run.Err($"litespec.project.json: paths.glossaryScan '{pattern}' matches no file; the vocabulary check over that surface did not run");
                continue;
            }

            // Source files are independent, so they scan in parallel and merge in
            // match order, keeping every finding exactly where the sequential
            // scan reported it.
            var perFile = new List<string>[matched.Count];

            Parallel.For(0, matched.Count, fileIndex =>
            {
                var relativePath = matched[fileIndex];
                var relative = relativePath.Replace('\\', '/');
                var findings = new List<string>();

                Prose.CheckLanguageInSource(relative, run.ReadText(Path.Join(run.Root, relativePath)), compiled, (_, line, code, message) =>
                {
                    findings.Add($"{relative}:{line}: {code} {message}");
                });

                perFile[fileIndex] = findings;
            });

            for (var fileIndex = 0; fileIndex < matched.Count; fileIndex++)
            {
                scannedSurfaces += 1;

                foreach (var finding in perFile[fileIndex])
                {
                    languageFindings.Add(finding);
                }
            }
        }

        surfacesWatch.Stop();
        Timing.Report($"[time] consumer-glossary-surfaces {surfacesWatch.Elapsed.TotalSeconds:0.0}s");

        // Every language finding is an error. The vocabulary is the project's own,
        // so a term it rejects or a mannered term it names is a term it chose to act on.
        foreach (var finding in languageFindings)
        {
            run.Err(finding);
        }

        var manneredCount = languageFindings.Count(finding => finding.Contains("GLOSSARY_MANNERED_TERM", StringComparison.Ordinal));
        var rejectedCount = languageFindings.Count - manneredCount;
        var surfaceNote = scannedSurfaces > 0
            ? $", {scannedSurfaces} non-Markdown surface(s) scanned"
            : ", documentation only";

        run.GlossarySummary = compiled is not null
            ? $"Glossary: {manneredCount} mannered term(s), {rejectedCount} rejected synonym(s){surfaceNote}"
            : "Glossary: no glossary found; the vocabulary and mannered-term checks did not run";
    }
}
