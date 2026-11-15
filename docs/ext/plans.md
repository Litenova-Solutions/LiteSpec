# Plans and Entitlements

**Stack:** any

**Concern:** application

A product that gates behavior and amounts by tier records each plan's entitlements in one place: the behavior the plan unlocks and the amounts it allows.

This page states how the product records its plans, how behavior and amounts resolve to what the acting plan entitles, and how the product answers a run past its plan.

A project selects it when tiers change what the software allows, so a plan change moves one record instead of scattered checks.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

A project selects `plans` when tiers gate behavior or amounts in the software, within one offering or across several. An offering with no gated behavior or amount leaves it unselected: without gated tiers there is nothing to entitle.

## Baseline Relationship

This extension replaces no baseline provision. It sits beside the [multitenancy extension](tenancy.md): tenancy isolates each tenant's data, while plans state what each tenant's tier allows.

It switches on no record type. Plans live as policy requirements, gated behavior resolves through permissions, and gated amounts resolve through settings, as the [intent page](../core/intent.md), the [access page](../core/access.md), and the [behavior page](../core/behavior.md) state.

## Standards

### Record Each Plan Once

**ID:** `litespec/ext/plans#record-each-plan-once`

**Strength:** required

**Applies to:** artifact: record:requirement

**Statement:** A consumer selecting `plans` MUST record each plan once with its name and entitlements.

**Rationale:** A plan named in two places disagrees the day one updates without the other: one record promises what the gate refuses. One record per plan holds the name and the entitlements, so a plan change is one edit in one place.

**Example:** The team plan's record names the export use case it unlocks and the row cap it allows.

**Reviewed for:** Does every plan have its own record with its entitlements? Read the requirement pages under `.litespec/intent/`.

### Gate Behavior Through Permissions

**ID:** `litespec/ext/plans#gate-behavior-through-permissions`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A gated use case MUST declare a permission that only an entitled plan grants.

**Rationale:** The permission chain already joins actors to use cases: the actor holds a role, the role allows a permission, and the use case declares it in its grant, as the [access page](../core/access.md) states. A plan grants its permissions through that chain, so entitlement is a grant like any other, not a second access system beside the first.

**Reviewed for:** Does every gated use case declare a permission in its grant that only an entitled plan grants? Read each gated use case's grant.

### Bound Plan Amounts in Settings

**ID:** `litespec/ext/plans#bound-plan-amounts-in-settings`

**Strength:** required

**Applies to:** artifact: record:setting-set

**Statement:** Each amount a plan allows MUST live as a setting the gated use case cites.

**Rationale:** An allowance buried in code changes only with a release, with each plan's number hiding in a different branch. A setting states the range, the setter, and the bound behavior, and the gated use case cites it, so raising a tier edits the setting instead of the code. `litespec/core/access#cite-the-setting-or-plan-behind-every-entitlement` requires the citation, and `litespec/core/use-cases#tie-every-setting-default-to-a-requirement` holds a commercial default to its requirement.

**Example:** The export-row cap lives as `setting/spending.max-export-rows` with an operator-set value, and the export command cites it.

**Reviewed for:** Does every plan allowance live in a setting the gated use case cites? Read the gated use cases beside their settings.

### Refuse Runs Beyond Entitlement

**ID:** `litespec/ext/plans#refuse-runs-beyond-entitlement`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case run past its plan MUST end in the refusal result the use case names for that run.

**Rationale:** A run past its plan is a refusal like any other: it ends in the named refusal result with its stable code, as the [behavior page](../core/behavior.md) states. Callers branch on one code instead of guessing from an error page, and the code stays stable while prices and allowances move.

**Reviewed for:** Does every run past its plan end in the named refusal result? Read the use case tests for plan limits.

### Resolve the Acting Plan from Trusted Context

**ID:** `litespec/ext/plans#resolve-the-acting-plan-from-trusted-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The product MUST resolve the acting plan from verified subscription state, never from caller input.

**Rationale:** The plan decides what runs, so only verified subscription state resolves it: a plan the caller supplies itself is a choice, not a fact. The [multitenancy extension](tenancy.md) treats tenant identity the same way.

**Reviewed for:** Does every acting plan resolve from verified subscription state rather than caller input? Read the plan resolution code.

### Hide Unentitled Actions on Screens

**ID:** `litespec/ext/plans#hide-unentitled-actions-on-screens`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST NOT offer an action the acting plan does not entitle.

**Rationale:** The server refuses what the plan does not entitle, and the screen reflects that refusal before the call. A button for an unentitled action invites a failure the product already decided. Hiding or disabling the action shows each plan only what it may run.

**Reviewed for:** Does any screen offer an action the acting plan does not entitle? Read each screen's actions.

## Conventions

### Resolve the Acting Plan in the Authorization Pipeline

**ID:** `litespec/ext/plans#resolve-the-acting-plan-in-the-authorization-pipeline`

**Strength:** default

**Applies to:** artifact: any

**Default:** Resolve the acting plan where the pipeline authorizes the use case.

**Replacement:** A consumer records a plan-resolution convention naming where its use cases find the acting plan.

**Rationale:** Authorization already runs in the pipeline with the actor and the target in hand. Resolving the plan there gives every use case the same answer from the same verified state, instead of each handler querying subscriptions its own way.

**Reviewed for:** Does every use case find the acting plan in the authorization pipeline, or does a plan-resolution convention name where? Read the authorization pipeline and that convention.

## Dependencies

Packages: None.

Related extensions: The `tenancy` extension owns tenant isolation when tenants sit on different tiers. The `audit` extension owns tamper-resistant evidence when an entitlement decision needs a record.

Guides: None.
