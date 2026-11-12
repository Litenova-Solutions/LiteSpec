# Changelog

## v5.1.0

LiteSpec 5.1.0 lets a project require an explanation file for every record on the layers it names, and adds behavior Standards for keyboards, screen readers, reduced motion, color schemes and older browsers. It pins third-party packages to their major version, so a consumer takes patch and minor releases without waiting for LiteSpec. `model check` is several times faster on large projects. It is a minor release: no provision ID is removed or renamed. A consumer with a shared UI package adds a Browser Support section to its design contract, as Part H describes.

### Part A: Explanation Files a Project Requires

- `explanations.layers` in `.litespec/project.yaml` names the layers whose records must each carry an explanation file. It takes `intent`, `behavior` or both. Every page record on a named layer then needs its `<name>.md` beside its record file, with the sections its record type defines. Projects that leave the key out are unaffected.
- `model check` reports `explanation-missing` for a missing file or a missing required section, and `project-invalid` when `explanations.layers` names a layer the project does not claim in `layers`.
- `litespec new` writes the explanation file from `templates/consumer/<type>.md` when the new record's layer is listed, and from `templates/consumer/explanation.md` for a type with no template of its own.
- `litespec new feature <module>.<name>` writes the record `feature/<name>` in the module's folder. In 5.0.0 it refused every name: without a module it asked for one, and with one it rejected the identifier.

The rule is `litespec/core/model#require-explanation-files-on-the-layers-a-project-names`, and the [project file reference](docs/reference/project-file.md) lists the field.

### Part B: How a Screen Behaves

- Controlled UI Governance names three layers of a screen: product, behavior and parts. The parts never lower the behavior: a baseline component that closes a modal on an outside press, or traps Tab in a menu, is overridden.
- The pattern and foundation pages gain behavior Standards drawn from WCAG 2.2. They cover overlay dismissal and initial focus, live regions by urgency, when a field error appears and how assistive technology reads it, one send in flight, sortable and scrolling tables, aligned figures, calendar dates without a time zone, sibling pages as links, 3:1 focus indicators and input boundaries, color schemes, reduced motion, pause controls, text sizes in `rem`, and focus on a route change.
- The route suite checks focus after a route change, every declared color scheme and reduced motion.
- The pattern pages state what they contradicted before:
  - Amounts show the decimals each currency defines, and money fields are text inputs.
  - The session warning no longer expires.
  - The theme follows the device by default.
  - Every Compact line states its pattern's narrow behavior.

### Part C: Packages Pinned to Their Major

- `litespec.manifest.yaml` pins each third-party package to its major, written `N.x`, such as `Marten: 9.x`. The consumer's central version file or lockfile holds the exact version, so a consumer takes a patch or minor release of a baseline package without waiting for a LiteSpec release.
- Three packages stay exact: `LiteSpec.Analyzers`, which matches the CLI release, and the `shadcn` and `shadcn-svelte` CLIs, whose exact version the UI source lock records. The `node` and `pnpm` entries under `stack` stay exact too.
- `dependency-version-drift` reports a direct package outside its pin: another major, or a prerelease inside the pinned major. Its message reads `... is outside its manifest pin '9.x'.` for a major pin and keeps `... differs from its manifest pin ...` for an exact pin.

Decision 0036, Pin Third-Party Packages to Their Major, records why.

### Part D: The Book

- A use case shows its Kind once, and a scenario shows Given, When, Then and Result in their own sections only.
- The `norun` proof state reads "Not run".
- A page record no longer lists its own entries as links. In 5.0.0 they appeared under raw keys such as `use-case.records.id`.
- The top bar carries the Litenova family mark.

### Part E: Faster Model Check

On a project of 3,142 records and 19,334 files, `litespec model check` took 195 s in 5.0.0 and takes about 18 s in 5.1.0, with the same output. The checks run in parallel, the schema evaluator stops at the first failing shape, and record lookups use indexes built once per run. A run also reads the release's provision pages and schemas once, where 5.0.0 read the provision pages three times and could parse a schema once per parallel worker.

### Part F: The Documentation and litespec.dev

- `learn/README.md` is the starting page: what LiteSpec is, a five-minute picture, then five pages in reading order. Each concept is explained on one page, and the others link to it.
- `learn/reading-tracks.md` and `guide/folder-reference.md` are removed. Their content is in `learn/how-litespec-works.md` and the [folder reference](docs/reference/folders.md).
- On litespec.dev:
  - The sidebar is one tree in five groups, in reading order: Learn, Tutorial, Guides, Standard and Reference. Every area and folder opens and closes, starts open when it holds the current page, and lists its overview first.
  - Breadcrumbs show every level, and every breadcrumb entry in the page's structured data links its page.
  - A folder's overview page is titled by its folder, such as "CLI Commands Overview".
  - The Standard has an index page of linked cards, and "On this page" opens the section in view.
  - A pattern lists its facts with each label beside its value, and its Default links to the option it names.
  - The landing page is the opening title and the release badge, which links to this changelog.
  - Screens 1600px and wider get a wider column and larger type, and muted text reads at 6:1 or more in light mode.
  - Pages animate only when the reader's system allows motion.
  - On a phone, the page browser has a filter.

### Part G: Provision Changes Since v5.0.0

<!-- generated:provision-diff -->
### Provision Changes Since v5.0.0

Added (41):

- `litespec/core/model#require-explanation-files-on-the-layers-a-project-names`
- `litespec/frontend/components#make-touch-targets-fit-a-finger`
- `litespec/frontend/components#show-a-focus-indicator-that-stands-out`
- `litespec/frontend/data#keep-a-calendar-date-free-of-a-time-zone`
- `litespec/frontend/patterns/create-edit-remove#refuse-a-second-send-while-one-is-in-flight`
- `litespec/frontend/patterns/dialogs-and-overlays#hold-a-toast-while-a-modal-is-open`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-tab-free-to-leave-a-menu-or-popover`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-the-page-behind-a-modal-still`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-unsaved-input-through-a-dismissal`
- `litespec/frontend/patterns/dialogs-and-overlays#pause-a-timed-toast-while-it-is-in-use`
- `litespec/frontend/patterns/dialogs-and-overlays#place-initial-focus-on-the-safe-action`
- `litespec/frontend/patterns/feedback-and-states#announce-each-status-by-its-urgency`
- `litespec/frontend/patterns/feedback-and-states#keep-unsent-input-when-the-connection-drops`
- `litespec/frontend/patterns/feedback-and-states#mark-a-loading-region-busy`
- `litespec/frontend/patterns/feedback-and-states#move-focus-to-a-refusal`
- `litespec/frontend/patterns/forms-and-fields#expose-a-field-error-to-assistive-technology`
- `litespec/frontend/patterns/forms-and-fields#keep-submit-enabled-to-reveal-errors`
- `litespec/frontend/patterns/forms-and-fields#mark-every-required-field`
- `litespec/frontend/patterns/forms-and-fields#match-the-keyboard-to-the-value`
- `litespec/frontend/patterns/forms-and-fields#refuse-enter-while-text-is-being-composed`
- `litespec/frontend/patterns/forms-and-fields#show-a-field-error-after-the-field-is-left`
- `litespec/frontend/patterns/forms-and-fields#state-each-fields-purpose`
- `litespec/frontend/patterns/forms-and-fields#take-a-name-in-one-field-of-any-script`
- `litespec/frontend/patterns/lists-and-tables#announce-a-lists-result-count`
- `litespec/frontend/patterns/lists-and-tables#keep-the-header-row-in-view`
- `litespec/frontend/patterns/lists-and-tables#make-a-scrolling-table-a-named-region`
- `litespec/frontend/patterns/lists-and-tables#mark-the-sorted-column`
- `litespec/frontend/patterns/lists-and-tables#shape-a-list-by-its-container`
- `litespec/frontend/patterns/money-time-density#align-figures-in-a-column`
- `litespec/frontend/patterns/money-time-density#mark-a-moment-with-its-machine-value`
- `litespec/frontend/patterns/navigation#draw-sibling-pages-as-links-not-tabs`
- `litespec/frontend/rendering#move-focus-on-a-route-change`
- `litespec/frontend/rendering#split-long-client-work`
- `litespec/frontend/ui#give-every-input-a-visible-boundary`
- `litespec/frontend/web/components#enforce-trusted-types-on-raw-markup`
- `litespec/frontend/web/ui#build-within-the-browser-support-target`
- `litespec/frontend/web/ui#give-moving-content-a-pause-control`
- `litespec/frontend/web/ui#honor-reduced-motion`
- `litespec/frontend/web/ui#render-each-declared-color-scheme`
- `litespec/frontend/web/ui#set-text-sizes-in-relative-units`
- `litespec/quality/ci#assert-loading-stability-and-blocking-time-per-route`

Removed (0):

None.

Renamed (0):

None.

Extensions added (0):

None.

Extensions removed (0):

None.

<!-- /generated:provision-diff -->

### Part H: Upgrading a Consumer

1. Install `LiteSpec.Cli` 5.1.0 (`dotnet tool update LiteSpec.Cli --version 5.1.0`) or rerun the install script, and set `reviewedLiteSpecVersion: 5.1.0` in `.litespec/project.yaml`.
2. A project with a shared UI package adds a `## Browser Support` section to its design contract, naming the browser target, as `templates/consumer/design-contract.md` shows. Without it, `model check` reports `ui-design-contract-invalid` with `missing required section 'Browser Support'`.
3. A project that references `LiteSpec.Analyzers` updates it to 5.1.0.
4. Run `litespec model check` and fix each finding.


## v5.0.0

LiteSpec 5.0.0 lets a project claim only the record layers it uses, writes every file a person edits in YAML, replaces exceptions with departures, and ships the `litespec` command line as a native executable for Windows, Linux and macOS. It is a major release: a consumer upgrades by following the steps in Part H, then running `litespec model check` until it is clean.

### Part A: Claim the Record Layers the Project Uses

- `layers` in the project file replaces the conformance level. A project names each record layer it claims, `intent`, `behavior`, `implementation` and `proof`, leaving layers out only from the bottom; `implementation` and `proof` each take `litespec`, LiteSpec's practice for the layer, or `own`, which keeps the team's practice and checks only that code and tests mark their records.
- Every provision and every finding code carries the record layer it governs and whether it ties that layer to the records (`trace`). `model check`, `rules`, the book and the analyzers report only what the claim covers, and the book shows an unclaimed layer as not claimed.
- A page's Scope block names its `Concern`; a provision's Applies to line may name its `layer` and `trace`.
- The operations alert and runbook checks belong to the `delivery` extension.
- The analyzers read the claim: the traceability rules run when Implementation is claimed, the architecture rules when it follows `litespec`, and LS4001 when Proof is claimed.

### Part B: People Write YAML, Programs Exchange JSON

- `.litespec/project.yaml`, `litespec.manifest.yaml`, the schemas (`schemas/<name>.schema.yaml`), the pattern catalog (`.litespec/implementation/patterns/catalog.yaml`), the UI source lock (`ui-source-lock.yaml`) and the presets are YAML that states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment. Check results, `--format json` output, the book data and the provision index stay JSON.
- The project file holds `frontends` and `ui.package` at the top level, `stackConfig.dotnet.webapi` and `worker` as lists, and no `conformance`, `$schema`, `unstructuredDocs`, `paths.records`, `surfaces`, `sites`, `apiSolution` or `refusals.prefixes`.
- The schemas publish at `https://litespec.dev/5.0.0/schemas/<name>.schema.yaml`, including the new `check-result.schema.yaml`; a harness result that fails it reports `proof-invalid`.
- `litespec model render` writes the UI source lock: each component's digest, the preset and the CLI pin.

### Part C: One Departure Record

- The departure record replaces the exception record. It covers provisions, pages or finding codes on its `subject` or `paths`, with a reason; a gap the team means to close carries `expiresOn` and `exit`, and a lasting choice carries neither. It lives in `.litespec/governance/departures/`.
- `departure-expired` and `departure-invalid` replace the exception codes. No departure covers the findings the model needs to load.

### Part D: One Name per Concept

- A finding's `outcome` is `blocking`, `reported` or `covered`; `severity` leaves the JSON, and `model check` ends with `N findings, M blocking, K covered.`
- Completion stages read `unfulfilled`, `specified`, `coded` and `verified`.
- The qualified identifier is gone; a record shows its folder path and its module.
- Provision strengths read `required`, `default` and `advisory`.

### Part E: A Smaller, Bound Standard

- Near-duplicate provisions keep one home, and every citation follows it. Departures, testing and formats each have one page; how-to guides live under `docs/guide/`.
- Each UI pattern offers its Default and at most two other options.
- The `jobs` extension is cut to what a small team follows; `realtime` and `customer-rules` are extensions; tenant delegation is part of `tenancy`; sagas and workflow engines leave the standard.
- `litespec-maintain validate` reports a cited finding code, template or example record that does not exist, a `Checked by` line for the model check that names no code, a page the index does not reach, and a pattern with more than three options. The finding-code, folder and pin tables are generated.
- `quality/testing.md` holds one mock rule for every stack: a test substitutes only what the code under test does not own and what is not deterministic.
- An agent never weakens a test that proves a scenario, and `model check --since` reports `proof-hash-drift` for a proving test that changed while its scenario did not.

