# Maintaining the Standards

**Stack:** any

**Concern:** workspace

Each LiteSpec release is one complete contract, and the repository behind the standard needs operating rules of its own: how a provision, one identified obligation on a page, moves from draft to active to retired, how an identifier rename replaces every citation of the old identifier in one change, and how a maintainer publishes a release from a git tag. These rules keep every provision identifier resolvable and every release reproducible from its tag.

## Concepts

### The Provision Lifecycle

A provision passes through three states, each promising something different about its identifier.

| State | Meaning |
|:---|:---|
| draft | The provision is being written and has no verification line yet, so it cannot become active. |
| active | The provision carries its ID line and its verification line. |
| retired | The provision no longer holds and nothing cites it. |

A provision becomes active when its verification line is written, and retires when a richer provision takes over its assertion. It never returns to draft, and retirement is permanent: a retired identifier is never reused, so a reader who sees one knows it names nothing current.

### The Identifier

Every provision ID has the form `litespec/<page>#<slug>`, stated on its `**ID:**` line, as `litespec/core/authoring#use-the-declared-identifier-grammar` states. The ID line is the provision identity: a heading may change without changing the ID.

Within a major version, identifiers stay stable: a minor release adds provisions and changes wording, and removes or renames no identifier, so every citation written against that major version keeps resolving. A major release may remove or rename identifiers, and its changelog carries the generated list of every added, removed, and renamed identifier.

An identifier rename replaces the ID outright, with no alias and no map from the old identifier to the new one. That is why an identifier rename replaces every citation in the same change.

### Citations

A citation names the cited provision by its current identifier, whether the citation appears on the same page or another one. Because the repository keeps no alias, a citation that names a retired identifier resolves to nothing, and `litespec-maintain validate` reports it.

### The LiteSpec Release

A LiteSpec release is a tagged snapshot of the repository, and the highest git tag names the current release.

Between releases, five places name the release being prepared: the version in `litespec.manifest.yaml`, the topmost `CHANGELOG.md` entry, the consumer template's `reviewedLiteSpecVersion`, and the package versions in `apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj` and `apps/analyzers/src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj`. Tagging that version number makes it current.

Releases use two tag forms. A tag `v<version>` on `main` publishes the release, while a tag `v<version>-rc.<n>` on `release/<version>` publishes a release candidate, such as `6.0.0-rc.1`, so a consumer can install and test the packages before the release branch merges. The workflow stamps the suffix into the packages at build time, so the committed files keep the plain version.

A candidate publishes the `LiteSpec.Cli` tool packages, the standalone archives with their checksums and install scripts, and the `LiteSpec.Analyzers` package at the suffixed version, and it marks the GitHub release as a prerelease. The site stays unpublished, so a candidate's links name the release version and resolve once that release ships.

The manifest lists every stack, shape, extension, and version pin the release carries. Publishing the release ships the `LiteSpec.Cli` tool packages, the standalone archives of the CLI with their checksums and install scripts, the `LiteSpec.Analyzers` package, and the site at the release version together, so every install of the CLI carries the manifest, schemas, docs, and templates the version names.

A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together. A consumer that pins a candidate records the suffixed version, and the tool compares only the base version of the record with the installed release.

Each schema `$id` carries the release under `https://litespec.dev/<version>/schemas/`. The site serves one release, so an older `$id` stops resolving at the next release. The CLI carries its release's schemas offline, so a consumer that pins the tool validates against the schemas it reviewed without the site.

### Release Diagnostics

The checks below report a manifest or schema defect that blocks the release, so they sit here instead of beside a provision.

| Group | Meaning | Codes |
|:---|:---|:---|
| `MANIFEST_*` | The manifest is not valid YAML, names a path or anchor that does not exist, lists a shape page twice, or pins an analyzer release that differs from the analyzer project version; a page cites an analyzer rule the pinned release does not ship. | `MANIFEST_YAML`, `MANIFEST_PATH`, `MANIFEST_ANCHOR`, `MANIFEST_DUPLICATE_PATH`, `MANIFEST_ANALYZER_PIN`, `MANIFEST_ANALYZER_RULE` |
| Documentation bindings | A page cites a finding code, template, or example record that does not exist, a `Checked by` line for `litespec model check` names no finding code, the documentation index reaches no path to a page, or a pattern offers more than three options. | `FINDING_CODE_UNKNOWN`, `CHECKED_BY_FINDING_MISSING`, `TEMPLATE_CITED_UNKNOWN`, `EXAMPLE_RECORD_UNKNOWN`, `INDEX_PAGE_MISSING`, `PATTERN_OPTION_COUNT` |
| Package paths | A file under `docs/`, `schemas/`, or `templates/` has a repository path longer than 128 characters, which NuGet refuses to pack inside the tool package (NU5123). | `PACKAGED_PATH_LONG` |
| Command pages | A command page shows in its Synopsis or Options section an option its command does not parse, or names a LiteSpec command that no usage text in source declares. | `COMMAND_OPTION_UNKNOWN` |
| `SCHEMA_*` | A schema carries the wrong release `$id`, is not valid YAML, rejects its values, or uses a keyword the evaluator does not implement. | `SCHEMA_ID`, `SCHEMA_YAML`, `SCHEMA_INVALID`, `SCHEMA_UNSUPPORTED_KEYWORD` |

