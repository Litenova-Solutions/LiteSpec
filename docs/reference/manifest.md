# Release Manifest

`litespec.manifest.yaml` is the inventory of one LiteSpec release (one published version of the standard): the technology stacks, project shapes, extensions, and pinned versions it ships. A LiteSpec maintainer edits it when the release gains a stack or an extension. A consumer pins the whole release and records the version it reviewed. This page lists every field with the current release real values and what each field drives.

## Reference

A consumer selects from this manifest. The [supported scope](../core/scope.md) page shows how a selection is made, and the [stacks](../core/stacks.md) page shows what a selection applies. The [project file](project-file.md) page lists what a consumer records, the [init](../tools/init.md) page shows how a new project starts from a preset, the [model](../tools/model.md) page shows how a selection is validated, and the [validate](../maintain/validate.md) page shows how the manifest itself is checked.

### File Location and Schema

| Item | Value |
|:---|:---|
| Path | `litespec.manifest.yaml` at the repository root |
| Schema | `schemas/litespec-manifest.schema.yaml` |
| Current version | `7.0.0` |
| Schema version | `6` |

The current release is `7.0.0`. Each release is a complete contract that states every active provision, so a consumer pins one release and adopts a later one by accepting that release as a whole.

### Top-Level Fields

| Field | Type | Required | Default |
|:---|:---|:---|:---|
| `$schema` | string | no | none |
| `schemaVersion` | constant `6` | yes | none |
| `version` | string | yes | none |
| `name` | string | yes | none |
| `agentsEntry` | string | yes | none |
| `stacks` | object | yes | none |
| `shapes` | object | yes | none |
| `extensions` | object | yes | none |
| `stack` | object | no | none |
| `packages` | object | no | none |
| `uiBaseline` | object | yes | none |

What each field means and what reads it. The list below covers every top-level field in table order.

- `$schema` holds the editor hint that points at the manifest schema. Only the editor reads it.
- `schemaVersion` is the constant `6`. It names the exact manifest shape this release uses, so a consumer adopting a later release absorbs every difference at that moment.
- `version` is the release version, currently `"7.0.0"`. `litespec init` stamps it into a new `.litespec/project.yaml` as `reviewedLiteSpecVersion`, and `litespec model check` compares the two and fails the run when they differ.
- `name` is the distribution name, currently `"litespec"`.
- `agentsEntry` points at the agent entry page, currently `"AGENTS.md"`. The authoring validator checks that the path exists.
- `stacks` lists the stacks the release ships, by stack id. See the stacks table below.
- `shapes` lists the project shapes the release ships, by shape id. See the shapes table below.
- `extensions` lists the extensions the release ships, by extension id. See the extensions table below.
- `stack` pins the toolchain versions. See the pins section below.
- `packages` pins the package versions. See the pins section below.
- `uiBaseline` pins the controlled Svelte baseline that `litespec model check` checks the shared package against. See the baseline section below.

### Stacks

| Stack | Folder | Kind |
|:---|:---|:---|
| `dotnet` | `docs/packs/dotnet` | backend |
| `svelte` | `docs/packs/svelte` | frontend |
| `android` | `docs/packs/android` | frontend |

Each entry holds `folder` with the stack kind, `backend` or `frontend`. The folder is the stack folder, and it holds a `stack.md` page: the [.NET stack](../packs/dotnet/stack.md), the [Svelte stack](../packs/svelte/stack.md), and the [Android stack](../packs/android/stack.md).

What the field drives:

- Selection: the folder chain decides which pages apply when a consumer names the stack. A selection applies the stack folder, every folder above it up to its area root, and their subfolders holding no stack. `litespec/core/stacks#apply-the-complete-stack` covers the whole selection.
- Validation: `litespec model check` refuses a stack id the manifest does not ship. The authoring validator reports a stack folder that does not exist and a stack folder that holds no `stack.md`.

### Shapes

| Shape | Backend stacks | Backend pages |
|:---|:---|:---|
| `server` | `1` | none |
| `client-only` | `0` | `docs/core/backend-architecture.md`, `docs/core/backend-domain.md`, `docs/core/backend-application.md` |

