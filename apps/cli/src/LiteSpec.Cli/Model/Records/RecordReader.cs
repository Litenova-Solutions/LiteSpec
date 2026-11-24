using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Records;

/// <summary>
///     Finds every written record under the consumer's roots: the records each record file
///     yields, the glossary terms, and the derived refusal results. The file rules that decide
///     what a record is live in <see cref="RecordFiles"/>; this reader adds the consumer's
///     own rules: which files it walks, the explanation beside each file, and the derived results.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordReader
{
    /// <summary>The From of a transition that creates the instance.</summary>
    public const string Creation = "-";

    /// <summary>Reads every record under the configured roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots and glossary.</param>
    /// <returns>The records, bodies, provisions and problems the read found, each sorted by file and line.</returns>
    public static RecordRead Read(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in config.RecordRoots)
        {
            files.AddRange(SourceTree.Files(root, RecordFiles.Extension, RecordFile.ExplanationExtension));
        }

        // The generated model and book under the record root hold no records.
        files.RemoveAll(file => IsGeneratedOutput(config, file));
        files.RemoveAll(file => file.Equals(config.GlossaryPath, StringComparison.OrdinalIgnoreCase));

        // The project file and the UI pattern catalog are YAML under the record root, but hold no record.
        files.RemoveAll(file => file.Equals(ConsumerRoot.ProjectFile(config.Root), StringComparison.OrdinalIgnoreCase)
            || file.Equals(config.UiPatternCatalog, StringComparison.OrdinalIgnoreCase));

        var records = new List<Record>();
        var bodies = new List<PageBody>();
        var problems = new List<Finding>();
        var gate = new object();

        System.Threading.Tasks.Parallel.ForEach(
            files,
            () => (Records: new List<Record>(), Bodies: new List<PageBody>(), Problems: new List<Finding>()),
            (file, _, local) =>
            {
                // SourceTree matches extensions without regard to case: a record file in any
                // casing still reads as one.
                if (file.EndsWith(RecordFiles.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    ReadRecordFile(config, file, local.Records, local.Bodies, local.Problems);
                }
                else
                {
                    ReadDocument(config, file, local.Bodies, local.Problems);
                }

                return local;
            },
            local =>
            {
                lock (gate)
                {
                    records.AddRange(local.Records);
                    bodies.AddRange(local.Bodies);
                    problems.AddRange(local.Problems);
                }
            });

        records.Sort((left, right) =>
        {
            var file = string.Compare(left.File, right.File, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });
        bodies.Sort((left, right) => string.Compare(left.File, right.File, StringComparison.Ordinal));
        problems.Sort((left, right) =>
        {
            var file = string.Compare(left.Path, right.Path, StringComparison.Ordinal);

            return file != 0 ? file : left.Line.CompareTo(right.Line);
        });

        ReadTerms(config, records, problems);
        DeriveResults(config, records, config.ForbiddenCode, config.InvalidInputCode);

        return new RecordRead(records, bodies, ReadProvisions(), problems);
    }

    /// <summary>Whether the file sits in the generated model or book folder under a record root.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="file">The file to test.</param>
    /// <returns>True when the file is generated output under <c>&lt;root&gt;/model/</c> or <c>&lt;root&gt;/book/</c>.</returns>
    private static bool IsGeneratedOutput(ConsumerConfig config, string file)
    {
        foreach (var root in config.RecordRoots)
        {
            foreach (var folder in new[] { "model", "book" })
            {
                var prefix = Path.GetFullPath(Path.Combine(root, folder)) + Path.DirectorySeparatorChar;

                if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Whether the document opens with a frontmatter fence line, so a parse failure is reported.</summary>
    /// <param name="text">The page text to test.</param>
    /// <returns>True when the first line of the text is the frontmatter fence.</returns>
    private static bool HasFrontmatterFence(string text)
    {
        var firstLine = text.Split('\n')[0].TrimEnd('\r');

        return string.Equals(firstLine, "---", StringComparison.Ordinal);
    }

    /// <summary>
    ///     Reads one record file into records, its explanation body and problems, all appended in place.
    ///     The page record and its entries come from <see cref="RecordFiles.Scan"/>; the explanation
    ///     beside the file is the consumer's own.
    /// </summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="file">The absolute path of the record file to read.</param>
    /// <param name="records">The records read so far, appended in place.</param>
    /// <param name="bodies">The explanation and document bodies read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadRecordFile(
        ConsumerConfig config,
        string file,
        List<Record> records,
        List<PageBody> bodies,
        List<Finding> problems)
    {
        // A module's refusal set holds refusal codes, not records; RefusalSetReader reads it.
        if (Path.GetFileName(file) == "refusals.yaml")
        {
            return;
        }

        var scan = RecordFiles.Scan(file, SourceTree.Read(file));

        problems.AddRange(scan.Problems);

        if (scan.Root is null)
        {
            return;
        }

        var explanationFile = RecordFile.ExplanationPathOf(file);
        var explanation = File.Exists(explanationFile) ? SourceTree.Read(explanationFile) : null;
        var pageId = scan.Records.FirstOrDefault(entry => !entry.IsEntry)?.Id;

        foreach (var entry in scan.Records)
        {
            records.Add(entry.IsEntry
                ? BuildRecord(config, entry, null, null)
                : BuildRecord(config, entry, explanation is null ? null : explanationFile, explanation));
        }

        if (explanation is not null)
        {
            bodies.Add(new PageBody(explanationFile, explanation, 1, pageId));
        }
    }

    /// <summary>
    ///     Reads one document into a body and problems, all appended in place. An explanation
    ///     file is skipped: its record file's read yields its body.
    /// </summary>
    /// <param name="config">The consumer configuration holding the glossary path.</param>
    /// <param name="file">The absolute path of the document to read.</param>
    /// <param name="bodies">The explanation and document bodies read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadDocument(
        ConsumerConfig config,
        string file,
        List<PageBody> bodies,
        List<Finding> problems)
    {
        var recordPath = RecordFile.RecordPathOf(file);

        if (!recordPath.Equals(config.GlossaryPath, StringComparison.OrdinalIgnoreCase) && File.Exists(recordPath))
        {
            return;
        }

        var text = SourceTree.Read(file);

        if (!Frontmatter.TryParse(text, out var parsed, out var parseError))
        {
            if (HasFrontmatterFence(text))
            {
                problems.Add(new Finding(
                    "record-invalid",
                    $"Document '{file}' could not be read: {parseError}",
                    file,
                    1,
                    null));
            }

            bodies.Add(new PageBody(file, text, 1, null));
            return;
        }

        bodies.Add(new PageBody(file, parsed.Body, parsed.BodyStartLine, null));
    }

    /// <summary>Builds one CLI record from the record the file reader yielded.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="indexed">The record the file reader yielded.</param>
    /// <param name="explanationFile">The explanation file path, for a page record with one beside it.</param>
    /// <param name="explanation">The explanation file's full text, for a page record with one beside it.</param>
    /// <returns>The built record, with its folder, its module and its fields as JSON.</returns>
    private static Record BuildRecord(
        ConsumerConfig config,
        IndexRecord indexed,
        string? explanationFile,
        string? explanation) => new()
        {
            Type = indexed.Type,
            Id = indexed.Id,
            Folder = RecordPlacement.FolderOf(indexed.File, config),
            Module = RecordPlacement.ModuleOf(indexed.File, config),
            Title = indexed.Title,
            File = indexed.File,
            Line = indexed.Line,
            ExplanationFile = explanationFile,
            Explanation = explanation,
            IsEntry = indexed.IsEntry,
            PageId = indexed.PageId,
            Raw = RecordFile.ElementOf(indexed.Tree),
        };

    /// <summary>
    ///     Derives the generic refusal results from each use case's access, inputs, kind and
    ///     knowledge reads. Scenarios and tests point at them; they are never written as records.
    ///     A derived not-found or rule-not-stated result carries the refusal of the product-level
    ///     rule that triggers it, so the product code is declared without restating it.
    /// </summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="records">The written records, appended in place with the derived results.</param>
    /// <param name="forbiddenCode">The refusal code the derived forbidden results carry.</param>
    /// <param name="invalidInputCode">The refusal code the derived invalid-input results carry.</param>
    private static void DeriveResults(ConsumerConfig config, List<Record> records, string forbiddenCode, string invalidInputCode)
    {
        var taken = records
            .Where(record => record.Id.Length > 0)
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);
        var byId = records
            .Where(record => record.Id.Length > 0)
            .GroupBy(record => record.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var derived = new List<Record>();

        foreach (var useCase in records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out var home, out var name) || home is null)
            {
                continue;
            }

            foreach (var code in RefusalSetReader.DeriveFor(useCase, byId))
            {
                var id = $"result/{name}.{code}";

                if (!taken.Add(id))
                {
                    continue;
                }

                var refusal = RefusalSetReader.ProductRefusalFor(useCase, code, byId);
                var wire = code switch
                {
                    "forbidden" => forbiddenCode,
                    "invalid-input" => invalidInputCode,
                    _ => code,
                };

                if (refusal is not null && RefusalCode.CodeOf(refusal) is { } product)
                {
                    wire = product;
                }

                derived.Add(DerivedResult(
                    config,
                    useCase,
                    id,
                    DerivedTitle(code),
                    DerivedMeaning(code),
                    wire,
                    refusal));
            }
        }

        records.AddRange(derived);
    }

    /// <summary>The title of a derived generic refusal result.</summary>
    /// <param name="code">The generic refusal code.</param>
    /// <returns>The result title.</returns>
    private static string DerivedTitle(string code) => code switch
    {
        "forbidden" => "Forbidden",
        "invalid-input" => "Invalid input",
        "not-found" => "Not found",
        "conflict" => "Conflict",
        "unauthenticated" => "Unauthenticated",
        "sign-in-too-old" => "Sign in too old",
        "rule-not-stated" => "Rule not stated",
        _ => code,
    };

    /// <summary>The meaning of a derived generic refusal result.</summary>
    /// <param name="code">The generic refusal code.</param>
    /// <returns>The result meaning.</returns>
    private static string DerivedMeaning(string code) => code switch
    {
        "forbidden" => "The caller lacks the grant the use case requires.",
        "invalid-input" => "An input value breaks the rules the use case declares.",
        "not-found" => "The record the input names does not exist.",
        "conflict" => "The record changed since the caller read it.",
        "unauthenticated" => "The caller is not signed in.",
        "sign-in-too-old" => "The caller's sign-in is older than the window the use case requires.",
        "rule-not-stated" => "The knowledge the use case reads states no rule for the asked key or day.",
        _ => code,
    };

    /// <summary>One derived generic result, carried by its use case's page.</summary>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <param name="useCase">The use case the derived result belongs to.</param>
    /// <param name="id">The derived result id.</param>
    /// <param name="title">The derived result title.</param>
    /// <param name="meaning">The derived result meaning sentence.</param>
    /// <param name="code">The refusal code the derived result carries.</param>
    /// <param name="refusal">The refusal identifier the derived result carries, or null when it keeps the generic code.</param>
    /// <returns>The derived result record, marked derived and carried by the use-case page.</returns>
    private static Record DerivedResult(ConsumerConfig config, Record useCase, string id, string title, string meaning, string code, string? refusal)
    {
        var properties = new List<KeyValuePair<string, JsonText>>
        {
            new(RecordFields.Type, JsonText.Text(RecordTypes.Result)),
            new(RecordFields.Id, JsonText.Text(id)),
            new(RecordFields.Title, JsonText.Text(title)),
            new(RecordFields.Outcome, JsonText.Text("refusal")),
        };

        if (refusal is not null)
        {
            properties.Add(new(RecordFields.Refusal, JsonText.Text(refusal)));
        }

        properties.Add(new(RecordFields.Code, JsonText.Text(code)));
        properties.Add(new(RecordFields.Meaning, JsonText.Text(meaning)));

        var fields = JsonText.ObjectOf(properties);

        return new Record
        {
            Type = RecordTypes.Result,
            Id = id,
            Folder = RecordPlacement.FolderOf(useCase.File, config),
            Module = RecordPlacement.ModuleOf(useCase.File, config),
            Title = title,
            File = useCase.File,
            Line = useCase.Line,
            IsEntry = true,
            PageId = useCase.Id,
            Derived = true,
            Raw = RecordFile.ElementOf(fields),
        };
    }

    /// <summary>Reads the glossary terms into records, reporting a glossary that is not YAML.</summary>
    /// <param name="config">The consumer configuration holding the glossary path.</param>
    /// <param name="records">The records read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadTerms(ConsumerConfig config, List<Record> records, List<Finding> problems)
    {
        if (!File.Exists(config.GlossaryPath))
        {
            return;
        }

        JsonText tree;
        var glossaryText = SourceTree.Read(config.GlossaryPath);

        try
        {
            tree = YamlText.Parse(glossaryText);
        }
        catch (JsonTextException exception)
        {
            problems.Add(new Finding(
                "record-invalid", $"The glossary is not YAML: {exception.Message}{YamlText.ColonHint(glossaryText, exception.Line) ?? string.Empty}",
                config.GlossaryPath, exception.Line, null));
            return;
        }

        if (tree.Kind != JsonKind.Object
            || !tree.TryGetProperty(RecordFields.Terms, out var terms)
            || terms.Kind != JsonKind.Array)
        {
            return;
        }

        foreach (var term in terms.Items)
        {
            if (term.Kind != JsonKind.Object)
            {
                continue;
            }

            var termId = StringOf(term, RecordFields.Id) ?? "";

            records.Add(new Record
            {
                Type = RecordTypes.Term,
                Id = termId,
                Folder = RecordPlacement.FolderOf(config.GlossaryPath, config),
                Module = RecordPlacement.ModuleOf(config.GlossaryPath, config),
                Title = StringOf(term, RecordFields.Term) ?? "",
                File = config.GlossaryPath,
                Line = term.Line,
                IsEntry = true,
                Raw = RecordFile.ElementOf(term),
            });
        }
    }

    /// <summary>Reads the provision ids of the carried LiteSpec release.</summary>
    /// <returns>The provision ids, or an empty set when no release is found.</returns>
    private static HashSet<string> ReadProvisions()
    {
        var home = LiteSpecHome.Root;

        if (home is null || !Directory.Exists(Path.Combine(home, "docs")))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return new HashSet<string>(
            Provisions.Collect(home).Select(row => row.Id),
            StringComparer.Ordinal);
    }

    /// <summary>Reads one optional string field of a tree object.</summary>
    /// <param name="node">The tree object to read.</param>
    /// <param name="name">The field name to read.</param>
    /// <returns>The field string value, or null when the field is missing or not a string.</returns>
    private static string? StringOf(JsonText node, string name) =>
        node.TryGetProperty(name, out var value) ? value.GetString() : null;
}
