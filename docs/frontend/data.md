# Frontend Data and State

**Stack:** any
**Concern:** frontend-data

A LiteSpec frontend owns no API contract: it calls the API through one generated client, reads answers through the client's cache, and sends each write as a command, one message that changes business state.

This page states how the client is built and configured, where each piece of state lives, and how every failure reaches the user as one problem, a project-owned failure shape carrying a stable code. Frontend developers and agents apply it for every read, write, and form, so a contract change breaks the build instead of the running page and every failure renders a sentence the team wrote for it.

## Concepts

### The Generated Client

The API publishes one OpenAPI document. Generation turns it into the client the frontend calls: typed operations, request and response types, validation schemas, and the operation options each read and write needs. Because the output is generated, a contract change becomes a compile error in every application at once instead of a wrong request at runtime. The workspace configures the client once per application, with its base address, credentials, headers, and failure mapping, and every read and write passes through it.

The frame is the part of a frontend holding everything cross-cutting, such as the client configuration and shared helpers ([Frontend Structure](structure.md)).

### Where State Lives

Every value the frontend holds has one owner, from the widest to the narrowest. Server data lives in the query cache, the client's store of answers it already fetched. Shareable values, such as filters and paging, live in the address, so the page stays a link somebody can share. Input lives in form state, and anything narrower lives in the component's own state. A value kept above its narrowest owner forces unrelated components to re-render and hides where the value changes.

### The Three State Classifications

State falls into three kinds by what the user made and what survives. Durable state is what the user made: persisted, versioned, and exported. The expense draft is durable state, because the user built it and it must survive a restart and an export. Device-local state is persisted but stays on the device, excluded from export, such as the device copy of a receipt photo. Derived state is computed from durable state and never persisted: an expense total is derived from the expense lines each time it is read.

### Outcomes of a Command

Every command ends in one outcome, and the page handles each outcome as the table states.

| Outcome | What the page does |
|:---|:---|
| Success | Invalidates the queries the command changes, then says so where nothing else on screen shows it |
| Refusal | Renders the refusal beside what was refused, in the sentence the application holds for that code |
| Field refusal | Places each problem on the field it belongs to, leaving the form open |

A success carries a visible answer wherever nothing else on screen shows it: a silent success tells the user nothing happened. Field problems belong on their fields; above a long form, a summary links to each field in error instead of replacing the messages on them.

### The Problem Type

Whatever produces a failure, a refused command, a failed read, or a network error, the frontend converts it into one project-owned problem carrying the same fields: a status, a code, a detail, an optional trace identifier, and field problems, each naming its field with its own code and message.

The code is the stable identifier the page branches on, so a reworded message leaves the page's decision unchanged. A code this build does not recognize is still handled: the page renders the problem by the code's presence rather than by matching a list of known codes.

The problem carries the trace identifier the API returns for the request, so support can find the failed request. [Controlled UI Governance](ui.md) states how a visible error shows that identifier in copyable form.

## Standards

### Generate the Client from the Published Contract

**ID:** `litespec/frontend/data#generate-the-client-from-the-published-contract`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST generate its client, types, schemas, and operation options from the API's published OpenAPI document.

**Rationale:** A handwritten copy of a request, a response, or a schema drifts from the contract silently: the API changes, the copy still compiles, and the page sends a request the API no longer accepts. The failure surfaces at runtime, in front of the user. A generated client turns the same contract change into a compile error in every application at once, at every call site the change affects.

**Reviewed for:** Is every client, type, schema, and operation option generated from the published OpenAPI document? Look in the generated client directory and the contract it names.

### Use One Configured Client per Application

**ID:** `litespec/frontend/data#use-one-configured-client-per-application`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A frontend MUST configure the generated client once, and route every read and write through it.

**Rationale:** The configured client owns four decisions: the base address, the credentials, the headers, and the mapping from a failure to a problem. A call building its own request opts out of all four at once: it can point at the wrong host, miss the session cookie, skip the write marker, and surface a raw failure outside the page's problem shape. Routing every read and write through the one client keeps those decisions in one place.

**Reviewed for:** Does every read and write pass through the one configured client? Look in the frame client configuration and the call sites.

### Keep the Session in an API Owned Cookie

**ID:** `litespec/frontend/data#keep-the-session-in-an-api-owned-cookie`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The API MUST hold the browser session in an HTTP-only cookie, with frontend code holding no token.