### Part F: A Native Command Line on Every Platform

- The CLI compiles ahead of time with .NET Native AOT for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64` and `osx-arm64`, and needs no .NET runtime.
- `LiteSpec.Cli` on NuGet is a per-platform tool package, so `dotnet tool install`, `dotnet tool restore` and `dotnet litespec` work unchanged; every GitHub release carries a standalone archive per platform, `SHA256SUMS`, `install.sh` and `install.ps1`, each with a build provenance attestation.
- `litespec init --install standalone` writes a project that runs the CLI as `litespec`; in a consumer root, the install scripts install the release that `.litespec/project.yaml` reviews.
- The Linux glibc binaries run on glibc 2.28 and later and the musl binaries on Alpine 3.21 and later; the CLI computes SHA-256 itself.
- The CLI and the maintainer tool check every schema with LiteSpec's own JSON Schema evaluator, draft 2020-12 without dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems` and `contentSchema`.
- Every archive and package ships `THIRD-PARTY-NOTICES.md`.

### Part G: Provision Changes Since v4.0.0

<!-- generated:provision-diff -->
### Provision Changes Since v4.0.0

Added (162):

- `litespec/backend/domain#raise-the-past-tense-of-a-methods-own-verb`
- `litespec/backend/dotnet/domain#keep-clock-reads-and-random-values-out-of-domain`
- `litespec/backend/dotnet/domain#keep-double-and-float-out-of-exact-quantities`
- `litespec/backend/dotnet/domain#mark-each-changing-root-method-with-one-use-case`
- `litespec/backend/dotnet/naming#check-architectural-role-suffixes`
- `litespec/backend/dotnet/naming#check-repository-variable-names`
- `litespec/backend/dotnet/naming#check-result-part-names`
- `litespec/backend/dotnet/persistence#keep-pipeline-owned-collaborators-out-of-handlers`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record`
- `litespec/backend/dotnet/stack#pin-the-test-packages-in-the-manifest`
- `litespec/backend/dotnet/stack#run-mutation-testing-with-stryker-net`
- `litespec/backend/dotnet/stack#start-the-integration-database-in-a-container`
- `litespec/backend/dotnet/stack#test-with-xunit-v3-on-microsoft-testing-platform`
- `litespec/backend/persistence#test-persistence-against-the-pinned-database`
- `litespec/core/agent#protect-proofs-from-implementation-edits`
- `litespec/core/departures#depart-from-advice-without-a-record`
- `litespec/core/departures#record-a-project-wide-lowering-as-a-departure`
- `litespec/core/departures#record-accepted-gaps-as-departures`
- `litespec/core/language#cite-shared-provisions-in-their-published-form`
- `litespec/core/language#exclude-declared-content-paths-from-the-ascii-check`
- `litespec/core/language#exclude-the-compound-that-names-another-concept`
- `litespec/core/language#follow-the-identifier-form-of-each-record-type`
- `litespec/core/language#keep-wire-codes-stable`
- `litespec/core/language#name-records-with-one-to-six-words`
- `litespec/core/language#record-the-project-vocabulary-as-data`
- `litespec/core/language#reject-a-recorded-synonym-inside-its-scope`
- `litespec/core/language#resolve-every-identifier-citation`
- `litespec/core/language#review-each-term-for-its-lookalikes`
- `litespec/core/language#state-mannered-terms-literally`
- `litespec/core/language#use-every-identifier-once`
- `litespec/core/language#use-the-declared-record-identifier-grammar`
- `litespec/core/model#classify-every-specification-file`
- `litespec/core/model#declare-structured-specification-metadata`
- `litespec/core/model#derive-a-computed-fact-instead-of-restating-it`
- `litespec/core/model#keep-code-out-of-intent-and-behavior-fields`
- `litespec/core/model#rewrite-every-link-on-a-rename`
- `litespec/core/model#scaffold-pages-and-reserve-identifiers`
- `litespec/core/model#start-each-explanation-file-with-its-title`
- `litespec/core/model#state-what-happens-in-every-behavior-statement`
- `litespec/core/model#use-one-metadata-carrier`
- `litespec/core/model#validate-a-single-page-before-committing`
- `litespec/core/release#record-accepted-debt-as-a-departure`
- `litespec/core/scope#claim-the-record-layers-the-project-uses`
- `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure`
- `litespec/core/scopes#give-every-provision-its-layer-and-trace`
- `litespec/ext/customer-rules#bound-tenant-rule-runs-per-period`
- `litespec/ext/customer-rules#isolate-hook-failures-from-the-trigger`
- `litespec/ext/customer-rules#record-the-customer-rules-catalog`
- `litespec/ext/customer-rules#refuse-unknown-catalog-entries`
- `litespec/ext/customer-rules#state-what-a-hook-may-not-assume`
- `litespec/ext/customer-rules#state-what-a-hook-may-rely-on`
- `litespec/ext/customer-rules#treat-tenant-rules-as-data`
- `litespec/ext/customer-rules#version-catalog-entries`
- `litespec/ext/delivery#ask-only-unanswered-questions`
- `litespec/ext/delivery#close-incident-departures-within-seven-days`
- `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`
- `litespec/ext/delivery#hold-back-example-rows-from-the-implementing-worker`
- `litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`
- `litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert`
- `litespec/ext/delivery#point-each-operations-file-at-its-decision`
- `litespec/ext/delivery#restrict-a-worker-to-its-paths`
- `litespec/ext/delivery#verify-before-accepting-worker-output`
- `litespec/ext/delivery#write-a-brief-before-handing-off`
- `litespec/ext/realtime#authenticate-and-authorize-subscriptions`
- `litespec/ext/realtime#bound-realtime-resources`
- `litespec/ext/realtime#declare-realtime-freshness`
- `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`
- `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`
- `litespec/ext/realtime#record-realtime-need`
- `litespec/ext/realtime#record-transport-support`
- `litespec/ext/realtime#reject-cosmetic-realtime-adoption`
- `litespec/ext/realtime#reject-unbounded-slow-client-buffering`
- `litespec/ext/realtime#revalidate-long-lived-access`
- `litespec/ext/realtime#subscribe-through-narrow-functions`
- `litespec/ext/realtime#tolerate-notification-ordering-differences`
- `litespec/ext/realtime#use-server-sent-events-for-notifications`
- `litespec/ext/realtime#use-signalr-for-interactive-messaging`
- `litespec/frontend/blazor/stack#bind-blazor-routes-to-their-address`
- `litespec/frontend/blazor/stack#build-blazor-forms-on-the-shared-form-entry-point`
- `litespec/frontend/blazor/stack#declare-blazor-outside-the-controlled-ui-contract`
- `litespec/frontend/blazor/stack#keep-blazor-test-mocks-to-the-owned-boundaries`
- `litespec/frontend/blazor/stack#keep-class-and-style-bindings-out-of-blazor-page-code`
- `litespec/frontend/blazor/stack#keep-layouts-and-error-components-unmarked`
- `litespec/frontend/blazor/stack#keep-the-blazor-frame-routes-and-modules`
- `litespec/frontend/blazor/stack#load-blazor-route-data-before-its-view-renders`
- `litespec/frontend/blazor/stack#localize-blazor-copy-through-a-pinned-dictionary-library`
- `litespec/frontend/blazor/stack#mark-each-blazor-route-entry-with-its-screen`
- `litespec/frontend/blazor/stack#name-blazor-drawings-with-the-visual-suffix`
- `litespec/frontend/blazor/stack#render-raw-html-only-through-markupstring`
- `litespec/frontend/blazor/stack#run-blazor-logic-tests-on-xunitv3`
- `litespec/frontend/blazor/stack#show-route-failure-in-the-frames-route-error-component`
- `litespec/frontend/blazor/stack#use-a-layout-for-the-blazor-shell`
- `litespec/frontend/blazor/stack#validate-blazor-route-parameters-before-loading`
- `litespec/frontend/mobile/android/stack#accept-app-links-in-the-host-activity`
- `litespec/frontend/mobile/android/stack#apply-system-insets-to-each-screen`
- `litespec/frontend/mobile/android/stack#declare-android-outside-the-controlled-ui-contract`
- `litespec/frontend/mobile/android/stack#hold-one-navigation-controller-in-the-platform-project`
- `litespec/frontend/mobile/android/stack#keep-android-core-test-mocks-to-the-owned-boundaries`
- `litespec/frontend/mobile/android/stack#keep-hosts-and-failure-views-unmarked`
- `litespec/frontend/mobile/android/stack#keep-screen-styles-in-resources`
- `litespec/frontend/mobile/android/stack#keep-the-android-frame-routes-and-modules`
- `litespec/frontend/mobile/android/stack#mark-each-android-screen-with-its-screen-record`
- `litespec/frontend/mobile/android/stack#read-android-copy-through-the-generated-resource-class`
- `litespec/frontend/mobile/android/stack#request-audio-focus-before-playing-sound`
- `litespec/frontend/mobile/android/stack#request-permissions-at-the-point-of-use`
- `litespec/frontend/mobile/android/stack#route-the-back-gesture-through-the-activity-dispatcher`
- `litespec/frontend/mobile/android/stack#run-android-core-tests-on-xunitv3`
- `litespec/frontend/mobile/android/stack#store-navigation-state-in-the-activity-bundle`
- `litespec/frontend/mobile/android/stack#store-persisted-state-in-app-private-storage`
- `litespec/frontend/mobile/android/stack#use-the-system-pickers-for-media`
- `litespec/frontend/web/react/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/react/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/react/stack#declare-route-search-parameters-in-validatesearch`
- `litespec/frontend/web/react/stack#hold-a-routes-parts-in-its-route-file`
- `litespec/frontend/web/react/stack#keep-layouts-error-components-and-route-groups-unmarked`
- `litespec/frontend/web/react/stack#keep-react-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/react/stack#keep-the-react-frame-routes-and-modules`
- `litespec/frontend/web/react/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/react/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/react/stack#mark-each-react-route-entry-with-its-screen`
- `litespec/frontend/web/react/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/react/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/react/stack#prefetch-route-reads-in-the-loader`
- `litespec/frontend/web/react/stack#read-route-data-with-usesuspensequery`
- `litespec/frontend/web/react/stack#render-raw-html-only-through-dangerouslysetinnerhtml`
- `litespec/frontend/web/react/stack#run-the-registry-build-command`
- `litespec/frontend/web/react/stack#show-route-failure-in-the-route-error-component`
- `litespec/frontend/web/react/stack#use-named-props-for-class-and-style-bindings`
- `litespec/frontend/web/react/stack#use-the-shared-package-form-hook`
- `litespec/frontend/web/react/stack#use-the-tsx-extension-on-shared-files`
- `litespec/frontend/web/svelte/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/svelte/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/svelte/stack#declare-route-parameters-in-a-schema`
- `litespec/frontend/web/svelte/stack#hold-a-routes-parts-in-its-folder`
- `litespec/frontend/web/svelte/stack#keep-layout-files-error-pages-and-route-groups-unmarked`
- `litespec/frontend/web/svelte/stack#keep-route-group-names-in-router-syntax`
- `litespec/frontend/web/svelte/stack#keep-svelte-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/svelte/stack#keep-the-svelte-frame-routes-and-modules`
- `litespec/frontend/web/svelte/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/svelte/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/svelte/stack#mark-svelte-route-entries`
- `litespec/frontend/web/svelte/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/svelte/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/svelte/stack#name-the-module-extension-on-each-import`
- `litespec/frontend/web/svelte/stack#prefetch-route-reads-in-the-load`
- `litespec/frontend/web/svelte/stack#read-route-data-with-a-query-creator`
- `litespec/frontend/web/svelte/stack#render-raw-html-only-through-one-template-tag`
- `litespec/frontend/web/svelte/stack#resolve-imports-through-the-lib-alias`
- `litespec/frontend/web/svelte/stack#run-the-registry-build-command`
- `litespec/frontend/web/svelte/stack#show-route-failure-in-the-route-error-page`
- `litespec/frontend/web/svelte/stack#use-named-attributes-for-class-and-style-bindings`
- `litespec/frontend/web/svelte/stack#use-the-shared-package-form-creator`
- `litespec/frontend/web/svelte/stack#use-the-svelte-extension-on-shared-files`
- `litespec/frontend/web/svelte/stack#write-components-in-runes-mode`
- `litespec/quality/testing#derive-expected-values-from-the-scenario`
- `litespec/quality/testing#keep-the-test-projects-the-size-requires`
- `litespec/quality/testing#prove-each-rule-through-its-scenarios`
- `litespec/quality/testing#prove-each-scenario-through-the-public-boundary`
- `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`
- `litespec/quality/testing#use-evidence-rather-than-one-coverage-target`
- `litespec/quality/testing#use-one-mock-rule-for-every-stack`

Removed (114):

