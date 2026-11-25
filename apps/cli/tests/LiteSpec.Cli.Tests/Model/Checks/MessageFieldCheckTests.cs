using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Handlers;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the message field checks: every command or query type carries the fields its use case input names, and every result type carries the fields its output names.</summary>
public sealed class MessageFieldCheckTests
{
    /// <summary>A message carrying every declared input reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MatchingInputReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}, {"name": "loanDays", "type": "integer"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, int LoanDays);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message lacking a property for a declared input reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MissingPropertyReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}, {"name": "dueDate", "type": "date"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: no property for input 'dueDate'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message carrying a property no input names reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void ExtraPropertyReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, DateTimeOffset HandledAt);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: extra property 'HandledAt'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An id input meets the property naming its field, with no added suffix.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdNameMapsToFieldProperty()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copy", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(CopyId Copy);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>An id input with only a suffixed property reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdNameWithAddedSuffixReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copy", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: no property for input 'copy'; extra property 'CopyId'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A file input riding as the carried bytes reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void FileBytesPropertyReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "scan", "type": "file"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(byte[] Scan);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A file input riding as text reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void FileTextPropertyReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "scan", "type": "file"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(string Scan);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: property 'Scan' is 'string' for input 'scan' of type 'file'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A money input riding as an amount with its currency beside it reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MoneyAmountPairReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "deposit", "type": "money"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(decimal Deposit, string Currency);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A money input riding as one money property reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MoneySinglePropertyReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "deposit", "type": "money"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Money Deposit);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A property whose type misses a certain pair reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TextTypeMismatchReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "note", "type": "text"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(int Note);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: property 'Note' is 'int' for input 'note' of type 'text'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>An instant input riding as a DateTimeOffset reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void InstantTypeMatchesDateTimeOffset()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "borrowedAt", "type": "instant"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(DateTimeOffset BorrowedAt);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message with no file keeps no field rule.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void MissingMessageFileReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}]}""", "AbsentCommand");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A result carrying every declared output field reports nothing.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void MatchingOutputReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}, {"name": "dueDate", "type": "date"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Guid CopyId, DateOnly DueDate);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>A result lacking a property for a declared output field reports output-field-drift.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void MissingOutputPropertyReportsOutputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}, {"name": "dueDate", "type": "date"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Guid CopyId);");

        var finding = Assert.Single(Run(model, "output-field-drift"));

