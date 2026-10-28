# Mobile Data and State

## Intent

Data handling in a mobile app routes every use case through an Application handler, the one place where that use case is defined. Every piece of state carries a declared classification deciding whether it is persisted, kept on the device, or recomputed. This page states how that handling follows the data rules in [Frontend Data and State](../data.md), and a team that applies it while writing handlers and screens keeps one definition of each use case and one source of each value.

## Concepts

### The Three State Classifications

Durable state is what the user made: persisted, versioned, and exported. The expense report the visitor files is durable state, because the visitor built it and it must survive a restart and an export. Device-local state is persisted but stays on the device, excluded from export. Derived state is computed from durable state and never persisted: an expense total is derived from the report lines each time it is read.

## Standards

### Route Use Cases Through Application

**ID:** `litespec/frontend/mobile/data#route-use-cases-through-application`

**Statement:** A screen MUST invoke an Application handler carrying a `Command` or `Query` role suffix rather than compose Domain calls or write storage.

**Rationale:** The handler is the one place a use case exists: its validation, its rules, and its result. A screen that composes its own sequence of Domain calls and storage writes creates a second definition that no record names and no test pins down. Routing every use case through a handler keeps the screen a caller and the handler the definition. This realizes litespec/frontend/blazor/data#route-use-cases-through-application for mobile.

**Reviewed for:** No screen resolves a Domain service or storage adapter.

### Classify State

**ID:** `litespec/frontend/mobile/data#classify-state`

**Statement:** An aggregate introducing state MUST classify it as durable, device-local, or derived.

**Rationale:** The classification decides what happens to the state: durable state is persisted, versioned, and exported, device-local state is persisted but excluded from export, and derived state is never persisted. Without a declared classification each reader guesses, and a value the user made can end up unpersisted while a computed value ends up stored and stale. The three classifications carry the meaning litespec/frontend/blazor/data#classify-state states.

**Reviewed for:** Each aggregate introducing state names durable, device-local, or derived on its page.

### Load Persisted State Once

**ID:** `litespec/frontend/mobile/data#load-persisted-state-once`

**Statement:** Persisted state MUST load once at startup into a typed store that screens read from, with screens showing a loading state until it is ready.

**Rationale:** Storage reads are slow and fallible, so the product reads once at startup into a typed store that screens read from. A screen that reads storage on render pays an input-output call per frame and can render half-loaded state. A screen renders its loading state until the store finishes loading, so it never shows an unloaded store as ready.

**Reviewed for:** Persisted state loads into the typed store at startup, with screens showing a loading state until it is ready.

### Keep Persisted State in App-Private Storage

**ID:** `litespec/frontend/mobile/data#keep-persisted-state-in-app-private-storage`

**Statement:** Persisted state and media the user adds MUST live in the app's private storage, written atomically.

**Rationale:** App-private storage keeps the user's data inside the app's own sandbox, away from other apps and from the shared media library. A crash mid-write leaves a half-written file, so each write lands in a temporary file that then replaces the target, keeping the previous complete file until the new one is whole. Media the user adds travels with the state that references it, so an export or a reinstall keeps both together.

**Example:** The filed expense report persists as versioned durable state in app-private storage, with the photographed receipts beside it in the same private files.

**Reviewed for:** Persisted state and user-added media live in app-private storage, each file written through a temporary file.

### Copy Picked Media into the App

**ID:** `litespec/frontend/mobile/data#copy-picked-media-into-the-app`

**Statement:** Media picked from the device MUST be copied into app-private storage on import rather than kept as a reference to the picker's URI.

**Rationale:** A picker's URI is a loan, not a gift: the platform can revoke it when the picker closes, and the source file can move or vanish. A screen holding only the URI shows a broken image after either happens. Copying the media into app-private storage on import keeps the app's copy under the app's own control.

**Reviewed for:** Imported media lives as a copy in app-private storage, with no stored picker URI.

### Make Refusals Explicit

**ID:** `litespec/frontend/mobile/data#make-refusals-explicit`

**Statement:** A handler MUST return a typed result union covering success and each named refusal, with each screen mapping every refusal to a message.

**Rationale:** Signaling a refusal through `null`, an empty collection, or a bare boolean tells the caller that something was refused but not what. Every caller then guesses, and two callers guess differently. A typed result union names success and each refusal, so each screen maps every refusal to the message the application holds for it. This realizes litespec/frontend/data#map-every-failure-to-one-problem-type.

**Reviewed for:** Each handler union covers success and every named refusal, and each screen maps every refusal to a message.

### Call the Server Only Through the Generated Client

**ID:** `litespec/frontend/mobile/data#call-the-server-only-through-the-generated-client`

**Statement:** A `server` consumer's app MUST call the backend only through the generated client in `{ProjectName}.Client`.

**Rationale:** The generated client owns the base address, the credentials, the headers, and the mapping from a failure to a result. A call that builds its own request opts out of all four at once: it can point at the wrong host, miss the session, and surface a raw failure outside the handler's result shape. Routing every backend call through the one client keeps those decisions in one place. This realizes litespec/frontend/data#use-one-configured-client-per-application.

**Reviewed for:** Every backend call in a `server` app travels through the generated client in `{ProjectName}.Client`.

## Conventions

### Update the Store from Command Results

**ID:** `litespec/frontend/mobile/data#update-the-store-from-command-results`

**Default:** Apply the state a command returns to the store instead of re-reading storage.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The command result already carries the new state, so the store can apply it directly. A screen that re-reads storage after success spends a read discovering what the result already said, and the extra read can return a different answer if anything changed between the two. This realizes litespec/frontend/data#update-the-cache-a-command-changed.

**Reviewed for:** The store applies the state each command result carries without re-reading storage.