- `litespec/backend/dotnet/domain#expose-cross-record-keys-from-the-aggregate`
- `litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record-at-traced`
- `litespec/backend/identifiers#cite-shared-provisions-in-their-published-form`
- `litespec/backend/identifiers#follow-the-identifier-form-of-each-record-type`
- `litespec/backend/identifiers#keep-wire-codes-stable`
- `litespec/backend/identifiers#name-records-with-one-to-six-words`
- `litespec/backend/identifiers#resolve-every-identifier-citation`
- `litespec/backend/identifiers#use-every-identifier-once`
- `litespec/backend/persistence#test-persistence-against-postgresql`
- `litespec/backend/testing#derive-expected-values-from-the-scenario`
- `litespec/backend/testing#mark-the-test-that-proves-each-scenario`
- `litespec/backend/testing#prove-each-rule-through-its-scenarios`
- `litespec/backend/testing#refuse-surviving-mutants-on-changed-lines`
- `litespec/backend/testing#test-application-coordination`
- `litespec/backend/testing#use-evidence-rather-than-one-coverage-target`
- `litespec/core/access#declare-every-operation-on-every-external-system`
- `litespec/core/access#name-every-entry-point-with-its-kind`
- `litespec/core/authoring#classify-every-specification-file`
- `litespec/core/authoring#declare-structured-specification-metadata`
- `litespec/core/authoring#derive-a-computed-fact-instead-of-restating-it`
- `litespec/core/authoring#exclude-declared-content-paths-from-the-ascii-check`
- `litespec/core/authoring#exclude-the-compound-that-names-another-concept`
- `litespec/core/authoring#keep-code-out-of-intent-and-behavior-fields`
- `litespec/core/authoring#record-the-project-vocabulary-as-data`
- `litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope`
- `litespec/core/authoring#review-each-term-for-its-lookalikes`
- `litespec/core/authoring#rewrite-every-link-on-a-rename`
- `litespec/core/authoring#scaffold-pages-and-reserve-identifiers`
- `litespec/core/authoring#start-each-explanation-file-with-its-title`
- `litespec/core/authoring#state-mannered-terms-literally`
- `litespec/core/authoring#state-what-happens-in-every-behavior-statement`
- `litespec/core/authoring#use-one-metadata-carrier`
- `litespec/core/authoring#use-the-declared-record-identifier-grammar`
- `litespec/core/authoring#validate-a-single-page-before-committing`
- `litespec/core/layers#close-incident-exceptions-within-seven-days`
- `litespec/core/model#give-every-use-case-its-results`
- `litespec/core/model#reach-behavior-from-every-intent-record`
- `litespec/core/model#record-a-project-wide-lowering-as-an-exception`
- `litespec/core/model#record-accepted-gaps-as-exceptions`
- `litespec/core/principles#build-only-what-the-specification-states`
- `litespec/core/principles#declare-every-edge`
- `litespec/core/principles#enter-facts-from-the-world-as-research-or-constraints`
- `litespec/core/principles#map-each-engine-step-to-one-use-case`
- `litespec/core/principles#prove-scenarios-with-tests`
- `litespec/core/principles#raise-what-a-lower-layer-needs-to-the-owner`
- `litespec/core/principles#write-each-layer-from-the-layer-above`
- `litespec/core/release#record-accepted-debt-as-an-exception`
- `litespec/core/scope#declare-the-conformance-level`
- `litespec/core/scope#keep-every-record-about-the-software`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-exception`
- `litespec/core/scopes#give-every-provision-a-conformance-level`
- `litespec/core/writing#keep-company-facts-out-of-intent-prose`
- `litespec/core/writing#name-each-title-in-its-record-type-form`
- `litespec/ext/automation#bound-tenant-rule-runs-per-period`
- `litespec/ext/automation#isolate-hook-failures-from-the-trigger`
- `litespec/ext/automation#record-the-automation-catalog`
- `litespec/ext/automation#refuse-unknown-catalog-entries`
- `litespec/ext/automation#state-what-a-hook-may-not-assume`
- `litespec/ext/automation#state-what-a-hook-may-rely-on`
- `litespec/ext/automation#treat-tenant-rules-as-data`
- `litespec/ext/automation#version-catalog-entries`
- `litespec/ext/compat#declare-published-facts-as-integration-events`
- `litespec/ext/delegation#ask-only-unanswered-questions`
- `litespec/ext/delegation#hold-back-example-rows-from-the-implementing-worker`
- `litespec/ext/delegation#restrict-a-worker-to-its-paths`
- `litespec/ext/delegation#verify-before-accepting-worker-output`
- `litespec/ext/delegation#write-a-brief-before-delegating`
- `litespec/ext/delivery#apply-schema-work-before-traffic`
- `litespec/ext/delivery#keep-schema-work-out-of-startup`
- `litespec/ext/jobs#bound-the-ingest-buffer`
- `litespec/ext/jobs#declare-ingest-steps-in-order`
- `litespec/ext/jobs#declare-the-engine-carrier`
- `litespec/ext/jobs#define-the-closed-drop-reason-set`
- `litespec/ext/jobs#discard-poison-records-after-three-attempts`
- `litespec/ext/jobs#expose-drop-counts-per-reason`
- `litespec/ext/jobs#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/jobs#map-each-engine-step-to-one-use-case`
- `litespec/ext/jobs#name-engine-steps-after-use-cases`
- `litespec/ext/jobs#read-enrichment-data-from-declared-sources`
- `litespec/ext/jobs#record-one-drop-reason-per-dropped-record`
- `litespec/ext/jobs#reject-unmapped-engine-steps`
- `litespec/ext/jobs#state-each-step-compensation`
- `litespec/ext/jobs#state-the-freshness-consequence`
- `litespec/ext/outbox#authenticate-and-authorize-subscriptions`
- `litespec/ext/outbox#bound-realtime-resources`
- `litespec/ext/outbox#declare-realtime-freshness`
- `litespec/ext/outbox#place-realtime-code-at-outer-boundaries`
- `litespec/ext/outbox#reconnect-and-refresh-authoritative-state`
- `litespec/ext/outbox#record-realtime-need`
- `litespec/ext/outbox#record-transport-support`
- `litespec/ext/outbox#reject-cosmetic-realtime-adoption`
- `litespec/ext/outbox#reject-unbounded-slow-client-buffering`
- `litespec/ext/outbox#revalidate-long-lived-access`
- `litespec/ext/outbox#subscribe-through-narrow-functions`
- `litespec/ext/outbox#tolerate-notification-ordering-differences`
- `litespec/ext/outbox#use-server-sent-events-for-notifications`
- `litespec/ext/outbox#use-signalr-for-interactive-messaging`
- `litespec/frontend/patterns/dialogs-and-overlays#bottom-sheet-on-a-phone`
- `litespec/frontend/patterns/forms-and-fields#keep-entered-values-after-a-refused-submit`
- `litespec/frontend/patterns/lists-and-tables#list-on-a-phone`
- `litespec/frontend/patterns/navigation#phone-navigation`
- `litespec/frontend/patterns/page-frames#record-drawer`
- `litespec/frontend/patterns/page-frames#show-one-primary-action-in-a-page-frame`
- `litespec/frontend/patterns/record-detail#hold-built-record-headers-to-one-primary-action`
- `litespec/frontend/patterns/record-detail#record-header-on-a-phone`
- `litespec/frontend/testing#keep-mocks-at-owned-boundaries`
- `litespec/frontend/ui#prove-every-result-a-route-calls`
- `litespec/frontend/web/svelte/components#write-components-in-runes-mode`
- `litespec/quality/operations#apply-schema-changes-outside-request-startup`
- `litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check`
- `litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`
- `litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert`
- `litespec/quality/operations#point-each-operations-file-at-its-decision`

Renamed (13):

- `litespec/backend/dotnet/application#keep-command-handlers-to-load-call-and-stage` to `litespec/backend/dotnet/application#bind-command-handlers-to-load-call-and-stage`
- `litespec/backend/dotnet/application#keep-query-handlers-free-of-writes` to `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories`
- `litespec/core/authoring#state-one-layer-per-page` to `litespec/core/authoring#state-one-tier-per-page`
- `litespec/core/release#qualify-the-checker-with-known-good-and-known-bad-fixtures` to `litespec/core/release#qualify-the-reader-with-known-good-and-known-bad-fixtures`
- `litespec/frontend/mobile/screens#restore-the-visitors-place` to `litespec/frontend/mobile/screens#restore-the-users-place`
- `litespec/frontend/patterns/admin-screens#bringing-data-from-spreadsheets` to `litespec/frontend/patterns/admin-screens#importing-data-from-a-file`
- `litespec/frontend/patterns/admin-screens#budget-codes-and-advances` to `litespec/frontend/patterns/admin-screens#budget-limits-and-advances`
- `litespec/frontend/patterns/admin-screens#supplier-invoice` to `litespec/frontend/patterns/admin-screens#subscription-invoices`
- `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-policy` to `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-draft`
- `litespec/frontend/patterns/forms-and-fields#color-field-for-the-mail-accent` to `litespec/frontend/patterns/forms-and-fields#color-field`
- `litespec/frontend/patterns/forms-and-fields#repeatable-group-for-budget-codes` to `litespec/frontend/patterns/forms-and-fields#repeatable-group`
- `litespec/frontend/patterns/lists-and-tables#trips-as-a-grid` to `litespec/frontend/patterns/lists-and-tables#the-layout-of-a-short-list`
- `litespec/frontend/patterns/record-detail#expense-record` to `litespec/frontend/patterns/record-detail#whole-record-screen`

Extensions added (2):

- `customer-rules`
- `realtime`

Extensions removed (2):

- `automation`
- `delegation`

<!-- /generated:provision-diff -->

### Part H: Upgrading a Consumer

1. Install `LiteSpec.Cli` 5.0.0 (`dotnet tool update LiteSpec.Cli --version 5.0.0`) or rerun the install script.
2. Rename `.litespec/project.json` to `.litespec/project.yaml` and write it as YAML: add the schema comment and `schemaVersion: 1`, set `reviewedLiteSpecVersion: 5.0.0`, and replace `conformance` with `layers`: `records` becomes `intent` and `behavior`; `traced` becomes all four with `implementation: own` and `proof: own`; `full` becomes all four with `litespec`, plus the `delivery` extension when the project runs alerts and runbooks.
3. Move `paths.frontends` to `frontends` and `paths.uiPackage` to `ui.package`, make `webapi` and `worker` lists, and delete the removed fields Part B names.
4. Convert the pattern catalog and the UI source lock to YAML at their 5.0.0 paths, drop `kind` and `shell` from the `DESIGN.md` metadata, and run `litespec model render` to rewrite the lock.
5. Rewrite each exception record as a departure under `.litespec/governance/departures/`: `kind: gap` keeps `expiresOn` and `exit`; `kind: departure` drops `expiresOn` when the choice is lasting; `covers` becomes a list.
6. Rename the code citations and marks of every provision the Part G list renames, and read `--format json` output for `outcome` instead of `severity` and `known`.
7. Run `litespec model check` and fix each finding.

## v4.0.0

LiteSpec 4.0.0 is a standard for specification-driven development of one bounded-context business application by people and coding agents. It defines a strict specification model of linked records beside graded engineering provisions, with git as the decision record. A consumer pins this release in `reviewedLiteSpecVersion`. The release takes effect as the `v4.0.0` tag.

### Part A: The Nature of LiteSpec

- The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve; a violation is a defect.
- Each provision states its strength, rule, default or advice, and where it applies; a check exists where one is cheap and sure.
- Git is the decision record: a record in the repository is decided, and the pull request that changes it is its review.
- The specification is closed: an agent builds what the specification states and takes anything missing to the owner or the backlog board.
- Every record is about the software: what the application does, holds, refuses at runtime or runs on.

### Part B: Git Is the Decision Record

- Records carry no history of their own; git carries who changed what and when.
- Completion is computed from code, tests and passing scenarios, and the book shows it first.
- An exception carries reason, exit and expiry; an expired exception is a blocking finding.
- A date about the outside world sits with its source as `readOn`.

### Part C: Eight Commands

- The CLI has eight commands: `init`, `new`, `rename`, `model check`, `model format`, `model query`, `book` and `rules`.
- `model check` is the one command CI runs: schema, folders, names, links, locality, reachability, marks and exceptions.
- `model render` has three regions: `alert-inventory`, `runbook-index` and `operations-log`.
- An additional command, finding code, analyzer rule or generated file needs an owner decision that records it.

### Part D: Provisions by Situation

- Each page declares its stack and layer; a provision adds `Applies to` only when narrower than its page.
- Every provision states `Strength`: rule, default or advice.
- The provision index carries scope and strength; the site groups Standard, Reference, Learn and LiteSpec's own records.

### Part E: Rules for the File at Hand

- `litespec rules <path or record id>` lists the applying provisions grouped by strength, with why each was retrieved; `--full` adds rationale and example.
- Project conventions and departures merge into the result; a departure shows in place of the provision it replaces.
- A retrieval evaluation with a precision target proves the command.

### Part F: The Consumer Uses the Same Structure