## Standards

### State the Provision Lifecycle

**ID:** `litespec/core/maintaining#state-the-provision-lifecycle`

**Strength:** required

**Statement:** A maintainer MUST keep each provision in exactly one of three states: draft, active, or retired.

**Rationale:** The three states tell a maintainer what an identifier promises. An active identifier names a rule that holds, with its verification line saying how to check it. A retired identifier names nothing, so a citation to it is a defect rather than a reference. Without the lifecycle, a half-written provision and a removed one look identical to a cited one.

**Example:** A new provision starts in draft. When its verification line is written, it becomes active. When a richer provision folds its assertion in, it retires.

**Reviewed for:** Does every provision sit in exactly one lifecycle state, with no citations to retired identifiers? Check the provisions beside their citations.

### Replace Every Citation on a Rename

**ID:** `litespec/core/maintaining#replace-every-citation-on-a-rename`

**Strength:** required

**Statement:** A provision rename MUST replace every citation of the old identifier in one change.

**Rationale:** An identifier rename changes the ID line, and no alias covers a missed citation. Every citation the rename misses names a provision that no longer exists, and readers following it find nothing. Replacing all of them in one change keeps the repository resolvable at every commit. A heading reword leaves the identifier unchanged, so it needs no citation changes.

**Example:** A rename changes the ID line and every citation of it in the same change.

**Checked by:** `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Keep Provision Identifiers Stable Within a Major Version

**ID:** `litespec/core/maintaining#keep-provision-identifiers-stable-within-a-major-version`

**Strength:** required

**Statement:** A minor LiteSpec release MUST keep every provision identifier of its major version, adding provisions and rewording provision text without removing or renaming an identifier.

**Rationale:** Consumers cite provision identifiers in code marks, records, and waivers, each written against the major version the project pins. A minor release that removes or renames an identifier breaks those citations inside a version line the consumer took as stable, with no major release to signal the break. A major release may remove or rename identifiers, and the generated provision diff in its changelog lists every added, removed, and renamed identifier, so a consumer moving across a major version finds each change in one place.

**Example:** A minor release adds `litespec/core/scope#claim-the-record-layers-the-project-uses` and rewords a neighboring provision; both keep their identifiers, so every mark and waiver written against the major version still resolves.

**Reviewed for:** Did a minor release remove or rename any provision identifier? Compare the provision index at the previous tag with the provision index of the release, beside the changelog entry that lists the generated diff.

### Publish a LiteSpec Release from a Tag

**ID:** `litespec/core/maintaining#publish-a-litespec-release-from-a-tag`

**Strength:** required

**Statement:** A LiteSpec tag that publishes MUST be `v<version>` on `main`, or a release candidate `v<version>-rc.<n>` on `release/<version>`, and the changelog entry, the consumer template version, and the tool and analyzer package versions name the same manifest version.

**Rationale:** A git tag is the only durable artifact that names a release: branches move and files change, but the tag stays. Reading the current release from the highest tag gives maintainers, consumers, and tools one shared answer. Writing that version into the manifest, the changelog, the template, and the tool package in the same release keeps the four consistent. A candidate lets a consumer test the packages a release will publish before the release branch merges. Its suffix keeps its packages apart from the release's, and the committed files keep the plain version, so a candidate never changes what `main` names. Each release publishes the CLI as one native executable per platform, in the tool packages and in standalone archives, so the version reaches a consumer with or without .NET, settled by [decision/0021-native-command-line](decision/0021-native-command-line) on the evidence in [research/native-command-line-evidence](research/native-command-line-evidence).

**Checked by:** `dotnet test --solution LiteSpec.slnx` fails `ReleaseVersionTests` when the changelog, the template, or either package version differs from the manifest version, and `.github/workflows/release.yml` refuses a tag that does not name the manifest version, a release tag whose commit is not on `main`, and a candidate tag whose commit is not on `release/<version>`.

### Publish Each Release Candidate from Its Release Branch

**ID:** `litespec/core/maintaining#publish-each-release-candidate-from-its-release-branch`

**Strength:** required

**Statement:** Every release candidate MUST publish from its release branch, `release/<version>`, and no candidate publishes from `main`.

**Rationale:** A consumer validates a candidate before the final release reaches `main`, so the candidate must hold the release branch's commit and nothing newer on `main`. Publishing from the branch means the tested packages are the packages the release will publish, and the release's final tag still comes from `main`. Proposed by [decision/0059-publish-candidates-from-release-branches](decision/0059-publish-candidates-from-release-branches).

**Reviewed for:** Does every release candidate tag name a commit on `release/<version>`, with no candidate packages published from `main`? Check the candidate tags and the release workflow runs that published them.

### List Every Stack, Shape, and Extension in the Manifest

