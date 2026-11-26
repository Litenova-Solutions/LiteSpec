# Plans and Entitlements

**Stack:** any

**Concern:** application

A product that gates behavior and amounts by tier records each plan's entitlements in one place: the behavior the plan unlocks and the amounts it allows. Behavior resolves through permissions, amounts through settings, and a run past its plan ends in the refusal the use case names.

These rules apply when tiers change what the software allows, so a plan change moves one record instead of scattered checks.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

A project selects `plans` when tiers gate behavior or amounts in the software, within one offering or across several. Without gated behavior or amounts there is nothing to entitle, so the project leaves it unselected.

## Baseline Relationship

This extension replaces no baseline provision. It sits beside the [multitenancy extension](../core/tenancy.md): tenancy isolates each tenant's data, while plans state what each tenant's tier allows.

It switches on no record type. Plans are policy requirements, as the [intent page](../core/intent.md) states. Gated behavior resolves through permissions under `litespec/core/access#authorize-every-use-case-through-a-declared-grant`: a plan grants the permissions its tier allows through the same grant chain as any other entitlement. Gated amounts resolve through settings the gated use case cites under `litespec/core/access#cite-the-setting-or-plan-behind-every-entitlement`, with each commercial default tied to its requirement under `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`. A run past its plan ends in the named refusal result under `litespec/core/use-cases#end-every-use-case-in-named-results`.

## Standards

### Record Each Plan Once

**ID:** `litespec/ext/plans#record-each-plan-once`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A consumer selecting `plans` MUST record each plan once with its name and entitlements.

**Rationale:** A plan named in two places disagrees when one updates without the other: one record allows what the gate refuses. One record per plan holds the name and the entitlements, so a plan change is one edit in one place.

**Example:** The team plan's record names the export use case it unlocks and the row cap it allows.

**Reviewed for:** Does every plan have its own record with its entitlements? Read the requirement pages under `.litespec/intent/`.

### Resolve the Acting Plan from Trusted Context

**ID:** `litespec/ext/plans#resolve-the-acting-plan-from-trusted-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The product MUST resolve the acting plan from verified subscription state, never from caller input.

**Rationale:** The plan decides what runs, so only verified subscription state resolves it: a plan the caller supplies itself is a choice, not a fact. The [multitenancy extension](../core/tenancy.md) treats tenant identity the same way.

**Reviewed for:** Does every acting plan resolve from verified subscription state rather than caller input? Read the plan resolution code.

### Hide Unentitled Actions on Screens

**ID:** `litespec/ext/plans#hide-unentitled-actions-on-screens`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST NOT offer an action the acting plan does not entitle.

**Rationale:** The server refuses what the plan does not entitle, and the screen reflects that refusal before the call. A button for an unentitled action leads to a failure the product already decided. Hiding or disabling the action shows each plan only what it may run.

**Reviewed for:** Does any screen offer an action the acting plan does not entitle? Read each screen's actions.

## Conventions

### Resolve the Acting Plan in the Authorization Pipeline

**ID:** `litespec/ext/plans#resolve-the-acting-plan-in-the-authorization-pipeline`

**Strength:** default

**Applies to:** artifact: any

**Default:** Resolve the acting plan where the pipeline authorizes the use case.

**Replacement:** A consumer records a plan-resolution convention naming where its use cases find the acting plan.

**Rationale:** Authorization already runs in the pipeline with the actor and the target available. Resolving the plan there gives every use case the same answer from the same verified state, instead of each handler querying subscriptions its own way.

**Reviewed for:** Does every use case find the acting plan in the authorization pipeline, or does a plan-resolution convention name where? Read the authorization pipeline and that convention.

## Dependencies

Packages: None.

Related extensions: The `tenancy` extension owns tenant isolation when tenants sit on different tiers. The `audit` extension owns tamper-resistant evidence when an entitlement decision needs a record.

Guides: None.