- Layer and artifact derive from the stack structure and the records a file marks.
- Project conventions live in `.litespec/governance/conventions/` in provision shape.
- A departure names the provision it replaces and a scope.
- Records use the same layer and artifact terms, so one query answers file, records and provisions together.

### Part G: Surfaces That State the Nature

- The README, AGENTS.md, the core pages, the command pages, the templates and this changelog state the nature above.
- The docs index groups pages with generated counts; the site navigation follows the same groups.

### Part H: Consumers Adopt the Whole Contract

- A consumer pins one release and adopts a later release by accepting its whole contract.
- A consumer declares the conformance level it claims in `.litespec/project.json`: `records`, `traced` or `full`. The levels nest, and `model check`, the book, `rules` and the analyzers report only the part of the contract the level claims.
- At `records` a project keeps its own code layout and needs no stack; at `traced` its code and tests mark their records and its test runs prove scenarios, in its own layout; at `full` it takes the selected stacks, the code layout, the analyzers and the operations provisions.
- Provision identifiers stay stable within a major version; the changelog of each release lists the provision changes since the previous tag, generated by `litespec-maintain changelog`.

### Part I: Every Record Is About the Software

- Intent reads module, then feature, then requirement; Behavior reads module, then aggregate, then use case.
- Features sit under `intent/modules/<module>/features/` with their requirements as entries; a requirement identifier has the form `requirement/<feature>.<name>`.
- A folder under `intent/modules/` that names no declared module is a blocking finding.
- Reachability blocks: a goal reaches a journey or a quality requirement, a problem reaches a feature or requirement, a feature or requirement reaches Behavior, a constraint is answered, research is cited, and a decision decides a record that reaches Behavior.
- Identifiers are short and unique in the product; the qualified identifier is computed from identifier segments plus placement, never written.

### Part J: One Placement Model for Every Layer

- Every record has one parent; the container is the parent; product, module, aggregate and entry point are the only folder nodes.
- Behavior, Implementation and Governance place each type per the placement table in the model page; Proof holds no authored records.
- Code and tests match the qualified identifier of the record they mark in folder and namespace, including frontend module folders.
- Module `dependsOn` holds no cycle; an integration event has one publishing module.

### Part K: Fields That State the Product

- Every record carries type, id, title, terms and records plus its type fields; every record carries one `summary`, and a record that makes a testable claim carries one `statement`.
- The project file carries project, size, shape, stacks, stackConfig, paths, marks, proof, dependencies, screenshots, refusals, selectedExtensions and `reviewedLiteSpecVersion`.
- `risk` sits on permission only; `setBy` uses product, operator or tenant; a rule names where retained facts live in `retainedIn`; a use case states its field read and write lifecycle.

### Part L: Complete Work Before Merging

- A branch may be incomplete; what merges is complete. `model check` reports reachability locally and blocks in the pull-request check; an exception with `expiresOn` parks one record.
- The folder wins: an identifier whose home segment disagrees with its folder is a blocking finding; each name segment is unique in the product.
- Tiers: G reads records and always blocks, S reads code and blocks where qualified, A reports and never blocks.
- An exception is live when present and unexpired.

### Part M: The Module Layout as the Default Architecture

- Layers are roles: Domain, Application, Infrastructure and WebApi with the reference table the architecture page states.
- One application project holds every module; hosts hold composition and HTTP plumbing.
- The folder tree uses closed folder sets per level; use case folders are flat; cross-module access runs only through `Contracts/` along `dependsOn`.
- The `layer-projects` extension holds the five-project layout for consumers that select it.

### Part N: Gaps Closed in the Standard

- A deviation is an exception of kind departure whose `covers` names the provision it replaces.
- Product patterns live in `implementation/patterns/`; product copy rules live in `implementation/writing/`; document pages may sit at a layer root.
- Module rules live in `behavior/modules/<module>/rules/`; adapter tests sit under the adapter's `Infrastructure/` path.
- Guidance precedence lives in one provision on the agent page.

### Part O: Checks a Partial Adopter Can Read

- Findings about Intent that no Behavior reaches yet report locally and block in the pull-request check, so Intent can run ahead of Behavior.
- The .NET reader reads several web and worker hosts, reports a default project root that matches nothing as `reader-missing`, and at `traced` finds an aggregate root by its mark and its state record.
- A JUnit test case proves a Behavior scenario through a `litespec.proves` property, so a runner whose steps carry no mark, or a stack with no code reader, still proves its scenarios.
- `model check` lists level A findings as a summary by code unless `--all` is given, and its JSON output maps G, S and A to error, warning and note.
- `init` writes a project file the schema accepts for every preset, including the `records` preset, and no size requires an exception record.

### Part P: Analyzers as Proof and a Smaller Rules Output

- A structural boundary needs an architecture test only where no analyzer or `model check` finding covers it; `analyzer-lowering-unrecorded` reports an analyzer rule lowered or switched off without an exception, so an analyzer can stand as the proof.
- LS1013 reports a switch over a state or closed-set record hierarchy that leaves a sealed case to its discard arm; the tests, not the compiler, catch a new case.
- LS2012 also reports a handler that takes the authorization guard.
- `litespec rules` prints statements only for the provisions that need judgment, the checked provisions as one line with their codes, and the layer-wide provisions by identifier; every provision on a page that names kinds of file names its own.

<!-- generated:provision-diff -->
### Provision Changes Since v3.0.0

Added (238):

- `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`
- `litespec/backend/architecture#read-another-module-only-through-its-contracts`
- `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`
- `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events`
- `litespec/backend/dotnet/api#place-endpoints-in-their-use-case-folders`
- `litespec/backend/dotnet/application#use-the-use-case-folder-layout`
- `litespec/backend/dotnet/architecture#follow-declared-module-dependencies`
- `litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file`
- `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`
- `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`
- `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`
- `litespec/backend/dotnet/architecture#keep-use-case-folders-isolated`
- `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`
- `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`
- `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`
- `litespec/backend/dotnet/architecture#use-one-public-assembly-marker`
- `litespec/backend/dotnet/domain#use-the-domain-layer-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#keep-application-dependencies-narrow`
- `litespec/backend/dotnet/ext/layer-projects#keep-domain-package-free`
- `litespec/backend/dotnet/ext/layer-projects#keep-net-production-and-test-projects-separate`
- `litespec/backend/dotnet/ext/layer-projects#keep-one-application-assembly`
- `litespec/backend/dotnet/ext/layer-projects#keep-the-test-projects-the-size-requires`
- `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`
- `litespec/backend/dotnet/ext/layer-projects#run-litespecanalyzers-in-every-application-project`
- `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects`
- `litespec/backend/dotnet/ext/layer-projects#use-the-domain-module-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-endpoint-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-operation-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-project-reference-graph`
- `litespec/backend/dotnet/ext/layer-projects#use-this-baseline-package-ownership`
- `litespec/backend/dotnet/ext/layer-projects#use-this-infrastructure-layout`
- `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`
- `litespec/backend/dotnet/persistence#use-the-persistence-layer-folder-layout`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record-at-traced`
- `litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host`
- `litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder`
- `litespec/core/access#keep-permission-lists-to-entries`
- `litespec/core/agent#load-applicable-provisions-before-editing`
- `litespec/core/agent#name-judged-provisions-in-the-completion-report`
- `litespec/core/agent#raise-open-figures-to-the-owner`
- `litespec/core/authoring#generate-the-provision-diff-for-each-release`
- `litespec/core/domain-model#name-each-module-after-its-capability`
- `litespec/core/intent#admit-only-outside-pains-as-problems`
- `litespec/core/intent#define-each-recurring-capitalized-phrase`
- `litespec/core/intent#describe-every-journey`
- `litespec/core/intent#explain-every-problem-in-four-sections`
- `litespec/core/intent#grade-problem-evidence-by-kind`
- `litespec/core/intent#group-product-records-by-module`
- `litespec/core/intent#keep-actors-to-people-and-organizations`
- `litespec/core/intent#keep-company-operation-out-of-intent`
- `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`
- `litespec/core/intent#name-each-sibling-problem-or-merge-it`
- `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`
- `litespec/core/intent#name-no-solution-in-a-problem-statement`
- `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`
- `litespec/core/intent#reach-every-intent-record-with-behavior`
- `litespec/core/intent#shape-each-module-page`
- `litespec/core/intent#state-each-feature-with-its-offering`
- `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`
- `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`
- `litespec/core/layers#send-commercial-and-observable-figures-to-the-owner`
- `litespec/core/maintaining#keep-provision-identifiers-stable-within-a-major-version`
- `litespec/core/model#compute-completion-in-the-builder`
- `litespec/core/model#connect-across-modules-only-through-declared-dependencies`
- `litespec/core/model#declare-size-and-stacks`
- `litespec/core/model#hold-module-rules-in-rule-sets`
- `litespec/core/model#keep-each-name-unique-in-the-product`
- `litespec/core/model#keep-module-dependencies-free-of-cycles`
- `litespec/core/model#move-each-identifier-with-its-module`
- `litespec/core/model#publish-each-integration-event-from-one-module`
- `litespec/core/model#reach-behavior-from-every-intent-record`
- `litespec/core/model#record-each-screens-captures-from-the-gallery`
- `litespec/core/model#take-decision-numbers-from-one-sequence`
- `litespec/core/principles#build-only-what-the-specification-states`
- `litespec/core/principles#complete-work-before-merging`
- `litespec/core/principles#compute-completion-from-code-and-tests`
- `litespec/core/principles#keep-git-as-the-decision-record`
- `litespec/core/principles#keep-tools-small`
- `litespec/core/principles#raise-what-a-lower-layer-needs-to-the-owner`
- `litespec/core/principles#state-the-strength-of-every-provision`
- `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-an-owner-question`
- `litespec/core/principles#write-every-record-about-the-software`
- `litespec/core/privacy#complete-the-subject-export-before-erasure`
- `litespec/core/privacy#discard-rows-that-leave-no-trace`
- `litespec/core/privacy#keep-a-tombstone-where-deletion-stays-visible`
- `litespec/core/privacy#keep-tombstone-fields-minimal`
- `litespec/core/privacy#move-data-to-its-next-controller-instead-of-erasing`
- `litespec/core/privacy#purge-erased-data-from-restored-backups`
- `litespec/core/privacy#record-which-duty-wins-each-conflict`
- `litespec/core/privacy#remove-child-rows-through-their-parent-only`
- `litespec/core/privacy#require-a-fresh-export-after-changes`
- `litespec/core/privacy#run-one-sweep-job-per-data-class`
- `litespec/core/privacy#scope-erasure-to-what-the-subject-requested`
- `litespec/core/privacy#state-the-erasure-sentence-in-intent`
- `litespec/core/scope#declare-the-conformance-level`
- `litespec/core/scope#keep-every-litespec-file-under-one-folder`
- `litespec/core/scope#keep-every-record-about-the-software`
- `litespec/core/scopes#declare-the-page-scope`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-exception`
- `litespec/core/scopes#give-every-provision-a-conformance-level`
- `litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one`
- `litespec/core/scopes#record-each-replaced-default`
- `litespec/core/scopes#state-applies-to-only-when-narrower`
- `litespec/core/scopes#use-only-declared-scope-values`
- `litespec/core/system#drive-work-from-records`
- `litespec/core/use-cases#write-prose-on-every-use-case`
- `litespec/core/writing#apply-plain-mechanics`
- `litespec/core/writing#comment-on-why-not-what`
- `litespec/core/writing#compare-real-options-in-each-decision-record`
- `litespec/core/writing#give-each-explanation-file-its-fixed-sections`
- `litespec/core/writing#give-each-paragraph-one-idea`
- `litespec/core/writing#keep-company-facts-out-of-intent-prose`
- `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words`
- `litespec/core/writing#name-each-title-in-its-record-type-form`
- `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`
- `litespec/core/writing#put-the-actor-first`
- `litespec/core/writing#state-each-statement-without-modal-words`
- `litespec/core/writing#use-literal-words`
- `litespec/core/writing#write-each-changelog-as-a-release-comparison`
- `litespec/core/writing#write-each-documentation-page-as-one-kind`
- `litespec/core/writing#write-every-record-title-in-title-case`
- `litespec/core/writing#write-for-the-reader-who-arrives-cold`
- `litespec/ext/automation#record-the-automation-catalog`
- `litespec/ext/compat#record-breaking-behavior-changes-as-decisions`
- `litespec/ext/compat#reject-breaking-diffs-without-authorization`
- `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`
- `litespec/ext/delivery#apply-oci-release-labels`
- `litespec/ext/delivery#apply-schema-work-before-traffic`
- `litespec/ext/delivery#apply-the-documented-defaults`
- `litespec/ext/delivery#attach-dashboard-links-to-production-objectives`
- `litespec/ext/delivery#build-from-the-workspace-root`
- `litespec/ext/delivery#build-release-images-in-stages`
- `litespec/ext/delivery#copy-only-runtime-files`
- `litespec/ext/delivery#cover-the-baseline-alert-set`
- `litespec/ext/delivery#declare-writable-paths-and-limits`
- `litespec/ext/delivery#define-actionable-baseline-alerts`
- `litespec/ext/delivery#define-backup-and-restore-behavior`
- `litespec/ext/delivery#enforce-the-supply-chain-gate`
- `litespec/ext/delivery#exclude-build-material-from-runtime-images`
- `litespec/ext/delivery#exclude-build-timestamps-from-contracts`
- `litespec/ext/delivery#exclude-environment-configuration-from-images`
- `litespec/ext/delivery#exclude-insecure-frontend-secret-carriers`
- `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data`
- `litespec/ext/delivery#exercise-every-runbook-before-release`
- `litespec/ext/delivery#keep-a-canonical-job-graph`
- `litespec/ext/delivery#keep-a-first-release-readiness-record`
- `litespec/ext/delivery#keep-deployment-descriptors-together`
- `litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure`
- `litespec/ext/delivery#keep-mutable-data-external`
- `litespec/ext/delivery#keep-rollback-executable`
- `litespec/ext/delivery#keep-schema-work-out-of-startup`
- `litespec/ext/delivery#keep-scripts-at-the-root`
- `litespec/ext/delivery#limit-container-privileges`
- `litespec/ext/delivery#match-every-shipped-package-to-its-pin`
- `litespec/ext/delivery#meet-release-readiness-gates`
- `litespec/ext/delivery#place-dockerfiles-beside-deployables`
- `litespec/ext/delivery#promote-verified-artifacts`
- `litespec/ext/delivery#protect-the-default-branch`
- `litespec/ext/delivery#provide-release-data-recovery`
- `litespec/ext/delivery#provide-release-delivery-controls`
- `litespec/ext/delivery#provide-release-operating-visibility`
- `litespec/ext/delivery#publish-a-book-preview-for-every-change`
- `litespec/ext/delivery#record-each-drill-run-as-a-check-result`
- `litespec/ext/delivery#record-every-branch-protection-bypass`
- `litespec/ext/delivery#record-image-digests`
- `litespec/ext/delivery#record-operating-readiness`
- `litespec/ext/delivery#record-release-evidence`
- `litespec/ext/delivery#report-liveness-from-every-recurring-job`
- `litespec/ext/delivery#review-schema-artifacts`
- `litespec/ext/delivery#route-each-severity-to-a-declared-destination`
- `litespec/ext/delivery#run-images-as-non-root`
- `litespec/ext/delivery#run-the-application-as-pid-1`
- `litespec/ext/delivery#separate-frontend-configuration-classes`
- `litespec/ext/delivery#set-sufficient-shutdown-time`
- `litespec/ext/delivery#state-availability-objectives-as-openslo-files`
- `litespec/ext/delivery#supply-runtime-configuration-externally`
- `litespec/ext/delivery#test-released-traffic`
- `litespec/ext/delivery#treat-check-warnings-as-failures`
- `litespec/ext/delivery#use-a-repeatable-deployment`
- `litespec/ext/delivery#use-protected-frontend-secret-delivery`
- `litespec/ext/delivery#use-stable-service-names`
- `litespec/ext/delivery#wait-for-replica-readiness`
- `litespec/ext/delivery#write-runtime-logs-to-streams`
- `litespec/ext/jobs#apply-item-read-authorization`
- `litespec/ext/jobs#audit-export-reads`
- `litespec/ext/jobs#avoid-speculative-reporting-systems`
- `litespec/ext/jobs#bound-the-ingest-buffer`
- `litespec/ext/jobs#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/jobs#declare-export-paging`
- `litespec/ext/jobs#declare-ingest-steps-in-order`
- `litespec/ext/jobs#declare-the-engine-carrier`
- `litespec/ext/jobs#define-export-encoding-and-columns`
- `litespec/ext/jobs#define-report-limits`
- `litespec/ext/jobs#define-the-closed-drop-reason-set`
- `litespec/ext/jobs#discard-poison-records-after-three-attempts`
- `litespec/ext/jobs#escape-formula-leading-export-values`
- `litespec/ext/jobs#expose-drop-counts-per-reason`
- `litespec/ext/jobs#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/jobs#keep-raw-sql-in-infrastructure`
- `litespec/ext/jobs#keep-report-definitions-in-application`
- `litespec/ext/jobs#keep-report-providers-in-infrastructure`
- `litespec/ext/jobs#map-each-engine-step-to-one-use-case`
- `litespec/ext/jobs#name-engine-steps-after-use-cases`
- `litespec/ext/jobs#order-queue-items-by-score`
- `litespec/ext/jobs#order-unscored-queues-by-age`
- `litespec/ext/jobs#parameterize-report-sql`
- `litespec/ext/jobs#protect-export-output`
- `litespec/ext/jobs#read-enrichment-data-from-declared-sources`
- `litespec/ext/jobs#record-one-drop-reason-per-dropped-record`
- `litespec/ext/jobs#record-reporting-need`
- `litespec/ext/jobs#reject-unmapped-engine-steps`
- `litespec/ext/jobs#release-claims-past-their-lease`
- `litespec/ext/jobs#review-report-plans`
- `litespec/ext/jobs#run-budget-exceeding-exports-in-worker`
- `litespec/ext/jobs#scope-export-output-to-the-tenant`
- `litespec/ext/jobs#separate-worker-orchestration`
- `litespec/ext/jobs#show-a-preview-before-bulk-money-actions`
- `litespec/ext/jobs#show-the-score-beside-each-item`
- `litespec/ext/jobs#state-each-step-compensation`
- `litespec/ext/jobs#state-the-claim-mode`
- `litespec/ext/jobs#state-the-freshness-consequence`
- `litespec/ext/jobs#support-report-cancellation`
- `litespec/ext/outbox#authenticate-and-authorize-subscriptions`
- `litespec/ext/outbox#bound-realtime-resources`
- `litespec/ext/outbox#declare-realtime-freshness`
- `litespec/ext/outbox#place-realtime-code-at-outer-boundaries`
- `litespec/ext/outbox#reconnect-and-refresh-authoritative-state`
- `litespec/ext/outbox#record-realtime-need`
- `litespec/ext/outbox#record-transport-support`
- `litespec/ext/outbox#reject-cosmetic-realtime-adoption`
- `litespec/ext/outbox#reject-unbounded-slow-client-buffering`
- `litespec/ext/outbox#revalidate-long-lived-access`
- `litespec/ext/outbox#subscribe-through-narrow-functions`
- `litespec/ext/outbox#tolerate-notification-ordering-differences`
- `litespec/ext/outbox#use-server-sent-events-for-notifications`
- `litespec/ext/outbox#use-signalr-for-interactive-messaging`
- `litespec/frontend/web/components#keep-component-boundaries-typed`
- `litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`

