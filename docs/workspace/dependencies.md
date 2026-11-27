# Dependencies

**Stack:** any
**Concern:** workspace

Dependencies are the packages a codebase takes on, and each one adds updates, advisories, and migration work. This page states which packages each layer, from Domain to the API host, takes, where each package version gets pinned, and what a new package needs before it gets added. It applies to anyone who adds a package, references another layer, or connects two frontends.

## Concepts

### Layers and Their Packages

The backend holds four architecture layers: Domain, Application, Infrastructure, and WebApi. A layer is a role a type plays, independent of which folder or project holds it, and each layer takes only the packages its rules allow. The declared graph names the crossings between layers and modules that review checks. A central version entry authorizes no layer by itself: the ownership table below decides which layer takes each package. A consumer references conditional packages only after their extension activates.

| Layer | Direct package groups |
|:---|:---|
| Domain | None |
| Application | Abstractions the layer needs, plus the declared read boundary |
| Infrastructure | Providers and adapters for implemented ports |
| WebApi | Host, authentication, and documentation packages |
| Test code | Test-only packages |

A new package belongs in the narrowest layer that needs it. Business rules outlive frameworks, and Domain holds the business rules, so Domain stays free of external packages and frameworks: persistence, messaging, web, injection, serialization, and logging libraries all stay outside it.

### Pins and the Manifest

Each version resolves in one owned place. `litespec.manifest.yaml` pins the baseline stack the standards name, the consumer own central file or lockfile pins the product own packages, and no version sits inline in a module file. Marten version resolves from the central file within the manifest `9.x` major. An inline version in one module file stays invisible to the file that owns it, so two modules drift to two versions of the same package.

### Approving a New Package

A package outside the manifest gets added only with a technical decision under `.litespec/implementation/decisions/`. The decision names the use case the package serves, the alternatives weighed, and the condition that removes it, with an `admits` entry per package naming its owning layer, license, processing location, account owner, failure behavior, and operating cost. Writing the removal condition while the alternatives stay fresh keeps each package traceable to the requirement that admitted it. Claimwick PDF library has a decision weighing it against an HTML-to-browser print.

A project that selects the Delivery extension generates a bill of materials in CI: an SPDX or CycloneDX JSON document that `paths.sbom` names, checked against the manifest pins and the admissions under that extension. A direct package is one the document root depends on in one step: an SPDX `DEPENDS_ON` relationship from the root package, or a CycloneDX `dependencies` entry of `metadata.component`. Its ecosystem comes from its package URL, with `pkg:nuget/` mapping to the manifest NuGet pins and to `nuget:` admission ids, and `pkg:npm/` to the manifest npm pins and to `npm:` ids.

### Frontends and Generated Code

Frontends share nothing by source import: one frontend imports neither another application source nor another module internal feature files. Sharing goes through a shared package with a documented public entry point that depends on no application. A React web frontend takes its UI packages from the `uiBaseline` in `litespec.manifest.yaml`, and a Svelte web frontend from `uiBaselineSvelte`.

Generated types mirror the API contract, and the shared client only passes them to queries. The generated API types stay free of dependencies, and the shared client stays limited to the pinned generator runtime, its query plugin, and those types. A generated package that owns state, UI behavior, or authentication policy is no longer regenerable.

Assertion, substitution, host, container, and architecture-test packages exist to verify code, not to run it. Once a production project references one, it ships to production with its own dependencies and advisories. Those packages stay inside test projects.

## Standards

### Pin Every Dependency Version Centrally

**ID:** `litespec/workspace/dependencies#pin-every-dependency-version-centrally`

**Strength:** required

**Statement:** Every dependency version MUST resolve through central management or the committed lockfile, within the major `litespec.manifest.yaml` pins where it pins one.

**Rationale:** An inline version in one module file stays invisible to the file that owns it, so two modules drift to two versions of the same package. Central management or the committed lockfile resolves each version in one owned place. The manifest pins the major of each baseline package, so a consumer takes a patch without waiting for a LiteSpec release, and it pins an exact version only where a check depends on exact output. Settled by decision/0036-pin-third-party-packages-to-their-major.

**Example:** The Claimwick central version file holds Marten at 9.50.2 while the manifest pins `9.x`, so the project takes a patch without waiting for a LiteSpec release. Its receipt PDF library is pinned exact in the same file with a decision record, because the manifest names only the baseline stack.

**Reviewed for:** Does every version resolve through central management or the committed lockfile within the manifest major where one exists, with no version inline in a module file? Read the central version files and the module files.

### Approve New Packages Explicitly

**ID:** `litespec/workspace/dependencies#approve-new-packages-explicitly`

**Strength:** required

**Statement:** A package absent from the manifest MUST be admitted by a technical decision whose `admits` entry names the package with its owning layer, license, processing location, account owner, failure behavior, and operating cost.

**Rationale:** Each package carries a cost nobody revisits: updates, advisories, license review, and migration work. A hosted one adds a place personal data travels and an account somebody owns. The decision answers those costs while the alternatives stay fresh: its options and choice give the use case and the alternatives, its `reopenedWhen` gives the removal condition, and its `admits` entry gives the facts a dependency gate asks per package.

**Example:** The Claimwick `decision/0005-render-receipts-as-pdf` decision chooses a PDF library over an HTML-to-browser print and admits `nuget:PdfSharp` with layer `Infrastructure`, license `MIT`, location `in-process`, account owner `none`, failure behavior "the receipt download fails and the expense stays readable", and operating cost `none`.

**Checked by:** `litespec model check` reports `dependency-undeclared` (level S) for a direct package in the bill of materials that the manifest does not pin and no decision admits, and `admission-invalid` (level G) for an `admits` entry missing any of its six facts.

## Conventions

None.
