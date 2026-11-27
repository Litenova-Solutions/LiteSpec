# Mobile Data and State

**Stack:** android

**Concern:** frontend-data

This page states where a mobile app keeps state, how screens read it, and how the app reaches the server. It is for developers who write handlers, stores, and screen code in a mobile consumer. One definition per use case and one source per value keep the app consistent when storage fails or the network drops.

## Concepts

### How State Is Classified

Durable state is the work the user created: it persists with a version and it travels in an export. Device-local state persists on the device and stays out of exports. Derived state is a value computed from durable state at read time, and it persists nowhere.

The Claimwick expense report shows the split. The filed report is durable state with a version, and its photographed receipts sit beside it. The expense total is derived state, computed from the report lines each time a screen reads it.

### How Screens Reach Data

Screens call Application handlers and read the typed store. The store loads once at startup, and screens show a loading state until it turns ready. The shape keeps slow storage off the render path and keeps half-loaded state off the screen.

A handler answers with a typed result that names success and each named refusal. Each screen maps each refusal to its message. On the `server` shape the handler reaches the backend through the generated client in `{ProjectName}.Client`, which owns the base address, the credentials, the headers, and the mapping from a failure to a result.

## Standards

### Load Persisted State Once

**ID:** `litespec/packs/android/data#load-persisted-state-once`

**Strength:** required

**Statement:** Persisted state MUST load once at startup into a typed store that screens read from, with screens showing a loading state until it is ready.

**Rationale:** Storage reads are slow and they fail, so one load at startup bounds the failure to one place. A screen that reads storage on render pays input-output on every frame and shows half-loaded state. A loading state until the store turns ready keeps the screen honest about what it holds.

**Reviewed for:** Does all persisted state load once at startup into the typed store, with screens showing a loading state until it is ready? Check the startup sequence and the screen loading states.

### Keep Persisted State in App-Private Storage

**ID:** `litespec/packs/android/data#keep-persisted-state-in-app-private-storage`

**Strength:** required

**Statement:** Persisted state and media the user adds MUST live in the app's private storage, written atomically.

**Rationale:** Private storage keeps user data inside the app sandbox, away from other apps and the shared media library. Each write lands in a temporary file that replaces the target, so a crash mid-write leaves the prior complete file. Media beside the state it belongs to keeps exports whole.

**Example:** A filed Claimwick expense report persists as versioned durable state in app-private storage. Its photographed receipts sit beside it in the same private files, so an export keeps both together.

**Reviewed for:** Does all persisted state and user-added media live in app-private storage, written through a temporary file? Check the storage writes.

### Copy Picked Media into the App

**ID:** `litespec/packs/android/data#copy-picked-media-into-the-app`

**Strength:** required

**Statement:** Media picked from the device MUST be copied into app-private storage on import rather than kept as a reference to the picker's URI.

**Rationale:** The platform revokes a picker URI when the picker closes, and the source file moves or disappears. A screen holding only the URI shows a broken image after either event. A copy under app-private storage stays under app control from import on.

**Reviewed for:** Does every media import copy the media into app-private storage instead of storing the picker's URI? Check the media imports.

### Return Every Refusal in a Typed Result

**ID:** `litespec/packs/android/data#return-every-refusal-in-a-typed-result`

**Strength:** required

**Statement:** A handler MUST return a typed result union covering success and each named refusal, with each screen mapping every refusal to a message.

**Rationale:** A null, an empty collection, or a bare boolean tells the caller that something failed without saying what. Each caller then guesses, and two callers guess differently. A typed union names success and each refusal, so each screen maps each outcome to the message the app holds for it.

**Reviewed for:** Does every handler return a typed union of success and each named refusal, with every screen mapping each refusal to a message? Check the handler result types and the screens.

### Call the Server Only Through the Generated Client

**ID:** `litespec/packs/android/data#call-the-server-only-through-the-generated-client`

**Strength:** required

**Statement:** A `server` consumer's app MUST call the backend only through the generated client in `{ProjectName}.Client`.

**Rationale:** The generated client owns the base address, the credentials, the headers, and the mapping from a failure to a result. A call that builds its own request steps outside all four at once: it points at the wrong host, misses the session, and surfaces a raw failure. One client keeps those four decisions in one place.

**Reviewed for:** Does every backend call in a `server` app go through the generated client in `{ProjectName}.Client`? Check the backend call sites.

## Conventions

### Refuse Commands While Offline

**ID:** `litespec/packs/android/data#refuse-commands-while-offline`

**Strength:** default

**Default:** While the network is unavailable, a screen shows its failure state and queues no command; the user retries the command in the foreground when the network returns.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A queued offline command runs later against state the user had no chance to see, so an expense filed on a train lands after its trip closed. Showing the failure state keeps the refusal on the screen that sent it. A project that needs offline capture writes its own convention with its replay rules.

**Example:** With no network, filing expense EXP-1042 for the Rotterdam client visit shows the failure state on the expense screen and sends nothing. The user submits the expense again when the network returns.

**Reviewed for:** While offline, does every screen show its failure state and queue only what a recorded local convention names with its replay order? Check the command call sites and the offline screens.
