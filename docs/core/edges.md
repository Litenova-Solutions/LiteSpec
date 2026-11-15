# Edges and Screens

**Stack:** any
**Concern:** records

## Intent

Edges and Screens states how the product meets the outside: the integration events, notifications, entry points, screens, and external systems. Agents write these records from Intent, and code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Integration Events

An integration event is a published fact crossing the bounded context in CloudEvents form. Its direction is `out` for facts the product emits and `in` for facts it accepts. Readers place one of two trusts in it: `trigger-only`, where the event wakes a read of the source, or `payload`, where the fields are fit to act on. It carries `summary` and `delivery`: what the event announces with who acts on it, and how the event reaches its readers. An outbound event names in `translates` the one domain event it carries outward, and it is published by the use cases whose `raises` holds that domain event, so it has exactly one publishing module. No record declares the publishing as an effect, and no user code publishes it: the pipeline step that stages raised domain events stages each integration event that translates them, built from the domain event alone. A read model in another module may copy its payload fields, as the [Automation page](automation.md#read-models) states. Every integration event names in `through` the external system it travels through. Consumers, Compatibility and Example live in the explanation file beside the record file.

| Field | Meaning |
|:---|:---|
| `direction` | `out` or `in`. |
| `eventType`, `source`, `subject` | The CloudEvents type, source, and what the subject identifies. |
| `payload` | `{ name, meaning, type, of, many, required, classification }` entries, with the field types and the list flag use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states. |
| `dataschema` | The URL the payload follows, optional. |
| `trust` | `trigger-only` or `payload`. |
| `correlation` | How an inbound event is matched, inbound only. |
| `translates` | The domain event it carries outward, required on an outbound event; the event's publishers are the use cases raising it. |
| `requires` | The permission reading it, outbound only, optional. |
| `through` | The `external-system/...` the event travels through, required. |
| `arrivesThrough` | How an inbound event arrives, required on an inbound event: an `entry-point/...` of kind `webhook` or an `external-system/...` whose broker or stream the module subscribes to. |

### Notifications

A notification is one person-facing message with its template, audience, channel and consent. It is a message to a person; a call to a system is an outbound integration event or an external-system operation, not a notification. It names in `deliveredThrough` the external system delivering it. Consent is recorded data: an aggregate holds it, use cases grant and withdraw it, and a rule requires it. Suppression lives on the notification in `suppressedWhen`, and suppression scenarios saying the message sends nothing are first class. It carries `summary`, and Content, Delivery and Example live in the explanation file beside the record file.

| Field | Meaning |
|:---|:---|
| `template`, `params`, `locale` | The template key, its parameters, and where the language comes from. |
| `audience`, `channel` | The actors addressed and the channels: `email`, `sms`, `push`. |
| `deliveredThrough` | The `external-system/...` delivering the message, required. |
| `requiresConsent` | The `rule/...` requiring consent for the send, where consent applies. |
| `frequency`, `dedupKey` | `once` or `per-attempt`, and the dedup template over the trigger payload. |
| `suppressedWhen` | The suppression condition, optional. |
| `claims` | `{ claim, requirement }` entries for what the message says. |
| `fulfills` | The requirements carried. |

### Entry Points

An entry point is one way callers reach the product from outside. Each entry point declares its `auth`, as the [Access page](access.md) states. Served content is a static script or legal text with its path, content type, cache control, and versioning: content served, not a use case run.

| Kind | Meaning |
|:---|:---|
| `browser`, `api`, `webhook`, `device`, `cli`, `mcp` | A browser app, a public interface, an inbound hook, a device, a command line, or a model context channel. |
| `served-content` | Static content with `path`, `contentType`, `cacheControl`, and `versioning`. |

An entry point people use states its `density`, one of `public-light`, `application-balanced`, or `admin-dense`, and its `voice`: a sentence or two on how the product addresses that entry point's audience. Density sets how much each screen shows at once, and voice sets how the product speaks to its people.

The optional `protocol` block belongs to the `devices` extension. The [access page](access.md) states actors, roles, and permissions.

### Screens

A screen is one page a person sees through a `browser` or `device` entry point. Its record lives at `behavior/entry-points/<entry-point>/screens/<entry-point>.<name>/<entry-point>.<name>.yaml` with the identifier `screen/<entry-point>.<name>`, such as `screen/claimwick-app.expense-list`. Its folder states its entry point, so it writes no up-link. It names its `address` (the route template its stack uses: a web path, a Blazor `@page` template, or an Android destination), the use cases it `offers`, and the flow steps it `serves`. An overlay with no address of its own names the screen it opens on in `host` instead. A screen carries exactly one of `address` and `host`, as [Give Every Screen an Address or a Host](#give-every-screen-an-address-or-a-host) states. Its `summary` states what a person can do on the page. A navigation screen is one that offers no use case and says so in its `summary`.

A screen states what a person can do there; what they see follows from it. The results a screen shows come from the use cases it offers, its outgoing links come from its route, and the sentence shown for a refusal is joined by the refusal's `code`, so none of those are written on the screen. Intent never names a screen: a flow step says what a person achieves, and the book adds the screen that carries the step as context.

The route entry that renders a screen carries its mark, in the form its stack page states, naming `screen/...`. The builder computes `calls` from the route entry: it follows imports within the route's own folder and the modules folder, so a module the route imports and every module that module composes count, collecting generated-client operation calls (a query helper the client generator emits for an operation, such as its options, query key or mutation, counts as a call to that operation), and maps each operation through the endpoint's mark to its use case. It follows neither the frame nor shared packages, so a call a shared component makes is not the screen's offer, while a call a composed use-case module makes is. An operation that resolves to two use cases is a reader diagnostic rather than a silent pick. The builder compares the computed `calls` with `offers`. A flow step is exercised when a passing test proved a scenario of a use case that serves the step.

### External Systems

An external system is one system outside the bounded context the product exchanges messages with. It stands alone and names no actor. An integration that calls the product is an external system calling through an API entry point, and each operation declares its direction, timeout, response, and cache, and may state its retry behavior in `retries` prose. The retry count lives on the calling edge in `onFailure.retry.maxAttempts`. Its erasure position says how personal data leaves it: `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "unsupported", "reason": "..." }`, `manual`, or `not-applicable` with its acceptance. Its `summary` states what the system does for the product and who operates it. It names in `processes` the aggregates and data classes it holds, as `{ aggregate: aggregate/<m>.<a>, dataClass: <name> }` entries.

| Field | Meaning |
|:---|:---|
| `operations` | `{ name, direction, timeout, onTimeout, retries, response, cache, rejected, accepted, unknown }` entries; direction is `out` or `in`, `retries` is optional prose. The outcome handling separates `rejected`, `accepted` and `unknown`; the `unknown` case names its handling in `onUnknown` as `dedupe-key`, `reconcile`, or `retry-safe`. |
| `erasure` | `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "unsupported" | "manual" | "not-applicable", "reason": "..." }` with `basedOn`. `unsupported` means the operator cannot remove the data, `manual` means people remove it outside the product, and `not-applicable` means the system holds no personal data after the product sends it. |
| `processes` | The aggregates and data classes the system holds, as `{ aggregate, dataClass }` entries. |

## Standards

### Describe Every Integration Event for Its Direction

**ID:** `litespec/core/edges#describe-every-integration-event-for-its-direction`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each integration event MUST declare its direction, its CloudEvents identity, its typed payload, the trust its readers place in it, and the external system it travels through in `through`.

**Rationale:** A published fact without a direction is read both ways: an outbound notice is answered as an order, an inbound claim is trusted as law. The direction, the CloudEvents type, source, and subject, the payload with classifications and types, the trust of `trigger-only` or `payload`, and the external system in `through` state what the fact is, how far readers follow it, and what it travels through. Each payload field carries the field types use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states, and a money field carries an amount with an ISO 4217 currency, as the [Record Language](language.md) states. An event may name the `dataschema` URL its payload follows. An inbound event also declares its `correlation` and its `arrivesThrough`; an outbound event declares its `translates` domain event and its optional `requires`.

**Checked by:** `litespec model check` reports `record-invalid` for an integration event without direction, CloudEvents identity, typed payload, trust, or `through`.

### Publish Every Outbound Integration Event from the Event It Translates

**ID:** `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`

**Strength:** required

**Applies to:** artifact: record:integration-event, record:use-case

**Statement:** Each outbound integration event MUST name in `translates` the domain event it carries, and is published only from that domain event, by the pipeline step that stages raised events.

**Rationale:** An integration event built by a handler from whatever the aggregate holds after the act publishes state the act never announced, and a second handler can publish the same event with a different payload. Deriving it from the one domain event it translates keeps every published field a field of the fact, and the publishing use cases follow from `raises` with no second declaration to drift. The domain event carries every field its integration events publish, as typed fields. A publish call in user code is an edge no record can declare, so the stack reader reports it. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** `integration-event/expense-submitted` translates `domain-event/expense.submitted`, so `use-case/spending.submit-expense`, which raises that event, publishes it; its handler publishes nothing.

**Checked by:** `litespec model check` reports `record-invalid` for an outbound integration event without `translates`, `integration-event-field-uncarried` (level G) for a payload field its translated domain event does not carry, and `effect-undeclared` for an event a handler, event handler or job publishes itself; `dotnet build` reports `LS2022` for an event published by user code.

### Name Published Events in the Domain's Words

**ID:** `litespec/core/edges#name-published-events-in-the-domains-words`

**Strength:** advisory

**Applies to:** artifact: record:integration-event

**Statement:** Each outbound integration event SHOULD be named for the business fact, without internal words.

**Departure:** An event kept for an internal outbox fact its readers already consume keeps its name until its readers move.

**Rationale:** An integrator reads the published name as the business fact; `staged`, `dispatch` or `outbox` describe the machinery that delivered it. A name carrying the fact travels across the boundary unchanged. The internal words are `staged`, `dispatch`, `dispatched`, `outbox`, `handler`, `queued`, `worker`, and `job`.

**Example:** `expense-reimbursement-requested` names the requested reimbursement; `expense-reimbursement-staged` names the staging step and is reported.

**Checked by:** `litespec model check` reports `integration-event-name-invalid` (level G) for an outbound event named with an internal word.

### Name How Every Inbound Integration Event Arrives

**ID:** `litespec/core/edges#name-how-every-inbound-integration-event-arrives`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each inbound integration event MUST name how it arrives in `arrivesThrough`.

**Rationale:** An inbound fact with no stated arrival is accepted anywhere: each handler invents its own intake, and trust scatters across the code. Naming the arrival gives each inbound fact one entry point with one auth, where trust and correlation hold: the `webhook` entry point a provider calls, or the external system whose broker or stream the module subscribes to. Settled by [decision/0075-draw-the-boundary-around-outside-traffic](decision/0075-draw-the-boundary-around-outside-traffic).

**Example:** The receipt-scan-available event names the scanning provider webhook entry point in `arrivesThrough`; the payout-settled event names the payout bank stream the spending module subscribes to.

**Checked by:** `litespec model check` reports `record-invalid` for an inbound integration event without `arrivesThrough` naming a webhook entry point or a subscribed external system.

### Describe Every Notification with Audience Channel and Consent

**ID:** `litespec/core/edges#describe-every-notification-with-audience-channel-and-consent`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each notification MUST declare its template, audience, channel, consent rule, frequency, dedup key, parameters, locale, and delivering system.

**Rationale:** A message without declared audience and consent is sent on hope: the wrong actors receive it, the channel ignores consent, repeats annoy. Declaring the template with its parameters and locale, the audience, the channel of `email`, `sms`, or `push`, the consent rule in `requiresConsent` naming the rule requiring consent, the frequency of `once` or `per-attempt`, the dedup key, and the delivering system in `deliveredThrough` states the message beside its promise. A notification is a message to a person; a webhook to a system is an outbound integration event. Suppression lives on the notification in `suppressedWhen`, and suppression scenarios show the message sending nothing. A use case needing consent names the same rule in its `requiresConsent`. The `notification-effect-missing` check reports a notification no use case, reaction, or job sends.

**Checked by:** `litespec model check` reports `notification-effect-missing` for a notification no use case, reaction, or job sends, and fails a notification without its declared fields.

### Keep Notification Templates and the Template Store One Set

**ID:** `litespec/core/edges#keep-notification-templates-and-the-template-store-one-set`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** The `template` keys the notification records declare and the keys the project's template store holds MUST be the same set.

**Rationale:** The template key is the one join between a notification's promise and the text a person receives. A declared key the store lacks fails at send time, and a stored template no notification declares is text nobody reviewed. The store is the stack's own file, so the check reads it where it is instead of copying the keys into a record. A notification whose body the sender composes at send time declares `template: none` and is left out.

**Example:** Claimwick's `notification/spending.receipt-reminder` declares `template: receipt-reminder-manager`, and the template store holds `receipt-reminder-manager`.

**Checked by:** `litespec model check` reports `template-unimplemented` (level S) for a declared key the store the stack page names lacks, and `template-unrecorded` (level S) for a stored key no notification declares, in a project whose stack configuration names a template store.

### Use Every Declared Template Parameter

**ID:** `litespec/core/edges#use-every-declared-template-parameter`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Every parameter a notification declares in `params` MUST appear as a placeholder in at least one of its template's texts.

**Rationale:** A declared parameter is part of the message's promise: the order number in a confirmation, the name in a reminder. A parameter the template never uses is a promise the message does not keep, and it usually means the template was rewritten without the record.

**Example:** `notification/spending.receipt-reminder` declares the parameter `submitterName`, and the `receipt-reminder-manager` template uses `{{SubmitterName}}`.

**Checked by:** `litespec model check` reports `template-param-drift` (level S) for a declared parameter whose placeholder, in the case the stack page states, appears in none of its template's texts.

### Name Each Template for Its Subject, Event and Audience

**ID:** `litespec/core/edges#name-each-template-for-its-subject-event-and-audience`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each notification template key MUST read `<subject>-<event>-<audience>`, with the audience name the record declares in `audience`.

**Rationale:** A key naming its subject, its event and its audience tells each reader what the message is about and who receives it; a key naming none of the three hides both.

**Example:** `receipt-reminder-manager` names the receipt, the reminder and the manager audience; `email-change-confirmation` names no audience and is reported.

**Checked by:** `litespec model check` reports `template-key-invalid` (level A) for a template key ending in no name the notification declares in `audience`.

### Claim Every Notification Promise from a Requirement

**ID:** `litespec/core/edges#claim-every-notification-promise-from-a-requirement`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** Each claim a notification makes MUST cite its requirement.

**Rationale:** A message promising what no requirement holds is a promise no requirement states: the text assures a refund the policy never states. Citing the requirement behind each claim keeps every assured sentence a carried promise.

**Reviewed for:** Which notification claims lack a cited requirement? Look at the claims beside each notification.

### Give Each Entry Point a Kind

**ID:** `litespec/core/edges#give-each-entry-point-a-kind`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST declare its kind and its `auth`.

**Rationale:** An entry point with no kind is entered any way: a webhook is called as a page, a device as a browser. Declaring the kind of `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, or `served-content` with its auth states how callers arrive and prove themselves.

**Checked by:** `litespec model check` reports `record-invalid` for an entry point without `kind` or `auth`.

### State the Density and Voice of Each Entry Point

**ID:** `litespec/core/edges#state-the-density-and-voice-of-each-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each browser or device entry point MUST state its density in `density` and how the product addresses its audience in `voice`.

**Rationale:** Each audience reads its own screens: a public page shows little, an application screen balances work and guidance, and an admin screen packs dense rows. One density per entry point keeps every screen of one audience at the same weight, and one voice keeps the product addressing that audience the same way on each screen. The `density` is `public-light`, `application-balanced`, or `admin-dense`, and the `voice` is a sentence or two. Both sit on the entry point record because the audience is what an entry point models. Proposed by [decision/0089-specify-the-ui-as-a-blueprint](decision/0089-specify-the-ui-as-a-blueprint).

**Example:** The Claimwick employee app entry point states `application-balanced` in `density`, and its `voice` reads "Claimwick addresses the employee plainly and names the next step on every screen."

**Checked by:** `litespec model check` reports `record-invalid` for a browser or device entry point without `density` or `voice`, and `field-invalid` for a `density` outside public-light, application-balanced, and admin-dense.

### Give Every Page Its Screen Record

**ID:** `litespec/core/edges#give-every-page-its-screen-record`

**Strength:** required

**Applies to:** artifact: route, record:screen; layer: implementation; trace: yes

**Statement:** Every route a browser or device entry point serves MUST be the implementation of exactly one screen record.

**Rationale:** A page is where a person meets the product, and an unrecorded page offers use cases nobody reviewed as a page: what it lets someone start, which flow step it carries, which refusal it shows. One record per page, with one mark on the route, makes the page a reviewable promise and lets every other link (calls, results, flow coverage) be computed from it.

**Example:** Claimwick's web route `/expenses` carries `implements: "screen/claimwick-app.expense-list"`, and `behavior/entry-points/claimwick-app/screens/claimwick-app.expense-list/claimwick-app.expense-list.yaml` offers `use-case/spending.list-expenses`.

**Checked by:** `litespec model check` reports `screen-unimplemented` (level S) for a screen no route marks, and `behavior-unrecorded` (level S) for a route with no mark or a mark naming no screen.

### Give Every Screen an Address or a Host

**ID:** `litespec/core/edges#give-every-screen-an-address-or-a-host`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST carry exactly one of `address` and `host`.

**Rationale:** A route screen without an address is unreachable, and an overlay with one is two pages fighting over a route. An address places a screen on its route; a host places an overlay on the screen it opens on. Exactly one of the two leaves every page placed once.

**Example:** The expense list carries the address `/expenses`; its confirm overlay names the expense list in `host` instead.

**Checked by:** `litespec model check` reports `record-invalid` for a screen without exactly one of `address` and `host`.

### Match a Screen's Offers to Its Calls

**ID:** `litespec/core/edges#match-a-screens-offers-to-its-calls`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen's `offers` MUST equal the use cases its route reaches through the generated client.

**Rationale:** `offers` is the recorded promise of what a person can start on the page, and the calls are what the page really does. When the two differ, either the page grew a capability nobody recorded or the record promises one the page dropped. Comparing them on every run keeps the record honest without anyone re-reading the page.

**Example:** The expense list offers `use-case/spending.list-expenses`, and its route calls `ListExpenses` through the generated client.

**Checked by:** `litespec model check` reports `screen-offers-drift` (level S) for a screen whose `offers` and computed calls differ, naming each use case on one side only.

### Serve Only the Steps a Screen's Use Cases Serve

**ID:** `litespec/core/edges#serve-only-the-steps-a-screens-use-cases-serve`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen's `serves` MUST name only flow steps that a use case it offers serves through a browser or device entry point.

**Rationale:** A flow step is achieved through use cases, and a person reaches those use cases on a screen. Naming the step on the screen lets the book say where each step happens, and checking it against the offered use cases finds a screen claiming a step its use cases do not carry.

**Example:** The expense form serves `flow/submit-and-reimburse#s1` because it offers `use-case/spending.submit-expense`, which serves that step.

**Checked by:** `litespec model check` reports `screen-serves-drift` (level G) for a served step no offered use case serves.

### Give Every Browser Step a Screen

**ID:** `litespec/core/edges#give-every-browser-step-a-screen`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Every flow step a use case serves through a browser or device entry point MUST be served by at least one screen.

**Rationale:** A step a person achieves in a browser happens on some page. A step no screen serves is either carried by a page nobody recorded or not reachable at all, and the book cannot tell the reader where it happens. The pair sits side by side: `step-use-case-missing` names a step no use case serves, and `step-screen-missing` names a step a browser or device use case serves that no screen serves.

**Example:** `flow/submit-and-reimburse#s1` is served by the expense form.

**Checked by:** `litespec model check` reports `step-screen-missing` (level G) for a step a browser or device use case serves that no screen serves.

### Write the Sentence a Person Reads on Each Refusal

**ID:** `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`

**Strength:** required

**Applies to:** artifact: record:screen, record:result

**Statement:** Every refusal result of a use case a screen offers MUST carry the sentence a person reads in `sentence`.

**Rationale:** A refusal a person can trigger on a page is part of what the page says to them. Without a sentence, the person sees a code or a generic error. The sentence sits on the result in the product's language, with one sentence per language beside it in `sentences` the way glossary terms carry `labels`, so the wording a person reads is record data the check reads beside the refusal. The sentence is required only where a screen can reach the refusal, which avoids demanding sentences for refusals no person meets. Proposed by [decision/0089-specify-the-ui-as-a-blueprint](decision/0089-specify-the-ui-as-a-blueprint).

**Example:** The expense form offers `use-case/spending.submit-expense`, whose `spending.receipt-missing` refusal result carries "Attach the receipt for this expense, then submit again." in `sentence`.

**Checked by:** `litespec model check` reports `refusal-sentence-missing` (level A) for a refusal result of a use case a screen offers that carries no `sentence`.

### Serve Static Content Through Served Content

**ID:** `litespec/core/edges#serve-static-content-through-served-content`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A static script or legal text MUST be served content with its path, content type, cache control, and versioning, never a use case.

**Rationale:** A static file modeled as a use case invents behavior where none exists: results, scenarios, and handlers for bytes served unchanged. Serving it as content with its path, content type, cache control, and versioning keeps content apart from behavior.

**Reviewed for:** Which static scripts or legal texts sit outside served content, or lack path, content type, cache control, or versioning? Look at the static files beside the served-content entries.

### Describe Every External System Operation

**ID:** `litespec/core/edges#describe-every-external-system-operation`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** Each external system MUST describe its operations with direction, timeout, timeout handling, response, and cache.

**Rationale:** An outside call without a declared shape fails by surprise: timeouts hang, retries double-charge, responses unparse. Describing each operation with its direction of `out` or `in`, its timeout with its timeout handling, its response, and its cache states the coupling beside the system. The `retries` prose states the retry behavior in words beside `onTimeout`. The retry count lives on the calling edge in `onFailure.retry.maxAttempts`. The system stands alone and names no actor.

**Checked by:** `litespec model check` reports `record-invalid` for an external system operation without direction, timeout, timeout handling, response, or cache.

### State the Outcome of Every Outside Call

**ID:** `litespec/core/edges#state-the-outcome-of-every-outside-call`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** Each external-system operation MUST separate its `rejected`, `accepted` and `unknown` outcomes, with the `unknown` case naming its handling in `onUnknown`.

**Rationale:** An outside call ending unknown is neither done nor safe to repeat: a timeout after the provider charged double-charges on retry, while a drop after no charge loses the act. Separating the three outcomes states what each ending means, and naming the unknown handling states how the product recovers: `dedupe-key` for a provider idempotency key, `reconcile` for a later comparison, or `retry-safe` for a call the receiver tolerates twice.

**Example:** The payout-bank charge operation names `rejected` for a refused charge, `accepted` for a queued payout, and `unknown` with `onUnknown: dedupe-key` for a timeout, retried with the provider key.

**Checked by:** `litespec model check` reports `record-invalid` for an external-system operation without separated `rejected`, `accepted` and `unknown` outcomes or with an `unknown` case naming no `onUnknown` handling.

## Conventions

None.
