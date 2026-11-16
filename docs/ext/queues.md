# Human Work Queues

**Stack:** any
**Concern:** application

Some work waits for a person: items queue up, people claim them, and each item clears through a listing. This extension states how a queue orders its items, how claims work, and what a bulk action shows and asks before it runs. A project selects it when people clear work items from a listing.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

Select `queues` for a use case when the use case lists work items people claim, or clears items in bulk. Ordinary reads and single-item commands leave it unselected.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type.

A listing query states its `claim` mode and its `score`, while a bulk money command states its `preview` and its `confirmBy` code.

## Standards

### State the Claim Mode

**ID:** `litespec/ext/queues#state-the-claim-mode`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST state its claim mode as disabled, optional, or required.

**Rationale:** Two people clearing the same item double-pay, double-approve, or double-refuse. The claim mode prevents two people from acting on the same item: items need no claim, accept an optional hold, or require one before action.

**Example:** Claimwick's payout queue requires a claim before release, while its review queue leaves claiming optional.

**Reviewed for:** Does every queue use case state its claim field as disabled, optional, or required? Check the use case pages.

### Order Queue Items by Score

**ID:** `litespec/ext/queues#order-queue-items-by-score`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A work queue MUST order its items by a score that cites a calculation rule.

**Rationale:** A queue without an order gets cleared by habit: newest first, easiest first, or whatever draws attention first. A score citing a calculation rule ties the order to declared inputs, so the riskiest or most valuable item comes first by rule instead of by accident.

**Example:** Claimwick's dispute queue orders by `rule/dispute.urgency-score`, which weighs amount, age, and evidence gaps.

**Reviewed for:** Does every queue listing sort by a score that cites its calculation rule? Check the listing queries.

### Show the Score Beside Each Item

**ID:** `litespec/ext/queues#show-the-score-beside-each-item`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A queue listing MUST show each item score with the inputs that set it.

**Rationale:** People work around an order they can't see or trust. Showing the score with its inputs lets each person check the order and question the inputs rather than distrusting the queue.

**Example:** Each Claimwick dispute row shows `urgency 82: EUR 1,900, 6 days old, evidence missing`.

**Reviewed for:** Does every queue row show its score with the inputs that set it? Check the queue rows in the interface.

### Release Claims Past Their Lease

**ID:** `litespec/ext/queues#release-claims-past-their-lease`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A claimed item MUST return to the queue when its claim lease expires.

**Rationale:** People hold claims and then leave them unattended. A claim with no expiry keeps the item out of the queue forever. The lease returns stale claims to the queue, so an abandoned item becomes available to the next person.

**Example:** A Claimwick payout claimed Friday and untouched returns to the queue when its one-hour lease expires.

**Reviewed for:** Does every claim test show an untouched item returning to the queue after its lease expires? Check the claim tests.

### Show a Preview Before Bulk Money Actions

**ID:** `litespec/ext/queues#show-a-preview-before-bulk-money-actions`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST render a preview of every affected item before acting.

**Rationale:** One wrong filter turns one mistake into hundreds when money moves on many items at once. The preview lists every affected item with its amount, so the person confirms the items they meant instead of trusting the filter text.

**Example:** Claimwick's bulk release previews forty-two payouts totaling EUR 18,400 before asking for confirmation.

**Reviewed for:** Does every bulk money command render every affected item with its amount before acting? Check the preview step of each command.

### Confirm Bulk Money Actions Out of Band

**ID:** `litespec/ext/queues#confirm-bulk-money-actions-out-of-band`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A bulk money command MUST require its `confirmBy` code from outside the session.

**Rationale:** A preview confirms the set, but the session itself can be hijacked, shared, or left open. An out-of-band code arrives on a channel the session doesn't control, so the confirmation proves the person, not just the browser.

**Example:** Claimwick's bulk release asks for the six-digit code sent to the finance phone before moving any money.

**Reviewed for:** Does every bulk money command ask for the out-of-band code before moving money? Check the command handlers.

## Conventions

### Order Unscored Queues by Age

**ID:** `litespec/ext/queues#order-unscored-queues-by-age`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** A queue without a score lists its oldest item first.

**Replacement:** A project names its fallback queue order in its queue documentation.

**Rationale:** Age is the simplest fallback: the item waiting longest goes first. Any other fallback works, when the queue states it.

**Reviewed for:** Does every queue without a score list oldest first, or name a fallback order in the queue documentation? Check the queue listings and the queue documentation.

## Dependencies

Packages: None.

Related extensions: `jobs` for scheduled work that outlives a request.

Guides: None.
