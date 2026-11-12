# Release Manifest

## Intent

`litespec.manifest.yaml` is the inventory of one LiteSpec release (one published version of the standard): the technology stacks, project shapes, extensions, and pinned versions it ships. A LiteSpec maintainer edits it when the release gains a stack or an extension, while a consumer (an application repository built against one pinned release) never edits it and instead pins the whole release and records the version it reviewed. This page lists every field with the current release's real values and states what each field drives.

## Reference

A consumer selects from this manifest: the [supported scope](../core/scope.md) page states how a selection is made, and the [stacks](../core/stacks.md) page states what a selection applies. The [project file](project-file.md) page states what a consumer records, the [init](../tools/init.md) page states how a new project starts from a preset, the [model](../tools/model.md) page states how a selection is validated, and the [validate](../maintain/validate.md) page states how the manifest itself is checked.

### File Location and Schema

| Item | Value |
|:---|:---|
| Path | `litespec.manifest.yaml` at the repository root |
| Schema | `schemas/litespec-manifest.schema.yaml` |
| Current version | `5.1.0` |
| Schema version | `6` |

The current release is `5.1.0`. Each release is a complete contract that states every active provision, so a consumer pins one release and adopts a later one by accepting that release as a whole.

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
| `uiBaselineSvelte` | object | no | none |

What each field means and what reads it:

- `$schema` holds the editor hint that points at the manifest schema. Only the editor reads it.
- `schemaVersion` is the constant `6`. It names the exact manifest shape this release uses, so a consumer adopting a later release absorbs every difference at that moment.
- `version` is the release version, currently `"5.1.0"`. `litespec init` stamps it into a new `.litespec/project.yaml` as `reviewedLiteSpecVersion`, and `litespec model check` compares the two and fails the run when they differ.
- `name` is the distribution name, currently `"litespec"`.
- `agentsEntry` points at the agent entry page, currently `"AGENTS.md"`. The authoring validator checks that the path exists.
- `stacks` lists the stacks the release ships, by stack id. See the stacks table below.
- `shapes` lists the project shapes the release ships, by shape id. See the shapes table below.
- `extensions` lists the extensions the release ships, by extension id. See the extensions table below.
- `stack` pins the toolchain versions. See the pins section below.
- `packages` pins the package versions. See the pins section below.
- `uiBaseline` pins the controlled React baseline that `litespec model check` checks the shared package against. See the baseline section below.
- `uiBaselineSvelte` pins the controlled Svelte baseline. It has the same shape as `uiBaseline`, with the Svelte system names.

### Stacks

| Stack | Folder | Requires | Pairs with |
|:---|:---|:---|:---|
| `dotnet` | `docs/backend/dotnet` | none | any backend |
| `react` | `docs/frontend/web/react` | none | any backend |
| `svelte` | `docs/frontend/web/svelte` | none | any backend |
| `blazor` | `docs/frontend/blazor` | none | any backend |
| `android` | `docs/frontend/mobile/android` | none | any backend |

Each entry holds `folder`, which is required, and the optional `requires` and `pairsWith` lists. The folder is the stack folder under `docs/backend/` or `docs/frontend/`, and it holds a `stack.md` page: the [.NET stack](../backend/dotnet/stack.md), the [React stack](../frontend/web/react/stack.md), the [Svelte stack](../frontend/web/svelte/stack.md), the [Blazor stack](../frontend/blazor/stack.md), and the [Android stack](../frontend/mobile/android/stack.md). `requires` names the stacks a project also selects with this one, and `pairsWith` names the only backend stacks a frontend stack works beside; absent `pairsWith` means any backend. This release ships no `requires` and no `pairsWith` values.

What the field drives:

- Selection: the folder chain decides which pages apply when a consumer names the stack. A selection applies the stack folder, every folder above it up to its area root, and their subfolders that hold no stack. `litespec/core/stacks#apply-the-complete-stack` requires the whole selection.
- Validation: `litespec model check` refuses a stack id the manifest does not ship. The authoring validator reports a stack folder that sits outside `docs/backend/` or `docs/frontend/`, a folder that does not exist, and a folder that holds no `stack.md`.

### Shapes

| Shape | Backend stacks | Backend pages |
|:---|:---|:---|
| `server` | `1` | none |
| `client-only` | `0` | `docs/backend/architecture.md`, `docs/backend/domain.md`, `docs/backend/application.md` |

Each entry holds `backendStacks`, which is required, and the optional `backendPages` list. The `server` shape names exactly one backend stack. The `client-only` shape names no backend stack and still applies the four backend pages in the table, because a client-only product keeps a domain model. The authoring validator requires each backend page to sit directly in `docs/backend/`.

What the field drives:

- Selection: the shape decides how many backend stacks the `stacks` list holds and which backend pages apply without one.
- Init: each preset selects the shape. `dotnet-react` and `dotnet-svelte` select `server`, and `blazor-client` and `android-client` select `client-only`.
- Validation: `litespec model check` refuses a `client-only` project that names a backend stack and a `server` project whose backend count differs from one.

