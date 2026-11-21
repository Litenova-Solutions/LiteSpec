# Composable Policies for Shared Access Conditions

An access condition shared by several use cases is declared once and evaluated once. This decision makes each such condition one composable authorization policy that use cases declare in their authorization definitions and the one authorization guard evaluates.

## Context

A sign-in freshness rule reached five use cases as five separate guards, each reading the window, checking the time, and choosing the refusal alone. One copy checked before the permission and one after, one let anonymous callers through, and a new sensitive use case silently got none.

The briefs ruled out custom guards for what a definition declares, but no page showed how a condition beyond a permission, a recent sign-in, an owner-only act, or a fresh second factor, joins the authorization definition. Each worker wrote a guard of its own.

## Options

LiteSpec compared 2 options for shared access conditions. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Declare each shared access condition once as a composable policy | One factory holds the condition, each use case declares the policy, and the one guard evaluates it in one order for all of them. | Use cases declare one more policy beside their permission. |
| Keep one guard per use case with a shared helper | Each use case keeps its own check inline. | Copies still drift in order, refusals, and anonymous handling, and each new use case repeats the wiring. |

## Decision

Access conditions shared by several use cases are composable policies: `Permission(id)`, `RecentSignIn(id)`, `Self(id)`, and `System`. Each use case whose record cites the governing rule declares the policy in its authorization definition.

The one authorization guard evaluates every policy in one order: the caller is signed in, then each declared condition holds, then the permission holds. A stale sign-in is refused with the recorded code and sentence before the permission check. A use case adds no guard of its own for a condition a policy covers.

## Consequences

Access conditions shared by several use cases are composable policies: Permission, RecentSignIn, Self, and System. Each use case declares its policies in its authorization definition, and the guard evaluates signed-in, then the condition, then the permission.

A use case adds no guard for a condition a policy covers.

The new provision is `litespec/backend/application#declare-shared-access-conditions-as-composable-policies`.

## Revisit Conditions

This decision reopens when a shared condition needs per-use-case evaluation that one policy fails to express.

That case means one policy no longer covers what its use cases need. The team then decides again how shared conditions are declared.
