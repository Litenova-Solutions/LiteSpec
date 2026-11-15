# Mobile Data and State

**Stack:** any
**Concern:** frontend-data

In a mobile app, every use case runs through an Application handler, the one place where that use case is defined. Every piece of state carries a declared classification that decides whether it persists, stays on the device, or recomputes.

This page states the mobile store, storage, and client those rules land in, following [Frontend Data and State](../data.md) (`litespec/frontend/data#route-use-cases-through-application`, `litespec/frontend/data#classify-state`, `litespec/frontend/data#update-the-cache-a-command-changed`). A team that applies it while writing handlers and screens keeps one definition of each use case and one source of each value.

## Concepts

### How State Is Classified

A classification decides what happens to each piece of state: whether it persists, stays on the device, or recomputes. Durable state is what the user made: it persists, carries a version, and exports. Device-local state persists but stays on the device, excluded from export. Derived state computes from durable state and persists nowhere.

For example, the expense report the user files is durable state: the user built it, and it must survive a restart and an export. An expense total is derived state: it is derived from the report lines each time it is read.

### How Screens Reach Data

Screens call Application handlers and read the typed store: the store loads once at startup, and screens show a loading state until it is ready. A handler returns a typed result naming success and each refusal, so the screen maps every refusal to its message.

On the `server` shape, the handler reaches the backend through the generated client in `{ProjectName}.Client`, which owns the base address, the credentials, the headers, and the mapping from a failure to a result.

## Standards

### Load Persisted State Once

**ID:** `litespec/frontend/mobile/data#load-persisted-state-once`

**Strength:** required

**Statement:** Persisted state MUST load once at startup into a typed store that screens read from, with screens showing a loading state until it is ready.

**Rationale:** Storage reads are slow and can fail, so the product reads once at startup into a typed store that screens read from. A screen that reads storage on render pays an input-output call per frame and can render half-loaded state. A screen renders its loading state until the store finishes loading, so it never shows an unloaded store as ready.

**Reviewed for:** Does all persisted state load once at startup into the typed store, with screens showing a loading state until it is ready? Check the startup sequence and the screen loading states.

### Keep Persisted State in App-Private Storage

**ID:** `litespec/frontend/mobile/data#keep-persisted-state-in-app-private-storage`

**Strength:** required

**Statement:** Persisted state and media the user adds MUST live in the app's private storage, written atomically.

**Rationale:** App-private storage keeps the user's data inside the app's own sandbox, away from other apps and from the shared media library. A crash mid-write leaves a half-written file, so each write lands in a temporary file that then replaces the target, keeping the previous complete file until the new one is whole. Media the user adds travels with the state that references it, so an export or a reinstall keeps both together.

**Example:** A filed Claimwick expense report persists as versioned durable state in app-private storage, with its photographed receipts beside it in the same private files.

**Reviewed for:** Does all persisted state and user-added media live in app-private storage, written through a temporary file? Check the storage writes.

### Copy Picked Media into the App

**ID:** `litespec/frontend/mobile/data#copy-picked-media-into-the-app`

**Strength:** required

**Statement:** Media picked from the device MUST be copied into app-private storage on import rather than kept as a reference to the picker's URI.

**Rationale:** The platform can revoke a picker's URI when the picker closes, and the source file can move or vanish. A screen holding only the URI shows a broken image after either. Copying the media into app-private storage on import keeps the app's copy under the app's own control.

**Reviewed for:** Does every media import copy the media into app-private storage instead of storing the picker's URI? Check the media imports.

### Return Every Refusal in a Typed Result

**ID:** `litespec/frontend/mobile/data#return-every-refusal-in-a-typed-result`

**Strength:** required

**Statement:** A handler MUST return a typed result union covering success and each named refusal, with each screen mapping every refusal to a message.

**Rationale:** Signaling a refusal through `null`, an empty collection, or a bare boolean tells the caller that something was refused but not what. Every caller then guesses, and two callers guess differently. A typed result union names success and each refusal, so each screen maps every refusal to the message the application holds for it. This realizes litespec/frontend/data#map-every-failure-to-one-problem-type.

**Reviewed for:** Does every handler return a typed union of success and each named refusal, with every screen mapping each refusal to a message? Check the handler result types and the screens.

### Call the Server Only Through the Generated Client

**ID:** `litespec/frontend/mobile/data#call-the-server-only-through-the-generated-client`

**Strength:** required

**Statement:** A `server` consumer's app MUST call the backend only through the generated client in `{ProjectName}.Client`.

**Rationale:** The generated client owns the base address, the credentials, the headers, and the mapping from a failure to a result. A call that builds its own request opts out of all four at once: it can point at the wrong host, miss the session, and surface a raw failure outside the handler's result shape. Routing every backend call through the one client keeps those decisions in one place. This realizes litespec/frontend/data#use-one-configured-client-per-application.

**Reviewed for:** Does every backend call in a `server` app go through the generated client in `{ProjectName}.Client`? Check the backend call sites.

## Conventions

### Refuse Commands While Offline

**ID:** `litespec/frontend/mobile/data#refuse-commands-while-offline`

**Strength:** default

**Default:** While the network is unavailable, a screen shows its failure state and queues no command; the user retries the command in the foreground when the network returns.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A command queued offline runs later against state the user cannot see, so an expense submitted on a train can land after its trip closed or its approval policy changed. Showing the failure state keeps the refusal on the screen that caused it, and queuing nothing keeps every command tied to the screen that sent it. A project that needs offline capture writes its own convention with its replay rules, instead of letting each screen improvise a queue.

**Example:** With no network, filing expense EXP-1042 for the Rotterdam client visit shows the failure state on the expense screen and sends nothing; the user submits the expense again when the network returns.

**Reviewed for:** While offline, does every screen show its failure state and queue only what a recorded local convention names with its replay order? Check the command call sites and the offline screens.
