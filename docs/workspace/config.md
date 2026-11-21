# Configuration

**Stack:** `any`
**Concern:** `workspace`

Configuration is every value a workspace sets outside code: version pins, backend and frontend settings, environment access, secrets, and toolchain versions. This page states where each value lives, who owns it, and when a bad value fails. It applies to anyone who adds a setting, a dependency, or a tool to the workspace.

## Concepts

### Settings and Environment

A settings type is one class per backend section that names each field, its range, and its format. The application validates it when it starts, so a missing or malformed value fails startup with a named field instead of failing the first request that needs it. The `Email` section binds an `EmailSettings` type that validates its endpoint and sender at startup.

A frontend reads its environment through one validated module that separates server-only values from browser-visible ones, so no secret slips into the bundle. Tracked files carry only safe defaults and placeholders; real values live in user secrets locally and on the deployment platform when hosted.

### Pins and Repeatability

A repeatable build resolves the same versions on every machine. That takes three pins: the manifest, `litespec.manifest.yaml`, pins the baseline stack the standards name; one root lockfile pins the JavaScript graph; and the toolchain pins name the package manager and Node.js releases. With every version pinned in one owned place, two checkouts of the same commit build the same result.

## Standards

### Validate Backend Configuration at Startup

**ID:** `litespec/workspace/config#validate-backend-configuration-at-startup`

**Strength:** required

**Statement:** A backend configuration section MUST bind to a named settings type validated for required fields, ranges, and formats at startup.

**Rationale:** A missing or malformed setting fails somewhere. Bound to a validated settings type, it fails at startup with a named field, while the deployment can still stop. Read through a raw configuration lookup, it fails at the first request that needs it.

**Reviewed for:** Does each backend section bind a settings type that fails startup on a missing field, a bad range, or a bad format? Read the settings types beside the backend host.

### Validate Frontend Environment Access

**ID:** `litespec/workspace/config#validate-frontend-environment-access`

**Strength:** required

**Statement:** A frontend MUST read environment variables through one validated module separating server-only from browser-visible values, at `src/lib/env.ts` through the framework alias.

**Rationale:** A frontend bundle ships to the browser, so every value it reads becomes public. One validated module at `src/lib/env.ts` separates server-only values from browser-visible ones in a single place a reviewer can read. Browser-visible names use the framework prefix, so an unprefixed value stays out of the bundle.

**Reviewed for:** Does each frontend read environment values only through `src/lib/env.ts`, with server-only values apart from browser-visible ones? Look at the environment imports in each frontend.

### Keep Secrets Outside Source Control

**ID:** `litespec/workspace/config#keep-secrets-outside-source-control`

**Strength:** required

**Statement:** A tracked configuration file MUST keep secrets out of tracked and observable data, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, and contain only safe defaults and placeholders.

**Rationale:** A tracked file reaches every clone and stays in history after deletion. No real value needs to be tracked: local development uses user secrets and hosted environments use the deployment platform. Safe defaults and placeholders keep the tracked file working as documentation.

**Reviewed for:** Does each tracked configuration file carry only safe defaults and placeholders, with real values in user secrets or the deployment platform? Read the tracked configuration files.

### Use One Frontend Dependency Graph

**ID:** `litespec/workspace/config#use-one-frontend-dependency-graph`

**Strength:** required

**Statement:** A TypeScript workspace MUST use one root `package.json`, `pnpm-workspace.yaml`, and `pnpm-lock.yaml`.

**Rationale:** One root manifest and one lockfile resolve one version of each package for the whole workspace. A nested lockfile lets one application resolve a different version than the one workspace CI installs, so local builds and CI builds disagree about the same code. One graph keeps every install identical.

**Reviewed for:** Does the workspace hold one root `package.json`, `pnpm-workspace.yaml`, and `pnpm-lock.yaml`, with no nested lockfile tracked? Look at the workspace root and each application folder.

### Pin the JavaScript Toolchain

**ID:** `litespec/workspace/config#pin-the-javascript-toolchain`

**Strength:** required

**Statement:** Root `package.json` MUST declare the manifest pnpm stack release in `packageManager` and a Node.js engine matching the manifest node stack release.

**Rationale:** Different Node.js or pnpm releases resolve, build, and lint differently. Declaring the manifest pnpm release in `packageManager` and a matching Node.js engine keeps every developer and CI on the same toolchain. CI provisions that Node.js release and invokes the declared pnpm release, so local and CI resolve identically.