Removed (439):

- `litespec/backend/api#state-an-unexpressible-transition-as-a-custom-method`
- `litespec/backend/api#use-put-for-a-whole-replacement`
- `litespec/backend/architecture#keep-one-application-part`
- `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`
- `litespec/backend/architecture#use-five-application-parts`
- `litespec/backend/dotnet/api#use-the-endpoint-folder-layout`
- `litespec/backend/dotnet/application#use-the-operation-folder-layout`
- `litespec/backend/dotnet/architecture#keep-one-application-assembly`
- `litespec/backend/dotnet/architecture#mirror-modules-in-folders-and-namespaces`
- `litespec/backend/dotnet/architecture#use-five-application-projects`
- `litespec/backend/dotnet/architecture#use-one-public-assembly-marker-per-scanned-project`
- `litespec/backend/dotnet/config#bind-setting-records-to-options`
- `litespec/backend/dotnet/config#validate-backend-options-at-startup`
- `litespec/backend/dotnet/domain#use-the-domain-module-layout`
- `litespec/backend/dotnet/ext/efcore#apply-migrations-as-release-work`
- `litespec/backend/dotnet/ext/efcore#avoid-per-aggregate-read-stores`
- `litespec/backend/dotnet/ext/efcore#commit-ef-core-command-work-once`
- `litespec/backend/dotnet/ext/efcore#configure-ef-core-mappings-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#configure-optimistic-tokens`
- `litespec/backend/dotnet/ext/efcore#configure-relational-details-explicitly`
- `litespec/backend/dotnet/ext/efcore#define-ef-core-query-roots`
- `litespec/backend/dotnet/ext/efcore#exclude-direct-handler-commits`
- `litespec/backend/dotnet/ext/efcore#generate-migrations-for-schema-changes`
- `litespec/backend/dotnet/ext/efcore#implement-query-roots-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#keep-migration-files-together`
- `litespec/backend/dotnet/ext/efcore#map-ef-core-concurrency-conflict`
- `litespec/backend/dotnet/ext/efcore#map-stable-relational-state-shape`
- `litespec/backend/dotnet/ext/efcore#map-unique-constraint-violations-to-their-refusals`
- `litespec/backend/dotnet/ext/efcore#map-unsupported-state-shapes-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#process-changed-aggregate-events`
- `litespec/backend/dotnet/ext/efcore#project-ef-core-queries-directly`
- `litespec/backend/dotnet/ext/efcore#record-ef-core-replacement-scope`
- `litespec/backend/dotnet/ext/efcore#record-provider-selection`
- `litespec/backend/dotnet/ext/efcore#register-one-command-post-handler`
- `litespec/backend/dotnet/ext/efcore#reject-automatic-command-retry`
- `litespec/backend/dotnet/ext/efcore#reject-cross-provider-outbox-storage`
- `litespec/backend/dotnet/ext/efcore#reject-mixed-provider-writes`
- `litespec/backend/dotnet/ext/efcore#reject-startup-schema-mutation`
- `litespec/backend/dotnet/ext/efcore#resolve-cross-provider-invariants`
- `litespec/backend/dotnet/ext/efcore#resolve-the-tenant-at-context-creation`
- `litespec/backend/dotnet/ext/efcore#retain-domain-state-values`
- `litespec/backend/dotnet/ext/efcore#retain-marten-query-boundary`
- `litespec/backend/dotnet/ext/efcore#retain-unaffected-baseline-persistence`
- `litespec/backend/dotnet/ext/efcore#review-migration-effects`
- `litespec/backend/dotnet/ext/efcore#review-new-persisted-states`
- `litespec/backend/dotnet/ext/efcore#stage-aggregate-writes-in-repositories`
- `litespec/backend/dotnet/ext/efcore#stage-ef-core-outbox-records-together`
- `litespec/backend/dotnet/ext/efcore#test-migration-starting-states`
- `litespec/backend/dotnet/ext/efcore#use-direct-state-mapping-only-when-supported`
- `litespec/backend/dotnet/ext/efcore#use-durable-outbox-delivery`
- `litespec/backend/dotnet/ext/efcore#use-one-write-provider`
- `litespec/backend/dotnet/ext/efcore#use-snake-case-relational-names`
- `litespec/backend/dotnet/ext/efcore#use-the-ef-core-infrastructure-layout`
- `litespec/backend/dotnet/naming#group-event-handlers-by-the-fact-they-handle`
- `litespec/backend/dotnet/naming#name-asynchronous-methods-completely`
- `litespec/backend/dotnet/naming#name-repository-variables-after-their-interface`
- `litespec/backend/dotnet/naming#name-result-parts-after-their-result`
- `litespec/backend/dotnet/packages#keep-application-dependencies-narrow`
- `litespec/backend/dotnet/packages#keep-domain-package-free`
- `litespec/backend/dotnet/packages#pin-every-dependency-centrally`
- `litespec/backend/dotnet/packages#run-litespecanalyzers-in-every-application-project`
- `litespec/backend/dotnet/packages#use-the-project-reference-graph`
- `litespec/backend/dotnet/packages#use-this-baseline-package-ownership`
- `litespec/backend/dotnet/persistence#use-this-infrastructure-layout`
- `litespec/backend/dotnet/testing#enforce-architecture-rules`
- `litespec/backend/dotnet/testing#gate-changed-lines-with-mutation-testing`
- `litespec/backend/dotnet/testing#generate-model-tests-from-declared-transitions`
- `litespec/backend/dotnet/testing#keep-the-test-projects-the-size-requires`
- `litespec/backend/dotnet/testing#prove-each-scenario-with-a-test-that-calls-its-use-case`
- `litespec/backend/dotnet/testing#seed-property-tests-from-example-tables`
- `litespec/backend/dotnet/testing#test-http-integration-through-the-real-host`
- `litespec/backend/dotnet/testing#use-one-production-faithful-integration-harness`
- `litespec/backend/dotnet/workspace#declare-size-and-stacks-in-project-config`
- `litespec/backend/dotnet/workspace#keep-net-production-and-test-projects-separate`
- `litespec/backend/identifiers#keep-aggregate-names-unique-across-the-project`
- `litespec/backend/identifiers#keep-use-case-names-unique-across-the-project`
- `litespec/backend/testing#assert-architecture-boundaries-with-tests`
- `litespec/core/access#keep-permission-and-entry-point-lists-to-entries`
- `litespec/core/access#name-the-actor-for-every-external-system`
- `litespec/core/access#route-cross-tenant-access-through-the-security-queue`
- `litespec/core/agent#mark-transcribed-records-during-adoption`
- `litespec/core/agent#raise-questions-in-ballots-with-recommendations`
- `litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets`
- `litespec/core/domain-model#close-every-saga-with-named-exits-and-a-driver`
- `litespec/core/domain-model#prove-every-saga-guard-and-exit-with-a-scenario`
- `litespec/core/domain-model#state-each-saga-step-with-its-compensation`
- `litespec/core/intent#group-product-records-by-area`
- `litespec/core/intent#group-questions-into-ballots`
- `litespec/core/intent#leave-business-choices-to-the-decider`
- `litespec/core/intent#name-the-person-who-approved-a-decision`
- `litespec/core/intent#replace-a-decision-with-a-new-record`
- `litespec/core/intent#shape-each-topic-page`
- `litespec/core/intent#state-each-feature-with-its-lifecycle`
- `litespec/core/language#name-each-change-code-with-a-past-participle`
- `litespec/core/layers#flag-records-when-a-parent-record-changes`
- `litespec/core/layers#raise-commercial-and-observable-figures-as-questions`
- `litespec/core/layers#raise-open-needs-as-draft-questions`
- `litespec/core/model#answer-each-question-on-its-record`
- `litespec/core/model#apply-guidance-precedence`
- `litespec/core/model#approve-tier-1-records-by-a-person`
- `litespec/core/model#declare-size-stacks-and-optional-types`
- `litespec/core/model#derive-generic-refusals`
- `litespec/core/model#end-transcription-at-the-named-decision`
- `litespec/core/model#flag-every-record-whose-parent-changed`
- `litespec/core/model#hold-approval-behind-draft-decisions`
- `litespec/core/model#hold-approval-behind-open-questions`
- `litespec/core/model#hold-each-finding-to-its-own-status`
- `litespec/core/model#hold-transcribed-records-in-draft`
- `litespec/core/model#keep-one-owner-and-status-on-every-record`
- `litespec/core/model#record-approval-as-data`
- `litespec/core/model#resolve-ties-by-the-later-decision`
- `litespec/core/model#retire-records-with-replaces`
- `litespec/core/model#use-the-layer-status-vocabulary`
- `litespec/core/principles#classify-record-authority`
- `litespec/core/principles#locate-record-status-in-frontmatter`
- `litespec/core/principles#raise-what-a-lower-layer-needs-as-a-question`
- `litespec/core/principles#refuse-approval-behind-a-draft-decision`
- `litespec/core/principles#render-consequences-as-context-outside-approval`
- `litespec/core/principles#retire-public-behavior-deliberately`
- `litespec/core/principles#separate-authority-from-implementation`
- `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-a-question`
- `litespec/core/privacy#review-every-erasure-exemption-before-its-date`
- `litespec/core/release#advise-without-gating-as-an-agent`
- `litespec/core/release#approve-drafts-in-bounded-batches`
- `litespec/core/release#compute-the-review-queue-from-the-model`
- `litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash`
- `litespec/core/release#hold-only-the-blocked-records-of-a-type-batch`
- `litespec/core/release#keep-a-first-release-readiness-record`
- `litespec/core/release#keep-unfinished-behavior-in-draft`
- `litespec/core/release#meet-release-readiness-gates`
- `litespec/core/release#name-the-security-reviewer-in-the-project-file`
- `litespec/core/release#provide-release-data-recovery`
- `litespec/core/release#provide-release-delivery-controls`
- `litespec/core/release#provide-release-operating-visibility`
- `litespec/core/release#record-operating-readiness`
- `litespec/core/release#record-release-evidence`
- `litespec/core/release#refuse-a-batch-holding-an-open-question`
- `litespec/core/release#refuse-a-batch-without-a-named-approver`
- `litespec/core/release#refuse-a-record-whose-effective-status-stays-draft`
- `litespec/core/release#refuse-self-review-alone-for-tier-1-behavior`
- `litespec/core/release#require-passing-scenarios-and-advisory-review-for-tier-1-behavior`
- `litespec/core/release#review-behavior-changes-through-the-behavior-diff`
- `litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots`
- `litespec/core/release#review-the-security-queue-separately-from-reviewed`
- `litespec/core/release#sample-tier-3-behavior-reviews`
- `litespec/core/release#treat-check-warnings-as-failures`
- `litespec/core/release#write-a-manifest-for-every-batch-approval`
- `litespec/core/scope#record-the-review-configuration`
- `litespec/core/scope#record-the-transcription-endpoint`
- `litespec/core/system#drive-work-from-approved-records`
- `litespec/core/use-cases#write-prose-on-significant-use-cases`
- `litespec/ext/automation#review-the-automation-catalog`
- `litespec/ext/compat#raise-breaking-behavior-changes-as-intent-questions`
- `litespec/ext/compat#reject-unapproved-breaking-diffs`
- `litespec/ext/containers#apply-oci-release-labels`
- `litespec/ext/containers#apply-schema-work-before-traffic`
- `litespec/ext/containers#build-from-the-workspace-root`
- `litespec/ext/containers#build-release-images-in-stages`
- `litespec/ext/containers#copy-only-runtime-files`
- `litespec/ext/containers#declare-writable-paths-and-limits`
- `litespec/ext/containers#exclude-build-material-from-runtime-images`
- `litespec/ext/containers#exclude-build-timestamps-from-contracts`
- `litespec/ext/containers#exclude-environment-configuration-from-images`
- `litespec/ext/containers#exclude-insecure-frontend-secret-carriers`
- `litespec/ext/containers#keep-mutable-data-external`
- `litespec/ext/containers#keep-schema-work-out-of-startup`
- `litespec/ext/containers#limit-container-privileges`
- `litespec/ext/containers#place-dockerfiles-beside-deployables`
- `litespec/ext/containers#record-image-digests`
- `litespec/ext/containers#retain-rollback-material`
- `litespec/ext/containers#run-images-as-non-root`
- `litespec/ext/containers#run-the-application-as-pid-1`
- `litespec/ext/containers#separate-frontend-configuration-classes`
- `litespec/ext/containers#set-sufficient-shutdown-time`
- `litespec/ext/containers#supply-runtime-configuration-externally`
- `litespec/ext/containers#test-released-traffic`
- `litespec/ext/containers#use-protected-frontend-secret-delivery`
- `litespec/ext/containers#wait-for-replica-readiness`
- `litespec/ext/containers#write-runtime-logs-to-streams`
- `litespec/ext/pipeline#bound-the-ingest-buffer`
- `litespec/ext/pipeline#declare-ingest-steps-in-order`
- `litespec/ext/pipeline#define-the-closed-drop-reason-set`
- `litespec/ext/pipeline#discard-poison-records-after-three-attempts`
- `litespec/ext/pipeline#expose-drop-counts-per-reason`
- `litespec/ext/pipeline#read-enrichment-data-from-declared-sources`
- `litespec/ext/pipeline#record-one-drop-reason-per-dropped-record`
- `litespec/ext/pipeline#state-the-freshness-consequence`
- `litespec/ext/plans#record-plan-prices-as-business-requirement-values`
- `litespec/ext/privacy#approve-the-erasure-sentence-in-intent`
- `litespec/ext/privacy#complete-the-subject-export-before-erasure`
- `litespec/ext/privacy#discard-rows-that-leave-no-trace`
- `litespec/ext/privacy#keep-a-tombstone-where-deletion-stays-visible`
- `litespec/ext/privacy#keep-tombstone-fields-minimal`
- `litespec/ext/privacy#move-data-to-its-next-controller-instead-of-erasing`
- `litespec/ext/privacy#purge-erased-data-from-restored-backups`
- `litespec/ext/privacy#record-which-duty-wins-each-conflict`
- `litespec/ext/privacy#remove-child-rows-through-their-parent-only`
- `litespec/ext/privacy#require-a-fresh-export-after-changes`
- `litespec/ext/privacy#run-one-sweep-job-per-data-class`
- `litespec/ext/privacy#scope-erasure-to-what-the-subject-requested`
- `litespec/ext/queues#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/queues#order-queue-items-by-score`
- `litespec/ext/queues#order-unscored-queues-by-age`
- `litespec/ext/queues#release-claims-past-their-lease`
- `litespec/ext/queues#show-a-preview-before-bulk-money-actions`
- `litespec/ext/queues#show-the-score-beside-each-item`
- `litespec/ext/queues#state-the-claim-mode`
- `litespec/ext/realtime#authenticate-and-authorize-subscriptions`
- `litespec/ext/realtime#bound-realtime-resources`
- `litespec/ext/realtime#declare-realtime-freshness`
- `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`
- `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`
- `litespec/ext/realtime#record-realtime-need`
- `litespec/ext/realtime#record-transport-support`
- `litespec/ext/realtime#reject-cosmetic-realtime-adoption`
- `litespec/ext/realtime#reject-unbounded-slow-client-buffering`
- `litespec/ext/realtime#revalidate-long-lived-access`
- `litespec/ext/realtime#subscribe-through-narrow-functions`
- `litespec/ext/realtime#tolerate-notification-ordering-differences`
- `litespec/ext/realtime#use-server-sent-events-for-notifications`
- `litespec/ext/realtime#use-signalr-for-interactive-messaging`
- `litespec/ext/report#apply-item-read-authorization`
- `litespec/ext/report#audit-export-reads`
- `litespec/ext/report#avoid-speculative-reporting-systems`
- `litespec/ext/report#declare-export-paging`
- `litespec/ext/report#define-export-encoding-and-columns`
- `litespec/ext/report#define-report-limits`
- `litespec/ext/report#escape-formula-leading-export-values`
- `litespec/ext/report#keep-raw-sql-in-infrastructure`
- `litespec/ext/report#keep-report-definitions-in-application`
- `litespec/ext/report#keep-report-providers-in-infrastructure`
- `litespec/ext/report#parameterize-report-sql`
- `litespec/ext/report#protect-export-output`
- `litespec/ext/report#record-reporting-need`
- `litespec/ext/report#review-report-plans`
- `litespec/ext/report#run-budget-exceeding-exports-in-worker`
- `litespec/ext/report#scope-export-output-to-the-tenant`
- `litespec/ext/report#separate-worker-orchestration`
- `litespec/ext/report#support-report-cancellation`
- `litespec/ext/site#avoid-client-scripts-without-a-named-interactive-need`
- `litespec/ext/site#declare-each-static-site-in-the-project-record`
- `litespec/ext/site#default-to-a-light-scheme-with-a-reader-chosen-dark-scheme`
- `litespec/ext/site#generate-a-sitemap-covering-every-page`
- `litespec/ext/site#generate-the-site-with-astro`
- `litespec/ext/site#give-every-page-one-main-heading-and-landmark-elements`
- `litespec/ext/site#keep-each-repeated-fact-in-one-data-source`
- `litespec/ext/site#list-every-page-for-language-models`
- `litespec/ext/site#load-every-script-from-the-site-origin`
- `litespec/ext/site#load-nothing-from-a-third-party-origin`
- `litespec/ext/site#mark-pages-with-json-ld-structured-data`
- `litespec/ext/site#mark-unfinished-facts-with-visible-placeholders`
- `litespec/ext/site#name-every-page-with-a-title-and-a-description`
- `litespec/ext/site#point-crawler-rules-at-the-sitemap`
- `litespec/ext/site#point-every-page-at-its-absolute-canonical-address`
- `litespec/ext/site#publish-a-markdown-copy-beside-every-page`
- `litespec/ext/site#publish-full-page-text-for-language-models`
- `litespec/ext/site#read-prices-and-claims-from-brief-and-business-requirements`
- `litespec/ext/site#read-site-prices-and-claims-from-consumer-records`
- `litespec/ext/site#reference-language-alternates-from-every-translated-page`
- `litespec/ext/site#reject-a-release-build-carrying-a-placeholder`
- `litespec/ext/site#render-components-on-the-server-with-astro-or-svelte`
- `litespec/ext/site#render-every-language-from-one-page-structure`
- `litespec/ext/site#render-every-page-to-static-files`
- `litespec/ext/site#state-allowed-sources-in-a-content-security-policy`
- `litespec/ext/site#use-tailwind-for-site-styles`
- `litespec/ext/stage#end-transcription-at-the-named-decision`
- `litespec/ext/stage#hold-transcribed-records-in-draft`
- `litespec/ext/stage#record-rested-on-draft-intent`
- `litespec/ext/stage#resolve-rested-on-draft-intent-at-promotion`
- `litespec/ext/tenancy#review-delegated-cross-tenant-grants-through-the-security-queue`
- `litespec/ext/workflow-engine#declare-the-engine-carrier`
- `litespec/ext/workflow-engine#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/workflow-engine#map-each-engine-step-to-one-use-case`
- `litespec/ext/workflow-engine#name-engine-steps-after-use-cases`
- `litespec/ext/workflow-engine#reject-unmapped-engine-steps`
- `litespec/ext/workflow-engine#state-each-step-compensation`
- `litespec/frontend/blazor/browser#bound-what-is-stored`
- `litespec/frontend/blazor/browser#keep-a-stable-serialization-contract`
- `litespec/frontend/blazor/browser#own-storage-in-infrastructure`
- `litespec/frontend/blazor/browser#provide-a-migration-for-every-changed-shape`
- `litespec/frontend/blazor/browser#provide-export-and-import`
- `litespec/frontend/blazor/browser#record-the-classification`
- `litespec/frontend/blazor/browser#select-the-storage-mechanism-per-shape`
- `litespec/frontend/blazor/browser#treat-reads-as-fallible`
- `litespec/frontend/blazor/browser#version-and-scope-every-storage-key`
- `litespec/frontend/blazor/components#apply-one-styling-system`
- `litespec/frontend/blazor/components#declare-every-parameter-type`
- `litespec/frontend/blazor/components#keep-accessibility-in-the-component`
- `litespec/frontend/blazor/components#keep-components-presentational`
- `litespec/frontend/blazor/components#name-for-the-boundary-role`
- `litespec/frontend/blazor/components#own-the-layer-contract`
- `litespec/frontend/blazor/components#place-one-component-per-file`
- `litespec/frontend/blazor/components#raise-events-rather-than-mutating`
- `litespec/frontend/blazor/components#resolve-browser-behavior-through-an-interface`
- `litespec/frontend/blazor/data#classify-state`
- `litespec/frontend/blazor/data#keep-derived-values-single-sourced`
- `litespec/frontend/blazor/data#keep-forms-typed`
- `litespec/frontend/blazor/data#load-persisted-state-once`
- `litespec/frontend/blazor/data#make-refusals-explicit`
- `litespec/frontend/blazor/data#prefer-explicit-refresh`
- `litespec/frontend/blazor/data#route-use-cases-through-application`
- `litespec/frontend/blazor/data#validate-at-the-boundary`
- `litespec/frontend/blazor/rendering#declare-routes-on-pages`
- `litespec/frontend/blazor/rendering#give-every-route-explicit-states`
- `litespec/frontend/blazor/rendering#keep-navigation-state-in-the-url`
- `litespec/frontend/blazor/rendering#keep-the-acquisition-surface-static`
- `litespec/frontend/blazor/rendering#name-routes-for-the-reader`
- `litespec/frontend/blazor/rendering#operate-offline-after-first-load`
- `litespec/frontend/blazor/rendering#render-on-the-client-only`
- `litespec/frontend/blazor/stack#keep-blazor-outside-the-ui-contract`
- `litespec/frontend/blazor/stack#publish-static-webassembly-output`
- `litespec/frontend/blazor/stack#record-a-first-load-budget`
- `litespec/frontend/blazor/stack#verify-with-the-net-toolchain`
- `litespec/frontend/blazor/structure#isolate-module-internals`
- `litespec/frontend/blazor/structure#keep-domain-free-of-browser-concerns`
- `litespec/frontend/blazor/structure#keep-imports-directional`
- `litespec/frontend/blazor/structure#keep-static-content-out-of-the-assembly`
- `litespec/frontend/blazor/structure#organize-features-by-module-and-use-case`
- `litespec/frontend/blazor/structure#place-interop-adapters-together`
- `litespec/frontend/blazor/structure#use-the-client-solution-tree`
- `litespec/frontend/blazor/testing#enforce-the-first-load-budget`
- `litespec/frontend/blazor/testing#keep-browser-capability-tests-honest`
- `litespec/frontend/blazor/testing#keep-end-to-end-tests-few`
- `litespec/frontend/blazor/testing#prove-each-scenario-with-a-test`
- `litespec/frontend/blazor/testing#replace-interop-in-component-tests`
- `litespec/frontend/blazor/testing#test-the-core-without-a-browser`
- `litespec/frontend/blazor/testing#use-the-four-client-test-projects`
- `litespec/frontend/blazor/testing#verify-real-browser-behavior-end-to-end`
- `litespec/frontend/mobile/android/stack#build-with-net-for-android`
- `litespec/frontend/mobile/android/stack#keep-android-outside-the-ui-contract`
- `litespec/frontend/mobile/android/stack#pin-every-android-binding-package`
- `litespec/frontend/mobile/android/stack#record-a-package-size-budget`
- `litespec/frontend/mobile/android/stack#record-the-minimum-android-version`
- `litespec/frontend/mobile/android/stack#store-streamed-media-uncompressed`
- `litespec/frontend/mobile/android/stack#verify-with-the-net-toolchain`
- `litespec/frontend/mobile/android/views#apply-insets-from-windowinsets`
- `litespec/frontend/mobile/android/views#avoid-platform-names-that-collide-with-the-app-namespace`
- `litespec/frontend/mobile/android/views#build-views-in-code-or-layout-resources-not-both`
- `litespec/frontend/mobile/android/views#deliver-app-links-to-the-controller`
- `litespec/frontend/mobile/android/views#host-screens-in-one-activity`
- `litespec/frontend/mobile/android/views#keep-text-colors-and-icons-in-resources`
- `litespec/frontend/mobile/android/views#persist-navigation-state-in-the-activity-bundle`
- `litespec/frontend/mobile/android/views#use-the-android-back-dispatcher`
- `litespec/frontend/mobile/data#classify-state`
- `litespec/frontend/mobile/data#route-use-cases-through-application`
- `litespec/frontend/mobile/data#update-the-store-from-command-results`
- `litespec/frontend/mobile/structure#keep-imports-directional`
- `litespec/frontend/testing#query-by-accessible-behavior`
- `litespec/frontend/web/components#compose-class-names-in-the-shared-package`
- `litespec/frontend/web/components#give-card-and-section-titles-heading-semantics`
- `litespec/frontend/web/components#keep-primitives-and-composites-in-one-shared-package`
- `litespec/frontend/web/components#meet-accessibility-requirements`
- `litespec/frontend/web/components#place-a-drawing-no-composite-makes-in-a-visual-file`
- `litespec/frontend/web/components#protect-rich-content-boundaries`
- `litespec/frontend/web/components#use-no-raw-element-in-page-code`
- `litespec/frontend/web/data#assign-state-to-the-narrowest-owner`
- `litespec/frontend/web/data#build-a-form-on-the-shared-form-library`
- `litespec/frontend/web/data#generate-the-client-from-the-published-contract`
- `litespec/frontend/web/data#keep-the-client-boundary-in-the-frame`
- `litespec/frontend/web/data#localize-copy-through-locale-dictionaries`
- `litespec/frontend/web/data#map-every-failure-to-one-problem-type`
- `litespec/frontend/web/data#place-a-refusal-beside-what-was-refused`
- `litespec/frontend/web/data#regenerate-the-client-rather-than-editing-it`
- `litespec/frontend/web/data#update-the-cache-a-command-changed`
- `litespec/frontend/web/data#validate-a-form-with-the-generated-schema`
- `litespec/frontend/web/patterns#implement-a-chosen-pattern-once`
- `litespec/frontend/web/patterns#keep-a-visual-registry`
- `litespec/frontend/web/react/components#keep-component-boundaries-typed`
- `litespec/frontend/web/react/components#write-components-as-functions`
- `litespec/frontend/web/react/stack#build-a-device-client-as-one-static-page`
- `litespec/frontend/web/react/stack#select-a-rendering-mode-by-audience`
- `litespec/frontend/web/rendering#build-the-server-query-cache-per-request`
- `litespec/frontend/web/rendering#keep-authenticated-caching-explicit`
- `litespec/frontend/web/rendering#keep-proxy-behavior-at-the-edge`
- `litespec/frontend/web/rendering#keep-search-parameters-validated-by-a-schema`
- `litespec/frontend/web/rendering#load-a-route-on-intent`
- `litespec/frontend/web/rendering#load-route-data-in-the-loader`
- `litespec/frontend/web/rendering#read-route-data-with-a-suspense-query`
- `litespec/frontend/web/rendering#split-one-chunk-per-route-and-per-language`
- `litespec/frontend/web/rendering#use-a-pathless-layout-route-for-a-shell`
- `litespec/frontend/web/structure#keep-a-route-file-thin`
- `litespec/frontend/web/structure#keep-imports-directional`
- `litespec/frontend/web/structure#keep-tests-near-their-ownership-boundary`
- `litespec/frontend/web/structure#use-explicit-public-entry-points-for-workspace-packages`
- `litespec/frontend/web/svelte/components#keep-component-boundaries-typed`
- `litespec/frontend/web/svelte/stack#build-a-device-client-as-one-static-page`
- `litespec/frontend/web/svelte/stack#select-a-rendering-mode-by-audience`
- `litespec/frontend/web/testing#assert-each-declared-refusal-by-its-code`
- `litespec/frontend/web/testing#gate-changed-lines-with-mutation-testing`
- `litespec/frontend/web/testing#keep-mocks-at-owned-boundaries`
- `litespec/frontend/web/testing#match-test-level-to-risk`
- `litespec/frontend/web/testing#prove-the-module-mirror-in-a-structure-test`
- `litespec/frontend/web/testing#query-by-accessible-behavior`
- `litespec/frontend/web/testing#run-browser-checks-through-the-playwright-cli`
- `litespec/frontend/web/testing#seed-property-tests-from-example-tables`
- `litespec/frontend/web/testing#use-network-mocks-for-component-tests`
- `litespec/frontend/web/ui#check-every-route-in-a-browser`
- `litespec/frontend/web/ui#compose-each-route-from-one-floorplan`
- `litespec/frontend/web/ui#derive-each-page-type-from-a-named-reference`
- `litespec/frontend/web/ui#hold-the-declared-base-color`
- `litespec/frontend/web/ui#keep-layout-inside-floorplans`
- `litespec/frontend/web/ui#keep-tokens-in-one-stylesheet`
- `litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package`
- `litespec/frontend/web/ui#prove-every-result-a-route-calls`
- `litespec/frontend/web/ui#prove-ui-behavior-and-appearance`
- `litespec/frontend/web/ui#refuse-token-violations-with-stylelint`
- `litespec/frontend/web/ui#select-a-tone-from-the-closed-set`
- `litespec/quality/ci#apply-the-documented-defaults`
- `litespec/quality/ci#approve-breaking-changes-by-the-highest-record-owner`
- `litespec/quality/ci#enforce-the-supply-chain-gate`
- `litespec/quality/ci#keep-a-canonical-job-graph`
- `litespec/quality/ci#map-record-areas-to-owners`
- `litespec/quality/ci#promote-verified-artifacts`
- `litespec/quality/ci#protect-the-default-branch`
- `litespec/quality/ci#publish-a-book-preview-for-every-change`
- `litespec/quality/ci#publish-the-intent-diff-on-every-pull-request`
- `litespec/quality/ci#record-every-branch-protection-bypass`
- `litespec/quality/ci#require-zero-surviving-mutants-on-changed-lines`
- `litespec/quality/ci#review-schema-artifacts`
- `litespec/quality/ci#show-intent-counts-in-the-job-summary`
- `litespec/quality/operations#attach-dashboard-links-to-production-objectives`
- `litespec/quality/operations#cover-the-baseline-alert-set`
- `litespec/quality/operations#define-actionable-baseline-alerts`
- `litespec/quality/operations#define-backup-and-restore-behavior`
- `litespec/quality/operations#exercise-a-restore-before-the-first-production-data`
- `litespec/quality/operations#exercise-every-runbook-before-release`
- `litespec/quality/operations#keep-deployment-descriptors-together`
- `litespec/quality/operations#keep-each-alert-threshold-to-its-requirements-measure`
- `litespec/quality/operations#keep-rollback-executable`
- `litespec/quality/operations#record-each-drill-run-as-a-check-result`
- `litespec/quality/operations#report-liveness-from-every-recurring-job`
- `litespec/quality/operations#route-each-severity-to-a-declared-destination`
- `litespec/quality/operations#state-availability-objectives-as-openslo-files`
- `litespec/quality/operations#use-a-repeatable-deployment`
- `litespec/quality/operations#use-stable-service-names`
- `litespec/quality/security#route-tier-1-access-and-data-changes-through-the-security-reviewer`
- `litespec/workspace/dependencies#accept-every-shipped-package-license-explicitly`
- `litespec/workspace/dependencies#match-every-shipped-package-to-its-pin`
- `litespec/workspace/structure#keep-scripts-at-the-root`