Each entry holds `backendStacks` with the optional `backendPages` list. The `server` shape names exactly one backend stack. The `client-only` shape carries no backend stack and still applies the three backend pages in the table, since a client-only product keeps a domain model.

What the field drives:

- Selection: the shape decides how many backend stacks the `stacks` list holds and which backend pages apply without one.
- Init: each preset selects the shape. `dotnet-svelte` selects `server`, and `android-client` selects `client-only`.
- Validation: `litespec model check` refuses a `client-only` project that names a backend stack and a `server` project whose backend count differs from one.

### Extensions

| Extension | Path | Scope | Switches on |
|:---|:---|:---|:---|
| `delivery` | `docs/ext/delivery.md` | `project` | `setting`, `runbook` |
| `ai-mcp` | `docs/ext/ai-mcp.md` | `project` | none |
| `compat` | `docs/ext/compat.md` | `project` | none |
| `realtime` | `docs/ext/realtime.md` | `local` | none |
| `plans` | `docs/ext/plans.md` | `project` | none |

Each entry holds `path` with the optional `activationScope`, `applicableKinds`, and `switchesOn`. The path is the extension page under `docs/ext/`, and the file name is the extension id. The scope is `project` for a selection covering the whole project, or `local` for one covering the named records. The kinds name the record types a local extension applies to, such as `use-case` and `flow` for `realtime`. `switchesOn` names the record types a selection turns on, such as `setting` and `runbook` for `delivery`. The [extensions index](../ext/README.md) lists each extension with its activation criterion.

What the field drives:

- Selection: naming an extension id in `selectedExtensions` applies its page. `litespec/core/scope#select-conditional-extensions-explicitly` covers selecting every extension with met criteria and none with unmet criteria.
- Validation: `litespec model check` refuses an id the manifest does not ship. The authoring validator checks each path, scope, and kind list.

### Version Pins

The `stack` object pins the toolchain this release builds with, and the `packages` object pins the package versions in two maps, `nuget` and `npm`. Each pin is one row of the table below, with its kind: `toolchain` for a `stack` pin, or `nuget` and `npm` for a package. The table is generated from the manifest, so it lists every pin the release ships.

