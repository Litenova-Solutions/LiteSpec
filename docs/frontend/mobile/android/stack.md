# Android Stack

## Intent

The Android stack is the technology binding for a product that ships a native Android application built with .NET for Android in C# against plain Android views. A team chooses it with either shape, `client-only` or `server`, declared in [Supported Scope](../../../core/scope.md). This page states what the stack itself requires, beside the shared rules in [Mobile overview](../README.md); exact .NET and Android API versions live only in `litespec.manifest.json`.

## Concepts

### Route Entry Bindings

A route entry is a destination: an `Activity` or fragment class that is a navigation destination. Its screen mark sits on that destination class in the .NET mark style the project declares in `litespec.project.json`, read by the .NET reader. A host activity that only hosts destinations, layout files, and classes that are not destinations are not route entries and carry no screen mark.

## Standards

### Build with .NET for Android

**ID:** `litespec/frontend/mobile/android/stack#build-with-net-for-android`

**Statement:** A consumer selecting this stack MUST build the Android app as a `net{dotnet}-android` project with .NET for Android at the .NET version and Android API level `litespec.manifest.json` pins (`stack.dotnet`, `stack.android`), using plain Android views with no .NET MAUI or Xamarin.Forms product screen.

**Rationale:** One target framework keeps every Android build on the same runtime and API surface, so a screen written against one API level behaves the same on every build machine. Pinning both versions in the manifest makes the toolchain a reviewed number instead of a local install detail. Plain Android views keep the app on the platform toolkit the views page governs, while a MAUI or Forms layer would add a second UI framework with its own rules.

**Reviewed for:** The Android project targets `net{dotnet}-android` at the manifest pins with plain Android views and no MAUI or Forms screen.

### Keep Android Outside the UI Contract

**ID:** `litespec/frontend/mobile/android/stack#keep-android-outside-the-ui-contract`

**Statement:** A consumer selecting this stack MUST NOT apply the floorplan, token, and route-suite contract of the controlled UI pages.

**Rationale:** That contract's floorplan, token, and route-suite checks read TypeScript sources, so they cannot read an Android view written in C#. Applying them to this stack would fail every check for lack of readable input, as `litespec/frontend/blazor/stack#keep-blazor-outside-the-ui-contract` states for Blazor. The mobile and Android pages state the styling, accessibility, and state rules that apply instead, so nothing is left ungoverned. Screen records and route marks still apply on this stack: the .NET reader reads the destination class marks, and the screen, `calls`, and `refusal-sentence-missing` checks run from those marks.

**Reviewed for:** The consumer's stack selection and CI jobs apply no floorplan, token, or route-suite check, while its destination classes carry screen marks the .NET reader reads.

### Record the Minimum Android Version

**ID:** `litespec/frontend/mobile/android/stack#record-the-minimum-android-version`

**Statement:** The app project MUST set `SupportedOSPlatformVersion` with a project decision recording why that floor was chosen.

**Rationale:** The floor decides which devices install the app and which platform APIs the code reaches without a guard. An unrecorded floor drifts with each toolchain update, silently dropping devices or blocking APIs. A decision naming the reason lets a later reader move the floor deliberately instead of guessing what it protected.

**Reviewed for:** The app project sets `SupportedOSPlatformVersion` with a decision recording why that floor was chosen.

### Pin Every Android Binding Package

**ID:** `litespec/frontend/mobile/android/stack#pin-every-android-binding-package`

**Statement:** An AndroidX or Google binding package MUST be pinned in `litespec.manifest.json` and approved before use, as `litespec/workspace/dependencies#approve-new-packages-explicitly` requires.

**Rationale:** Binding packages track platform releases closely, so an unpinned binding moves the app to untested platform behavior on the next restore. Pinning the version in the manifest makes each binding a reviewed number with a fixed API surface. Approval before use records the use case, the owning layer, and the removal condition, so an unused binding leaves the tree.

**Reviewed for:** Each AndroidX or Google binding resolves to a manifest pin with an approval decision.

## Conventions

### Verify with the .NET Toolchain

**ID:** `litespec/frontend/mobile/android/stack#verify-with-the-net-toolchain`

**Default:** Build and publish the Android app through `dotnet` rather than Gradle or Android Studio, with publish producing an AAB.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The Android project is a .NET project, so `dotnet` builds and publishes the whole mobile solution. A Gradle or Android Studio step in CI would add a second toolchain with its own downloads and configuration for work `dotnet` already does. A consumer keeps platform tooling on developer machines for emulators and debugging, while CI stays on one toolchain.

**Example:**

```bash
dotnet build apps/mobile/{ProjectName}.Mobile.slnx
dotnet publish apps/mobile/src/{ProjectName}.Android -c Release
```

**Reviewed for:** The CI mobile job runs `dotnet build` and `dotnet publish` with no Gradle or Android Studio step.

### Record a Package Size Budget

**ID:** `litespec/frontend/mobile/android/stack#record-a-package-size-budget`

**Default:** Record the published AAB size as a limit entry in `docs/behavior/operations/limits.md`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** An install payload is the slowest moment of adoption, so its size deserves a reviewed number instead of a surprise. A recorded budget on the published AAB size makes growth visible before the store listing carries it. Untracked sizes grow with each asset until installs stall on slow networks.

**Reviewed for:** A limit entry in `docs/behavior/operations/limits.md` records the AAB size budget the publish job enforces.

### Store Streamed Media Uncompressed

**ID:** `litespec/frontend/mobile/android/stack#store-streamed-media-uncompressed`

**Default:** List media the app plays from assets through `AssetManager.OpenFd` in `AndroidStoreUncompressedFileExtensions`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** `AssetManager.OpenFd` maps a file descriptor onto the asset, which works only when the package stores the file uncompressed. A compressed asset forces the player onto a slower path or fails the open, with the failure surfacing on a device rather than at build time. Listing each streamed extension keeps the packaging choice beside the playback code that depends on it.

**Reviewed for:** Each media extension the app streams through `AssetManager.OpenFd` appears in `AndroidStoreUncompressedFileExtensions`.
