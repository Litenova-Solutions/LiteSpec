# Plans and Entitlements

**Stack:** any

**Concern:** application

A product with priced tiers states each tier in one place: the behavior the tier unlocks and the amounts it allows. This page is for teams whose tiers change what the software permits. One plan record per tier keeps a price change to a single edit instead of scattered checks.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

A project selects `plans` when tiers gate behavior or amounts in the software, within one offering or across several. Without gated behavior or amounts there is nothing to entitle, so the project leaves it unselected.

## Baseline Relationship

This extension replaces no baseline provision. It sits beside the [multitenancy extension](../core/tenancy.md): tenancy isolates each tenant's data, while plans state what each tenant's tier allows.

It switches on no record type. Plans are policy requirements, as the [intent page](../core/intent.md) states. Gated behavior resolves through permissions under `litespec/core/access#authorize-every-use-case-through-a-declared-grant`: a plan grants the permissions its tier allows through the same grant chain as any other entitlement. Gated amounts resolve through settings the gated use case cites under `litespec/core/use-cases#name-the-settings-each-use-case-reads`, with each commercial default tied to its requirement under `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`. A run past its plan ends in the named refusal result under `litespec/core/use-cases#end-every-use-case-in-named-results`.

### One Record per Tier

Each tier lives in its own requirement record with its name and its entitlements. The team plan record states the export use case it unlocks and the row cap it allows. One record per tier holds the full picture, so a tier change touches one file and every reader sees the same list.

### Trusted Plan Resolution

The product resolves the acting plan from verified subscription state, the same source the pipeline uses for tenant identity. A plan value arriving from caller input stays out of the decision, since input states a wish while subscription state states the paid fact. Resolution runs in the authorization pipeline, where actor and target are already present, so every use case reads the same answer from the same place.

### Screens Show Only Entitled Actions

Each screen lists the actions the acting plan entitles and leaves the rest off the display. The server refuses a run past the plan with the named refusal result, and the screen mirrors that outcome before the call. The person sees the choices the server accepts, with no button leading to a failure the product already decided.

## Standards

None.

## Conventions

None.

## Dependencies

Packages: None.

Related extensions: The `tenancy` extension owns tenant isolation when tenants sit on different tiers. The `audit` extension owns tamper-resistant evidence when an entitlement decision needs a record.

Guides: None.