<!-- generated:manifest-pins -->
| Kind | Name | Version |
|:---|:---|:---|
| toolchain | `dotnet` | `10` |
| toolchain | `android` | `36` |
| toolchain | `postgresql` | `18` |
| toolchain | `node` | `24.22.0` |
| toolchain | `pnpm` | `11.28.5` |
| nuget | `Aspire.Hosting.AppHost` | `13.x` |
| nuget | `Aspire.Hosting.JavaScript` | `13.x` |
| nuget | `Aspire.Hosting.PostgreSQL` | `13.x` |
| nuget | `AwesomeAssertions` | `9.x` |
| nuget | `dotnet-stryker` | `5.x` |
| nuget | `EFCore.NamingConventions` | `10.x` |
| nuget | `FsCheck` | `3.x` |
| nuget | `LightBDD.XUnit3` | `3.x` |
| nuget | `LiteBus.Analyzers` | `8.0.0-preview.8` |
| nuget | `LiteBus.Auditing` | `8.0.0-preview.8` |
| nuget | `LiteBus.Auditing.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Auditing.Storage.Marten` | `8.0.0-preview.8` |
| nuget | `LiteBus.Commands` | `8.0.0-preview.8` |
| nuget | `LiteBus.Commands.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Commands.Recurring` | `8.0.0-preview.8` |
| nuget | `LiteBus.Events` | `8.0.0-preview.8` |
| nuget | `LiteBus.Events.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Extensions.AspNetCore` | `8.0.0-preview.8` |
| nuget | `LiteBus.Extensions.Microsoft.DependencyInjection` | `8.0.0-preview.8` |
| nuget | `LiteBus.Messaging` | `8.0.0-preview.8` |
| nuget | `LiteBus.Messaging.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Dispatch.Amqp` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Dispatch.InProcess` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Dispatch.Kafka` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Extensions.OpenTelemetry` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Storage.EntityFrameworkCore` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Storage.Marten` | `8.0.0-preview.8` |
| nuget | `LiteBus.Outbox.Storage.PostgreSql` | `8.0.0-preview.8` |
| nuget | `LiteBus.Queries` | `8.0.0-preview.8` |
| nuget | `LiteBus.Queries.Abstractions` | `8.0.0-preview.8` |
| nuget | `LiteBus.Storage.PostgreSql` | `8.0.0-preview.8` |
| nuget | `LiteBus.Testing.DurableMessaging` | `8.0.0-preview.8` |
| nuget | `LiteBus.Testing.Mediation` | `8.0.0-preview.8` |
| nuget | `LiteSpec.Analyzers` | `7.0.0` |
| nuget | `Marten` | `9.x` |
| nuget | `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.x` |
| nuget | `Microsoft.AspNetCore.DataProtection` | `10.x` |
| nuget | `Microsoft.AspNetCore.Mvc.Testing` | `10.x` |
| nuget | `Microsoft.AspNetCore.OpenApi` | `10.x` |
| nuget | `Microsoft.EntityFrameworkCore` | `10.x` |
| nuget | `Microsoft.Extensions.ApiDescription.Server` | `10.x` |
| nuget | `Microsoft.Extensions.Configuration.Abstractions` | `10.x` |
| nuget | `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.x` |
| nuget | `Microsoft.Extensions.Diagnostics.HealthChecks` | `10.x` |
| nuget | `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` | `10.x` |
| nuget | `Microsoft.Extensions.Hosting.Abstractions` | `10.x` |
| nuget | `Microsoft.Extensions.Http` | `10.x` |
| nuget | `Microsoft.Extensions.Http.Resilience` | `10.x` |
| nuget | `Microsoft.Extensions.Logging.Abstractions` | `10.x` |
| nuget | `Microsoft.Extensions.ServiceDiscovery` | `10.x` |
| nuget | `Microsoft.NET.Test.Sdk` | `18.x` |
| nuget | `Microsoft.OpenApi` | `2.x` |
| nuget | `Microsoft.Testing.Extensions.CodeCoverage` | `18.x` |
| nuget | `Microsoft.Testing.Extensions.TrxReport` | `2.x` |
| nuget | `NSubstitute` | `6.x` |
| nuget | `Npgsql` | `10.x` |
| nuget | `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.x` |
| nuget | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.x` |
| nuget | `OpenTelemetry.Extensions.Hosting` | `1.x` |
| nuget | `OpenTelemetry.Instrumentation.AspNetCore` | `1.x` |
| nuget | `OpenTelemetry.Instrumentation.Http` | `1.x` |
| nuget | `OpenTelemetry.Instrumentation.Runtime` | `1.x` |
| nuget | `Scalar.AspNetCore` | `2.x` |
| nuget | `Testcontainers.PostgreSql` | `4.x` |
| nuget | `WireMock.Net` | `2.x` |
| nuget | `YamlDotNet` | `18.x` |
| nuget | `xunit.runner.visualstudio` | `4.x` |
| nuget | `xunit.v3` | `4.x` |
| nuget | `xunit.v3.mtp-v2` | `3.x` |
| npm | `@astrojs/svelte` | `9.x` |
| npm | `@axe-core/playwright` | `4.x` |
| npm | `@eslint/js` | `10.x` |
| npm | `@fontsource-variable/geist` | `5.x` |
| npm | `@fontsource-variable/inter` | `5.x` |
| npm | `@fontsource-variable/source-serif-4` | `5.x` |
| npm | `@hey-api/openapi-ts` | `0.x` |
| npm | `@lucide/svelte` | `1.x` |
| npm | `@modelcontextprotocol/server` | `2.x` |
| npm | `@playwright/test` | `1.x` |
| npm | `@stryker-mutator/core` | `10.x` |
| npm | `@stryker-mutator/vitest-runner` | `10.x` |
| npm | `@sveltejs/adapter-node` | `6.x` |
| npm | `@sveltejs/adapter-static` | `4.x` |
| npm | `@sveltejs/kit` | `3.x` |
| npm | `@sveltejs/vite-plugin-svelte` | `7.x` |
| npm | `@tailwindcss/cli` | `4.x` |
| npm | `@tailwindcss/vite` | `4.x` |
| npm | `@tanstack/eslint-plugin-query` | `5.x` |
| npm | `@tanstack/eslint-plugin-router` | `1.x` |
| npm | `@tanstack/router-cli` | `1.x` |
| npm | `@tanstack/router-plugin` | `1.x` |
| npm | `@tanstack/svelte-form` | `1.x` |
| npm | `@tanstack/svelte-query` | `6.x` |
| npm | `@tanstack/svelte-query-devtools` | `6.x` |
| npm | `@tanstack/svelte-table` | `9.x` |
| npm | `@testing-library/svelte` | `5.x` |
| npm | `@types/node` | `26.x` |
| npm | `bits-ui` | `2.x` |
| npm | `class-variance-authority` | `0.x` |
| npm | `clsx` | `2.x` |
| npm | `date-fns` | `4.x` |
| npm | `eslint` | `10.x` |
| npm | `eslint-plugin-svelte` | `3.x` |
| npm | `globals` | `17.x` |
| npm | `jsdom` | `30.x` |
| npm | `jsqr` | `1.x` |
| npm | `msw` | `3.x` |
| npm | `shadcn-svelte` | `1.7.0` |
| npm | `svelte` | `5.x` |
| npm | `svelte-check` | `4.x` |
| npm | `svelte-i18n` | `4.x` |
| npm | `tailwind-merge` | `3.x` |
| npm | `tailwindcss` | `4.x` |
| npm | `tw-animate-css` | `1.x` |
| npm | `typescript` | `6.x` |
| npm | `typescript-eslint` | `8.x` |
| npm | `vite` | `8.x` |
| npm | `vitest` | `5.x` |
| npm | `yaml` | `2.x` |
| npm | `zod` | `4.x` |
<!-- /generated:manifest-pins -->