Renamed (2):

- `litespec/core/model#connect-every-use-case-to-intent` to `litespec/core/model#connect-every-behavior-record-to-intent`
- `litespec/core/system#use-this-consumer-documentation-layout` to `litespec/core/system#use-the-docs-layout-on-the-model-page`

Extensions added (2):

- `delivery`
- `layer-projects`

Extensions removed (8):

- `containers`
- `pipeline`
- `privacy`
- `queues`
- `realtime`
- `report`
- `site`
- `workflow-engine`

<!-- /generated:provision-diff -->

## v3.0.0

LiteSpec 3.0.0 is a top-down specification standard for one bounded-context business application, written by agents and judged by machines, so people review only what is theirs to decide. A consumer installs this release as the `LiteSpec.Cli` local .NET tool at the release version and records it in `reviewedLiteSpecVersion`. The release takes effect as the `v3.0.0` tag.

### Layers

- Four layers: Intent (why and what we promise; product people review it), Behavior (how the system behaves; developers review it), Implementation (code) and Proof (tests, checks, computed statuses and the book).
- Each layer is written from the one above. Nothing above Implementation is computed from code; code and tests point up. Facts from the world enter Intent as research or constraints, and what a lower layer needs travels up as a question with a decider.
- The translation rules Specify, Implement and Verify state what each boundary receives, must add, may not decide, and checks.
- Consumer docs sit in one folder per layer: `docs/intent`, `docs/behavior`, `docs/implementation`, `docs/proof` and `docs/governance`.

