# Android Stack

**Stack:** android

**Concern:** frontend-ui

This page states how a native Android app fills the platform project, the navigation controller, the screens, and the device code. It is for developers who place Android screens, styles, storage, and tests in a LiteSpec mobile consumer. One frame, one controller, and one resource discipline keep every Android app reading the same way.

## Concepts

### How the Solution Is Organized

The platform project `{ProjectName}.Android` holds the host Activity, the navigation controller, the `Screens/` folder, the platform adapters, and the `Resources/` folder. In the `client-only` shape the core projects hold the rest: Domain for rules and state, Application for handlers and interfaces, and Infrastructure for storage and platform-neutral adapters. In the `server` shape one Client project with the generated API client takes the place of the three core projects.

The split keeps the core compiling for every platform unchanged. Core code names plain .NET types, while platform code names the Android SDK. Imports point from screens toward the shared core, with the reverse edge absent.

### How Screens Navigate

Screens are plain Android views, and the navigation controller shows them with the host Activity owning the controller. One controller holds the current screen, the open overlay, and the history. Back, insets, and saved state arrive as platform APIs of the Activity and its window.

A route entry is one screen class under `Screens/`, and it carries the `<implements>` mark of the screen it renders in the form the .NET stack states. The host Activity and failure views draw no screen of their own, so they carry no screen mark. The shape puts the frame, the screens, and the use-case code in the same place in every Android app.

### Where Styles and Sentences Live

Colors and text styles live in resources, and screen code draws with plain Android views. A screen takes its colors and text styles from `Resources/values/styles.xml` and sets no style value in code. Copy resolves through the generated `Resource` class, with its fallback in `Resources/values/strings.xml`: the generated class gives every copy key a typed name, and the default values folder gives every key a fallback where a language lacks it.

The Claimwick expense list reads its empty-state sentence as `Resource.String.ExpenseListEmpty`, with the Spanish file supplying the translation. The Claimwick expense total uses the `TotalText` style from `Resources/values/styles.xml`, setting no color in its screen class. The entry point record names the review standing in for the page-code lint the stack skips.

### Where Data Lives

The app keeps its persisted state in app-private storage and reads it through the typed store. Persisted state and user media sit under `Context.FilesDir`, written atomically, so a write stopped midway leaves the prior complete file. The Claimwick receipts the visitor adds sit in the app files directory, replacing their prior copy only after the new one turns whole.

On the `server` shape the `{ProjectName}.Client` project holds the generated API client, which owns the base address, the credentials, the headers, and the mapping from a failure to a result. The shape keeps backend decisions in one client instead of spreading them across screens. Each screen maps each refusal to its message through the handler result.

### How Tests Run with and Without a Device

Core tests run as plain .NET test projects with no emulator: the core holds no platform type, so its tests need no device. The Claimwick expense claim rule carries its test in an xunit.v3 project beside the Domain project it proves. Device tests run on an emulator or a device, where screens meet the platform toolkit.

The manifest pins no device-test library. The release hand check on hardware covers what emulators approximate: performance, sensors, and vendor behavior. Fast core tests and on-device screen tests together prove behavior and presentation.

### What Replaces the Controlled UI Contract

Screens draw with native Android views, and this frontend sits outside the controlled UI contract, which covers the Svelte web stack only. Its entry point record names Android and the checks replacing the contract, so the frontend reports named replacements instead of nothing to check. The native view review stands in for the registry build command the contract uses elsewhere.

The replacement keeps the record honest about what review covers. A reader opening the entry point sees the platform and the boundary checks in one place. The shape avoids reading an Android frontend as a frontend with nothing to check.

### Which Versions Are Pinned

The `stack.dotnet` key in `litespec.manifest.yaml` pins the .NET version, the `stack.android` key pins the Android API level, and the `packages.nuget` keys pin every NuGet package version. These keys name the pins while the manifest holds the values. One pinned runner keeps every core project on the same test version.

## Standards

### Keep the Android Frame, Routes, and Modules

**ID:** `litespec/packs/android/stack#keep-the-android-frame-routes-and-modules`

**Strength:** required

**Statement:** An Android frontend MUST hold its frame in the host code of the platform project, its routes as screen classes under `Screens/`, and its use-case code as Application handlers, with no module mirror: each screen maps to the records it serves through its mark.