**Rationale:** Anything a script can read, any script on the page can read, including one an attacker injected. Tokens kept in frontend code or browser storage are therefore readable by the same attack the session is meant to resist. The API runs the authorization code flow instead and keeps the tokens encrypted in an HTTP-only cookie the browser sends on its own, so frontend code holds no token. A refused sign-in lands on a signed-out page rather than on an error, because the user is not signed in.

**Reviewed for:** Does any token sit in frontend sources instead of the API-owned HTTP-only cookie? Look in the API session handling and the frontend sources.

### Set the Write Marker on Every Request

**ID:** `litespec/frontend/data#set-the-write-marker-on-every-request`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A client MUST send its marker header on every write, with the API accepting a cookie-authenticated write only when it carries that header.

**Rationale:** A browser attaches its cookies to a cross-site request on its own, so the cookie alone cannot tell the page's own write from a forged post submitted from another site. The marker header tells them apart: the page's client adds it to every write, and a page on another site adds no custom header without a preflight, which the API grants to no foreign origin. The API therefore accepts a cookie-authenticated write only when the header is present. The header name varies by project, so configuration owns it rather than code; it has the form `X-<Project>-Request` with the value `1`.

**Reviewed for:** Does every write carry the configured marker header of the form `X-<Project>-Request`? Look in the client configuration and the API write acceptance.

### Map Every Failure to One Problem Type

**ID:** `litespec/frontend/data#map-every-failure-to-one-problem-type`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A frontend MUST convert every failure into one problem type, deciding from its code.

**Rationale:** A raw response body carries whatever the API included: field names, internals, sometimes data kept from the user. Rendering it leaks that content, and each failure looks different, so the user gets no consistent recovery path. One problem type gives every failure the same shape: the page branches on the published code, so its decision survives a reworded message, and the problem carries the trace identifier support needs to find the request.

**Reviewed for:** Does every failure render through the one problem type? Look in the frame problem mapping and the error surfaces.

### Place a Refusal Beside What Was Refused

**ID:** `litespec/frontend/data#place-a-refusal-beside-what-was-refused`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A refusal MUST render beside the record or control that was refused, in the sentence the application holds for that code.

**Rationale:** A refusal at the top of a page says that something failed but not what, so the user scans the page for the control that caused it. Beside the refused record or control, the refusal answers both questions at once. The sentence comes from the application's own dictionary for that code rather than from the response body, so the words stay consistent and translatable. The code in the markup lets a test provoke the refusal by name.

**Example:** Submitting an expense above the receipt threshold with no receipt refuses with `spending.receipt-missing` under `result/submit-expense.receipt-missing`. The page renders the application's sentence for that code beside the Submit control, carries the code in the markup so a test provokes it by name, and keeps the trace identifier in the detail in copyable form.

**Reviewed for:** Does every refusal render beside the refused record or control, with the application's sentence and the code in the markup? Look in the screen's page files beside the refusing controls.

### Place a Field Refusal on Its Field

**ID:** `litespec/frontend/data#place-a-field-refusal-on-its-field`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A field problem MUST reach the field it names through the frame's one helper, with the form staying open until resolved or abandoned.

**Rationale:** A problem on its field shows the user what to fix where they fix it, through the frame's one helper. Above a long form, a summary links to each field in error, so a problem below the fold stays one tap away. The summary is a navigation aid added to the messages on each field, and it replaces none of them. The form stays open until each problem is resolved or the user abandons the form, so no input is lost to a closed dialog.

**Reviewed for:** Does every field problem reach its field through the frame helper, with the form open until resolved or abandoned? Look in the forms and the frame helper.

### Validate a Form with the Generated Schema

**ID:** `litespec/frontend/data#validate-a-form-with-the-generated-schema`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A form MUST validate against the generated schema for its request body, extended with the application's own rules rather than replaced by them.

**Rationale:** The generated schema states what the API accepts, so it holds even where the browser accepts more. Validating the form against it refuses bad input before the request leaves the page. The application extends the schema with the message shown for each rule and any rule beyond the contract's reach, rather than replacing it, so the form refuses everything the API refuses.

**Reviewed for:** Does every form validate against the generated request schema extended with application rules? Look in the form sources and the generated schemas.

### Update the Cache a Command Changed

**ID:** `litespec/frontend/data#update-the-cache-a-command-changed`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A successful command MUST invalidate every query whose answer it changed, and no other query.

**Rationale:** A page renders the cached answer, not a new request, so the query cache owns freshness. A command leaving a changed query cached shows the old record until something else happens to refetch, and the user's own write appears to have no effect. Invalidating exactly the queries the command changed refreshes what changed; invalidating more costs a needless request per affected page.

