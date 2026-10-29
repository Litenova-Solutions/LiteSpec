# Maintaining the Standards

## Intent

LiteSpec publishes each release as one complete contract, so the repository holding the standard needs its own operating rules. This page states them: how a provision, one identified obligation on a page, moves from draft to active to retired, how a rename replaces every citation of the old name in one change, and how a maintainer publishes a release from a git tag. The rules keep every provision identifier resolvable and every release reproducible from its tag.

## Concepts

### The Provision Lifecycle

A provision passes through three states.

| State | Meaning |
|:---|:---|
| draft | The provision is being written and has no verification line yet, so it cannot land. |
| active | The provision carries its ID line and its verification line. |
| retired | The provision no longer holds and nothing cites it. |

A provision moves from draft to active when its verification line is written, and from active to retired when a richer provision folds its assertion in. It never moves back to draft, and retirement is permanent: a retired identifier stays gone, so a reader who meets one knows it names nothing current.

### The Identifier

Every provision ID is `litespec/<page>#<slug>`, computed from the page path and the heading, as `litespec/core/authoring#use-the-declared-identifier-grammar` states. A rename replaces the identifier outright: the repository keeps no alias and no map from the old identifier to the new one. That is why a rename replaces every citation in the same change.

### Citations

A citation names the cited provision by its current identifier, whether the citation sits on the same page or another one. Because no alias exists, a citation that names a retired identifier resolves nowhere, and `litespec-maintain validate` reports it.

### The LiteSpec Release

A LiteSpec release is a tagged snapshot of the repository, and the highest git tag names the current release. Between releases, five places name the release being prepared: the version in `litespec.manifest.json`, the topmost `CHANGELOG.md` entry, the consumer template's `reviewedLiteSpecVersion`, and the package versions in `apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj` and `apps/analyzers/src/LiteSpec.Analyzers/LiteSpec.Analyzers.csproj`. Tagging that version number makes it current. The manifest lists every stack, shape, extension, and version pin the release carries. Publishing the release puts the `LiteSpec.Cli` tool package, the `LiteSpec.Analyzers` package, and the site at the release version together, so the tool carries the manifest, schemas, docs, and templates the version names. A consumer upgrades by changing the tool version and `reviewedLiteSpecVersion` together. Each schema `$id` carries that release under `https://litespec.dev/<version>/schemas/`, so a consumer that pins the release keeps resolving the shapes it reviewed.

### Release Diagnostics

These checks report a manifest or schema the release cannot ship, so they are listed here instead of beside a provision.

| Family | Meaning | Codes |
|:---|:---|:---|
| `MANIFEST_*` | The manifest is not valid JSON, names a path or anchor that does not exist, or lists a shape page twice. | `MANIFEST_JSON`, `MANIFEST_PATH`, `MANIFEST_ANCHOR`, `MANIFEST_DUPLICATE_PATH` |
| `SCHEMA_*` | A schema carries the wrong release `$id`, is not valid JSON, rejects its values, or uses a keyword the evaluator does not implement. | `SCHEMA_ID`, `SCHEMA_JSON`, `SCHEMA_INVALID`, `SCHEMA_UNSUPPORTED_KEYWORD` |

## Standards

### State the Provision Lifecycle

**ID:** `litespec/core/maintaining#state-the-provision-lifecycle`

**Statement:** A maintainer MUST keep each provision in exactly one of three states: draft, active, or retired.

**Rationale:** The three states tell a maintainer what an identifier promises. An active identifier names a rule that holds and that its verification line says how to check. A retired identifier names nothing, so a citation to it is a defect rather than a reference. Without the lifecycle, a half-written provision and a removed one look identical to a cited one.

**Example:** A provision is added in draft. When its verification line is written it becomes active. When its assertion is folded into a richer provision, the provision is retired.

**Reviewed for:** Each provision sits in exactly one lifecycle state, with retired identifiers cited nowhere.

### Replace Every Citation on a Rename

**ID:** `litespec/core/maintaining#replace-every-citation-on-a-rename`

**Statement:** A provision rename MUST replace every citation of the old identifier in one change.

**Rationale:** A heading rename renames the identifier, and the repository keeps no alias to catch stragglers. Every citation the rename misses names a provision that no longer exists, and readers follow it nowhere. Replacing all of them in one change keeps the repository resolvable at every commit.

**Example:** Renaming a provision changes its heading, its ID line, and every citation together.

**Checked by:** `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Publish a LiteSpec Release from a Tag

**ID:** `litespec/core/maintaining#publish-a-litespec-release-from-a-tag`

**Statement:** A LiteSpec release MUST carry the highest git tag in the manifest version, the changelog entry, the consumer template version, and the tool and analyzer package versions.

**Rationale:** A git tag is the only durable artifact that names a release: branches move and files change, but the tag stays. Reading the current release from the highest tag gives maintainers, consumers, and tools one shared answer. Writing that version into the manifest, the changelog, the template, and the tool package in the same release keeps the four from disagreeing.

**Checked by:** `dotnet test --solution LiteSpec.slnx` fails `ReleaseVersionTests` when the changelog, the template, or either package version differs from the manifest version, and `.github/workflows/release.yml` refuses a tag that does not name the manifest version.

### List Every Stack Shape and Extension in the Manifest

**ID:** `litespec/core/maintaining#list-every-stack-shape-and-extension-in-the-manifest`

**Statement:** The LiteSpec manifest MUST list every stack, shape, extension, and version pin the release carries.

**Rationale:** The manifest is the release record that tools read: stacks and shapes decide which pages apply, extensions decide which conditional bundles a project selects from, and pins decide which versions build. A stack, shape, extension, or pin the manifest omits is one no consumer selects and no tool resolves. Listing all of them in one file keeps the release reproducible from its tag.

**Example:** The Behavior extensions `privacy`, `devices`, `automation`, `workflow-engine`, `pipeline`, `queues`, and `ai` each appear with their page path and activation scope.

**Checked by:** `litespec-maintain validate` fails when a stack, shape, extension, or pin the release carries is missing from the manifest.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/maintaining#state-the-provision-lifecycle`, `litespec/core/maintaining#replace-every-citation-on-a-rename`, and `litespec/core/maintaining#publish-a-litespec-release-from-a-tag`.

### Step One, Adding a Provision

A maintainer writes a new provision on a page. The provision stays in `draft` until it carries its ID line and its verification line.

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

A rule changes, so the maintainer updates the heading and replaces the identifier and every citation of it in one change. When the change lands, no citation names the old identifier.

### Step Three, Releasing

The maintainer runs `git describe --tags --abbrev=0` to find the tag with the highest version number. That tag names the current release. The maintainer sets the manifest and tool package versions, adds the changelog entry, and tags the commit with the new version number.