**Rationale:** Three fixed locations let a reader and a tool find the frame, the screens, and the use-case code in the same place in every Android app. The three-segment module mirror adds nothing on this stack, so screens group by module under `Screens/` instead. Each screen class carries the mark of the screen it renders, which links the address to the record.

**Example:** The Claimwick Android app keeps its navigation host in `Claimwick.Android`, its expense list screen in `Screens/Expenses/`, and its expense handlers in the Application project. Each screen maps to its record through its mark.

**Reviewed for:** Does every screen class sit under `Screens/` and call Application handlers instead of holding use-case logic? Check the platform project and the core projects.

### Mark Each Android Screen with Its Screen Record

**ID:** `litespec/packs/android/stack#mark-each-android-screen-with-its-screen-record`

**Strength:** required

**Statement:** A screen class under `Screens/` MUST carry the `<implements>` mark of the screen it renders, in the form the .NET stack states.

**Rationale:** The mark links the screen to its record the way a handler carries its use-case mark. The builder and the check read one link instead of guessing from names. A screen without the mark reads as an address with no record behind it.

**Example:** The Claimwick `ExpenseListScreen` carries `/// <implements>screen/claimwick-app.expense-list</implements>`. The check accepts the mark under the `Screens/` root.

**Checked by:** `litespec model check` reports `code-placement-drift` for a screen mark outside the `Screens/` root.

### Keep Hosts and Failure Views Unmarked

**ID:** `litespec/packs/android/stack#keep-hosts-and-failure-views-unmarked`

**Strength:** required

**Statement:** A host Activity or a failure view MUST NOT carry a screen mark.

**Rationale:** A host or failure view draws no screen of its own, so a mark on it links an address to a screen it fails to show. Leaving hosts and failure views unmarked keeps each mark pointing at a real screen. The check reads the absence as correct.

**Example:** The Claimwick host Activity class carries no `<implements>` mark. Its failure view carries none either, since neither draws a product screen.

**Reviewed for:** Do any host Activities or failure views carry a screen mark? Check the platform project.

### Hold One Navigation Controller in the Platform Project

**ID:** `litespec/packs/android/stack#hold-one-navigation-controller-in-the-platform-project`

**Strength:** required

**Statement:** The Android app MUST hold its navigation controller in one class in the platform project, owned by the host Activity.

**Rationale:** One controller holds the current screen, the open overlay, and the history, so no screen navigates on its own. A second controller splits the history and back behavior disagrees with what the screen shows. One class owned by the host Activity gives navigation one answer for every event.

**Example:** The Claimwick host Activity creates the one controller class, and every expense screen raises events to it. No expense screen opens another screen directly.

**Reviewed for:** Does navigation run through the one controller owned by the host Activity, with no screen navigating on its own? Check the screen classes and the host Activity.

### Accept App Links in the Host Activity

**ID:** `litespec/packs/android/stack#accept-app-links-in-the-host-activity`

**Strength:** required

**Statement:** The host Activity MUST receive app links in `OnCreate` and `OnNewIntent`, through an `IntentFilter` with `AutoVerify` set.

**Rationale:** Handling both entry points lets a cold start and a link that arrives while the app runs open the same screen. `AutoVerify` lets the platform open the app instead of a browser. The controller then resolves the link to one screen with one history behind it.

**Example:** A link to `https://claimwick.example/claims/` opens the claim screen its path names, whether the app was closed or open. Both entry points reach the same controller.

**Reviewed for:** Does the host Activity receive app links on both a cold start and while running? Check the intent filter and both overrides.

### Route the Back Gesture Through the Activity Dispatcher

**ID:** `litespec/packs/android/stack#route-the-back-gesture-through-the-activity-dispatcher`

**Strength:** required

**Statement:** The host Activity MUST deliver the system back gesture to the navigation controller through its `OnBackInvokedDispatcher`.

**Rationale:** The dispatcher is the platform path for the back signal, so predictive back reaches the controller in the same overlay-then-screen-then-leave order on every screen. A second path splits back behavior across screens. One dispatcher keeps the gesture consistent from the first screen to the last.

**Example:** On the Claimwick expense detail screen, a back gesture closes an open overlay first, then steps back to the expense list. The app leaves only from the first screen.

