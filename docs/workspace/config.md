# Configuration

**Stack:** `any`
**Concern:** `workspace`

Configuration is each value a workspace sets outside code: version pins, backend and frontend settings, environment access, secrets, and toolchain versions. This page is for anyone who adds a setting, a dependency, or a tool. A bad value fails at startup with a named field instead of failing the first request that needs it.

## Concepts

### Settings and Environment

One settings type covers one backend section, naming each field with its range and format. The application validates it at startup, so a missing or malformed value stops the start with the field named. The `Email` section binds an `EmailSettings` type that validates its endpoint and sender at startup.

A frontend reads its environment through one validated module that separates server-only values from browser-visible ones. The module lives at `src/lib/env.ts` through the framework alias. Browser-visible names use the framework prefix, so an unprefixed value stays out of the bundle.

### Pins and Repeatability

A repeatable build resolves the same versions on each machine. Three pins give that: the manifest `litespec.manifest.yaml` pins the baseline stack the standards name, one root lockfile pins the JavaScript graph, and the toolchain pins name the package manager and Node.js releases. With each version pinned in one owned place, two checkouts of the same commit build the same result.

The root `package.json` declares the manifest pnpm stack release in `packageManager` and a Node.js engine matching the manifest node stack release. CI provisions that Node.js release and invokes the declared pnpm release, so local installs and CI installs resolve identically. One root `package.json`, `pnpm-workspace.yaml`, and `pnpm-lock.yaml` cover the TypeScript workspace, with nested lockfiles left out.

When the workspace holds a React frontend, its ESLint flat config sets a concrete `settings.react.version` instead of `detect`. The `detect` setting calls context APIs that the pinned ESLint release removed, so it fails under the pinned toolchain. A concrete version skips detection entirely and lints against the version the workspace ships.

### Secrets and Local Values

A tracked configuration file reaches each clone and stays in history after deletion. Tracked files hold safe defaults and placeholders only, while real values live in user secrets locally and on the deployment platform when hosted. A new developer copies the example to start, and a reviewer reads it to learn which names the application needs.

A `.env.example` sits beside each frontend and deployable host, listing the names and safe values. Kept beside the frontend or host it documents, next to the options module that reads those names, the example changes in the same edit as the names. Kept anywhere else, it goes stale without anyone noticing.

Developer overrides differ per machine by nature: ports, paths, and local endpoints. Overrides live in ignored local files or a platform secret store, so one developer setup turns into no team default. The tree holds no second tracked environment-specific source competing with the deployment platform for the same value.

### Logging and Documentation Directories

Application code writes through standard logging abstractions while each host configures its exporter. Application code carries no provider choice that way, and a log backend switch stays inside deployment settings instead of touching each call site.

Platform `.gitignore` templates match broad names, so a documentation directory with such a name stops being tracked silently. Excluding the documentation directory from the rule keeps records tracked, and `git check-ignore docs/<directory>/<file>.yaml` confirms that each record stays visible:

```text
[Oo]utput/
!docs/<directory>/
!docs/<directory>/*.yaml
!docs/<directory>/*.md
```

## Standards

None.

## Conventions

None.
