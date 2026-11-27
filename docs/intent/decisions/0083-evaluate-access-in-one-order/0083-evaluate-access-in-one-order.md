# Evaluate Access in One Order

Every request reaches a use case through grants, roles, permissions, and scope. This decision evaluates each request in one stated order over explicit permissions, so two readers never decide one request two ways.

## Context

No page stated the evaluation order. A role assignment bound a scope level but no instance, so no reader could tell whether an assignment matched the target. Permissions implied others by name: each reader assumed `manage-*` covered a different set. Destructive use cases shared permissions with harmless ones. `system` and device callers had no stated meaning.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| One stated algorithm over explicit permissions | Same steps for every request; no implied grants. | Roles split; assignments bind instances. |
| Keep evaluation order unstated | Nothing moves. | Readers decide one request two ways. |
| A central policy engine | One evaluator answers all. | Records stop stating grants. |

## Decision

Access evaluates in one order: the authenticated principal, then the role assignments whose bound scope instance matches the target, then the permission, then the use case's tenant reach, then the self-ownership restriction, then delegation validity. Several assignments combine as a union. A role assignment binds a scope instance as well as a level: a tenant, or the person for self.

The grant states the authentication each operation needs. `signed-in` requires an authenticated person with no permission. `system` means an internal trigger with no outside caller and never authorizes an outside request. A device caller authenticates with its own `auth` kind `device`.

Permissions are explicit. A permission allows exactly the use cases whose `access.grant` names it; no permission implies others by its name, and `manage-*` is a naming convention only. A destructive use case takes its own permission when roles must differ, and carries its `risk`.

## Consequences

Roles that relied on implied permissions split into the permissions their use cases grant.

The pipeline refuses in the algorithm's order, so each refusal points to the step that refused it.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) surveyed access practice below Intent, where the order was missing. The unstated steps were read from the access page with high confidence.

## Revisit Conditions

This decision reopens when a request needs a check outside the six steps. It also reopens when two assignments combine beyond a union.