**Reviewed for:** Does every back gesture reach the controller through the Activity's dispatcher, leaving the app only from the first screen? Check the host Activity and the controller.

### Apply System Insets to Each Screen

**ID:** `litespec/packs/android/stack#apply-system-insets-to-each-screen`

**Strength:** required

**Statement:** A screen MUST apply the system bar and display cutout insets it reads from `WindowInsets` to its controls and text.

**Rationale:** The window supplies these insets, so reading them from `WindowInsets` keeps controls and text clear of the bars and cutouts on every device. Drawing under the bars keeps the full-bleed background while the content stays visible. Each screen reads the same source, so insets behave the same across screens.

**Example:** The Claimwick expense claim screen pads its submit button by the system bar inset while its background draws full bleed. Its text stays clear of the cutout on every device.

**Reviewed for:** Does every screen keep its controls and text clear of the system bars and cutouts? Check the screen layouts and the inset handling.

### Store Navigation State in the Activity Bundle

**ID:** `litespec/packs/android/stack#store-navigation-state-in-the-activity-bundle`

**Strength:** required

**Statement:** The host Activity MUST save the navigation controller's state in its `Bundle` from `OnSaveInstanceState` and restore it in `OnCreate`.

**Rationale:** The platform ends the process in the background when memory runs short, and the saved bundle returns the visitor to the same screen. Saving from `OnSaveInstanceState` and restoring in `OnCreate` covers the full kill-and-restore path. The saved overlay and history return with the screen.

**Example:** When the platform ends the Claimwick process, the app reopens on the expense detail screen with its overlay still open. The history behind it returns as well.

**Reviewed for:** Does the host Activity save and restore the controller's navigation state through its `Bundle`? Check the host Activity and the controller.

### Keep Screen Styles in Resources

**ID:** `litespec/packs/android/stack#keep-screen-styles-in-resources`

**Strength:** required

**Statement:** A screen MUST take its colors and text styles from `Resources/values/styles.xml` and set no style value in code.

**Rationale:** Resources hold every color and text style in one place, and a screen that sets a value in code creates a second style no resource answers to. One palette keeps two authors aligned without a meeting. A theme change touches the token file instead of each screen.

**Example:** The Claimwick expense total uses the `TotalText` style from `Resources/values/styles.xml`, setting no color in its screen class. A theme edit touches the style file once.

**Reviewed for:** Does every screen take its colors and text styles from `Resources/values/styles.xml`, with none set in code? Check the screen classes.

### Read Android Copy Through the Generated Resource Class

**ID:** `litespec/packs/android/stack#read-android-copy-through-the-generated-resource-class`

**Strength:** required

**Statement:** A screen MUST read every copy string through the generated `Resource` class, with its fallback in `Resources/values/strings.xml`.

**Rationale:** The generated class gives every copy key a typed name, and the default values folder gives every key a fallback where a language lacks it. Typed names turn a renamed key into a build break instead of a silent miss. The fallback keeps the screen readable in every language.

**Example:** The Claimwick expense list reads its empty-state sentence as `Resource.String.ExpenseListEmpty`. The Spanish file supplies the translation while the default file holds the fallback.

**Reviewed for:** Does every screen read its copy through the generated `Resource` class? Check the screen classes and the layouts.

### Store Persisted State in App-Private Storage

**ID:** `litespec/packs/android/stack#store-persisted-state-in-app-private-storage`

**Strength:** required

**Statement:** Persisted state and user media MUST live under `Context.FilesDir`, written atomically.

**Rationale:** The app sandbox keeps the data away from other apps and the shared media library. An atomic write keeps the prior complete file where a write stops midway. State beside the media that references it keeps exports whole.

**Example:** The Claimwick receipts the visitor adds sit in the app files directory, replacing their prior copy only after the new one turns whole. An export carries state and receipts together.

**Reviewed for:** Does all persisted state and user media live under `Context.FilesDir`, written atomically? Check the storage adapters.

### Use the System Pickers for Media

**ID:** `litespec/packs/android/stack#use-the-system-pickers-for-media`

**Strength:** required

**Statement:** A screen MUST pick photos through the Photo Picker action `android.provider.action.PICK_IMAGES` and files through `android.intent.action.OPEN_DOCUMENT`.

