# Android Views

## Intent

Android screens are plain Android views hosted in one activity, with user-visible text, colors, and icons held in resources. This page states how an Android app meets the shared rules in [Mobile Screens and Navigation](../screens.md): the single activity, the view approach, resources, back handling, and insets. A team that applies it while adding screens keeps one navigation owner and one resource home for every string and color.

## Concepts

### The Single Activity

One activity hosts every screen as a view, and its navigation controller owns the current screen, the open overlay, and the history. Screens raise events, and the controller answers by showing the next screen or overlay. [Mobile Screens and Navigation](../screens.md) states the shared navigation rules this page binds to Android dispatchers and insets.

## Standards

### Host Screens in One Activity

**ID:** `litespec/frontend/mobile/android/views#host-screens-in-one-activity`

**Statement:** The app MUST host its screens as views inside one activity whose controller owns navigation.

**Rationale:** One activity gives the app one lifecycle owner and one place where navigation state lives. Screens raise events instead of navigating, so the controller answers each event from the full history it holds. This realizes `litespec/frontend/mobile/screens#own-navigation-in-one-controller` for Android.

**Reviewed for:** Which apps host screens outside the one activity, or hold navigation state outside its controller? Look in the activities and the controller.

### Build Views in Code or Layout Resources, Not Both

**ID:** `litespec/frontend/mobile/android/views#build-views-in-code-or-layout-resources-not-both`

**Statement:** A screen MUST be built either in C# code or from a layout resource, with one app using one approach for its screens.

**Rationale:** Two approaches in one app force every reader to learn both and every change to guess which one the next screen uses. Code-built views keep logic and layout in one language, while layout resources keep structure in XML the tooling previews. One approach per app keeps every screen readable the same way.

**Reviewed for:** Which screens mix C# code and layout resources, or break the one approach across the app? Look in the screen implementations.

### Keep Text, Colors, and Icons in Resources

**ID:** `litespec/frontend/mobile/android/views#keep-text-colors-and-icons-in-resources`

**Statement:** User-visible text MUST come from `Resources/values*/strings.xml`, with colors and icons from color and drawable resources.

**Rationale:** A language becomes a resource folder when every string lives in `strings.xml`, with no code change per locale. A palette change becomes one file when every color and icon lives in color and drawable resources. Hard-coded text and colors scatter both across screens, so each translation or rebrand hunts them down.

**Reviewed for:** Which user-visible text, colors, or icons sit outside `strings.xml`, color, or drawable resources? Look in the screens and the resources.

### Use the Android Back Dispatcher

**ID:** `litespec/frontend/mobile/android/views#use-the-android-back-dispatcher`

**Statement:** Back handling MUST go through `OnBackInvokedDispatcher` on API 33 and later and `OnBackPressed` below it, routed to the navigation controller.

**Rationale:** The dispatcher is the one entry the system back gesture reaches, so handling it there keeps overlays, screens, and the app exit in one order. Routing each press to the navigation controller keeps the decision with the history it owns. This realizes `litespec/frontend/mobile/screens#follow-the-system-back-gesture` for Android.

**Reviewed for:** Which back handling bypasses the API-level dispatcher or the navigation controller? Look in the back handling.

### Apply Insets from WindowInsets

**ID:** `litespec/frontend/mobile/android/views#apply-insets-from-windowinsets`

**Statement:** Views MUST read system bar and cutout insets from `WindowInsets` and apply them as padding or margins.

**Rationale:** Edge-to-edge drawing puts content under the system bars, so each view offsets its controls and text by the insets the platform reports. Reading them from `WindowInsets` tracks bars, cutouts, and keyboards as they change. This realizes `litespec/frontend/mobile/screens#draw-edge-to-edge-with-insets` for Android.

**Reviewed for:** Which views miss system bar or cutout insets from `WindowInsets` as padding or margins? Look in the view layouts.

### Persist Navigation State in the Activity Bundle

**ID:** `litespec/frontend/mobile/android/views#persist-navigation-state-in-the-activity-bundle`

**Statement:** The host activity MUST write the controller navigation state into the `Android.OS.Bundle` in `Activity.OnSaveInstanceState` and restore it from the bundle passed to `Activity.OnCreate` or `Activity.OnRestoreInstanceState`.

**Rationale:** The activity bundle is the saved-state mechanism this stack uses on .NET for Android, taking the place a `SavedStateHandle` holds in a Jetpack ViewModel. The platform calls `OnSaveInstanceState` before it kills the activity so the state can be restored in `OnCreate` or `OnRestoreInstanceState`, which returns the visitor to the same screen after process death without treating a finished activity as restorable. Saving the current screen, the open overlay, and the history in that bundle keeps restoration inside the one controller rather than in each view. This realizes `litespec/frontend/mobile/screens#restore-the-visitors-place` for Android. [The `Activity.OnSaveInstanceState` reference](https://learn.microsoft.com/en-us/dotnet/api/android.app.activity.onsaveinstancestate) states the contract.

**Example:** Before the platform reclaims the process behind the EXP-1042 expense screen, the host activity writes the expense screen and its place in the history into the bundle; recreation restores that screen from the bundle instead of opening the expenses list.

**Reviewed for:** Which host activities save no navigation state in `OnSaveInstanceState`, or restore it outside `OnCreate` and `OnRestoreInstanceState`? Look in the host activity lifecycle methods.

### Deliver App Links to the Controller

**ID:** `litespec/frontend/mobile/android/views#deliver-app-links-to-the-controller`

**Statement:** The host activity MUST declare an intent filter for each app link its screens answer and pass the arriving `Intent.Data` to the navigation controller in `Activity.OnCreate` and `Activity.OnNewIntent`.

**Rationale:** An app link reaches the app as an intent on the host activity, on a cold start through `OnCreate` and on a running app through `OnNewIntent`. Declaring the intent filter for each link the screens answer lets the platform offer the app for those links, and passing `Intent.Data` to the controller in both entry points makes the two arrivals open the same journey entry screen. A link handled in a view instead bypasses the history the controller owns, so back leaves through a different path than the screen shows. This realizes `litespec/frontend/mobile/screens#route-app-links-to-journey-entry-screens` for Android. [The Android app links guidance](https://developer.android.com/training/app-links/add-applinks) states the intent-filter form.

**Example:** The approval link for expense EXP-1042 arrives as `Intent.Data` on the host activity; the controller opens the approval screen for EXP-1042 with the expenses list behind it, on a cold start and on a running app alike.

**Reviewed for:** Which app links have no intent filter on the host activity, and which arriving intents never reach the navigation controller from `OnCreate` or `OnNewIntent`? Look in the host activity attributes and its intent handling.

## Conventions

### Avoid Platform Names That Collide with the App Namespace

**ID:** `litespec/frontend/mobile/android/views#avoid-platform-names-that-collide-with-the-app-namespace`

**Default:** In a root namespace ending in `.Android`, refer to SDK types through `using` directives or the `global::Android` qualifier.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A bare `Android.` prefix resolves to the app namespace when the root ends in `.Android`, hiding the SDK types the screen meant to name. A `using` directive or the `global::Android` qualifier names the SDK without the clash. One habit per codebase keeps every file readable without checking which `Android` each prefix names.

**Example:**

```csharp
using Android.Graphics;
using Android.Views;

namespace Claimwick.Android.Screens;

internal sealed class PosterView : View
{
}
```

**Reviewed for:** Which SDK types in the `.Android` root resolve through a bare `Android.` prefix instead of `using` directives or `global::Android`? Look in the `.Android` sources.
