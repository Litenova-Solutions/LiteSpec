# Dependencies

**Stack:** any
**Concern:** workspace

Dependencies are the packages a codebase takes on, and each one adds updates, advisories, and migration work. This page states which dependencies each layer, from Domain to the API host, may take, where each package version is pinned, and what a new package needs before it lands. It applies to anyone who adds a package, references another layer, or connects two frontends.

## Concepts

### Layers and Their Packages

The backend holds four architecture layers: Domain, Application, Infrastructure, and WebApi. A layer is a role a type plays, whatever folder or project holds it, and each layer takes only the packages its provisions allow:

- Domain takes none.
- Application takes messaging and read abstractions, plus the one declared read boundary.
- Infrastructure takes providers and adapters.
- WebApi, the layer that hosts the API, takes host, authentication, and documentation packages.
- Test projects take test-only packages.

### Pins and the Manifest

Every version resolves in one owned place. `litespec.manifest.yaml` pins the baseline stack the standards name, the consumer's own central file or lockfile pins the product's own packages, and no version sits inline in a module file. A central entry still does not authorize every layer to reference the package: the ownership table in the conventions below decides who takes what. Marten's version resolves from the central file within the manifest's `9.x` major.

### Approving a New Package

A package outside the manifest lands only with a technical decision under `.litespec/implementation/decisions/`. The decision names the use case the package serves, the alternatives weighed, and the condition that removes it, with an `admits` entry per package naming its owning layer, license, processing location, account owner, failure behavior, and operating cost. Writing the removal condition while the alternatives are still fresh keeps each package traceable to the requirement that admitted it. Claimwick's PDF library arrived with a decision weighing it against an HTML-to-PDF browser.

A project that selects the [Delivery extension](../ext/delivery.md) generates a bill of materials in CI: an SPDX or CycloneDX JSON document that `paths.sbom` names, checked against the manifest pins and the admissions under that extension. A direct package is one the document root depends on in one step: an SPDX `DEPENDS_ON` relationship from the root package, or a CycloneDX `dependencies` entry of `metadata.component`. Its ecosystem comes from its package URL, with `pkg:nuget/` mapping to the manifest NuGet pins and to `nuget:` admission ids, and `pkg:npm/` to its npm pins and `npm:` ids.

## Standards

### Follow the Declared Dependency Graph

**ID:** `litespec/workspace/dependencies#follow-the-declared-dependency-graph`

**Strength:** required

**Statement:** A dependency between layers or modules MUST follow the declared graph and add no other edge.

**Rationale:** The declared graph lists the crossings review checks. An undeclared edge lets a layer reach past its boundary while review finds no graph that forbids it. Each new edge then needs an explicit decision instead of arriving silently with an import.

**Reviewed for:** Does each layer and module dependency follow the declared graph? Read the references between layer folders under `apps/api/src` and the imports in `apps/`.

### Keep Domain Dependency-Free

**ID:** `litespec/workspace/dependencies#keep-domain-dependency-free`

**Strength:** required

**Statement:** The Domain layer MUST depend on no external package or framework.

**Rationale:** Business rules outlive frameworks, and Domain holds the business rules. Depending on no external package or framework keeps the rules testable and portable on their own. Persistence, messaging, web, injection, serialization, and logging libraries all stay outside.

**Reviewed for:** Does any Domain layer folder need an external package or framework? Read the Domain layer folders and the manifests that serve them.

### Keep Application Free of Provider Packages

**ID:** `litespec/workspace/dependencies#keep-application-free-of-provider-packages`

**Strength:** required

**Statement:** Application MUST depend only on messaging abstractions, required read abstractions, and the one declared read boundary.

**Rationale:** Application coordinates use cases through abstractions, while Infrastructure hides the technology behind them. A provider package in Application makes the coordination layer depend on the technology Infrastructure was meant to hide, so a provider change reaches two layers. Only messaging abstractions, required read abstractions, and the one declared read boundary stay.

**Reviewed for:** Does the Application layer reference only messaging abstractions, read abstractions, and the declared read boundary? Read those folders and the manifests that serve them.

### Keep Providers in Infrastructure

**ID:** `litespec/workspace/dependencies#keep-providers-in-infrastructure`

**Strength:** required

**Statement:** A database provider, external SDK, resilience, discovery, or serialization adapter MUST live in Infrastructure.

**Rationale:** Database providers, external SDKs, and resilience, discovery, and serialization adapters all change with their technology. Keeping them in Infrastructure keeps each provider change inside one layer, apart from the read boundary the ownership table assigns to Application.

**Reviewed for:** Do database providers, external SDKs, and resilience, discovery, and serialization adapters appear only in the Infrastructure layer folders? Read those folders and the manifests that serve them.

### Pin Every Dependency Version Centrally

**ID:** `litespec/workspace/dependencies#pin-every-dependency-version-centrally`

**Strength:** required

**Statement:** Every dependency version MUST resolve through central management or the committed lockfile, within the major `litespec.manifest.yaml` pins where it pins one.

**Rationale:** An inline version in one module file is invisible to the file that should own it, so two modules drift to two versions of the same package. Central management or the committed lockfile resolves every version in one owned place. The manifest pins the major of each baseline package, so a consumer takes a patch without waiting for a LiteSpec release, and it pins an exact version only where a check depends on exact output. The manifest belongs to the LiteSpec release rather than to the consumer, so the exact versions of a consumer live beside its code, admitted by `litespec/workspace/dependencies#approve-new-packages-explicitly`. Settled by [decision/0036-pin-third-party-packages-to-their-major](decision/0036-pin-third-party-packages-to-their-major).