**Rationale:** Both pickers grant access to the chosen items only, so the app reads one receipt without storage permission. The platform UI shows the user exactly what the app receives. The manifest stays free of a storage permission no feature needs.

**Example:** In the Claimwick app, adding a receipt photo opens the Photo Picker, and the visitor choice is the one image the app receives. No storage permission covers the pick.

**Reviewed for:** Does every screen pick photos and files through the system pickers, with no storage permission requested for it? Check the picking call sites and the manifest.

### Request Permissions at the Point of Use

**ID:** `litespec/packs/android/stack#request-permissions-at-the-point-of-use`

**Strength:** required

**Statement:** The Android app MUST request each runtime permission through `RequestPermissions` when the action that needs it starts, never in `OnCreate`.

**Rationale:** A request at the point of use arrives when the visitor knows what it serves, and the same permission answers a question the visitor just asked. A request at launch arrives before the need exists, so the safe answer is refusal. Each request names the waiting action, so refusal disables that action instead of the app.

**Example:** The Claimwick app requests the camera permission when the visitor taps Scan receipt, not at launch. Refusing it disables scanning while the rest of the app keeps working.

**Reviewed for:** Is every runtime permission requested through `RequestPermissions` when its action starts, none in `OnCreate`? Check the host Activity and the action handlers.

### Request Audio Focus Before Playing Sound

**ID:** `litespec/packs/android/stack#request-audio-focus-before-playing-sound`

**Strength:** required

**Statement:** A screen that plays sound MUST request audio focus with an `AudioFocusRequest` before it plays, and pause when focus is lost.

**Rationale:** One speaker serves every app, so the platform arbitrates only the apps that ask for focus. Requesting focus before playing lets navigation prompts and calls yield cleanly. Pausing on loss yields to whatever took it, and resuming waits for focus to return.

**Example:** The Claimwick confirmation chime plays only after the app holds audio focus, and a phone call pauses it. Playback resumes after the call releases focus.

**Reviewed for:** Does every screen that plays sound request audio focus first and pause on focus loss? Check the playback call sites.

### Run Android Core Tests on xunit.v3

**ID:** `litespec/packs/android/stack#run-android-core-tests-on-xunitv3`

**Strength:** required

**Statement:** The Android app MUST run its core tests as plain .NET test projects on `xunit.v3`, the version `packages.nuget` pins.

**Rationale:** Core tests need no emulator, so a plain .NET runner keeps them fast. One pinned runner keeps every core project on the same version. The manifest pin gives each project one version to follow.

**Example:** The Claimwick expense claim rule has its test in an xunit.v3 project beside the Domain project it proves. The test runs with no emulator.

**Reviewed for:** Does every core test run in a plain .NET test project on the pinned `xunit.v3`, with no emulator? Check the core test projects.

### Keep Android Core Test Mocks to the Owned Boundaries

**ID:** `litespec/packs/android/stack#keep-android-core-test-mocks-to-the-owned-boundaries`

**Strength:** required

**Statement:** A core test MUST NOT replace the Domain types or the Application handlers it proves.

**Rationale:** A stand-in for the code under test proves the stand-in, so the test reaches the rule only through the real types and handlers. Replacing the platform edge keeps the rule real while the slow side stays fake. A test that replaces the handler tests nothing the app ships.

**Example:** The Claimwick expense claim test builds a real Domain claim and calls the real submit handler, replacing only the platform storage adapter. The rule under test stays real.

**Reviewed for:** Does any core test replace a Domain type or an Application handler? Check the test doubles.

### Declare Android Outside the Controlled UI Contract

**ID:** `litespec/packs/android/stack#declare-android-outside-the-controlled-ui-contract`

**Strength:** required

**Statement:** An Android frontend MUST declare an entry point record naming Android and the boundaries that replace the controlled UI contract.

**Rationale:** The shared contract covers the Svelte web stack, so an Android frontend with no declaration reads as a frontend with nothing to check. Naming Android and its replacement boundaries reports it as a frontend with named replacements instead. The entry point record gives reviewers one place to find them.

**Example:** The Claimwick Android entry point record names Android and the native view review standing in for the controlled contract. Each replacement boundary appears beside the platform it covers.

**Reviewed for:** Does every Android frontend declare an entry point record naming Android and its replacement boundaries? Check the entry point records.

## Conventions

None.
