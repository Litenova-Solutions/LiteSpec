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

**Reviewed for:** The app holds one activity, with navigation state owned by its controller.

### Build Views in Code or Layout Resources, Not Both

**ID:** `litespec/frontend/mobile/android/views#build-views-in-code-or-layout-resources-not-both`

**Statement:** A screen MUST be built either in C# code or from a layout resource, with one app using one approach for its screens.

**Rationale:** Two approaches in one app force every reader to learn both and every change to guess which one the next screen uses. Code-built views keep logic and layout in one language, while layout resources keep structure in XML the tooling previews. One approach per app keeps every screen readable the same way.

**Reviewed for:** Each screen builds in C# code or from a layout resource, with one approach across the app.

### Keep Text, Colors, and Icons in Resources

**ID:** `litespec/frontend/mobile/android/views#keep-text-colors-and-icons-in-resources`

**Statement:** User-visible text MUST come from `Resources/values*/strings.xml`, with colors and icons from color and drawable resources.

**Rationale:** A language becomes a resource folder when every string lives in `strings.xml`, with no code change per locale. A palette change becomes one file when every color and icon lives in color and drawable resources. Hard-coded text and colors scatter both across screens, so each translation or rebrand hunts them down.

**Reviewed for:** User-visible text resolves to `strings.xml` entries, with colors and icons in color and drawable resources.

### Use the Android Back Dispatcher

**ID:** `litespec/frontend/mobile/android/views#use-the-android-back-dispatcher`

**Statement:** Back handling MUST go through `OnBackInvokedDispatcher` on API 33 and later and `OnBackPressed` below it, routed to the navigation controller.

**Rationale:** The dispatcher is the one entry the system back gesture reaches, so handling it there keeps overlays, screens, and the app exit in one order. Routing each press to the navigation controller keeps the decision with the history it owns. This realizes `litespec/frontend/mobile/screens#follow-the-system-back-gesture` for Android.

**Reviewed for:** Back handling routes through the API-level dispatcher to the navigation controller.

### Apply Insets from WindowInsets

**ID:** `litespec/frontend/mobile/android/views#apply-insets-from-windowinsets`

**Statement:** Views MUST read system bar and cutout insets from `WindowInsets` and apply them as padding or margins.

**Rationale:** Edge-to-edge drawing puts content under the system bars, so each view offsets its controls and text by the insets the platform reports. Reading them from `WindowInsets` tracks bars, cutouts, and keyboards as they change. This realizes `litespec/frontend/mobile/screens#draw-edge-to-edge-with-insets` for Android.

**Reviewed for:** Views apply system bar and cutout insets from `WindowInsets` as padding or margins.

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

namespace Shop.Android.Screens;

internal sealed class PosterView : View
{
}
```

**Reviewed for:** SDK types in the `.Android` root resolve through `using` directives or `global::Android`.