**Reviewed for:** Does the root `package.json` declare `packageManager` and a Node.js engine matching the manifest releases, with CI provisioning the same releases? Read the root `package.json` and the CI setup.

### Pin the React Version in the ESLint Flat Config

**ID:** `litespec/workspace/config#pin-the-react-version-in-the-eslint-flat-config`

**Strength:** required

**Statement:** When the workspace holds a React frontend, its ESLint flat config MUST set a concrete `settings.react.version` rather than leave it at `detect`.

**Rationale:** The `detect` setting calls context APIs that the pinned ESLint release removed, so it fails under the pinned toolchain. A concrete `settings.react.version` skips detection entirely and lints against the version the workspace ships.

**Reviewed for:** Does each React ESLint flat config set a concrete `settings.react.version`? Look at the flat config of each React frontend.

## Conventions

### Keep Environment Examples Beside Applications

**ID:** `litespec/workspace/config#keep-environment-examples-beside-applications`

**Strength:** default

**Default:** Keep a `.env.example` beside each frontend and deployable host listing required names and safe values.

**Replacement:** A project that documents its environment centrally records its own example location in its project documentation.

**Rationale:** A new developer copies the example to start, and a reviewer reads it to learn which names the application needs. Kept beside the frontend or host it documents, next to the options module that reads those names, the example changes in the same edit as the names. Kept anywhere else, it goes stale without anyone noticing.

**Reviewed for:** Does each frontend and deployable host directory hold a `.env.example` with required names and safe values, or does the project documentation name a central example location? Look beside each frontend and host.

### Keep Local Overrides Untracked

**ID:** `litespec/workspace/config#keep-local-overrides-untracked`

**Strength:** default

**Default:** Keep developer overrides in ignored local files or a platform secret store.

**Replacement:** A project that shares override defaults records its own override files and their safe contents in its project documentation.

**Rationale:** Developer overrides differ per machine by nature: ports, paths, and local endpoints. Committing them turns one developer's setup into everyone's default. Ignored local files or a platform secret store keep overrides personal, so no second committed source competes with the deployment platform for the same value.

**Reviewed for:** Do developer overrides live in ignored local files or a platform secret store, with no second tracked environment-specific source? Look at the ignore rules and the tracked configuration files.

### Keep Logging Configuration Provider-Neutral

**ID:** `litespec/workspace/config#keep-logging-configuration-provider-neutral`

**Strength:** default

**Default:** Emit structured logs through standard logging abstractions and keep exporter configuration in hosts.

**Replacement:** A project committed to one log backend records its own logging setup in its host configuration.

**Rationale:** Application code writes through standard logging abstractions while each host configures its exporter, so application code carries no provider choice. Switching a log backend stays inside deployment settings instead of touching every call site.

**Reviewed for:** Does application source log through standard abstractions with exporter configuration in hosts, or does the host configuration name the project's own logging setup? Look at the logging calls and the host configuration.

### Keep Documentation Directories Out of Build-Artifact Ignore Rules

**ID:** `litespec/workspace/config#keep-documentation-directories-out-of-build-artifact-ignore-rules`

**Strength:** default

**Default:** Exclude a documentation directory from a build-artifact ignore rule that matches it by accident.

**Replacement:** A project that renames around the clash records its own directory names in its project documentation.

**Rationale:** Platform `.gitignore` templates match broad names, so a documentation directory with such a name silently stops being tracked. Excluding the documentation directory from the rule keeps records tracked, and `git check-ignore` confirms each record stays visible.

**Example:**

```gitignore
[Oo]utput/
!docs/<directory>/
!docs/<directory>/*.yaml
!docs/<directory>/*.md
```

Confirm with `git check-ignore docs/<directory>/<file>.yaml` that no record is ignored.

**Reviewed for:** Does each documentation directory stay tracked, with `git check-ignore` reporting no match for the record paths under `docs/`? Run the check on the record paths.

## Reference Example

This informative example demonstrates `litespec/workspace/config#validate-backend-configuration-at-startup` and `litespec/workspace/config#keep-secrets-outside-source-control`.

The `EmailSettings` type binds the `Email` section and validates its endpoint and sender at startup, and the application injects it as a settings type instead of reading raw configuration. The tracked file carries only placeholders, while the consumer workspace secret store supplies the credential.