        Assert.Equal("output-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' result 'BorrowCopyQueryResult' drifts from its declared output: no property for output field 'dueDate'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A result holding one nested type per output group meets each group.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void MultiPartOutputReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}]}, "receipt": {"fields": [{"name": "receiptNo", "type": "text"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Loan Loan, Receipt Receipt);");
        WriteSource(folder, "Loan.cs", "internal sealed record Loan(Guid CopyId);");
        WriteSource(folder, "Receipt.cs", "internal sealed record Receipt(string ReceiptNo);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>A result holding one nested nullable type for its single optional group meets the group.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void SingleOptionalGroupReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"required": false, "fields": [{"name": "copyId", "type": "id"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Loan? Loan);");
        WriteSource(folder, "Loan.cs", "internal sealed record Loan(Guid CopyId);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>Several output groups with no property per group keep no output rule.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void MultiPartOutputWithoutBijectionReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}]}, "receipt": {"fields": [{"name": "receiptNo", "type": "text"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(object Rows);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>A command declaring no output keeps no output rule.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void CommandWithoutOutputReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command"}""", "AbsentCommand");
        WriteSource(folder, "BorrowCopyCommandHandler.cs", """
            internal sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
            {
                public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyCommandResult.cs", "internal sealed record BorrowCopyCommandResult(Guid CopyId);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>A message carrying the idempotency key its use case implies through key mode reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdempotencyKeyImpliedWhenModeIsKey()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}], "idempotency": {"mode": "key", "key": "{copyId}", "keyScope": "account", "replay": "returns-first-result"}}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, string IdempotencyKey);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message carrying an idempotency key no key mode implies reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void IdempotencyKeyStillDriftWhenModeIsNone()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}], "idempotency": {"mode": "none", "reason": "Each run lends another copy."}}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, string IdempotencyKey);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: extra property 'IdempotencyKey'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A query carrying the cursor and limit its paging implies reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void PagingCursorLimitImplied()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "query", "input": [{"name": "copyId", "type": "id"}], "paging": {"mode": "pages", "order": "Due date, soonest first.", "cursor": "The due date and identity of the last row.", "defaultLimit": 25, "maxLimit": 100}}""", "BorrowCopyQuery");
        WriteSource(folder, "BorrowCopyQuery.cs", "internal sealed record BorrowCopyQuery(Guid CopyId, string Cursor, int Limit);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A query carrying the offset its offset paging implies reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void PagingOffsetImplied()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "query", "input": [{"name": "copyId", "type": "id"}], "paging": {"mode": "pages", "order": "Due date, soonest first.", "offset": "The row number the page starts at.", "defaultLimit": 25, "maxLimit": 100}}""", "BorrowCopyQuery");
        WriteSource(folder, "BorrowCopyQuery.cs", "internal sealed record BorrowCopyQuery(Guid CopyId, int Offset, int Limit);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A query carrying a cursor its paging does not imply reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void PagingCursorStillDriftWhenModeIsNone()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "query", "input": [{"name": "copyId", "type": "id"}], "paging": {"mode": "none", "reason": "The query returns one loan."}}""", "BorrowCopyQuery");
        WriteSource(folder, "BorrowCopyQuery.cs", "internal sealed record BorrowCopyQuery(Guid CopyId, string Cursor);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyQuery' drifts from its declared input: extra property 'Cursor'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A result carrying the next cursor its paging implies reports nothing.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void PagingNextCursorImpliedOnResult()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}]}}}, "paging": {"mode": "pages", "order": "Due date, soonest first.", "cursor": "The due date and identity of the last row.", "defaultLimit": 25, "maxLimit": 100}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Guid CopyId, string NextCursor);");

        Assert.Empty(Run(model, "output-field-drift"));
    }

    /// <summary>A result carrying a property no output names or implies reports output-field-drift.</summary>
    /// <proves>litespec/core/use-cases#type-every-output-field</proves>
    [Fact]
    public void ResultExtraPropertyStillDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(
            temp.Path,
            folder,
            """{"kind": "query", "output": {"include": {"loan": {"fields": [{"name": "copyId", "type": "id"}]}}}}""",
            "AbsentQuery");
        WriteSource(folder, "BorrowCopyQueryHandler.cs", """
            internal sealed class BorrowCopyQueryHandler : IQueryHandler<BorrowCopyQuery, BorrowCopyQueryResult>
            {
                public Task<BorrowCopyQueryResult> HandleAsync(BorrowCopyQuery query, CancellationToken cancellationToken) => throw new InvalidOperationException();
            }
            """);
        WriteSource(folder, "BorrowCopyQueryResult.cs", "internal sealed record BorrowCopyQueryResult(Guid CopyId, DateTimeOffset HandledAt);");

        var finding = Assert.Single(Run(model, "output-field-drift"));

        Assert.Equal("output-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' result 'BorrowCopyQueryResult' drifts from its declared output: extra property 'HandledAt'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message carrying the tenant identifier its tenant scope implies reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TenantTermIdPropertyImplied()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        WriteProject(temp.Path, "term/workspace");
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "access": {"grant": "permission/lend-copy", "tenant": "own"}, "input": [{"name": "copyId", "type": "id"}]}""");
        AddTerm(model, temp.Path);
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, Guid WorkspaceId);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message carrying the tenant under its bare term name reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TenantTermBarePropertyImplied()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        WriteProject(temp.Path, "term/workspace");
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "access": {"grant": "permission/lend-copy", "tenant": "own"}, "input": [{"name": "copyId", "type": "id"}]}""");
        AddTerm(model, temp.Path);
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, Guid Workspace);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message carrying a tenant identifier no tenant term implies reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TenantPropertyStillDriftWhenTenantUnset()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "access": {"grant": "permission/lend-copy", "tenant": "own"}, "input": [{"name": "copyId", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, Guid WorkspaceId);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: extra property 'WorkspaceId'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message carrying a tenant identifier outside tenant scope reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void TenantPropertyStillDriftWhenScopeIsPublic()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        WriteProject(temp.Path, "term/workspace");
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "access": {"grant": "permission/lend-copy", "tenant": "public"}, "input": [{"name": "copyId", "type": "id"}]}""");
        AddTerm(model, temp.Path);
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, Guid WorkspaceId);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: extra property 'WorkspaceId'.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A tenancy selection naming a term no record holds reports reference-unknown, and implies no tenant property.</summary>
    /// <proves>litespec/core/language#resolve-every-identifier-citation</proves>
    [Fact]
    public void UnknownTenantTermReportsReferenceUnknown()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        WriteProject(temp.Path, "term/workspace");
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "access": {"grant": "permission/lend-copy", "tenant": "own"}, "input": [{"name": "copyId", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(Guid CopyId, Guid WorkspaceId);");

        var unknown = Assert.Single(Run(model, "reference-unknown"));

        Assert.Equal("reference-unknown", unknown.Code);
        Assert.Equal("The tenancy extension names tenant term 'term/workspace', which no record holds.", unknown.Message);
        Assert.Equal(Path.Combine(".litespec", "project.yaml"), Path.GetRelativePath(temp.Path, unknown.Path));
        Assert.Equal(1, unknown.Line);
        Assert.Null(unknown.Subject);

        var drift = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: extra property 'WorkspaceId'.", drift.Message);
    }

    /// <summary>A message carrying a read-only list for a many-valued input reports nothing.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void ManyValuedInputReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "tags", "type": "text", "many": true}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(IReadOnlyList<string> Tags);");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>A message carrying a single value for a many-valued input reports input-field-drift.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void ManyValuedInputWithSingleValueReportsInputFieldDrift()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "tags", "type": "text", "many": true}]}""");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed record BorrowCopyCommand(string Tags);");

        var finding = Assert.Single(Run(model, "input-field-drift"));

        Assert.Equal("input-field-drift", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' message 'BorrowCopyCommand' drifts from its declared input: property 'Tags' is 'string' for input 'tags' of type 'text' holding many.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A message split into a generated part and a hand-written sibling is judged from the generated part.</summary>
    /// <proves>litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification</proves>
    [Fact]
    public void GeneratedPartWinsOverSibling()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = FieldModel(temp.Path, folder, """{"kind": "command", "input": [{"name": "copyId", "type": "id"}]}""");
        WriteSource(folder, "BorrowCopyCommand.generated.cs", "internal sealed partial record BorrowCopyCommand(Guid CopyId);");
        WriteSource(folder, "BorrowCopyCommand.cs", "internal sealed partial record BorrowCopyCommand\n{\n    public string Label => \"borrow\";\n}");

        Assert.Empty(Run(model, "input-field-drift"));
    }

    /// <summary>Runs the message field checks and keeps their findings for one code.</summary>
    /// <param name="model">The project model to check.</param>
    /// <param name="code">The finding code to keep.</param>
    /// <returns>The message field findings for the code.</returns>
    static List<Finding> Run(ProjectModel model, string code)
    {
        var findings = new List<Finding>();
        MessageFieldChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == code).ToList();
    }

    /// <summary>A model with the use case record and its handler in the Application folder.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    /// <param name="json">The use case record fields as a JSON object.</param>
    /// <param name="message">The message type the handler handles.</param>
    /// <returns>The project model holding the record and its handler.</returns>
    static ProjectModel FieldModel(string root, string folder, string json, string message = "BorrowCopyCommand")
    {
        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(RecordTypes.UseCase, "use-case/lending.borrow-copy", Path.Combine(root, ".litespec", "borrow-copy.yaml"), json));
        var handler = message.EndsWith("Query", StringComparison.Ordinal) ? "BorrowCopyQueryHandler" : "BorrowCopyCommandHandler";
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, handler + ".cs"),
            handler,
            MessageType: message));

        return model;
    }

    /// <summary>The use case Application folder holding the handler and its messages.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The Application folder.</returns>
    static string ApplicationFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "BorrowCopy");
        Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Writes one source file holding the text.</summary>
    /// <param name="folder">The Application folder holding the file.</param>
    /// <param name="file">The source file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string folder, string file, string text)
    {
        File.WriteAllText(Path.Combine(folder, file), text + "\n");
    }

    /// <summary>Writes a project file selecting tenancy with the tenant term.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="tenant">The tenant term id the tenancy selection names.</param>
    static void WriteProject(string root, string tenant)
    {
        var folder = Path.Combine(root, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "project.yaml"),
            "selectedExtensions:\n- id: tenancy\n  criterion: Independent customers share one deployment.\n  tenant: " + tenant + "\n");
    }

    /// <summary>Adds the workspace tenant term to the model.</summary>
    /// <param name="model">The project model holding the records.</param>
    /// <param name="root">The fixture root folder.</param>
    static void AddTerm(ProjectModel model, string root)
    {
        model.Records.Add(TestModel.Record(
            RecordTypes.Term,
            "term/workspace",
            Path.Combine(root, ".litespec", "glossary.yaml"),
            """{"term": "workspace", "meaning": "One customer organization sharing the deployment.", "layer": "behavior"}"""));
    }
}
