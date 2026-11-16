# Android Stack

**Stack:** android

**Concern:** frontend-ui

The Android stack is the technology binding for a product that ships a native Android application built with .NET for Android in C# against plain Android views. A team chooses it with either shape, `client-only` or `server`, declared in [Supported Scope](../../../core/scope.md).

The stack is one .NET for Android project holding the screens, the navigation controller, and the device code. It publishes an AAB through the .NET toolchain, which builds, tests, and publishes the whole mobile solution.

The shared mobile pages in the [Mobile overview](../README.md) state the rules every mobile stack follows, and [Technology Stacks](../../../core/stacks.md) states the rules every stack shares. This frontend sits outside the controlled UI contract, which covers React and Svelte only, so its entry point record names the boundaries that replace that contract.

## Concepts

### How the Solution Is Organized

The platform project is `{ProjectName}.Android`: it holds the host Activity, the navigation controller, the `Screens/` folder, the platform adapters, and the `Resources/` folder.

The core projects hold the rest: Domain for rules and state, Application for handlers and interfaces, and Infrastructure for storage and platform-neutral adapters. On the `server` shape, the Client project joins them, holding the generated API client. The split keeps the core compiling for every platform unchanged.

### How Screens Navigate

Screens are plain Android views that the navigation controller shows, and the host Activity owns the controller: one controller holds the current screen, the open overlay, and the history.

Back, insets, and saved state are platform APIs of the Activity and its window, not of a UI library.

A route entry is one screen class under `Screens/`, and it carries the `<implements>` mark of the screen it renders, in the form the .NET stack states. The host Activity and failure views are not route entries: they draw no screen of their own, so they carry no screen mark.

### Where Styles and Sentences Live

Colors and text styles live in resources, and screen code draws with plain Android views: a screen takes its colors and text styles from `Resources/values/styles.xml` and sets no style value in code.

Copy resolves through the generated `Resource` class, with its fallback in `Resources/values/strings.xml`: the generated class gives every copy key a typed name, and the default values folder gives every key a fallback when a language lacks it. The stack has no page-code lint, so the entry point record names the review that replaces it.

### Where Data Lives

The app keeps its persisted state in app-private storage and reads it through the typed store. Persisted state and user media live under `Context.FilesDir`, written atomically, so a write stopped midway leaves the previous complete file.

On the `server` shape, the `{ProjectName}.Client` project holds the generated API client, which owns the base address, the credentials, the headers, and the mapping from a failure to a result.

### How Tests Run with and Without a Device

Core tests run as plain .NET test projects with no emulator: the core holds no platform type, so its tests need no device. For example, the Claimwick expense claim rule has its test in an xunit.v3 project beside the Domain project it proves.

Device tests run on an emulator or a device, where screens meet the platform toolkit. The manifest pins no device-test library.

### What Replaces the Controlled UI Contract

Screens draw with native Android views, and this frontend sits outside the controlled UI contract, so it has no registry build command. Its entry point record names Android and the checks that replace the contract, so the frontend reports named replacements instead of nothing to check.

### Which Versions Are Pinned

The `stack.dotnet` key in `litespec.manifest.yaml` pins the .NET version, the `stack.android` key pins the Android API level, and the `packages.nuget` keys pin every NuGet package version. These keys name the pins; the manifest holds the values.

## Standards

### Keep the Android Frame, Routes, and Modules

**ID:** `litespec/frontend/mobile/android/stack#keep-the-android-frame-routes-and-modules`

**Strength:** required

**Statement:** An Android frontend MUST hold its frame in the host code of the platform project, its routes as screen classes under `Screens/`, and its use-case code as Application handlers.

**Rationale:** Three fixed locations let a reader and a tool find the frame, the screens, and the use-case code in the same place in every Android app.

**Example:** The Claimwick Android app keeps its navigation host in `Claimwick.Android`, its expense list screen in `Screens/Expenses/`, and its expense handlers in the Application project.

**Reviewed for:** Does every screen class sit under `Screens/` and call Application handlers instead of holding use-case logic? Check the platform project and the core projects.

### Mark Each Android Screen with Its Screen Record

**ID:** `litespec/frontend/mobile/android/stack#mark-each-android-screen-with-its-screen-record`

**Strength:** required

**Statement:** A screen class under `Screens/` MUST carry the `<implements>` mark of the screen it renders, in the form the .NET stack states.

**Rationale:** The mark links the screen to its record the way a .NET handler carries its use-case mark, so the builder and the check read the same link shape.

**Example:** The Claimwick `ExpenseListScreen` carries `/// <implements>screen/claimwick-app.expense-list</implements>`.

**Reviewed for:** Does every screen class under `Screens/` carry its screen mark? Check each screen file.

### Keep Hosts and Failure Views Unmarked

**ID:** `litespec/frontend/mobile/android/stack#keep-hosts-and-failure-views-unmarked`