**ID:** `litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest`

**Strength:** required

**Statement:** The LiteSpec manifest MUST list every stack, shape, extension, and version pin the release carries.

**Rationale:** The manifest is the release record that tools read: stacks and shapes decide which pages apply, extensions decide which conditional bundles a project selects from, and pins decide which versions build. A stack, shape, extension, or pin the manifest omits is one no consumer selects and no tool resolves. Listing all of them in one file keeps the release reproducible from its tag.

**Example:** The manifest lists the `cache` extension with its page path `docs/ext/cache.md` and its `local` activation scope.

**Checked by:** `litespec-maintain validate` emits `EXTENSION_UNLISTED` for an extension page no manifest entry lists, `LAYER_UNREACHABLE` for a page no stack selection reaches, and `MANIFEST_ANALYZER_PIN` for an analyzer pin that differs from the analyzer project version.

### Mark the Tools with the Provisions They Follow

**ID:** `litespec/core/maintaining#mark-the-tools-with-the-provisions-they-follow`

**Strength:** required

**Statement:** Every top-level class that declares a method in a .NET source project under `apps/`, and every test that verifies a provision, MUST name its provision in a mark: a `follows` mark on the class and a `proves` mark on the test.

**Rationale:** LiteSpec's own tools are technical classes: they serve no product record, and each exists because a provision requires a check, a reader, or a generator of its shape. A `follows` mark is the reverse index from that code to the provision, so a retired or renamed provision finds every class still built for it. A `proves` mark finds the tests that verify its check. LiteSpec keeps the research and decisions behind its provisions as records under `docs/intent/`, in the shapes a consumer uses; its provisions remain its specification, and the marks point at them directly. The site under `apps/site` is TypeScript with no mark form in the maintainer tool, so it carries no marks.

**Example:** The .NET scenario reader carries `/// <follows>litespec/core/model#name-the-lowest-record-each-test-proves</follows>`, and the test that confirms a scenario driven through a shared probe step carries `/// <proves>litespec/core/model#name-the-lowest-record-each-test-proves</proves>`.

**Checked by:** `litespec-maintain validate` emits `MARK_MISSING` for a class with a method and no `follows` mark in a source project under `apps/`, and `MARK_UNRESOLVED` for a `follows` or `proves` mark in a .NET project under `apps/`, outside its `Fixtures` folders, that names no provision in `docs/reference/provisions.md`.

### Record the Reasoning Behind Each Release

**ID:** `litespec/core/maintaining#record-the-reasoning-behind-each-release`

**Strength:** required

**Statement:** A maintainer MUST keep the research and decisions behind each release as records under `docs/intent/`.

**Rationale:** Provisions state obligations without the questions, sources, or conclusions behind them. Without that reasoning on record, a later change cannot tell why a rule reads as it does. Settled by [decision/0001-keep-release-reasoning-as-records](decision/0001-keep-release-reasoning-as-records) on the evidence in [research/comparable-rationale-practice](research/comparable-rationale-practice).

**Example:** Claimwick states its five-day reimbursement target in a provision, with the benchmark behind the number in a research record beside it.

**Checked by:** `litespec-maintain validate` emits no `RECORD_SCHEMA`, `RECORD_ID_MISMATCH`, `RECORD_SOURCE`, `RECORD_UNKNOWN_REFERENCE`, or `RECORD_DECISION_UNCITED` diagnostic.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/maintaining#state-the-provision-lifecycle`, `litespec/core/maintaining#replace-every-citation-on-a-rename`, and `litespec/core/maintaining#publish-a-litespec-release-from-a-tag`.

### Step One, Adding a Provision

A maintainer writes a new provision on a page. It stays in `draft` until it carries its ID line and its verification line.

```text
### Submit Each Expense Once

**Statement:** A spender MUST submit each expense once.

**Rationale:** A second submission of one receipt pays it twice.
```

The validator reports the provision with no ID line and no verification line. The maintainer adds both, and the provision becomes `active`:

```text
### Submit Each Expense Once

**ID:** `litespec/core/example#submit-each-expense-once`

**Statement:** A spender MUST submit each expense once.

**Rationale:** A second submission of one receipt pays it twice.

**Reviewed for:** Which expense was submitted twice? Look at the submitted expenses for one receipt.
```

### Step Two, Renaming

When a rule changes, the maintainer updates the heading, and the identifier stays. When the identifier itself must change, the maintainer replaces the ID line and every citation of it in one change. When the change is merged, no citation names the old identifier.

### Step Three, Releasing

The maintainer runs `git describe --tags --abbrev=0` to find the tag with the highest version number. That tag names the current release. The maintainer sets the manifest and tool package versions, writes the changelog entry for the release, and tags the commit with the new version number.

When a consumer tests a release first, the maintainer runs the same steps on its release branch. The maintainer tags `v<version>-rc.1` on `release/<version>`, the consumer moves to that candidate, and each defect the consumer finds is fixed on the branch and tagged as the next candidate. The maintainer tags `v<version>` on `main` once a candidate passes testing with the consumer.
