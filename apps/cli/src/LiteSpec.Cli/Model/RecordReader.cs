using System.Text.Json;
using LiteSpec.Authoring;

namespace LiteSpec.Model;

/// <summary>
///     Finds every written record: page records, entries and terms.
/// </summary>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
static class RecordReader
{
    /// <summary>Container page styles: record files that hold entries but no page record.</summary>
    static readonly HashSet<string> ContainerStyles = new(StringComparer.Ordinal)
    {
        RecordTypes.PermissionList,
        RecordTypes.EntryPointList,
        RecordTypes.SettingSet,
        RecordTypes.RuleSet,
        RecordTypes.RetentionSchedule,
    };

    /// <summary>Reads every record under the configured roots.</summary>
    /// <param name="config">The consumer configuration holding the record roots and glossary.</param>
    /// <returns>The records, bodies, provisions and problems the read found, each sorted by file and line.</returns>
    public static RecordRead Read(ConsumerConfig config)
    {
        var files = new List<string>();

        foreach (var root in config.RecordRoots)
        {
            files.AddRange(SourceTree.Files(root, RecordFile.Extension, RecordFile.ExplanationExtension));
        }

        files.RemoveAll(file => file.Equals(config.GlossaryPath, StringComparison.OrdinalIgnoreCase));

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
                if (file.EndsWith(RecordFile.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    ReadRecordFile(file, local.Records, local.Bodies, local.Problems);
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
        DeriveResults(records, config.ForbiddenCode, config.InvalidInputCode);

        return new RecordRead(records, bodies, ReadProvisions(), problems);
    }

    /// <summary>Whether the document opens with a frontmatter fence line, so a parse failure is reported.</summary>
    /// <param name="text">The page text to test.</param>
    /// <returns>True when the first line of the text is the frontmatter fence.</returns>
    private static bool HasFrontmatterFence(string text)
    {
        var firstLine = text.Split('\n')[0].TrimEnd('\r');

        return string.Equals(firstLine, "---", StringComparison.Ordinal);
    }

    /// <summary>Reads one record file into records, an explanation body and problems, all appended in place.</summary>
    /// <param name="file">The absolute path of the record file to read.</param>
    /// <param name="records">The records read so far, appended in place.</param>
    /// <param name="bodies">The explanation and document bodies read so far, appended in place.</param>
    /// <param name="problems">The problems found so far, appended in place.</param>
    private static void ReadRecordFile(
        string file,
        List<Record> records,
        List<PageBody> bodies,
        List<Finding> problems)
    {
        var text = SourceTree.Read(file);

        if (!RecordFile.TryParse(text, out var root, out var tree, out var parseError))
        {
            problems.Add(new Finding(
                "record-invalid",
                $"Record file '{file}' could not be read: {parseError}",
                file,
                ErrorLine(parseError),
                null));
            return;
        }

        string? pageId = null;
        string? pageOwner = null;
        string? pageStatus = null;
        string? pageDecider = null;
        string? pageDecideBy = null;

        var type = root.ValueKind == JsonValueKind.Object ? StringField(root, RecordFields.Type) : null;

        if (type is null || (!RecordTypes.All.Contains(type) && !ContainerStyles.Contains(type)))
        {
            problems.Add(new Finding(
                "record-invalid",
                type is null
                    ? $"Record file '{file}' holds no record: its top level names no record type."
                    : $"Record file '{file}' holds no record: type '{type}' is neither a record type nor a container page style.",
                file,
                1,
                null));
        }
        else if (RecordTypes.All.Contains(type))
        {
            var explanationFile = RecordFile.ExplanationPathOf(file);
            var explanation = File.Exists(explanationFile) ? SourceTree.Read(explanationFile) : null;
            var record = BuildRecord(
                root, file, 1, false, null, null, null, null, null,
                explanation is null ? null : explanationFile, explanation);

            pageId = record.Id;
            pageOwner = record.Owner;
            pageStatus = record.Status;
            pageDecider = record.Decider;
            pageDecideBy = record.DecideBy;
            records.Add(record);

            if (explanation is not null)
            {
                bodies.Add(new PageBody(explanationFile, explanation, 1, pageId));
            }
        }
        else
        {
            pageOwner = StringField(root, RecordFields.Owner);
            pageStatus = StringField(root, RecordFields.Status);
            pageDecider = StringField(root, RecordFields.Decider);
            pageDecideBy = StringField(root, RecordFields.DecideBy);

            var explanationFile = RecordFile.ExplanationPathOf(file);

            if (File.Exists(explanationFile))
            {
                bodies.Add(new PageBody(explanationFile, SourceTree.Read(explanationFile), 1, null));
            }
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty(RecordFields.Records, out var entries)
            && entries.ValueKind == JsonValueKind.Array)
        {
            var lines = tree.TryGetProperty(RecordFields.Records, out var treeRecords)
                && treeRecords.Kind == JsonKind.Array
                    ? treeRecords.Items
                    : null;
            var index = 0;

            foreach (var entry in entries.EnumerateArray())
            {
                var line = lines is not null && index < lines.Count ? lines[index].Line : 1;
                index++;

                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var entryType = StringField(entry, RecordFields.Type);

                if (!RecordTypes.All.Contains(entryType ?? ""))
                {
                    problems.Add(new Finding(
                        "field-invalid",
                        $"Entry type '{entryType ?? "missing"}' is not a record type.",
                        file,
                        line,
                        pageId));
                    continue;
                }

                records.Add(BuildRecord(
                    entry, file, line, true, pageId,
                    pageOwner, pageStatus, pageDecider, pageDecideBy, null, null));
            }
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

    /// <summary>The From of a transition that creates the instance.</summary>
    public const string Creation = "-";

    /// <summary>Builds one record from its element, inheriting page owner, status, decider and decide-by.</summary>
    /// <param name="element">The written element carrying the record fields.</param>
    /// <param name="file">The absolute path of the record file.</param>
    /// <param name="line">The 1-based line of the record in its file.</param>
    /// <param name="isEntry">Whether the record is an entry on its page rather than the page itself.</param>
    /// <param name="pageId">The id of the carrying page, or null for a page record.</param>
    /// <param name="pageOwner">The page owner inherited when the record names none.</param>
    /// <param name="pageStatus">The page status inherited when the record names none.</param>
    /// <param name="pageDecider">The page decider inherited when the record names none.</param>
    /// <param name="pageDecideBy">The page decide-by date inherited when the record names none.</param>
    /// <param name="explanationFile">The explanation file path, for a page record with one beside it.</param>
    /// <param name="explanation">The explanation file's full text, for a page record with one beside it.</param>
    /// <returns>The built record.</returns>
    private static Record BuildRecord(
        JsonElement element,
        string file,
        int line,
        bool isEntry,
        string? pageId,
        string? pageOwner,
        string? pageStatus,
        string? pageDecider,
        string? pageDecideBy,
        string? explanationFile,
        string? explanation) =>
        new()
        {
            Type = StringField(element, RecordFields.Type) ?? "",
            Id = StringField(element, RecordFields.Id) ?? "",
            Title = StringField(element, RecordFields.Title) ?? "",
            Owner = StringField(element, RecordFields.Owner) ?? pageOwner,
            Status = StringField(element, RecordFields.Status) ?? pageStatus,
            Decider = StringField(element, RecordFields.Decider) ?? pageDecider,
            DecideBy = StringField(element, RecordFields.DecideBy) ?? pageDecideBy,
            ApprovedBy = StringField(element, RecordFields.ApprovedBy),
            ApprovedOn = StringField(element, RecordFields.ApprovedOn),
            ApprovedHash = StringField(element, RecordFields.ApprovedHash),
            File = file,
            Line = line,
            ExplanationFile = explanationFile,
            Explanation = explanation,
            IsEntry = isEntry,
            PageId = pageId,
            Raw = element.Clone(),
        };

    /// <summary>
    ///     Derives the generic refusal results: forbidden for every use case with a permission
    ///     grant, and invalid-input for every use case with a validated input. Scenarios and
    ///     tests point at them; they are never written as records.
    /// </summary>
    /// <param name="records">The written records, appended in place with the derived results.</param>
    /// <param name="forbiddenCode">The refusal code the derived forbidden results carry.</param>
    /// <param name="invalidInputCode">The refusal code the derived invalid-input results carry.</param>
    private static void DeriveResults(List<Record> records, string forbiddenCode, string invalidInputCode)
    {
        var taken = records
            .Where(record => record.Id.Length > 0)
            .Select(record => record.Id)
            .ToHashSet(StringComparer.Ordinal);
        var derived = new List<Record>();

        foreach (var useCase in records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!Identifiers.TrySplit(useCase.Id, out _, out var home, out var name) || home is null)
            {
                continue;
            }

            if (ModelBuilder.GrantedPermission(useCase) is not null
                && taken.Add($"result/{name}.forbidden"))
            {
                derived.Add(DerivedResult(
                    useCase,
                    $"result/{name}.forbidden",
                    "Forbidden",
                    "The caller lacks the grant the use case requires.",
                    forbiddenCode));
            }

            if (ModelBuilder.HasValidatedInput(useCase)
                && taken.Add($"result/{name}.invalid-input"))
            {
                derived.Add(DerivedResult(
                    useCase,
                    $"result/{name}.invalid-input",
                    "Invalid input",
                    "An input value breaks the rules the use case declares.",
                    invalidInputCode));
            }
        }

        records.AddRange(derived);
    }

    /// <summary>One derived generic result, carried by its use case's page.</summary>
    /// <param name="useCase">The use case the derived result belongs to.</param>
    /// <param name="id">The derived result id.</param>
    /// <param name="title">The derived result title.</param>
    /// <param name="meaning">The derived result meaning sentence.</param>
    /// <param name="code">The refusal code the derived result carries.</param>
    /// <returns>The derived result record, marked derived and carried by the use-case page.</returns>
    private static Record DerivedResult(Record useCase, string id, string title, string meaning, string code)
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [RecordFields.Type] = RecordTypes.Result,
            [RecordFields.Id] = id,
            [RecordFields.Title] = title,
            [RecordFields.Outcome] = "refusal",
            [RecordFields.Code] = code,
            [RecordFields.Meaning] = meaning,
        });

        using var document = JsonDocument.Parse(json);

        return new Record
        {
            Type = RecordTypes.Result,
            Id = id,
            Title = title,
            Owner = useCase.Owner,
            Status = useCase.Status,
            Decider = useCase.Decider,
            DecideBy = useCase.DecideBy,
            File = useCase.File,
            Line = useCase.Line,
            IsEntry = true,
            PageId = useCase.Id,
            Derived = true,
            Raw = document.RootElement.Clone(),
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

        var text = SourceTree.Read(config.GlossaryPath);

        if (!RecordFile.TryParse(text, out var root, out var tree, out var error))
        {
            problems.Add(new Finding(
                "field-invalid", $"The glossary is not YAML: {error}",
                config.GlossaryPath, ErrorLine(error), null));
            return;
        }

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(RecordFields.Terms, out var terms)
            || terms.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var lines = tree.TryGetProperty(RecordFields.Terms, out var treeTerms)
            && treeTerms.Kind == JsonKind.Array
                ? treeTerms.Items
                : null;
        var index = 0;

        foreach (var term in terms.EnumerateArray())
        {
            var line = lines is not null && index < lines.Count ? lines[index].Line : 1;
            index++;

            if (term.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            records.Add(new Record
            {
                Type = RecordTypes.Term,
                Id = StringField(term, RecordFields.Id) ?? "",
                Title = StringField(term, RecordFields.Term) ?? "",
                Owner = StringField(term, RecordFields.Owner),
                Status = StringField(term, RecordFields.Status),
                Decider = StringField(term, RecordFields.Decider),
                DecideBy = StringField(term, RecordFields.DecideBy),
                ApprovedBy = StringField(term, RecordFields.ApprovedBy),
                ApprovedOn = StringField(term, RecordFields.ApprovedOn),
                ApprovedHash = StringField(term, RecordFields.ApprovedHash),
                File = config.GlossaryPath,
                Line = line,
                IsEntry = true,
                Raw = term.Clone(),
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

    /// <summary>Reads one optional string field of an element.</summary>
    /// <param name="element">The element to read.</param>
    /// <param name="name">The field name to read.</param>
    /// <returns>The field string value, or null when the field is missing or not a string.</returns>
    private static string? StringField(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>The 1-based line a parse error names, or 1 when it names none.</summary>
    /// <param name="error">The parse error, ending in "at line N.".</param>
    /// <returns>The named line, or 1.</returns>
    private static int ErrorLine(string error)
    {
        const string marker = "at line ";
        var at = error.LastIndexOf(marker, StringComparison.Ordinal);

        return at >= 0
            && int.TryParse(error[(at + marker.Length)..].TrimEnd('.'), out var line)
            && line > 0
                ? line
                : 1;
    }
}
