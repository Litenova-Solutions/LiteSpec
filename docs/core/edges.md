# Edges and Screens

**Stack:** any
**Concern:** records

Edges and Screens states how the product meets the outside: its integration events, notifications, entry points, screens, and external systems. Agents write these records from Intent, and code carries them out. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value, and record name is written.

## Concepts

### Integration Events

An integration event is a published fact that crosses the bounded context in CloudEvents form. Its `action` says whether the product sends or receives the fact: `send` for facts the product emits, and `receive` for facts it accepts from outside. A fact for the other modules of the same product is a module event instead, which never crosses the bounded context, as the [Automation page](automation.md#module-events) states. An integration event carries a full typed `payload`, because it crosses the bounded context and needs a full contract; a module event instead selects `fields` from the domain event it translates.

Each event states how far readers trust it. With `trigger-only` trust, the event only wakes the reader, and the reader goes back to the source for the facts. With `payload` trust, the fields on the event are fit to act on directly. The event carries `summary` and `delivery`: what it announces and who acts on it, and how it reaches its readers.

An outbound event names exactly one domain event in `translates`: the inside fact it carries outward. The use cases whose `raises` holds that domain event publish it, so each outbound event has exactly one publishing module.

No record declares the publishing as an effect, and no user code publishes the event. The pipeline step that stages raised domain events also stages each module event and each integration event that translates them, built from the domain event alone.

A read model in another module copies module events or integration events, as the [Automation page](automation.md#read-models) states. Claimwick emits `integration-event/reimbursement-requested` when spending approves an expense, for example, and the payout bank settles the reimbursement from it.

Every integration event names the external system it travels through in `through`. An outbound event publishes only fields its translated domain event carries. The record carries `compatibility`, and the explanation file beside the record restates Compatibility from `compatibility`, holds Consumers, and carries Example.

| Field | Meaning |
|:---|:---|
| `action` | `send` or `receive`. |
| `eventType`, `source`, `subject` | The CloudEvents type, source, and what the subject identifies. |
| `payload` | `{ name, meaning, type, identifies, many, required, classification }` entries, with the field types and the list flag use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states. |
| `dataschema` | The URL the payload follows, optional. |
| `trust` | `trigger-only` or `payload`. |
| `correlation` | How an inbound event is matched, inbound only. |
| `translates` | The domain event it carries outward, required on an outbound event; the event's publishers are the use cases raising it. |
| `requires` | The permission reading it, outbound only, optional. |
| `through` | The `external-system/...` the event travels through, required. |
| `arrivesThrough` | How an inbound event arrives, required on an inbound event: an `entry-point/...` of kind `webhook` or an `external-system/...` whose broker or stream the module subscribes to. |
| `compatibility` | How the event stays compatible with its consumers, required. |

### Facts Two Modules Keep

One module owns each fact, and any other module keeps only a copy. The owning module publishes a module event, and the reading module keeps a read model from it, as [Automation](automation.md) states. A decision that needs the owner's current value is decided in the owning module instead, as `litespec/core/automation#decide-current-truth-in-the-owning-module` states.

A copy lags behind its owner: state one module derives from another module's event is eventually consistent. A command that refuses or gates on a derived copy decides on stale state, since the event moving the fact can arrive after the command commits.

Where two modules need to agree on what arrived late, each compares timestamps the owning modules wrote themselves. Claimwick closes each month for new reimbursements. Each expense carries the approved moment spending wrote, and each close carries the moment the close was recorded. An approval stamped after the month's recorded close counts toward the next month, even when its event arrives late:

```text
close of March recorded at 2026-04-02 09:00
expense A approved at 2026-03-30 14:00 -> counted in March as closed
expense B approved at 2026-04-03 10:00 -> counted in April, although its event arrives with late March events
```

This removes both the lock and the race: each arrival classifies itself against owner-written moments. Where comparing moments leaves the act unsettled, the acting module confirms with the owner before it commits, through its port to the owner answered by the owner's public query, as [Automation](automation.md) states.

### Notifications

A notification is one message to a person, with its template, audience, channel, and consent. A call to a system is not a notification: it is an outbound integration event or an external-system operation.

Each notification names the external system that delivers it in `deliveredThrough`. Consent is recorded data, not a flag on the message: an aggregate holds it, use cases grant and withdraw it, and a rule requires it.

Suppression lives on the notification in `suppressedWhen`, and the scenarios that show the message sending nothing count as full scenarios.

Claimwick's `notification/spending.receipt-reminder` sends the `receipt-reminder-manager` template to the manager. The notification carries `summary`, and Content, Delivery, and Example live in the explanation file beside the record file.

| Field | Meaning |
|:---|:---|
| `template`, `parameters`, `locale` | The template key, its parameters, and where the language comes from. |
| `audience`, `channel` | The actors addressed and the channels: `email`, `sms`, `push`. |
| `deliveredThrough` | The `external-system/...` delivering the message, required. |
| `consentRule` | The `rule/...` requiring consent for the send, where consent applies. |
| `frequency`, `deduplicationKey` | `once` or `per-attempt`, and the deduplication template over the trigger payload. |
| `suppressedWhen` | The suppression condition, optional. |
| `claims` | `{ claim, requirement }` entries for what the message says. |
| `fulfills` | The requirements carried. |

### Entry Points

An entry point is one way outside callers reach the product. Each entry point declares its `authentication`, as the [Access page](access.md) states, and names its `audience` in `actor/...` ids.

Served content is a static script or legal text with its path, content type, cache control, and versioning, listed in `servedContent` on a browser entry point. It is content served unchanged, not a use case run, so it carries no behavior.

| Kind | Meaning |
|:---|:---|
| `browser`, `api`, `webhook`, `device`, `cli`, `mcp` | A browser app, a public interface, an inbound hook, a device, a command line, or a model context channel. |

| Field | Meaning |
|:---|:---|
| `audience` | The `actor/...` ids using this entry point, required. |
| `session` | How long a session lasts, `{ lifetime, idleTimeout }` as ISO 8601 durations, where sessions apply. |
| `reauthenticationTriggers` | When a fresh sign-in is required, one sentence per trigger. |
| `limits` | What the entry point does not do, one sentence per limit. |

The explanation restates Audience from `audience`, Authentication from `authentication`, `session`, and `reauthenticationTriggers`, and Limits from `limits`.

An entry point that people use states two things about its audience. Its `density` says how much each screen shows at once: `public-light` for pages that show little, `application-balanced` for screens that balance work and guidance, or `admin-dense` for screens that pack dense rows. Its `voice` says in a sentence or two how the product addresses that audience.

One density and one voice per entry point keep every screen of one audience at the same weight and in the same tone. The Claimwick employee app states `application-balanced`, for example.

The optional `protocol` block belongs to the `devices` extension. The [access page](access.md) states actors, roles, and permissions.

### Screens

A screen is one page a person sees through a `browser` or `device` entry point. Its record lives at `behavior/entry-points/<entry-point>/screens/<entry-point>.<name>/<entry-point>.<name>.yaml` with the identifier `screen/<entry-point>.<name>`, such as `screen/claimwick-app.expense-list`. The folder already states its entry point, so the record writes no up-link.

It names its `route`, the route template its stack uses: a web path, a Blazor `@page` template, or an Android destination. It names the use cases it `offers` and the flow steps it `serves`.

An overlay with no route of its own names the screen it opens on in `host` instead. A screen carries exactly one of `route` and `host`, as [Give Every Screen a Route or a Host](#give-every-screen-a-route-or-a-host) states. Its `summary` states what a person can do on the page. A navigation screen offers no use case and says so in its `summary`.

A screen states what a person can do there, and what the person sees follows from it. The results it shows come from the use cases it offers, its outgoing links come from its route, and the sentence shown for a refusal is joined by the refusal's `code`. None of those are written on the screen. Intent never names a screen either: a flow step states what a person achieves, and the book adds the screen that carries the step as context.

The route entry that renders a screen carries its mark, in the form its stack page states, naming `screen/...`.

The builder computes `calls` from the route entry. It follows imports within the route's own folder and the modules folder, so a module the route imports and every module that module composes count. It collects generated-client operation calls: a query helper the client generator emits for an operation, such as its options, query key, or mutation, counts as a call to that operation. It maps each operation through the endpoint's mark to its use case.

It follows neither the frame nor shared packages. A call a shared component makes is not the screen's offer, while a call a composed use-case module makes is. An operation that resolves to two use cases is reported as a reader diagnostic instead of being silently assigned to one. The builder compares the computed `calls` with `offers`.

A flow step is exercised when a passing test proved a scenario of a use case that serves the step.

### External Systems

An external system is one system outside the bounded context that the product exchanges messages with. It stands alone and names no actor.

When the integration calls the product rather than the other way around, that is still an external system: one calling through an API entry point. Claimwick's payout bank is an external system whose charge operation the spending module calls.

Each operation declares its action, timeout, response, and cache, and may state its retry behavior in `retries` prose. The timeout is an ISO 8601 duration such as `PT10S`. The retry count lives on the calling edge in `onFailure.retry.maxAttempts`.

Its erasure position states how personal data leaves it: `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "unsupported", "reason": "..." }`, `manual`, or `not-applicable` with its acceptance.

Its `summary` states what the system does for the product and who operates it. It names the aggregates and data classes it holds in `processes`, as `{ aggregate: aggregate/<m>.<a>, dataClass: <name> }` entries.

| Field | Meaning |
|:---|:---|
| `operations` | `{ name, action, timeout, onTimeout, retries, response, cache, rejected, accepted, unknown }` entries; action is `send` or `receive`, `timeout` is an ISO 8601 duration, `retries` is optional prose. The outcome handling separates `rejected`, `accepted`, and `unknown`; the `unknown` case names its handling in `onUnknown` as `dedupe-key`, `reconcile`, or `retry-safe`. |
| `erasure` | `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "unsupported" | "manual" | "not-applicable", "reason": "..." }` with `basedOn`. `unsupported` means the operator cannot remove the data, `manual` means people remove it outside the product, and `not-applicable` means the system holds no personal data after the product sends it. |
| `processes` | The aggregates and data classes the system holds, as `{ aggregate, dataClass }` entries. |

## Standards

### Describe Every Integration Event the Product Sends or Receives

**ID:** `litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each integration event MUST declare its action, its CloudEvents identity, its typed payload, the trust its readers place in it, and the external system it travels through in `through`.

**Rationale:** Without a stated action, a published fact is read both ways: an outbound notice is answered as an order, and an inbound claim is taken as authoritative. The action, the CloudEvents identity, the typed payload, the trust level, and the external system in `through` state what the fact is, how far readers follow it, and what it travels through. Each payload field carries the field types use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states, and a money field carries an amount with an ISO 4217 currency, as the [Record Language](language.md) states. An event may name the `dataschema` URL its payload follows. An inbound event also declares its `correlation` and its `arrivesThrough`, while an outbound event declares its `translates` domain event and its optional `requires`.

**Checked by:** `litespec model check` reports `record-invalid` for an integration event without action, CloudEvents identity, typed payload, trust, or `through`.

### Publish Every Outbound Integration Event from the Event It Translates

**ID:** `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`

**Strength:** required

**Applies to:** artifact: record:integration-event, record:use-case

**Statement:** Each outbound integration event MUST name in `translates` the domain event it carries, and is published only from that domain event, by the pipeline step that stages raised events.

**Rationale:** When a handler builds an integration event from whatever the aggregate holds after the act, it publishes state the act never announced, and a second handler can publish the same event with a different payload. Deriving the event from the one domain event it translates keeps every published field a field of the announced fact. The publishing use cases then follow from `raises`, with no second declaration to drift, and the domain event carries every field its integration events publish, as typed fields. A publish call in user code is an edge no record can declare, so the stack reader reports it. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** Claimwick's `integration-event/expense-submitted` translates `domain-event/expense.submitted`, so `use-case/spending.submit-expense`, which raises that event, publishes it, and its handler publishes nothing.

**Checked by:** `litespec model check` reports `record-invalid` for an outbound integration event without `translates`, `event-field-uncarried` (level G) for a payload field its translated domain event does not carry, and `effect-undeclared` for an event a handler, event handler or job publishes itself; `dotnet build` reports `LS2022` for an event published by user code.

### Name Published Events in the Domain's Words

**ID:** `litespec/core/edges#name-published-events-in-the-domains-words`

**Strength:** advisory

**Applies to:** artifact: record:integration-event

**Statement:** Each outbound integration event SHOULD be named for the business fact, without internal words.

**Deviation:** An event kept for an internal outbox fact its readers already consume keeps its name until its readers move.

**Rationale:** An integrator reads the published name as the business fact. Words like `staged`, `dispatch`, or `outbox` describe the machinery that delivered it instead of the fact itself. A name that carries the fact travels across the boundary unchanged. The internal words are `staged`, `dispatch`, `dispatched`, `outbox`, `handler`, `queued`, `worker`, and `job`.

**Example:** Claimwick names its event `expense-reimbursement-requested` for the requested reimbursement, while `expense-reimbursement-staged` names the staging step and is reported.

**Checked by:** `litespec model check` reports `integration-event-name-invalid` (level G) for an outbound event named with an internal word.

### Name How Every Inbound Integration Event Arrives

**ID:** `litespec/core/edges#name-how-every-inbound-integration-event-arrives`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each inbound integration event MUST name how it arrives in `arrivesThrough`.

**Rationale:** An inbound fact with no stated arrival is accepted anywhere: each handler invents its own intake, with its own trust decision. Naming the arrival gives each inbound fact one entry point with one authentication, where trust and correlation hold. That is either the `webhook` entry point a provider calls or the external system whose broker or stream the module subscribes to. Settled by [decision/0075-draw-the-boundary-around-outside-traffic](decision/0075-draw-the-boundary-around-outside-traffic).

**Example:** Claimwick's receipt-scan-available event names the scanning provider webhook entry point in `arrivesThrough`, and the payout-settled event names the payout bank stream the spending module subscribes to.

**Checked by:** `litespec model check` reports `record-invalid` for an inbound integration event without `arrivesThrough` naming a webhook entry point or a subscribed external system.

### Describe Every Notification with Audience Channel and Consent

**ID:** `litespec/core/edges#describe-every-notification-with-audience-channel-and-consent`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each notification MUST declare its template, audience, channel, consent rule, frequency, deduplication key, parameters, locale, and delivering system.

**Rationale:** A message without a declared audience and consent reaches the wrong actors, ignores consent, and repeats without control. Declaring the template with its parameters and locale, the audience, the channel of `email`, `sms`, or `push`, the consent rule in `consentRule`, the frequency of `once` or `per-attempt`, the deduplication key, and the delivering system in `deliveredThrough` puts the message beside its promise. A notification is a message to a person; a webhook to a system is an outbound integration event. Suppression lives on the notification in `suppressedWhen`, and suppression scenarios show the message sending nothing. A use case that needs consent names the same rule in its `consentRule`.

**Checked by:** `litespec model check` reports `notification-effect-missing` for a notification no use case, reaction, or job sends, and fails a notification without its declared fields.

### Match Notification Templates to the Template Store

**ID:** `litespec/core/edges#match-notification-templates-to-the-template-store`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** The `template` keys the notification records declare and the keys the project's template store holds MUST be the same set.

**Rationale:** The template key is the one join between a notification's promise and the text a person receives. A declared key the store lacks fails at send time, and a stored template no notification declares is text nobody reviewed. The store is the stack's own file, so the check reads it where it is instead of copying the keys into a record. A notification whose body the sender composes at send time declares `template: none` and stays out of the comparison.

**Example:** Claimwick's `notification/spending.receipt-reminder` declares `template: receipt-reminder-manager`, which the template store holds.

**Checked by:** `litespec model check` reports `template-unimplemented` (level S) for a declared key the store the stack page names lacks, and `template-unrecorded` (level S) for a stored key no notification declares, in a project whose stack configuration names a template store.

### Use Every Declared Template Parameter

**ID:** `litespec/core/edges#use-every-declared-template-parameter`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Every parameter a notification declares in `parameters` MUST appear as a placeholder in at least one of its template's texts.

**Rationale:** A declared parameter is part of the message's promise: the order number in a confirmation, or the name in a reminder. A parameter the template never uses is a promise the message does not keep, and it usually means someone rewrote the template without updating the record.

**Example:** Claimwick's `notification/spending.receipt-reminder` declares the parameter `submitterName`, and the `receipt-reminder-manager` template renders it as `{{SubmitterName}}`.

**Checked by:** `litespec model check` reports `template-param-drift` (level S) for a declared parameter whose placeholder, in the case the stack page states, appears in none of its template's texts.

### Name Each Template for Its Subject, Event and Audience

**ID:** `litespec/core/edges#name-each-template-for-its-subject-event-and-audience`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each notification template key MUST read `<subject>-<event>-<audience>`, with the audience name the record declares in `audience`.

**Rationale:** A key that names its subject, its event, and its audience tells every reader what the message is about and who receives it. A key that names none of the three hides both.

**Example:** Claimwick's `receipt-reminder-manager` names the receipt, the reminder, and the manager audience, while `email-change-confirmation` names no audience and is reported.

**Checked by:** `litespec model check` reports `template-key-invalid` (level A) for a template key ending in no name the notification declares in `audience`.

### Cite the Requirement Behind Every Notification Claim

**ID:** `litespec/core/edges#cite-the-requirement-behind-every-notification-claim`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each claim a notification makes MUST cite its requirement.

**Rationale:** A message can promise what no requirement holds: the text assures a refund the policy never states. Citing the requirement behind each claim keeps every assured sentence a carried promise.

**Reviewed for:** Does every notification claim cite the requirement behind it? Check the claims beside each notification.

### Give Each Entry Point a Kind

**ID:** `litespec/core/edges#give-each-entry-point-a-kind`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST declare its kind and its `authentication`.

**Rationale:** An entry point with no kind is entered any way: a webhook is called as a page, and a device as a browser. Declaring the kind, one of `browser`, `api`, `webhook`, `device`, `cli`, or `mcp`, together with its `authentication`, states how callers arrive and how they prove themselves.

**Checked by:** `litespec model check` reports `record-invalid` for an entry point without `kind` or `authentication`.

### State the Density and Voice of Each Entry Point

**ID:** `litespec/core/edges#state-the-density-and-voice-of-each-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each browser or device entry point MUST state its density in `density` and how the product addresses its audience in `voice`.

**Rationale:** Each audience reads its own screens: a public page shows little, an application screen balances work and guidance, and an admin screen packs dense rows. One density per entry point keeps every screen of one audience at the same weight, and one voice keeps the product addressing that audience the same way on each screen. Both sit on the entry point record, because the audience is what an entry point models. Proposed by [decision/0089-specify-the-ui-as-a-blueprint](decision/0089-specify-the-ui-as-a-blueprint).

**Example:** The Claimwick employee app entry point states `application-balanced` in `density`, with the `voice` "Claimwick addresses the employee plainly and names the next step on every screen."

**Checked by:** `litespec model check` reports `record-invalid` for a browser or device entry point without `density` or `voice`, and `record-invalid` for a `density` outside public-light, application-balanced, and admin-dense.

### Give Every Page Its Screen Record

**ID:** `litespec/core/edges#give-every-page-its-screen-record`

**Strength:** required

**Applies to:** artifact: route, record:screen; layer: implementation; trace: yes

**Statement:** Every route a browser or device entry point serves MUST be the implementation of exactly one screen record.

**Rationale:** A page is where a person meets the product. An unrecorded page offers use cases nobody reviewed as a page: what it lets someone start, which flow step it carries, and which refusal it shows. One record per page, with one mark on the route, makes the page a reviewable promise, and lets every other link (calls, results, and flow coverage) be computed from it.

**Example:** Claimwick's web route `/expenses` carries `implements: "screen/claimwick-app.expense-list"`, and the screen record at `behavior/entry-points/claimwick-app/screens/claimwick-app.expense-list/claimwick-app.expense-list.yaml` offers `use-case/spending.list-expenses`.

**Checked by:** `litespec model check` reports `screen-unimplemented` (level S) for a screen no route marks, and `behavior-unrecorded` (level S) for a route with no mark or a mark naming no screen.

### Give Every Screen a Route or a Host

**ID:** `litespec/core/edges#give-every-screen-a-route-or-a-host`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST carry exactly one of `route` and `host`.

**Rationale:** A route screen without a route is unreachable, and an overlay with one is two pages fighting over a route. A route places a screen on its route, while a host places an overlay on the screen it opens on. Exactly one of the two places every page once.

**Example:** The Claimwick expense list carries the route `/expenses`, and its confirm overlay names the expense list in `host` instead.

**Checked by:** `litespec model check` reports `record-invalid` for a screen without exactly one of `route` and `host`.

### Match a Screen's Offers to Its Calls

**ID:** `litespec/core/edges#match-a-screens-offers-to-its-calls`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen's `offers` MUST equal the use cases its route reaches through the generated client.

**Rationale:** `offers` is the recorded promise of what a person can start on the page, and the calls are what the page actually does. When the two differ, the page either gained a capability nobody recorded or the record promises one the page dropped. Comparing them on every run keeps the record honest without anyone re-reading the page.

**Example:** Claimwick's expense list offers `use-case/spending.list-expenses`, and its route calls `ListExpenses` through the generated client.

**Checked by:** `litespec model check` reports `screen-offers-drift` (level S) for a screen whose `offers` and computed calls differ, naming each use case on one side only.

### Limit Each Screen to Steps Its Use Cases Serve

**ID:** `litespec/core/edges#limit-each-screen-to-steps-its-use-cases-serve`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen's `serves` MUST name only flow steps that a use case it offers serves through a browser or device entry point.

**Rationale:** A person achieves a flow step through use cases, and reaches those use cases on a screen. Naming the step on the screen lets the book say where each step happens. Checking it against the offered use cases catches a screen that claims a step its use cases do not carry.

**Example:** Claimwick's expense form serves `step/submit-and-reimburse.submit-expense` because it offers `use-case/spending.submit-expense`, which serves that step.

**Checked by:** `litespec model check` reports `screen-serves-drift` (level G) for a served step no offered use case serves.

### Give Every Browser Step a Screen

**ID:** `litespec/core/edges#give-every-browser-step-a-screen`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Every flow step a use case serves through a browser or device entry point MUST be served by at least one screen.

**Rationale:** A step a person achieves in a browser happens on some page. A step that no screen serves is either carried by a page nobody recorded or not reachable at all, and then the book cannot tell the reader where it happens. Two findings sit side by side: `step-use-case-missing` names a step no use case serves, and `step-screen-missing` names a step that a browser or device use case serves and no screen serves.

**Example:** Claimwick's `step/submit-and-reimburse.submit-expense` is served by the expense form.

**Checked by:** `litespec model check` reports `step-screen-missing` (level G) for a step a browser or device use case serves that no screen serves.

### Write the Sentence a Person Reads on Each Refusal

**ID:** `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`

**Strength:** required

**Applies to:** artifact: record:screen, record:result

**Statement:** Every refusal result of a use case a screen offers MUST carry the sentence a person reads in `sentence`.

**Rationale:** A refusal a person can trigger on a page is part of what the page says to them. Without a sentence, the person sees a code or a generic error. The sentence sits on the result in the product's language, with one sentence per language beside it in `sentences`, the way glossary terms carry `labels`. That makes the wording a person reads record data the check reads beside the refusal. The sentence is required only where a screen can reach the refusal, so refusals no person meets need none. Proposed by [decision/0089-specify-the-ui-as-a-blueprint](decision/0089-specify-the-ui-as-a-blueprint).

**Example:** Claimwick's expense form offers `use-case/spending.submit-expense`, whose `spending.receipt-missing` refusal result carries "Attach the receipt for this expense, then submit again." in `sentence`.

**Checked by:** `litespec model check` reports `refusal-sentence-missing` (level A) for a refusal result of a use case a screen offers that carries no `sentence`.

### Serve Static Files as Served Content

**ID:** `litespec/core/edges#serve-static-files-as-served-content`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A static script or legal text MUST be served through a browser entry point's `servedContent` list, with its path, content type, cache control, and versioning, never as a use case.

**Rationale:** A static file modeled as a use case invents behavior where none exists: results, scenarios, and handlers for bytes that are served unchanged. Serving it as content keeps content apart from behavior.

**Reviewed for:** Is every static script and legal text served through a browser entry point's `servedContent` list, with its path, content type, cache control, and versioning? Check the static files beside the `servedContent` entries.

### Describe Every External System Operation

**ID:** `litespec/core/edges#describe-every-external-system-operation`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** Each external system MUST describe its operations with action, timeout, timeout handling, response, and cache.

**Rationale:** Without a declared shape, an outside call fails unpredictably: timeouts hang, retries double-charge, and responses fail to parse. Describing each operation with its action of `send` or `receive`, its timeout with its timeout handling, its response, and its cache states the coupling beside the system. The `retries` prose states the retry behavior in words beside `onTimeout`, while the retry count lives on the calling edge in `onFailure.retry.maxAttempts`.

**Checked by:** `litespec model check` reports `record-invalid` for an external system operation without action, timeout, timeout handling, response, or cache.

### State the Outcome of Every Outside Call

**ID:** `litespec/core/edges#state-the-outcome-of-every-outside-call`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** Each external-system operation MUST separate its `rejected`, `accepted`, and `unknown` outcomes, with the `unknown` case naming its handling in `onUnknown`.

**Rationale:** An outside call that ends unknown is neither done nor safe to repeat: a timeout after the provider charged double-charges on retry, while a drop after no charge loses the act. Separating the three outcomes states what each ending means, and naming the unknown handling in `onUnknown` states how the product recovers: `dedupe-key` for a provider idempotency key, `reconcile` for a later comparison, or `retry-safe` for a call the receiver tolerates twice.

**Example:** Claimwick's payout-bank charge operation names `rejected` for a refused charge, `accepted` for a queued payout, and `unknown` with `onUnknown: dedupe-key` for a timeout retried with the provider key.

**Checked by:** `litespec model check` reports `record-invalid` for an external-system operation without separated `rejected`, `accepted`, and `unknown` outcomes or with an `unknown` case naming no `onUnknown` handling.

### Resolve Late Arrivals Against Owner-Written Facts

**ID:** `litespec/core/edges#resolve-late-arrivals-against-owner-written-facts`

**Strength:** advisory

**Applies to:** artifact: record:use-case, record:read-model

**Statement:** Each module that acts on a fact another module owns SHOULD resolve late arrivals against facts the owning modules wrote themselves, or confirm with the owner before acting.

**Deviation:** A fact whose owner answers every read through its public query needs no separate resolution, since each act already confirms.

**Rationale:** The owner's event can arrive after the reader acts, so an arrival the reader takes at face value lands in the wrong period. Comparing moments each owner wrote, such as an approval moment against a recorded close, classifies each arrival without a lock. Where moments cannot settle the act, confirming with the owner before committing keeps the decision on current truth, as `litespec/core/automation#decide-current-truth-in-the-owning-module` states.

**Example:** Claimwick counts an expense approved after the recorded March close toward April, even when its event arrives with late March events.

**Reviewed for:** When one module acts on a fact another module owns, which owner-written moment or public query settles a late arrival? Check the timestamps and consumer ports beside each cross-module read.

### State Every Boundary Check as a Rule of Its Module

**ID:** `litespec/core/edges#state-every-boundary-check-as-a-rule-of-its-module`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A module that confirms work for another module MUST state the receiving module's preconditions as its own rules at its boundary.

**Rationale:** Each module refuses at its own boundary: the confirming module states the receiving module's preconditions as its own rules and names the receiving rules in prose, and the receiving module checks again when it acts. The second check guards against changes between the two acts, so the pair is two boundaries, not one rule written twice. A rule governs another module's records only through a declared dependency, as `litespec/core/model#connect-across-modules-only-through-declared-dependencies` states; without one, each module states the check at its own boundary.

**Example:** Claimwick's payout module confirms each transfer before calling the bank. It states its own rule that a transfer settles only a payable reimbursement, naming spending's reimbursed-once rule in prose, and spending checks the rule again when it posts the settlement.

**Reviewed for:** Does every module confirming work for another state the receiving preconditions as its own rules? Check the rules at each module boundary against the receiving module's rules.

## Conventions

None.