### Extensions

| Extension | Path | Scope | Switches on |
|:---|:---|:---|:---|
| `bdd` | `docs/backend/dotnet/ext/bdd.md` | `local` | `scenario` |
| `compat` | `docs/ext/compat.md` | `project` | none |
| `cache` | `docs/ext/cache.md` | `local` | `setting` |
| `concurrency` | `docs/ext/concurrency.md` | `local` | `setting` |
| `lifecycle` | `docs/ext/lifecycle.md` | `local` | `setting` |
| `integrations` | `docs/ext/integrations.md` | `local` | none |
| `audit` | `docs/ext/audit.md` | `project` | none |
| `locale` | `docs/ext/locale.md` | `project` | none |
| `mcp` | `docs/ext/mcp.md` | `project` | none |
| `tenancy` | `docs/ext/tenancy.md` | `project` | none |
| `plans` | `docs/ext/plans.md` | `project` | none |
| `outbox` | `docs/ext/outbox.md` | `local` | `event`, `setting`, `runbook` |
| `realtime` | `docs/ext/realtime.md` | `local` | none |
| `jobs` | `docs/ext/jobs.md` | `local` | `setting`, `runbook` |
| `stage` | `docs/ext/stage.md` | `project` | none |
| `devices` | `docs/ext/devices.md` | `local` | none |
| `customer-rules` | `docs/ext/customer-rules.md` | `project` | none |
| `ai` | `docs/ext/ai.md` | `local` | none |

Each entry holds `path`, which is required, and the optional `activationScope`, `applicableKinds`, `replaces`, and `switchesOn`. The path is the extension page: `docs/ext/<id>.md` for an extension that supports every stack, or `<stack folder>/ext/<id>.md` for one that supports a single stack, such as `bdd` and `layer-projects` under `docs/backend/dotnet/`. The file name is the extension id. The scope is `project` or `local`. The kinds name the record types the extension applies to, such as `use-case` and `journey` for `bdd`. `switchesOn` names the record types a selection requires, such as `scenario` for `bdd` and `event`, `setting`, and `runbook` for `outbox`. `replaces` names the baseline provisions an extension supersedes, and the manifest holds the list. The [extensions index](../ext/README.md) states each extension with its activation criterion.

What the field drives:

- Selection: naming an extension id in `selectedExtensions` applies its page. `litespec/core/scope#select-conditional-extensions-explicitly` requires selecting every extension whose criteria apply and none whose do not.
- Validation: `litespec model check` refuses an id the manifest does not ship and rejects a selection whose stack does not support the extension. The authoring validator checks each path, scope, kind list, and replacement id.

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
| nuget | `LiteBus.Analyzers` | `7.x` |
| nuget | `LiteBus.Commands` | `7.x` |
| nuget | `LiteBus.Commands.Abstractions` | `7.x` |
| nuget | `LiteBus.Commands.Recurring` | `7.x` |
| nuget | `LiteBus.Events` | `7.x` |
| nuget | `LiteBus.Events.Abstractions` | `7.x` |
| nuget | `LiteBus.Extensions.Microsoft.DependencyInjection` | `7.x` |
| nuget | `LiteBus.Messaging` | `7.x` |
| nuget | `LiteBus.Messaging.Abstractions` | `7.x` |
| nuget | `LiteBus.Outbox` | `7.x` |
| nuget | `LiteBus.Outbox.Abstractions` | `7.x` |
| nuget | `LiteBus.Outbox.Dispatch.InProcess` | `7.x` |
| nuget | `LiteBus.Outbox.Storage.PostgreSql` | `7.x` |
| nuget | `LiteBus.Queries` | `7.x` |
| nuget | `LiteBus.Queries.Abstractions` | `7.x` |
| nuget | `LiteBus.Storage.PostgreSql` | `7.x` |
| nuget | `LiteBus.Testing.Mediation` | `7.x` |
| nuget | `LiteSpec.Analyzers` | `5.1.0` |
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
| npm | `@base-ui/react` | `1.x` |
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
| npm | `@tanstack/react-form` | `1.x` |
| npm | `@tanstack/react-query` | `5.x` |
| npm | `@tanstack/react-query-devtools` | `5.x` |
| npm | `@tanstack/react-router` | `1.x` |
| npm | `@tanstack/react-router-devtools` | `1.x` |
| npm | `@tanstack/react-start` | `1.x` |
| npm | `@tanstack/react-table` | `9.x` |
| npm | `@tanstack/router-cli` | `1.x` |
| npm | `@tanstack/router-plugin` | `1.x` |
| npm | `@tanstack/svelte-form` | `1.x` |
| npm | `@tanstack/svelte-query` | `6.x` |
| npm | `@tanstack/svelte-query-devtools` | `6.x` |
| npm | `@tanstack/svelte-table` | `9.x` |
| npm | `@testing-library/react` | `16.x` |
| npm | `@testing-library/svelte` | `5.x` |
| npm | `@types/node` | `26.x` |
| npm | `@types/react` | `19.x` |
| npm | `@types/react-dom` | `19.x` |
| npm | `@vitejs/plugin-react` | `6.x` |
| npm | `bits-ui` | `2.x` |
| npm | `class-variance-authority` | `0.x` |
| npm | `clsx` | `2.x` |
| npm | `cmdk` | `1.x` |
| npm | `date-fns` | `4.x` |
| npm | `eslint` | `10.x` |
| npm | `eslint-plugin-jsx-a11y` | `6.x` |
| npm | `eslint-plugin-react` | `7.x` |
| npm | `eslint-plugin-react-hooks` | `7.x` |
| npm | `eslint-plugin-svelte` | `3.x` |
| npm | `globals` | `17.x` |
| npm | `jsdom` | `30.x` |
| npm | `jsqr` | `1.x` |
| npm | `lucide-react` | `1.x` |
| npm | `msw` | `3.x` |
| npm | `react` | `19.x` |
| npm | `react-day-picker` | `10.x` |
| npm | `react-dom` | `19.x` |
| npm | `recharts` | `3.x` |
| npm | `shadcn` | `4.21.5` |
| npm | `shadcn-svelte` | `1.7.0` |
| npm | `sonner` | `2.x` |
| npm | `svelte` | `5.x` |
| npm | `svelte-check` | `4.x` |
| npm | `svelte-i18n` | `4.x` |
| npm | `tailwind-merge` | `3.x` |
| npm | `tailwindcss` | `4.x` |
| npm | `tw-animate-css` | `1.x` |
| npm | `typescript` | `6.x` |
| npm | `typescript-eslint` | `8.x` |
| npm | `vaul` | `1.x` |
| npm | `vite` | `8.x` |
| npm | `vitest` | `5.x` |
| npm | `yaml` | `2.x` |
| npm | `zod` | `4.x` |
<!-- /generated:manifest-pins -->