**Example:** The Claimwick central version file holds Marten at 9.50.2 while the manifest pins `9.x`, so a patch lands without a LiteSpec release. Its receipt PDF library sits exact in the same file with a decision record, because the manifest names only the baseline stack.

**Reviewed for:** Does every version resolve through central management or the committed lockfile within the manifest major where one exists, with no version inline in a module file? Read the central version files and the module files.

### Approve New Packages Explicitly

**ID:** `litespec/workspace/dependencies#approve-new-packages-explicitly`

**Strength:** required

**Statement:** A package absent from the manifest MUST be admitted by a technical decision whose `admits` entry names the package with its owning layer, license, processing location, account owner, failure behavior, and operating cost.

**Rationale:** Every package arrives with a cost nobody revisits: updates, advisories, license review, and migration work. A hosted one adds a place personal data can travel and an account someone must own. The decision answers those costs while the alternatives are still fresh: its options and choice give the use case and the alternatives, its `reopenedWhen` gives the removal condition, and its `admits` entry gives the facts a dependency gate asks per package. A baseline package lands in the manifest in the same change as its decision, while a consumer-owned package lands in the consumer central file instead, because the manifest belongs to the LiteSpec release rather than to the consumer.

**Example:** The Claimwick `decision/0005-render-receipts-as-pdf` decision chooses a PDF library over an HTML-to-PDF browser and admits `nuget:PdfSharp` with layer `Infrastructure`, license `MIT`, location `in-process`, account owner `none`, failure behavior "the receipt download fails and the expense stays readable", and operating cost `none`.

**Checked by:** `litespec model check` reports `dependency-undeclared` (level S) for a direct package in the bill of materials that the manifest does not pin and no decision admits, and `admission-invalid` (level G) for an `admits` entry missing any of its six facts.

### Keep Frontend Applications Isolated

**ID:** `litespec/workspace/dependencies#keep-frontend-applications-isolated`

**Strength:** required

**Statement:** A frontend MUST NOT import another application's source or another module's internal feature files.

**Rationale:** When one frontend imports the source of another, neither can change its internals without checking the other, so the two release as one. The same holds for the internal feature files of another module. Sharing goes through a shared package with a documented public entry point that depends on no application.

**Reviewed for:** Does any import in one frontend name another application's source or another module's internal feature files? Search the frontend imports.

### Take Web UI Packages from the Approved Baseline

**ID:** `litespec/workspace/dependencies#take-web-ui-packages-from-the-approved-baseline`

**Strength:** required

**Statement:** A React web frontend MUST take its UI packages from the `uiBaseline` in `litespec.manifest.yaml`, and a Svelte web frontend from `uiBaselineSvelte`.

**Rationale:** A fixed UI baseline keeps every screen drawing from the same design system, styling, base components, icons, and utilities. Taking UI packages only from `uiBaseline` for React and `uiBaselineSvelte` for Svelte keeps that set reviewed and versioned in one place. Each baseline pins the design system, styling, base components, icons, and the utility packages the installed source imports directly.

**Reviewed for:** Does each React frontend take UI packages only from `uiBaseline`, and each Svelte one only from `uiBaselineSvelte`? Read each frontend manifest beside `litespec.manifest.yaml`.

## Conventions

### Keep Generated Packages Dependency-Light

**ID:** `litespec/workspace/dependencies#keep-generated-packages-dependency-light`

**Strength:** default

**Default:** Keep generated API types free of dependencies and limit the shared client to the pinned generator's runtime, its query plugin, and those types.

**Replacement:** A project whose client needs more records its own client dependency set in the client package manifest.

**Rationale:** Generated types mirror the API contract, and the shared client only carries them to queries. Keeping the types free of dependencies and the client limited to the generator runtime, its query plugin, and those types keeps regeneration a pure function of the contract. A generated package that owns state, UI behavior, or authentication policy is no longer regenerable.

**Reviewed for:** Does the generated API types package carry no dependencies, with the shared client limited to the generator runtime, its query plugin, and those types, or does the client package manifest name the project's own client dependency set? Read the generated package manifests.

### Keep Test Dependencies in Test Projects

**ID:** `litespec/workspace/dependencies#keep-test-dependencies-in-test-projects`

**Strength:** default

**Default:** Keep assertion, substitution, host, container, and architecture-test packages inside test projects.

**Replacement:** A project that ships a test helper records the helper and its reason in its project documentation.

**Rationale:** Assertion, substitution, host, container, and architecture-test packages exist to verify code, not to run it. Once a production project references one, it ships to production with its own dependencies and advisories. Keeping them inside test projects keeps the shipped graph free of verification weight.

**Reviewed for:** Does any production project manifest reference an assertion, substitution, host, container, or architecture-test package, or does the project documentation name the shipped helper and its reason? Read the production project manifests.

### Assign Each Baseline Package to Its Owning Layer

**ID:** `litespec/workspace/dependencies#assign-each-baseline-package-to-its-owning-layer`

**Strength:** default

**Default:** Assign each baseline package to the owning layer named in the table in this section.

**Replacement:** A project that moves a baseline package records its own owning layer in the decision that admits the move.

**Example:**

| Layer | Direct package groups |
|:---|:---|
| Domain | None |
| Application | Required abstractions only, plus the declared read boundary |
| Infrastructure | Providers and adapters required by implemented ports |
| WebApi | Host, authentication, and documentation packages |
| Test code | Test-only packages |

A new package joins the narrowest layer that needs it. A central version entry does not authorize every layer to reference that package; the table decides who takes what. A consumer references conditional packages only after their extension activates.

**Reviewed for:** Does each baseline package sit in the owning layer the table in this section assigns, or does the admitting decision name the project's own owning layer? Read the layer manifests beside the table.