**Strength:** required

**Statement:** A host Activity or a failure view MUST NOT carry a screen mark.

**Rationale:** A host or failure view draws no screen of its own, so a mark on it links an address to a screen it does not show.

**Example:** The Claimwick host Activity class carries no `<implements>` mark.

**Reviewed for:** Do any host Activities or failure views carry a screen mark? Check the platform project.

### Hold One Navigation Controller in the Platform Project

**ID:** `litespec/frontend/mobile/android/stack#hold-one-navigation-controller-in-the-platform-project`

**Strength:** required

**Statement:** The Android app MUST hold its navigation controller in one class in the platform project, owned by the host Activity.

**Rationale:** One controller holds the current screen, the open overlay, and the history, so no screen navigates on its own.

**Example:** The Claimwick host Activity creates the one controller class, and every expense screen raises events to it.

**Reviewed for:** Does navigation run through the one controller owned by the host Activity, with no screen navigating on its own? Check the screen classes and the host Activity.

### Accept App Links in the Host Activity

**ID:** `litespec/frontend/mobile/android/stack#accept-app-links-in-the-host-activity`

**Strength:** required

**Statement:** The host Activity MUST receive app links in `OnCreate` and `OnNewIntent`, through an `IntentFilter` with `AutoVerify` set.

**Rationale:** Handling both entry points lets a cold start and a link that arrives while the app runs open the same screen, and `AutoVerify` lets the platform open the app instead of a browser.

**Example:** A link to `https://claimwick.example/claims/` opens the claim screen its path names, whether the app was closed or open.

**Reviewed for:** Does the host Activity receive app links on both a cold start and while running? Check the intent filter and both overrides.

### Route the Back Gesture Through the Activity Dispatcher

**ID:** `litespec/frontend/mobile/android/stack#route-the-back-gesture-through-the-activity-dispatcher`

**Strength:** required

**Statement:** The host Activity MUST deliver the system back gesture to the navigation controller through its `OnBackInvokedDispatcher`.

**Rationale:** The dispatcher is the platform's own path for the back signal, so predictive back reaches the controller as the same overlay-then-screen-then-leave order on every screen.

**Example:** On the Claimwick expense detail screen, a back gesture closes an open overlay first, then steps back to the expense list.

**Reviewed for:** Does every back gesture reach the controller through the Activity's dispatcher, leaving the app only from the first screen? Check the host Activity and the controller.

### Apply System Insets to Each Screen

**ID:** `litespec/frontend/mobile/android/stack#apply-system-insets-to-each-screen`

**Strength:** required

**Statement:** A screen MUST apply the system bar and display cutout insets it reads from `WindowInsets` to its controls and text.

**Rationale:** The window supplies these insets, so reading them from `WindowInsets` keeps controls and text clear of the bars and cutouts on every device.

**Example:** The Claimwick expense claim screen pads its submit button by the system bar inset while its background draws full bleed.

**Reviewed for:** Does every screen keep its controls and text clear of the system bars and cutouts? Check the screen layouts and the inset handling.

### Store Navigation State in the Activity Bundle

**ID:** `litespec/frontend/mobile/android/stack#store-navigation-state-in-the-activity-bundle`

**Strength:** required

**Statement:** The host Activity MUST save the navigation controller's state in its `Bundle` from `OnSaveInstanceState` and restore it in `OnCreate`.

**Rationale:** The platform ends the process in the background when memory runs short, and the saved bundle returns the visitor to the same screen with the same overlay and history.

**Example:** When the platform ends the Claimwick process, the app reopens on the expense detail screen with its overlay still open.

**Reviewed for:** Does the host Activity save and restore the controller's navigation state through its `Bundle`? Check the host Activity and the controller.

### Keep Screen Styles in Resources

**ID:** `litespec/frontend/mobile/android/stack#keep-screen-styles-in-resources`

**Strength:** required

**Statement:** A screen MUST take its colors and text styles from `Resources/values/styles.xml` and set no style value in code.

**Rationale:** Resources hold every color and text style in one place, and a screen that sets a value in code is a second style that no resource answers to.

**Example:** The Claimwick expense total uses the `TotalText` style from `Resources/values/styles.xml`, setting no color in its screen class.

**Reviewed for:** Does every screen take its colors and text styles from `Resources/values/styles.xml`, with none set in code? Check the screen classes.

### Read Android Copy Through the Generated Resource Class

**ID:** `litespec/frontend/mobile/android/stack#read-android-copy-through-the-generated-resource-class`

**Strength:** required

**Statement:** A screen MUST read every copy string through the generated `Resource` class, with its fallback in `Resources/values/strings.xml`.

**Rationale:** The generated class gives every copy key a typed name, and the default values folder gives every key a fallback when a language lacks it.