**Example:** The generated client holds both sides of one write: the expenses route calls the submit operation with the expense values, and the list reads through the list operation. After the submit succeeds, the page invalidates the list query and no other query, so the list shows the new expense without refetching unrelated pages.

**Reviewed for:** Does every successful command invalidate exactly the queries whose answers it changed? Look in the command call sites and the cache keys.

### Assign State to the Narrowest Owner

**ID:** `litespec/frontend/data#assign-state-to-the-narrowest-owner`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** A value MUST live with the narrowest owner that can hold it: server data, then the address, then form state, then the component's own state.

**Rationale:** A value with the smallest audience that can use it re-renders only the components needing it, and its changes stay visible where they happen. The [Where State Lives](#where-state-lives) concept names the order and what keeping a value above its owner costs.

**Reviewed for:** Does every value live with its narrowest owner: the query cache, the address, form state, or component state? Look in the state declarations.

### Route Use Cases Through Application

**ID:** `litespec/frontend/data#route-use-cases-through-application`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A frontend MUST invoke an Application handler carrying a `Command` or `Query` role suffix for each use case rather than compose Domain calls or write storage.

**Rationale:** The handler is the one place a use case exists: its validation, its rules, and its result. A screen or component composing its own sequence of Domain calls and storage writes creates a second definition that no record names and no test pins down. Routing every use case through a handler keeps the screen or component a caller and the handler the definition.

**Example:** The expense screen invokes the submit-expense Application handler for EXP-1042 rather than composing the Domain expense calls and writing the expense store itself.

**Reviewed for:** Does every screen and component invoke an Application handler carrying a `Command` or `Query` role suffix, instead of composing Domain calls or writing storage? Look in the screen and component sources.

### Compose a Screen from Each Owner's Query

**ID:** `litespec/frontend/data#compose-a-screen-from-each-owners-query`

**Strength:** required

**Applies to:** artifact: route, component

**Statement:** A screen that shows data from several modules MUST call each owning module's own query and join the answers itself, rather than call one query that reads another module's data.

**Rationale:** A query existing only to show another module's data couples two modules for one page: the owner grows a reader method named for that page, and the reading module calls it in its query handler. The screen already knows both answers it needs, so it calls each owner's query side by side and joins the answers itself. Every query stays inside its own module, and the screen record's `offers` lists both use cases, so the composition stays visible in the model. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model).

**Example:** A finance page showing each reimbursed expense beside its payout status calls the spending module's list-expenses query and the payout module's list-payouts query, and joins the two lists by expense identifier in the page.

**Reviewed for:** Does every screen showing several modules' data call each owner's query and join the answers itself? Look at each screen's `offers` beside the queries it calls.

### Classify State

**ID:** `litespec/frontend/data#classify-state`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate introducing state MUST classify it as durable, device-local, or derived.

**Rationale:** The classification decides what happens to the state: durable state is persisted, versioned, and exported, device-local state is persisted but excluded from export, and derived state is never persisted. Without a declared classification each reader guesses, and a value the user made can end up unpersisted while a computed value ends up stored and stale.

**Example:** The expense draft for the Rotterdam client visit is durable state, the device copy of the receipt photo is device-local state, and the expense total for EXP-1042 is derived state computed from its lines.

**Reviewed for:** Does every aggregate introducing state name its durable, device-local, or derived classification? Look on the aggregate pages.

### Route a Personal Value in a Header

**ID:** `litespec/frontend/data#route-a-personal-value-in-a-header`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A value naming a person MUST travel in a request header rather than the query string, with the contract declaring the header each operation reads.

**Rationale:** A query string leaks three ways: proxies log it, the browser keeps it in history, and the next page the user opens receives it as the referrer. A value naming a person therefore travels in a request header the contract declares per operation, reaching the API without landing in any of those places. The address keeps its own copy for filters and paging, so a filtered list stays a link somebody can share.

**Reviewed for:** Does every person-naming value travel in a contract-declared header instead of the query string? Look in the contract headers and the call sites.

### Read a Permission from the Session

**ID:** `litespec/frontend/data#read-a-permission-from-the-session`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A page MUST offer a command only when the session actor holds its permissions at the record's scope, with the API deciding every command.

**Rationale:** Permissions are the project's permission records: an actor holds a role, a role allows permissions, and a use case needs permissions. The page reads the permissions the signed-in actor holds from the session the API returns, and offers only the commands those permissions allow at the record's scope. The user meets no offer ending in a refusal. The API still decides every command, so an action offered in error is refused rather than executed.

**Reviewed for:** Does every offered command stay within the permissions the session actor holds at the record scope? Look in the page command guards and the session shape.

### Name the Zone for Every Timestamp

**ID:** `litespec/frontend/data#name-the-zone-for-every-timestamp`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A formatter rendering a timestamp MUST receive an explicit IANA time zone, resolved by the caller from the record the screen shows.

**Rationale:** An instant from the API carries no zone, so a formatter left to pick one shows the user's own zone: a payout cut-off reads as midnight in the user's city instead of the office's. The caller resolves the zone from the record the screen shows: a record with its own zone states it, otherwise the account's zone applies, and an account choosing none falls back to UTC. Passing the zone explicitly names whose day each timestamp belongs to.

**Reviewed for:** Does every timestamp formatter receive an explicit IANA zone resolved from the shown record? Look in the formatter call sites.

### Keep a Calendar Date Free of a Time Zone

**ID:** `litespec/frontend/data#keep-a-calendar-date-free-of-a-time-zone`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A date with no time of day, such as a trip day, a payout day or a billing month, MUST travel and display as a calendar date, never as an instant converted through a time zone.

**Rationale:** A trip starting 8 October shows as 7 October to a reader west of the server once the date passes through a zone. The calendar date means the same day to every reader, wherever they sit. The frontend sends and renders the date itself, with no zone attached.

**Reviewed for:** Does every date with no time of day travel and display as a calendar date? Look in the date sends and renders beside the trip, payout, and billing reads.

### Keep Secrets Out of Browser Storage

**ID:** `litespec/frontend/data#keep-secrets-out-of-browser-storage`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST NOT store a refresh token, a provider secret, or a privileged credential in browser storage or a browser-visible variable.

**Rationale:** Local storage, session storage, IndexedDB, and a build-time public variable are all readable by any script on the page. A secret kept there is available to the same injected script the secret is meant to resist. Refresh tokens, provider secrets, and privileged credentials therefore stay out of the browser entirely. A preference the user chooses, such as a theme or a locale, is no secret and can be stored under a named key in the frame.

**Reviewed for:** Is any refresh token, provider secret, or privileged credential stored in browser storage or a browser-visible variable? Look in the storage writes and the public variables.

### Make Optimistic Behavior Recoverable

**ID:** `litespec/frontend/data#make-optimistic-behavior-recoverable`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** An optimistic update MUST declare its stable client identity, its conflict behavior, its failure rollback, and its reconciliation path.

**Rationale:** An optimistic update shows a value the server has not confirmed. Without a stable client identity, the retry after a failure creates a duplicate. Without declared conflict behavior, two writers overwrite each other silently. Without a rollback path, a failed update leaves the shown value wrong. Declaring identity, conflict behavior, rollback, and reconciliation up front makes each case designed instead of improvised. Money, irreversible actions, and uncertain authorization skip optimism entirely and wait for the server result.

**Reviewed for:** Does every optimistic update declare its client identity, conflict behavior, rollback, and reconciliation path, and does every money, irreversible, or uncertainly authorized action wait for the server? Look in the optimistic update definitions.

### Announce the Outcome of an Optimistic Update

**ID:** `litespec/frontend/data#announce-the-outcome-of-an-optimistic-update`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** An optimistic update MUST announce its reconciled outcome through a live region when the server result differs from the value shown.

**Rationale:** A rollback is a silent visual change: the shown value changes back with no event a screen reader announces. The user heard the optimistic value confirmed and hears nothing when it is withdrawn, so they act on a value the server refused. Announcing the reconciled outcome through a live region tells them what actually holds. [The ARIA live-region technique](https://www.w3.org/WAI/WCAG21/Techniques/aria/ARIA19) is the mechanism, and a test of the route asserts the announcement.

**Reviewed for:** Does every reconciled outcome differing from the shown value announce through a live region? Look in the route tests asserting the announcement.

### Localize Copy Through Locale Dictionaries

**ID:** `litespec/frontend/data#localize-copy-through-locale-dictionaries`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** Copy MUST resolve through locale dictionaries with fill and plural support, one chunk per language, and typed keys with a declared fallback.

**Rationale:** One sentence written in two components drifts at the first rewording: the author finds one copy and misses the other. A dictionary per language keeps each sentence in one place, keyed by a typed key the compiler checks, with fill and plural support so variable content and counts render correctly in every language. One chunk per language keeps a user's download to the language they read, and a key the catalog lacks fails where the declared fallback says it fails instead of rendering a raw key.

**Example:** A workspace carrying its own dictionary machinery still meets the same contract: typed keys, fill and plural support, one chunk per language, and a declared fallback.

**Reviewed for:** Does every copy resolve through the locale dictionaries with fill, plural, chunking, typed keys, and a declared fallback? Look in the locale dictionaries and the copy call sites.

## Conventions

### Keep the Client Boundary in the Frame

**ID:** `litespec/frontend/data#keep-the-client-boundary-in-the-frame`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place the client configuration, problem mapping, and download helpers in the frame root, keeping them out of every use-case folder.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The client configuration, the problem mapping, and the download helpers serve every use case. A copy in each use-case folder turns one change to the base address or the headers into an edit per folder, with the missed folder failing at runtime. The frame root holds them once, where every use case reaches them and none duplicates them.

**Reviewed for:** Do all client configuration, problem mapping, and download helpers sit in the frame root instead of a use-case folder? Look in the frame root and the use-case folders.

### Regenerate the Client Rather Than Editing It

**ID:** `litespec/frontend/data#regenerate-the-client-rather-than-editing-it`

**Strength:** default

**Applies to:** artifact: any

**Default:** Regenerate the client from the document, and edit nothing under the generated directory.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A hand edit under the generated directory works until the next generation overwrites it: the change appears sound and then silently disappears. Regenerating from the document keeps the committed output provably equal to the contract. A reviewer regenerates and compares, and any difference is either a contract change or an edit belonging outside the generated directory.

**Reviewed for:** Does the committed client match fresh generation output, with no hand edits below the generated directory? Look in the generated directory and the generator command.

### Keep Schemas Use-Case Specific

**ID:** `litespec/frontend/data#keep-schemas-use-case-specific`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep a form schema or a view model in its owning use-case folder, and let a second use case that needs the same shape declare its own.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A schema shared before its second use case exists is a contract written for one reader. The second use case then either contorts itself to fit or breaks the first reader by changing the shared shape. Keeping each form schema or view model in its owning use-case folder, with a copy where a second use case needs the same shape, keeps each use case free to change for its own reasons, as `litespec/backend/architecture#give-each-use-case-its-own-models` states for the server.

**Reviewed for:** Does every form schema and view model live in its owning use-case folder? Look in the use-case folders and the shared schema locations.

### Brand Every Identifier from the Contract

**ID:** `litespec/frontend/data#brand-every-identifier-from-the-contract`

**Strength:** default

**Applies to:** artifact: any

**Default:** Give each identifier of the published contract its own branded type in the generated client, from the identifier's named schema component, and parse each route parameter that holds an identifier with the generated schema in the route loader.

**Replacement:** A consumer can replace this default with an explicit local convention that names which identifiers stay plain strings.

**Rationale:** The backend gives every identifier its own type, but a client typing every identifier as `string` lets a caller pass an expense identifier where a receipt identifier belongs. The call compiles and fails at runtime. A branded type per identifier turns that swap into a compile error. Deriving the brands from the contract's named identifier components needs no hand-kept list, so a new identifier is branded the moment the contract names it. Parsing route parameters with the generated schema gives a route the branded value at its edge. The brands stay until the presentation boundary converts them, as `litespec/frontend/components#keep-domain-values-typed-until-display` states.

**Example:** `listExpenseReceipts(expenseId: ExpenseId, receiptId: ReceiptId)` refuses `listExpenseReceipts(receiptId, expenseId)` at compile time, and the receipt route reads `ExpenseIdSchema.parse(params.expenseId)`.

**Reviewed for:** Does every generated identifier carry its branded type, and does every route loader parse identifier parameters with the generated schema? Look at the generated client's identifier types and the route loaders.

### Build a Form on the Shared Form Library

**ID:** `litespec/frontend/data#build-a-form-on-the-shared-form-library`

**Strength:** default

**Applies to:** artifact: form

**Default:** Build a form on the shared package's form entry point and its registered field components.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A form library used directly in every use-case folder gives each folder its own fields, its own validation wiring, and its own error display, and none of them knows how to show a refusal on its field. The shared package's form entry point and registered field components hold those decisions once, so every form validates, renders, and refuses the same way.

**Reviewed for:** Does every form build on the shared package form entry point and its registered field components? Look in the form sources and the shared package form entry point.

## Reference Example

This informative example demonstrates `litespec/frontend/data#place-a-field-refusal-on-its-field`, `litespec/frontend/data#update-the-cache-a-command-changed`, and `litespec/frontend/data#validate-a-form-with-the-generated-schema`.

A redelivery dialog holds one field. Submitting validates against the generated schema and applies any field problem back to that field. Success invalidates the expense it changed and closes the dialog.
