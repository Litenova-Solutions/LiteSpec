# Edges and Screens

## Intent

Edges and Screens states how the product meets the outside: the integration events, notifications, entry points, screens, and external systems. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Integration Events

An integration event is a published fact crossing the bounded context in CloudEvents form. Its direction is `out` for facts the product emits and `in` for facts it accepts. Readers place one of two trusts in it: `trigger-only`, where the event wakes a read of the source, or `payload`, where the fields are fit to act on.

| Field | Meaning |
|:---|:---|
| `direction` | `out` or `in`. |
| `eventType`, `source`, `subject` | The CloudEvents type, source, and what the subject identifies. |
| `payload` | `{ name, meaning, type, of, many, required, classification }` entries, with the field types and the list flag use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states. |
| `dataschema` | The URL the payload follows, optional. |
| `trust` | `trigger-only` or `payload`. |
| `correlation` | How an inbound event is matched, inbound only. |
| `translates` | The domain event it carries outward, outbound only. |
| `addresses` | The permission reading it, outbound only, optional. |

### Notifications

A notification is one person-facing message with its template, addresses, and channel. Suppression lives on the notification in `suppressedWhen`, and suppression scenarios saying the message sends nothing are first class.

| Field | Meaning |
|:---|:---|
| `template`, `params`, `locale` | The template key, its parameters, and where the language comes from. |
| `addresses`, `channel` | The actors addressed and the channels: `email`, `sms`, `push`, `webhook`. |
| `consent` | The consent rule, such as SMS only with consent. |
| `frequency`, `dedupKey` | `once` or `per-attempt`, and the dedup template over the trigger payload. |
| `suppressedWhen` | The suppression condition, optional. |
| `claims` | `{ claim, requirement }` entries for what the message says. |
| `fulfills` | The requirements carried. |

### Entry Points

An entry point is one door from outside: a way callers reach the product. Each door declares its `auth`, as the [Access page](access.md) states. Served content is a static script or legal text with its path, content type, cache control, and versioning: content served, not a use case run.

| Kind | Meaning |
|:---|:---|
| `browser`, `api`, `webhook`, `device`, `cli`, `mcp` | A browser app, a public interface, an inbound hook, a device, a command line, or a model context channel. |
| `served-content` | Static content with `path`, `contentType`, `cacheControl`, `versioning`, and `fulfills`. |

The optional `protocol` block belongs to the `devices` extension. The [access page](access.md) states actors, roles, and permissions.

### Screens

