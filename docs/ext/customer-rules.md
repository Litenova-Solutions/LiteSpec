# Customer Rules and Hooks

**Stack:** any

**Concern:** application

Some products let customers build their own rules from triggers, conditions, and actions, and call customer endpoints when those rules fire. This page states the catalog those rules draw from, how tenant instances stay data, and the two contracts each hook carries: what a customer endpoint may rely on, and what it never assumes. A project selects this extension when tenants compose their own rules or the product calls customer hooks.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

A project selects `customer-rules` when tenants compose their own rules or customer endpoints receive hook calls. Product-owned reactions and jobs never select it; they are Behavior records whether or not the project selects this extension.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type. It divides customer rules into catalog records and tenant data: developers own the `catalog` of triggers, conditions, and actions as records, while each tenant `instance` stays tenant data.

Each hook carries two contracts: a may contract stating what the customer endpoint relies on, and a may-not contract stating the ordering, timing, and payload bounds the endpoint never assumes.

## Standards

### Record the Customer Rules Catalog

**ID:** `litespec/ext/customer-rules#record-the-customer-rules-catalog`

**Strength:** required

**Statement:** Each trigger, condition, and action in the customer rules catalog MUST be a record.

**Rationale:** Every tenant rule is built from the catalog, so the catalog is what the product promises its tenants. Recording each entry states what it does, what it costs, and what it refuses, so tenant rules compose from parts the team has specified.

**Example:** The action `invoice.send-reminder` is a record stating its rate cap and its suppression rule.

**Reviewed for:** Does every catalog trigger, condition, and action have a record? Read the catalog records.

### Treat Tenant Rules as Data

**ID:** `litespec/ext/customer-rules#treat-tenant-rules-as-data`

**Strength:** required

**Statement:** A tenant rule instance MUST live as tenant data, not as a record.

**Rationale:** Tenants write thousands of rules the team never sees, so recording each one is impractical. The catalog carries the records instead: instances compose only catalog entries, so the unrecorded part is the composition, never the parts.

**Example:** A tenant's rule `remind overdue invoices every Monday` is stored data, while its trigger, condition, and action each point at catalog entries.

**Reviewed for:** Is every tenant rule instance stored as data rather than as a record? Read the catalog records and the tenant storage.

### Refuse Unknown Catalog Entries

**ID:** `litespec/ext/customer-rules#refuse-unknown-catalog-entries`

**Strength:** required

**Statement:** An instance naming a trigger, condition, or action outside the catalog MUST be refused.

**Rationale:** A renamed or retired entry lingers in old instances, and a crafted request can name entries that never existed. Refusing unknown entries keeps every running rule inside the catalog instead of executing a name nobody defined.

**Example:** An instance naming the retired trigger `expense.submitted` is refused with the catalog's current trigger list.

**Reviewed for:** Does every instance test refuse entries outside the catalog with the current catalog? Read the instance tests.

### Version Catalog Entries

**ID:** `litespec/ext/customer-rules#version-catalog-entries`

**Strength:** required

**Statement:** Each catalog entry MUST carry a version that instances cite.

**Rationale:** Catalog entries evolve: a condition gains an operator, or an action changes its payload. Versions pin each instance to the behavior it was composed against, so an entry upgrade never silently rewrites running tenant rules.

**Example:** The condition `order.total-above` moves from version 3 to version 4, and existing instances keep running version 3 until the tenant re-saves.

**Reviewed for:** Does every catalog entry carry a version, and does every stored instance cite one? Read the catalog records and the stored instances.

### State What a Hook May Rely On

**ID:** `litespec/ext/customer-rules#state-what-a-hook-may-rely-on`

**Strength:** required

**Statement:** Each hook MUST state its may contract: the payload, retries, timeout, and signature the customer endpoint relies on.

**Rationale:** A customer endpoint is built by developers outside the product team, against the hook's contract. The may contract states the payload shape, the retry count and spacing, the timeout, and the signature, so the endpoint is written against stated behavior.

**Example:** The hook `invoice.paid` states three retries spaced a minute apart, a ten-second timeout, and an HMAC signature header.

**Reviewed for:** Does every hook record state the payload, retries, timeout, and signature? Read each hook record.

### State What a Hook May Not Assume

**ID:** `litespec/ext/customer-rules#state-what-a-hook-may-not-assume`

**Strength:** required

**Statement:** Each hook MUST state its may-not contract: the ordering, timing, and payload bounds the customer endpoint never assumes.

**Rationale:** Customers assume what the contract omits: order, instant delivery, and payloads that never grow. The may-not contract states these limits in advance, so a hook that fires twice or arrives late stays within what the hook stated.

**Example:** The hook `invoice.paid` states that deliveries may repeat, may arrive out of order, and carry no line-item detail.

**Reviewed for:** Does every hook record state the ordering, timing, and payload bounds? Read each hook record.

### Isolate Hook Failures from the Trigger

**ID:** `litespec/ext/customer-rules#isolate-hook-failures-from-the-trigger`

**Strength:** required

**Statement:** A hook MUST leave its trigger's outcome unchanged when the customer endpoint fails.

**Rationale:** The trigger's work finishes before the hook fires: the invoice is paid, the expense is reimbursed. Failing the trigger because a customer endpoint timed out marks finished work as failed because of a customer-side outage. The hook records its own outcome and retries on its own schedule.

**Example:** A hook call that times out is retried three times and then logged as failed, while the paid invoice stays paid.

**Reviewed for:** Does any hook test show a trigger outcome changing when the endpoint fails? Read the hook tests.

## Conventions

### Bound Tenant Rule Runs per Period

**ID:** `litespec/ext/customer-rules#bound-tenant-rule-runs-per-period`

**Strength:** default

**Default:** Each tenant instance states a monthly run cap taken from the catalog default.

**Replacement:** A project names its run-cap scheme in its automation documentation.

**Rationale:** One tenant's runaway rule can starve every other tenant's automation. A monthly cap from the catalog default bounds the damage, and the tenant raises the cap only by asking.

**Reviewed for:** Does every stored instance carry a monthly run cap, or does the automation documentation name a cap scheme? Read the stored instances and that documentation.

## Dependencies

Packages: None.

Related extensions: `integrations` for the provider isolation and failure mapping behind hook calls.

Guides: None.
