# {{task}}: write this slice completely

You are one of several workers writing wave {{wave}} at the same time. Write your whole slice, then stop. Nobody checks your work while the wave writes. When every worker has finished, one build, the project's checks and a review against the specification run over everything. Any error in your files comes back to you or to the next worker, with the exact text.

## Your files

You may create and edit only paths matching:

{{owns}}

Do not edit, move or delete anything else. If you need a change outside your files (a shared type, a navigation entry, a translation file, a generated client), do not make it: list it under "Requests" in your done file and write your code as if it exists.

## Copy this pattern

Open {{example}} first, then the files beside it in every layer. Copy its shape, naming, folder layout, comments and tags exactly. Your slice must look as if the same person wrote both.

## Already written, read but do not edit

{{depends}}

## Note from the judge

{{note}}

## Rules every slice follows

- **Part 0 of the specification wins** (source/spec.md lines 8 to 70, included below). It holds the owner's binding decisions D1 to D46; where a later part disagrees, follow Part 0.
- **Read `AGENTS.md` and `docs/core/authoring.md` first, then the whole page you change.** ASCII only, Title Case headings, one term per concept, capitalized modal words only inside a Statement, nothing about earlier releases.
- **A provision has its parts in order:** a heading starting with an action verb the validator accepts (`apps/maintain/src/LiteSpec.Maintain/Authoring/StandardsValidator.cs`), then `**ID:**`, `**Statement:**` (one sentence, one modal), `**Rationale:**`, `**Example:**` (Claimwick or the library product), and `**Checked by:**` naming a check that exists, or `**Reviewed for:**` as a question.
- **Use the provision IDs in `contracts.md` beside this ledger** when you cite a provision another slice of this wave writes. Never invent another ID for it.
- **Rename every citation in the same change.** No alias, no compatibility shim, no history between LiteSpec releases (`litespec/core/maintaining#replace-every-citation-on-a-rename`). A citation in a file you do not own goes under Requests with the exact old and new text.
- **Never name a consumer project in LiteSpec.** Say "the largest consumer" where the spec cites NightHall evidence.
- **Each fact once:** a rule stated on two pages keeps one home and the other page cites it.
- **Approval stays loose (D25):** add no reviewer, person-only approver or review gate.
- **C#:** follow the file beside it; one `Check*` method per finding family; a test per finding code in the nearest test file's style. Every new finding code is registered in `apps/model/src/LiteSpec.Model.Core/Finding.cs` and `docs/tools/model.md` by the slice that owns them, or under Requests.
- **Check your C# with Fuse:** run `fuse check <every .cs file you changed>` after each edit and fix every error it reports in your files before the done file. Run the tests that cover your files with `fuse test --project <test project> -- --filter-class "*<YourTestClass>"` (Fuse builds what changed first) only after `fuse check` is clean; never build or test the whole solution.
- **Docs:** run `dotnet run --project apps/maintain/src/LiteSpec.Maintain --no-build -- validate` when you change a page, and fix the errors in your files.

## Do not

- Run a formatter, a code generator, a package install, or any git command.
- Leave a `TODO`, a stub, `NotImplementedException`, an empty method, or a comment such as `// ... rest`. A slice with one is rejected and handed to another worker.
- Invent a type, a method, a package or a file. Search the repository for the real name first; if it does not exist and is not yours to write, list it under "Requests".
- Stop early. Every rule, refusal, field, state, event and screen the specification names below is part of your slice.
- Ask a question. Choose what best fits the pattern and the specification, and record the choice under "Decisions".

## Done file

When the slice is complete, write `{{run}}/done/{{task}}.md`:

```markdown
### Changed
- <path>: <what it holds>
### Specification coverage
- <each rule, refusal or screen the specification names>: <the file and member that implements it>
### Decisions
- <a choice the specification left open, and why>
### Requests
- <a change you needed outside your files, or "none">
```

If the slice needed no change at all, write a first line `No change needed: <why>` instead.