### Records

- Each record page is a YAML record file holding one mapping with its entries in `records`; an optional explanation file beside it holds Markdown under H2 sections.
- A module page is `module.yaml` in its module folder and an aggregate page is `aggregate.yaml` in its aggregate folder; the glossary is `docs/glossary.yaml`.
- Anything a check reads or a reviewer approves is a record field; the explanation file only helps a reader understand. Product, constraint, rule set, actor, role and external system hold `summary`; goal holds `whyItMatters`; problem holds `situation`; feature holds `offering`; decision holds `consequences`; topic holds `summary`, `position` and `refusals`; journey holds `betweenSteps`.
- Use cases hold `purpose`, `whatHappens` and `exposure`; reactions, jobs, notifications and setting sets hold `purpose` and `whatHappens`; screens hold `summary`; integration events hold `summary` and `delivery`; runbooks hold `impact`, `prerequisites`, `procedure`, `verification`, `recovery` and `escalation`; exceptions hold `exit` and `consequences`; research sources carry a `key`.
- Prose cites a record by its identifier, such as `[the refund rule](rule/refund.within-window)`; research cites a source as `[text][key]`.
- A Markdown page that holds no record is a document with an optional frontmatter block holding only its page style and common fields.
- Journeys are product sentences with step ids, `finishedWhen` and a per-step `failure`; use cases point up with `serves`.
- Requirements state exceptions with `except` and a `fallback`, and carry `risk` and `setBy`.
- Use cases are commands or queries with `access` (`grant` and `tenant`; `auth` sits on the entry point), `changes`, `reads`, `consistency`, `effects` (`runs`, `sends`, `emits`, `calls`), `idempotency`, `audit`, typed query output in `include` groups, freshness and paging. Generic refusals are derived.
- Record types include reaction, job, notification, setting and integration event. Aggregates have `entity`, `saga` and `store` kinds, and data classes with erasure positions. A retention schedule page holds retention rows.
- Reactions and jobs declare edges in `effects` with the same verb grammar as use cases; a reaction may omit `timing`, which reads as `async`, and a top-level verb beside `effects` is refused.
- An idempotency `key` templates `{field}` placeholders over the record's `input` fields, and `scope` is `tenant`, `account`, `login` or `global`; reaction and job placeholders name triggering event or run window fields and are not checked.
- Reaction and job `onFailure` is a structured policy: `mode` is `retry`, `skip-and-log` or `discard`; a `retry` policy holds `maxAttempts`, `initialDelay`, `maxDelay` and `backoff`; `then` is `dead-letter`, with `deadLetter`, `surfacesIn` and `runbook`, with ISO 8601 durations.
- Approval is data: `approvedBy`, `approvedOn` and a semantic `approvedHash`, written by `litespec model approve`.

