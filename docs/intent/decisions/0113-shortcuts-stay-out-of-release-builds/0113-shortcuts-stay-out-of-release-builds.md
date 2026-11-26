# Keep Development Shortcuts Out of Release Builds

A development shortcut is excluded from release builds at compile time, refuses to run outside development, and carries the development-shortcut mark. This decision keeps the shortcuts that make local work fast out of the artifact a release ships, and sets the defaults a project starts from.

## Context

Local development needs shortcuts: a sign-in that skips the real identity provider, a stand-in for an external system, a seed or reset command, a relaxed guard. A shortcut held back only by a runtime setting stays in the release artifact. A missing setting fails open, so a sign-in shortcut in production becomes a sign-in bypass, and nothing reports which shortcuts actually shipped.

A new Local Development page needed one rule that keeps shortcuts out of release builds and a set of defaults for how to build them. The one-command startup provision belonged on that page too, rather than on Operations beside the rules for running a release.

## Options

LiteSpec compared 2 options for keeping shortcuts out of a release. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Exclude shortcuts at compile time and require the development-shortcut mark | A release build carries no shortcut, a missing setting cannot fail open, and an analyzer reports a marked type that reaches a compilation without `DEBUG`. | Each stack page states how its builds exclude a shortcut, and existing shortcuts move behind the compile-time boundary and the mark. |
| Gate shortcuts at runtime with an environment setting | No build boundary or analyzer work. | A missing setting fails open, a shipped sign-in shortcut is a sign-in bypass, and no tool reports which shortcuts reached a release. |

## Decision

The provision `litespec/quality/development#keep-development-shortcuts-out-of-release-builds` states that a development shortcut is excluded from release builds at compile time, refuses to run outside the development environment, and carries the development-shortcut mark. A project with no shortcuts has nothing to satisfy.

Each stack page states how its builds exclude a shortcut. On .NET the shortcut lives in a project referenced only when `$(Configuration)` is `Debug`, its registration sits behind `#if DEBUG`, and its types carry `/// <development-shortcut>reason</development-shortcut>`. `dotnet build` reports `LS5014` for a type carrying that tag in a compilation without `DEBUG`. Web frontends import shortcut modules only under `import.meta.env.DEV`, and Android uses the debug source set.

The page also carries five defaults. A development sign-in replaces only the identity provider and completes sign-in through the product's own sign-in use case as a cast person. Seed data comes from the cast and goes through public use cases, not direct writes to storage. The local orchestrator offers seed and reset commands. A second launch profile runs the real identity provider, which a project runs before each release. A development stand-in answers the operations its external-system record declares, as the integrations extension defines them.

## Consequences

The Local Development page moves `litespec/quality/development#start-the-stack-with-one-local-command` to `litespec/quality/development#start-the-stack-with-one-local-command` and adds the required shortcut provision and the five conventions. The shortcut rule is checked by `LS5014` on .NET and reviewed on other stacks, where the reviewer reads the build configuration the stack page states. Citations in the getting-started tutorial and the rules-eval fixture are replaced in the same change, and the release owner regenerates the provision index.

A developer keeps one named command for startup and the same cast values for seed data, so every developer starts from the same state. The real identity provider stays one launch profile away, and the release build contains no shortcut to bypass it.

## Revisit Conditions

This decision reopens when a stack cannot exclude code at compile time, or when a project needs a shortcut that must run in a release build.

Either case leaves a shortcut in the artifact and takes away the guarantee that a missing setting cannot turn it on. The team then decides again how that stack keeps a shortcut out of a release.