A screen is one page a person sees through a `browser` or `device` entry point. Its record lives at `docs/behavior/screens/<entry-point>.<name>.md` with the identifier `screen/<entry-point>.<name>`, such as `screen/claimwick-app.expense-list`. It names its `entryPoint`, its `address` (the route template its stack uses: a web path, a Blazor `@page` template, or an Android destination), the use cases it `offers`, the journey steps it `serves`, and optionally the `floorplan` it is composed from. A floorplan is a named arrangement of regions the screen fills. An overlay with no address of its own names the screen it opens on in `host` instead. A screen carries exactly one of `address` and `host`, as [Give Every Screen an Address or a Host](#give-every-screen-an-address-or-a-host) states. A navigation screen is one that offers no use case and says so in its summary.

A screen states what a person can do there; what they see follows from it. The results a screen shows come from the use cases it offers, its outgoing links come from its route, and the sentence shown for a refusal is joined by the refusal's `code`, so none of those are written on the screen. Intent never names a screen: a journey step says what a person achieves, and the book adds the screen that carries the step as context outside the approval hash.

The route entry that renders a screen carries its mark, in the form its stack page states, naming `screen/...`. The builder computes `calls` from the route entry: it follows imports within the route's own folder and the modules folder, so a module the route imports and every module that module composes count, collecting generated-client operation calls (a query helper the client generator emits for an operation, such as its options, query key or mutation, counts as a call to that operation), and maps each operation through the endpoint's mark to its use case. It follows neither the frame nor shared packages, so a call a shared component makes is not the screen's offer, while a call a composed use-case module makes is. An operation that resolves to two use cases is a reader diagnostic rather than a silent pick. The builder compares the computed `calls` with `offers`. A journey step is exercised when a passing test proved a scenario of a use case that serves the step.

### External Systems

An external system is one system outside the bounded context the product exchanges messages with. It acts inside the product as one actor, and each operation declares its direction, timeout, response, and cache, and may state its retry behavior in `retries` prose. The retry count lives on the calling edge in `onFailure.retry.maxAttempts`. Its erasure position says how personal data leaves it: `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "impossible", "reason": "..." }` with its acceptance.

| Field | Meaning |
|:---|:---|
| `actor` | The actor the system acts as. |
| `operations` | `{ name, direction, timeout, onTimeout, retries, response, cache }` entries; direction is `out` or `in`, `retries` is optional prose. |
| `erasure` | `{ "mode": "call", "operation": "external-system/<name>#<operation>" }`, `{ "mode": "event", "event": "integration-event/<name>" }`, or `{ "mode": "impossible", "reason": "..." }` with `acceptedBy`. |

## Standards

### Describe Every Integration Event for Its Direction

**ID:** `litespec/core/edges#describe-every-integration-event-for-its-direction`

**Statement:** Each integration event MUST declare its direction, its CloudEvents identity, its typed payload, and the trust its readers place in it.

**Rationale:** A published fact without a direction is read both ways: an outbound notice is answered as an order, an inbound claim is trusted as law. The direction, the CloudEvents type, source, and subject, the payload with classifications and types, and the trust of `trigger-only` or `payload` state what the fact is and how far readers follow it. Each payload field carries the field types use-case inputs use, as `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` states, and a money field carries an amount with an ISO 4217 currency, as the [Record Language](language.md) states. An event may name the `dataschema` URL its payload follows. An inbound event also declares its `correlation`; an outbound event declares its `translates` domain event and its optional `addresses`.

**Checked by:** `litespec consumer` fails an integration event without direction, CloudEvents identity, typed payload, or trust.

### Publish Every Outbound Integration Event from Effects

**ID:** `litespec/core/edges#publish-every-outbound-integration-event-from-effects`

**Statement:** Each use case, reaction, or job emitting an integration event MUST declare it in its effects as `emits`.

**Rationale:** An emitted fact no edge declares is a hidden coupling: downstream readers depend on a promise the records never made, and the promise breaks without a diff. Declaring each emission as an `emits` effect states the coupling beside the behavior causing it. The `effect-undeclared` check reports an emitted event no record declares.

**Checked by:** `litespec model check` reports `effect-undeclared` for an emitted integration event no record declares in effects.

### Name Published Events in the Domain's Words

**ID:** `litespec/core/edges#name-published-events-in-the-domains-words`

**Statement:** Each outbound integration event SHOULD be named for the business fact, without internal words.

**Deviation:** An event kept for an internal outbox fact its readers already consume keeps its name until its readers move.

**Rationale:** An integrator reads the published name as the business fact; `staged`, `dispatch` or `outbox` describe the machinery that delivered it. A name carrying the fact travels across the boundary unchanged. The internal words are `staged`, `dispatch`, `dispatched`, `outbox`, `handler`, `queued`, `worker`, and `job`.

**Example:** `expense-reimbursement-requested` names the requested reimbursement; `expense-reimbursement-staged` names the staging step and is reported.

**Checked by:** `litespec model check` reports `integration-event-name-invalid` (level G) for an outbound event named with an internal word.

### Reach Every Inbound Integration Event Through a Webhook

**ID:** `litespec/core/edges#reach-every-inbound-integration-event-through-a-webhook`

**Statement:** Each inbound integration event MUST arrive through an entry point of kind `webhook`.

**Rationale:** An inbound fact with no door is accepted anywhere: each handler invents its own intake, and trust scatters across the code. Arriving through a webhook entry point gives each inbound fact one door with one auth, where trust and correlation hold.

**Reviewed for:** Which inbound integration events lack a webhook entry point as their door? Look at the inbound events beside the entry points.

### Describe Every Notification with Audience Channel and Consent

**ID:** `litespec/core/edges#describe-every-notification-with-audience-channel-and-consent`

**Statement:** Each notification MUST declare its template, addresses, channel, consent, frequency, dedup key, parameters, and locale.

**Rationale:** A message without declared addresses and consent is sent on hope: the wrong actors receive it, the channel ignores consent, repeats annoy. Declaring the template with its parameters and locale, the addresses, the channel, the consent rule, the frequency of `once` or `per-attempt`, and the dedup key states the message beside its promise. Suppression lives on the notification in `suppressedWhen`, and suppression scenarios show the message sending nothing. The `notification-sender-missing` check reports a notification no use case, reaction, or job sends.

**Checked by:** `litespec model check` reports `notification-sender-missing` for a notification no use case, reaction, or job sends; `litespec consumer` fails a notification without its declared fields.

### Keep Notification Templates and the Template Store One Set

**ID:** `litespec/core/edges#keep-notification-templates-and-the-template-store-one-set`

**Statement:** The `template` keys the notification records declare and the keys the project's template store holds MUST be the same set.

**Rationale:** The template key is the one join between a notification's promise and the text a person receives. A declared key the store lacks fails at send time, and a stored template no notification declares is text nobody reviewed. The store is the stack's own file, so the check reads it where it is instead of copying the keys into a record. A notification whose body the sender composes at send time declares `template: none` and is left out.

**Example:** Claimwick's `notification/spending.approval-reminder` declares `template: approval-reminder-manager`, and the template store holds `approval-reminder-manager`.

**Checked by:** `litespec model check` reports `template-unimplemented` (level S) for a declared key the store the stack page names lacks, and `template-unrecorded` (level S) for a stored key no notification declares, in a project whose stack configuration names a template store.

### Use Every Declared Template Parameter

**ID:** `litespec/core/edges#use-every-declared-template-parameter`

**Statement:** Every parameter a notification declares in `params` MUST appear as a placeholder in at least one of its template's texts.

**Rationale:** A declared parameter is part of the message's promise: the order number in a confirmation, the name in a reminder. A parameter the template never uses is a promise the message does not keep, and it usually means the template was rewritten without the record.

**Example:** `notification/spending.approval-reminder` declares the parameter `submitterName`, and the `approval-reminder-manager` template uses `{{SubmitterName}}`.

**Checked by:** `litespec model check` reports `template-param-drift` (level S) for a declared parameter whose placeholder, in the case the stack page states, appears in none of its template's texts.

### Name Each Template for Its Subject, Event and Audience

**ID:** `litespec/core/edges#name-each-template-for-its-subject-event-and-audience`

**Statement:** Each notification template key MUST read `<subject>-<event>-<audience>`, with the audience name the record declares in `addresses`.

**Rationale:** A key naming its subject, its event and its audience tells each reader what the message is about and who receives it; a key naming none of the three hides both.

**Example:** `approval-reminder-manager` names the approval, the reminder and the manager audience; `email-change-confirmation` names no audience and is reported.

**Checked by:** `litespec model check` reports `template-key-invalid` (level A) for a template key ending in no name the notification declares in `addresses`.

### Claim Every Notification Promise from a Requirement

**ID:** `litespec/core/edges#claim-every-notification-promise-from-a-requirement`

**Statement:** Each claim a notification makes MUST cite its requirement.

**Rationale:** A message promising what no requirement holds is a promise nobody approved: the text assures a refund the policy never states. Citing the requirement behind each claim keeps every assured sentence a carried promise.

**Reviewed for:** Which notification claims lack a cited requirement? Look at the claims beside each notification.

### Give Each Entry Point a Kind

**ID:** `litespec/core/edges#give-each-entry-point-a-kind`

**Statement:** Each entry point MUST declare its kind and its `auth`.

**Rationale:** A door with no kind is entered any way: a webhook is called as a page, a device as a browser. Declaring the kind of `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, or `served-content` with its auth states how callers arrive and prove themselves.

**Checked by:** `litespec consumer` fails an entry point without `kind` or `auth`.

### Give Every Page Its Screen Record

**ID:** `litespec/core/edges#give-every-page-its-screen-record`

**Statement:** Every route a browser or device entry point serves MUST be the implementation of exactly one screen record.

**Rationale:** A page is where a person meets the product, and an unrecorded page offers use cases nobody reviewed as a page: what it lets someone start, which journey step it carries, which refusal it shows. One record per page, with one mark on the route, makes the page a reviewable promise and lets every other link (calls, results, journey coverage) be computed from it.

**Example:** Claimwick's web route `/expenses` carries `implements: "screen/claimwick-app.expense-list"`, and `docs/behavior/screens/claimwick-app.expense-list.md` offers `use-case/spending.list-expenses`.

**Checked by:** `litespec model check` reports `screen-unimplemented` (level S) for a screen no route marks, and `behavior-unrecorded` (level S) for a route with no mark or a mark naming no screen.

### Give Every Screen an Address or a Host

**ID:** `litespec/core/edges#give-every-screen-an-address-or-a-host`

**Statement:** Each screen MUST carry exactly one of `address` and `host`.

**Rationale:** A route screen without an address is unreachable, and an overlay with one is two pages fighting over a route. An address places a screen on its route; a host places an overlay on the screen it opens on. Exactly one of the two leaves every page placed once.

**Example:** The expense list carries the address `/expenses`; its confirm overlay names the expense list in `host` instead.

**Checked by:** `litespec consumer` fails a screen without exactly one of `address` and `host`.

### Match a Screen's Offers to Its Calls

**ID:** `litespec/core/edges#match-a-screens-offers-to-its-calls`

**Statement:** A screen's `offers` MUST equal the use cases its route reaches through the generated client.

**Rationale:** `offers` is the reviewed promise of what a person can start on the page, and the calls are what the page really does. When the two differ, either the page grew a capability nobody approved or the record promises one the page dropped. Comparing them on every run keeps the record honest without anyone re-reading the page.

**Example:** The expense list offers `use-case/spending.list-expenses`, and its route calls `ListExpenses` through the generated client.

**Checked by:** `litespec model check` reports `screen-offers-drift` (level S) for a screen whose `offers` and computed calls differ, naming each use case on one side only.

### Serve Only the Steps a Screen's Use Cases Serve

**ID:** `litespec/core/edges#serve-only-the-steps-a-screens-use-cases-serve`

**Statement:** A screen's `serves` MUST name only journey steps that a use case it offers serves through a browser or device entry point.

**Rationale:** A journey step is achieved through use cases, and a person reaches those use cases on a screen. Naming the step on the screen lets the book say where each step happens, and checking it against the offered use cases finds a screen claiming a step its use cases do not carry.

**Example:** The expense form serves `journey/submit-and-reimburse#s1` because it offers `use-case/spending.submit-expense`, which serves that step.

**Checked by:** `litespec model check` reports `screen-serves-drift` (level G) for a served step no offered use case serves.

### Give Every Browser Step a Screen

**ID:** `litespec/core/edges#give-every-browser-step-a-screen`

**Statement:** Every journey step a use case serves through a browser or device entry point MUST be served by at least one screen.

**Rationale:** A step a person achieves in a browser happens on some page. A step no screen serves is either carried by a page nobody recorded or not reachable at all, and the book cannot tell the reader where it happens. The pair sits side by side: `step-use-case-missing` names a step no use case serves, and `step-screen-missing` names a step a browser or device use case serves that no screen serves.

**Example:** `journey/submit-and-reimburse#s1` is served by the expense form.

**Checked by:** `litespec model check` reports `step-screen-missing` (level G) for a step a browser or device use case serves that no screen serves.

### Show Every Refusal a Screen Can Reach

**ID:** `litespec/core/edges#show-every-refusal-a-screen-can-reach`

**Statement:** Every refusal a use case a screen offers can return MUST have a sentence in the refusal dictionary its frontend names in `refusalDictionary` or a browser test that asserts its code.

**Rationale:** A refusal a person can trigger on a page is part of what the page says to them. Without a sentence, the person sees a code or a generic error; without a test, nobody knows what they see. Requiring one of the two only where a screen can reach the refusal avoids demanding sentences for refusals no person meets. Each frontend names its dictionary in its `paths.frontends[]` entry, so the check reads that frontend's words rather than a convention.

**Example:** The expense form offers `use-case/spending.submit-expense`, whose `spending.receipt-missing` refusal has a sentence in the dictionary the Claimwick web frontend names in `refusalDictionary`.

**Checked by:** `litespec model check` reports `refusal-sentence-missing` (level A) for a refusal a screen can reach with neither a sentence in its frontend's `refusalDictionary` nor a browser test asserting its code.

### Serve Static Content Through Served Content

**ID:** `litespec/core/edges#serve-static-content-through-served-content`

**Statement:** A static script or legal text MUST be served content with its path, content type, cache control, and versioning, never a use case.

**Rationale:** A static file modeled as a use case invents behavior where none exists: results, scenarios, and handlers for bytes served unchanged. Serving it as content with its path, content type, cache control, versioning, and fulfilled requirements keeps content apart from behavior.

**Reviewed for:** Which static scripts or legal texts sit outside served content, or lack path, content type, cache control, or versioning? Look at the static files beside the served-content entries.

### Describe Every External System Operation

**ID:** `litespec/core/edges#describe-every-external-system-operation`

**Statement:** Each external system MUST describe its operations with direction, timeout, timeout handling, response, and cache.

**Rationale:** An outside call without a declared shape fails by surprise: timeouts hang, retries double-charge, responses unparse. Describing each operation with its direction of `out` or `in`, its timeout with its timeout handling, its response, and its cache states the coupling beside the system. The `retries` prose states the retry behavior in words beside `onTimeout`. The retry count lives on the calling edge in `onFailure.retry.maxAttempts`. The system acts inside the product as one named actor.

**Checked by:** `litespec consumer` fails an external system operation without direction, timeout, timeout handling, response, or cache.

## Conventions

None.