### Review and checks

- Product people read journey chapters and ballots; developers review Behavior diffs; a named security reviewer sees tier 1 access and data changes. Tier 3 questions auto-approve after a veto window.
- Every check has a level: G (record graph, blocking), S (stack reader, blocking where qualified) and A (advisory).
- Every edge between use cases, reactions, jobs, notifications, integration events and external systems is declared and checked. A calls edge counts only through a port anchored to an external-system record.
- A refusal proof confirms from the asserted code, a client code `refusals.aliases` translates, or the HTTP status `refusals.statuses` maps to the code.
- An integration fact that drives a use case or rule confirms its scenario or rule claim from inside the host.
- An `effect-undeclared` finding names a one-hop helper as `via 'Type.Method'`.
- A term's `labels` sit outside the approval hash, so adding a language reopens no approval.
- A project declares its use-case verb vocabulary in `vocabulary`: `verbs`, `phrasal` verbs counted as one, and `retired` synonyms with their replacement, checked by `use-case-verb-unknown`.
- `effect-timing-invalid` (level S) reports a command whose `sends` or `calls` effect runs inline; consequences run from an event handler or a job after the commit.
- `event-tense-invalid` accepts a past tense followed by a prepositional complement and a mark method naming its state, and `term-clash` skips a hyphenated pair opening with a function word.
- `state-unreached` (level G) reports every state no transition enters. An aggregate writes its creation as a transition from `-` in its `transitions` field, so its first state is reached too.
- The approval hash covers every written record field except the identity, workflow and approval fields the guide lists, and compares numbers by value, so `50.00` and `50` hash alike. For research, decision and topic pages it also covers the explanation file.
- `prose-missing` reports a significant use case with no `purpose` field and a significant command with no `whatHappens` field.
- `explanation-orphaned`, `explanation-missing`, `format-drift`, `source-link-bare`, `source-key-unknown` and `source-unused` (level A) report an orphaned explanation, a missing article explanation, a record outside canonical form, and a research source linked directly, cited by an unknown key, or never cited.
- `record-invalid` reports a record file that does not parse, with the parser message and line.
- `reader-missing` (level S) also reports a .NET file that cannot be read or parsed, a declared stack root that resolves to no folder, a `server` project with no backend reader, and a configured bill of materials that cannot be read, is not SPDX or CycloneDX, or yields no packages.
- A test proves a scenario across machines: the proof ledger hashes each test file by its path relative to the project root and its text with normalized line endings.

### Application Layer

- A command handler loads or creates one aggregate, calls one domain method, stages it and returns; validation, authorization, idempotency, audit, the timestamp, holds, the commit and fault classification are pipeline stages, and a change to another aggregate is a consequence. A record declaring `consistency: atomic` is the one exception.
- Each command implements one use case with one success path; a find-or-create flow is two use cases the owning boundary chooses between, and a lost race ends with the use case's conflict refusal at commit.
- Uniqueness is enforced by the store: a unique index, an identity derived from the owner, aggregate state for a once-only rule, or a key the aggregate declares and persistence claims at commit. Each constraint maps by name to its conflict refusal, and no handler reserves or checks a key.
- A refusal code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in the project file `refusals.composed`, and `refusal-unimplemented` reads the code as raised where its key is a literal in the code.
- Every aggregate carries a version the store compares at commit.
- A message whose target must be the caller carries no target or declares a self grant the guard enforces.
- Audit targets and reasons the message carries are declared beside it; the handler adds only values it learns while running.
- Messages, provider calls and changes to other aggregates run in event handlers after the commit; the outbox delivers each event to each handler separately.
- Query handlers take no repository and never write. Work over many records is a job issuing one command per record, each in its own unit of work.
- Result parts are named `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`, event handlers are `{Action}On{Fact}Handler` in an `On{Fact}` folder of the subscriber's aggregate, and repository variables are named after their interface.

### Mobile

- An `android` stack builds a native Android app with .NET for Android and plain Android views: no MAUI, no WebView screens. It sits at `docs/frontend/mobile/android/` under shared mobile pages for structure, screens and navigation, data and state, device capabilities, and testing, so a later mobile stack reuses them.
- The mobile solution lives at `apps/mobile/` with platform-free core projects beside one app project per platform; a `client-only` app holds Domain, Application and Infrastructure, a `server` app holds the generated API client.
- The `android-client` preset starts a client-only Android consumer, and the manifest pins the Android API level in `stack.android`.

### Tools

- `litespec model diff` has `--intent` and `--behavior` views with a security queue, and `litespec book` renders journey chapters and a ballots page.
- `litespec new` scaffolds a record file with its explanation file for an article type, `litespec rename` rewrites an id in record files, explanation files and documents, and `litespec model approve` records approval in the record file.
- `litespec new result` and `litespec new scenario` append one entry to a use-case record file in house order: successes, refusals, then scenarios.
- `litespec model format` rewrites every record file and the glossary file into canonical form; `--check` writes nothing, lists each file that would change, and exits 1 when any would. It exits 2 when a file does not parse.
- `litespec append` adds an entry to a record file; `litespec init` writes `docs/glossary.yaml`.
- Consumer templates ship as `<type>.yaml` record files, with `<type>.md` explanation files for article types and `<type>.md` documents for document styles.
- `litespec model check --staged` reports only staged records, their direct links, and the records staged code implements.
- `litespec model approve --dry-run` prints which records would be stamped and which would be reopened, without writing.
- The `litespec-maintain` tool checks the standard's own pages, schemas, manifest, and provision index from `apps/maintain/` with `validate` and `provisions`; the `litespec` tool serves consumers only.
- Extensions added: privacy, devices, automation, workflow-engine, pipeline, queues and ai. LS2002 is retired.
- `litespec model check` prints each finding once, the blocking ones first, then a count per finding code and level, and ends with the number of findings, blocking findings and known findings. `--level` limits the findings and counts to the named levels, such as `--level G,S`.
- `litespec model check --since` and `--staged` leave out files under `.litespec/model/`, which the tool writes itself.
- `litespec model approve --all --dry-run` lists exactly the records `litespec model approve --all` stamps.
- Every command reports an unexpected failure as one line and exit code 1; with `--format json` the line is a JSON object with an `error` field. A repeated option, an empty value, and an unknown option given with `--help` are usage errors.
- The book links only `http`, `https`, `mailto`, in-page fragments, relative paths, record identifiers and the configured editor scheme, and renders any other link target as text. `litespec book` exits with code 2 when the carried release is missing.
- A project that opts out of the analyzers with `LiteSpecLayer=None` gets no diagnostics and no generated code from the analyzer package.
- The navigation and page-frame patterns draw their wireframes on the Claimwick cast. Their option ids follow the option names: `cost-centre-sections-as-tabs-on-the-cost-centre-page`, `no-switcher-back-to-the-cost-centre-list`, `the-count-the-cost-centres-the-exceptions`, `per-cost-centre-cards-with-a-rate`, `payouts-as-a-timeline`, `split-payouts-left-incidents-right`, `month-grid-with-trips-marked`, `week-lanes-per-cost-centre` and `receipt-scanner-is-offline`.
- Provisions cover file uploads and the `429` status on the API, and offline commands, saved navigation state, app links and touch and text accessibility on Android. The shared frontend data rules live on the frontend data page for every stack, and the `jobs` extension runs scheduled jobs in a Worker. FsCheck is pinned for property-based Domain tests.
- Record files use a strict YAML subset in canonical key order with two-space indentation; `litespec model format` owns the layout.
- The YAML block at the top of a document is called frontmatter throughout the standard.

### Record Hierarchy

- Every record has exactly one parent: by identifier containment, by one required written up-link, or, for a type with no record above it, its layer list under the product. A record whose required parent is missing is an error at level G, reported even when the record is a draft. Every inverse link is computed, never written.
- A feature names at least one problem in `solves`; its parent is the first entry. Every non-refused feature names at least one goal in `advances`. A refused feature names `refusedBecause` and the problem it declines in `solves`, and carries no `advances`.
- Every journey names its goal record in a required single link `serves`, which is its parent. A use case, reaction, job or screen serves journey steps; a journey serves one goal, and a use case never serves a goal directly.
- Every requirement names its parent: a carrying feature, a constraint or research in `basedOn`, or a decision naming it in `decides`, in that precedence.
- Written result and scenario identifiers use the `result/<use-case>.<name>` and `scenario/<use-case>.<name>` form, and derived generic results take the same form. A written result no scenario shows is reported: level G for a success result, level A for a refusal, on large projects.
- Structural parents are level G for all records; other required links are level A for tier 3 records and level G for tier 1 records.
- Field names follow one grammar: a classifier is `kind`; a link is a present-tense verb, plural when it holds a list; a deadline is `<verb>By`, a recorded date `<verb>On`; a reason is `reason` or `<verb>Because`. Feature `lifecycle`, goal and requirement `dueBy`, feature `plannedFor`, decision `approvedOn`, and constraint `authority` and `kind` follow it, checked by `litespec-maintain validate`. Glossary terms carry `layer` and `aliases`.
- Use cases list the settings they read in `reads`; a setting's readers are computed, never written. The Links table carries a "reads as" phrase in both directions for every link, and the book takes its connection labels from it. A follows edge marks export beside implements in the built model.
- Research states its `conclusion`, and LiteSpec keeps its own research and decision records under `docs/intent/`.
- `litespec book` is one offline shell with sharded data, generated from the model, with the outline from the parent table and the labels from the Links table and the glossary.
- Approval batches a journey with the goal it serves, keeps each entry a page stamp did not hold at the page's former status, refuses a decision naming no chosen option, and keeps a transcribed record serving a draft journey in draft. Screens are Behavior, and reaction and job scenarios carry their own stamp.
