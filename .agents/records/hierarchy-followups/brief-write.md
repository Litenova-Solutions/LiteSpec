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

- **Part 0 of the specification wins.** It holds the owner's binding decisions and the judge's corrections; where a later part disagrees, follow Part 0 (spec.md lines 7 to 35, included below).
- **ASCII only, Title Case headings, one term per concept.** Every LiteSpec page and record (AGENTS.md, Writing; `litespec/core/authoring#keep-authored-prose-ascii-safe`).
- **A provision has its parts in order:** a heading starting with an action verb from the list in `apps/maintain/src/LiteSpec.Maintain/Authoring/StandardsValidator.cs`, then `**ID:**`, `**Statement:**` with capitalized modal words, `**Rationale:**`, `**Example:**` (Claimwick or the library product), and `**Checked by:**` or `**Reviewed for:**` (AGENTS.md, Writing).
- **Rename every citation in the same change.** A renamed field, provision or identifier form is replaced everywhere it appears; no alias, no compatibility shim, no history between LiteSpec releases (`litespec/core/maintaining#replace-every-citation-on-a-rename`, `litespec/core/authoring#exclude-cross-release-compatibility-work`).
- **Never name a consumer project in LiteSpec.** LiteSpec pages and code never mention NightHall; examples use Claimwick or the library product (AGENTS.md, Principles).
- **Write links on the record that points; never a down-pointing link field;** inverses are computed (`litespec/core/model#write-links-on-the-record-that-points`).
- **The book shows only LiteSpec terms, LiteSpec's own explanations, and computed facts** (the six book standards at the end of the Standards section of docs/core/model.md).
- **New C# follows the file beside it:** one `Check*` method per finding family appending findings with path, line, message and subject; a test per finding in the nearest test file's style.
- **NightHall judgement items are yours to decide** (decision 14, Part G2): research each gap in NightHall's records, code and research and on the web, cite every source with its read date, pick what the evidence supports, write it, and record each judgement with its evidence under Decisions in your done file. Approval happens in wave 9, not in your slice. NightHall tests change only by mechanical call-site replacement.

## Do not

- Run a formatter, a code generator, a package install, or any git command. You may build the one project you change (`dotnet build <project>`) and run the tests that cover your files (`dotnet test --project <test project> --no-build --filter <your classes>`); never build or test the whole solution, and never while holding a file another worker owns open. The judge runs the full ladder once for everyone.
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