**Example:** The Claimwick expense list reads its empty-state sentence as `Resource.String.ExpenseListEmpty`, with the Spanish file supplying the translation.

**Reviewed for:** Does every screen read its copy through the generated `Resource` class? Check the screen classes and the layouts.

### Store Persisted State in App-Private Storage

**ID:** `litespec/frontend/mobile/android/stack#store-persisted-state-in-app-private-storage`

**Strength:** required

**Statement:** Persisted state and user media MUST live under `Context.FilesDir`, written atomically.

**Rationale:** The app's own sandbox keeps the data away from other apps and the shared media library, and an atomic write keeps the previous complete file if a write stops midway.

**Example:** The Claimwick receipts the visitor adds are copied into the app's files directory, replacing their previous copy only after the new one is whole.

**Reviewed for:** Does all persisted state and user media live under `Context.FilesDir`, written atomically? Check the storage adapters.

### Use the System Pickers for Media

**ID:** `litespec/frontend/mobile/android/stack#use-the-system-pickers-for-media`

**Strength:** required

**Statement:** A screen MUST pick photos through the Photo Picker action `android.provider.action.PICK_IMAGES` and files through `android.intent.action.OPEN_DOCUMENT`.

**Rationale:** Both pickers grant access to the chosen items only, so the app needs no storage permission to read one receipt.

**Example:** In the Claimwick app, adding a receipt photo opens the Photo Picker, and the visitor's choice is the only image the app receives.

**Reviewed for:** Does every screen pick photos and files through the system pickers, with no storage permission requested for it? Check the picking call sites and the manifest.

### Request Permissions at the Point of Use

**ID:** `litespec/frontend/mobile/android/stack#request-permissions-at-the-point-of-use`

**Strength:** required

**Statement:** The Android app MUST request each runtime permission through `RequestPermissions` when the action that needs it starts, never in `OnCreate`.

**Rationale:** A request at the point of use arrives when the visitor knows what it is for, and the same permission answers a question the visitor just asked.

**Example:** The Claimwick app requests the camera permission when the visitor taps Scan receipt, not at launch.

**Reviewed for:** Is every runtime permission requested through `RequestPermissions` when its action starts, none in `OnCreate`? Check the host Activity and the action handlers.

### Request Audio Focus Before Playing Sound

**ID:** `litespec/frontend/mobile/android/stack#request-audio-focus-before-playing-sound`

**Strength:** required

**Statement:** A screen that plays sound MUST request audio focus with an `AudioFocusRequest` before it plays, and pause when focus is lost.

**Rationale:** One speaker serves every app, so the platform arbitrates only the apps that ask for focus, and pausing on loss yields to navigation prompts and calls.

**Example:** The Claimwick confirmation chime plays only after the app holds audio focus, and a phone call pauses it.

**Reviewed for:** Does every screen that plays sound request audio focus first and pause on focus loss? Check the playback call sites.

### Run Android Core Tests on xunit.v3

**ID:** `litespec/frontend/mobile/android/stack#run-android-core-tests-on-xunitv3`

**Strength:** required

**Statement:** The Android app MUST run its core tests as plain .NET test projects on `xunit.v3`, the version `packages.nuget` pins.

**Rationale:** Core tests need no emulator, so a plain .NET runner keeps them fast, and one pinned runner keeps every core project on the same version.

**Example:** The Claimwick expense claim rule has its test in an xunit.v3 project beside the Domain project it proves.

**Reviewed for:** Does every core test run in a plain .NET test project on the pinned `xunit.v3`, with no emulator? Check the core test projects.

### Keep Android Core Test Mocks to the Owned Boundaries

**ID:** `litespec/frontend/mobile/android/stack#keep-android-core-test-mocks-to-the-owned-boundaries`

**Strength:** required

**Statement:** A core test MUST NOT replace the Domain types or the Application handlers it proves.

**Rationale:** A stand-in for the code under test proves the stand-in, so the test reaches the rule only through the real types and handlers.

**Example:** The Claimwick expense claim test builds a real Domain claim and calls the real submit handler, replacing only the platform storage adapter.

**Reviewed for:** Does any core test replace a Domain type or an Application handler? Check the test doubles.

### Declare Android Outside the Controlled UI Contract

**ID:** `litespec/frontend/mobile/android/stack#declare-android-outside-the-controlled-ui-contract`

**Strength:** required

**Statement:** An Android frontend MUST declare an entry point record naming Android and the boundaries that replace the controlled UI contract.

**Rationale:** The controlled contract covers React and Svelte, so an Android frontend with no declaration reads as a frontend with nothing to check. Naming Android and its replacement boundaries reports it as a frontend with named replacements instead.

**Example:** The Claimwick Android entry point record names Android and the native view review standing in for the controlled contract.

**Reviewed for:** Does every Android frontend declare an entry point record naming Android and its replacement boundaries? Check the entry point records.

## Conventions

None.