What the pins drive:

- Resolution: every SDK, framework, and package version resolves from the manifest. `litespec/core/principles#locate-package-versions-in-the-manifest` points to the pins over prose, examples, and agent memory.
- UI validation: `litespec model check` reads the pinned npm packages and the baselines below when it checks the shared package and the stylesheets.
- Floor: the outbox, auditing and problem details provisions need the LiteBus packages pinned at `8.0.0-preview.8`.

### UI Baselines

The `uiBaseline` object pins the controlled Svelte baseline. The tables below list the real values of the current release.

| Item | Value |
|:---|:---|
| `platform` | `svelte` |
| `system` | `shadcn-svelte` |
| `styling` | `tailwindcss-v4` |
| `base` | `bits-ui` |
| `style` | `nova` |
| `cssVariables` | `true` |
| `baseColor` | `neutral` |
| `theme` | `neutral` |
| `font` | `geist` |
| `fontHeading` | `inherit` |
| `icons` | `lucide` |
| `radius` | `default` |
| `menuAccent` | `subtle` |
| `menuColor` | `default` |
| `componentsStyle` | `base-nova` |
| `sourceModel` | `shared-ui-package` |
| `nativeBoundary` | `separate-platform-profile` |

The `registry` block names the component source. The table below lists its three fields.

| Item | Value |
|:---|:---|
| `name` | `shadcn-svelte` |
| `url` | `https://shadcn-svelte.com/registry` |
| `allowAdditional` | `true` |

`litespec model check` checks the registry configuration, the source lock, the design contract, the token stylesheet, and each frontend boundary against the baseline. `litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives` covers the one shared package, and `litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream` covers the pin.

## Notes

The field lists follow `schemas/litespec-manifest.schema.yaml` and the values follow `litespec.manifest.yaml` at version `7.0.0`. The selection behavior follows `apps/model/src/LiteSpec.Standards/Pages/StackSelection.cs`, and the manifest checks follow `apps/maintain/src/LiteSpec.Maintain/Validate/Repository/ManifestValidator.cs`.

The consumer checks follow `apps/cli/src/LiteSpec.Cli/Model/Consumer/ProjectFileChecks.cs`, and the baseline checks follow `apps/cli/src/LiteSpec.Cli/Model/Ui/UiValidator.cs`. All five extensions live under `docs/ext/`, one page per extension id.