What the pins drive:

- Resolution: every SDK, framework, and package version resolves from the manifest. `litespec/core/stacks#use-manifest-version-pins` requires the pins over prose, examples, and agent memory.
- UI validation: `litespec model check` reads the pinned npm packages and the baselines below when it checks the shared package and the stylesheets.

### UI Baselines

The `uiBaseline` object pins the controlled React baseline, and `uiBaselineSvelte` pins the controlled Svelte baseline with the same fields. The values below are the real values of the current release.

| Item | React value | Svelte value |
|:---|:---|:---|
| `platform` | `react` | `svelte` |
| `system` | `shadcn/ui` | `shadcn-svelte` |
| `styling` | `tailwindcss-v4` | `tailwindcss-v4` |
| `base` | `base-ui` | `bits-ui` |
| `style` | `nova` | `nova` |
| `cssVariables` | `true` | `true` |
| `baseColor` | `neutral` | `neutral` |
| `theme` | `neutral` | `neutral` |
| `chartColor` | `neutral` | `neutral` |
| `font` | `geist` | `geist` |
| `fontHeading` | `inherit` | `inherit` |
| `icons` | `lucide` | `lucide` |
| `radius` | `default` | `default` |
| `menuAccent` | `subtle` | `subtle` |
| `menuColor` | `default` | `default` |
| `componentsStyle` | `base-nova` | `base-nova` |
| `sourceModel` | `shared-ui-package` | `shared-ui-package` |
| `profiles` | `public-light`, `application-balanced`, `admin-dense` | `public-light`, `application-balanced`, `admin-dense` |
| `nativeBoundary` | `separate-platform-profile` | `separate-platform-profile` |

The `registry` block names the component source:

| Item | React value | Svelte value |
|:---|:---|:---|
| `name` | `shadcn` | `shadcn-svelte` |
| `url` | `https://ui.shadcn.com` | `https://shadcn-svelte.com/registry` |
| `allowAdditional` | `true` | `true` |

`litespec model check` selects the baseline by the shared package platform: a Svelte package reads `uiBaselineSvelte`, and any other package reads `uiBaseline`. It checks the registry configuration, the source lock, the design contract, the token stylesheet, and each frontend boundary against the baseline. `litespec/frontend/ui#select-one-visual-authority` requires the one shared package, and `litespec/frontend/ui#pin-the-baseline-in-the-shared-package` requires the pin.

## Notes

The field lists follow `schemas/litespec-manifest.schema.yaml` and the values follow `litespec.manifest.yaml` at version `5.1.0`. The selection behavior follows `apps/model/src/LiteSpec.Standards/Pages/StackSelection.cs`. The manifest checks follow `apps/maintain/src/LiteSpec.Maintain/Validate/Repository/ManifestValidator.cs`. The consumer checks follow `apps/cli/src/LiteSpec.Cli/Model/Consumer/ProjectFileChecks.cs`. The baseline checks follow `apps/cli/src/LiteSpec.Cli/Model/Ui/UiValidator.cs`. The [cache extension](../ext/cache.md) and the [bdd extension](../backend/dotnet/ext/bdd.md) show the two extension paths: a shared page under `docs/ext/` and a stack page under `<stack folder>/ext/`.
