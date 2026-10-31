# Mobile Screens and Navigation

## Intent

A mobile app renders its screens with the platform's native views, holds navigation in one controller, and follows the platform's back, inset, and motion behavior. This page states those screen and navigation rules for every mobile stack. A team that applies it while adding screens keeps navigation in one place and every screen behaving the way the platform's users expect.

## Concepts

### Screens, Overlays, and the Controller

A screen is one full-window state of the app, such as the expense list or the receipt form; an overlay is a sheet or dialog above the current screen. The controller owns the navigation state: the current screen, the open overlay, and the history behind them. Screens raise events, and the controller answers each event by showing a screen, opening an overlay, or stepping back.

## Standards

### Render with the Platform's Own Views

**ID:** `litespec/frontend/mobile/screens#render-with-the-platforms-own-views`

**Statement:** A mobile app MUST render every screen with the platform's native UI toolkit, with no product screen in a WebView or a cross-platform UI layer.

**Rationale:** The platform's native toolkit gives each screen the platform's behavior for focus, text input, and accessibility without extra work. A WebView or a cross-platform layer reimplements that behavior and lags behind it, so the screen feels foreign on its own platform. One toolkit also keeps every screen testable through the platform's own device tests.

**Reviewed for:** Which product screens render in a WebView or a cross-platform UI layer instead of the platform's native UI toolkit? Look in the screen implementations.

### Own Navigation in One Controller

**ID:** `litespec/frontend/mobile/screens#own-navigation-in-one-controller`

**Statement:** The current screen, the open overlay, and the history MUST live in one controller per app, with screens raising events instead of navigating.

**Rationale:** One controller holding the current screen, the open overlay, and the history gives every navigation question one answer. A screen that navigates on its own hides a second history no other screen can read, so back behavior and deep links disagree with what the screen shows. Events keep screens decoupled from the controller, so the controller alone decides what each event opens.

**Reviewed for:** Which navigation state lives outside the one controller, and which screens navigate instead of raising events? Look in the controller and the screens.

### Route App Links to Journey Entry Screens

**ID:** `litespec/frontend/mobile/screens#route-app-links-to-journey-entry-screens`

**Statement:** A screen a journey can be entered from MUST open directly from its app link, with the controller resolving the link to that screen and its record.

**Rationale:** A journey that starts outside the app, in a message or a browser page, lands on the screen the link names rather than on the first screen with the work still to find. The controller resolves the link, so a cold start and a link that arrives while the app runs open the same screen with the same history behind it. Each stack page names how its platform delivers the link to the controller.

**Example:** An approval message for expense EXP-1042 carries an app link that opens the approval screen for EXP-1042, with the expenses list behind it for back.

**Reviewed for:** Which journey entry screens open the first screen instead of the screen and record their app link names, and which links resolve outside the one controller? Look in the app link handling and the controller.

### Follow the System Back Gesture

**ID:** `litespec/frontend/mobile/screens#follow-the-system-back-gesture`

**Statement:** The system back gesture MUST close the open overlay first, then step back one screen, and leave the app only from the first screen.

**Rationale:** Users learn one back behavior per platform: the gesture dismisses what is on top before it leaves where they are. An app that leaves from a deeper screen, or that traps the gesture on the first screen, breaks a habit the whole platform teaches. Overlay first, then one screen back, then out from the first screen matches that habit on every screen. Each stack page names how the platform's back signal reaches the controller.

**Reviewed for:** Which back gestures skip the overlay, jump more than one screen, or leave the app from a deeper screen? Look in the back handling.

### Draw Edge to Edge with Insets

**ID:** `litespec/frontend/mobile/screens#draw-edge-to-edge-with-insets`

**Statement:** Screens MUST draw under the system bars and apply the system bar and cutout insets to their controls and text.

**Rationale:** Drawing under the system bars uses the whole display, but controls and text under a bar or a cutout are unreadable and untappable. Applying the system bar and cutout insets moves controls and text into the visible area while backgrounds keep the full-bleed look. Each stack page names the inset source for its platform.

**Reviewed for:** Which screens clip controls or text under the system bars without applying the insets? Look in the screen layouts.

### Honor Reduced Motion

**ID:** `litespec/frontend/mobile/screens#honor-reduced-motion`

**Statement:** When the system turns animations off, a screen MUST replace looping motion and video with a still state.

**Rationale:** Some users turn animations off because motion causes discomfort, so a screen that keeps looping motion ignores an explicit request. A still state shows the same content without the movement: the final frame, the resting layout. Applying the system's setting on every screen keeps the whole app honoring one choice.

**Reviewed for:** Which screens keep looping motion or video with animations off instead of showing a still state? Look in the screens with animations off.

### Release Media with Its Screen

**ID:** `litespec/frontend/mobile/screens#release-media-with-its-screen`

**Statement:** A player, animator, or decoded image MUST be released or cancelled when its view leaves the window.

**Rationale:** Media holds memory and hardware the app no longer shows once its view leaves the window. A player that keeps playing, or a decoded image that stays resident, drains the battery and starves the screen the user now sees. Releasing with the view ties each resource's life to the screen that uses it.

**Reviewed for:** Which players, animators, or decoded images survive their view leaving the window? Look in the view teardown.

### Meet Touch and Text Accessibility

**ID:** `litespec/frontend/mobile/screens#meet-touch-and-text-accessibility`

**Statement:** Every interactive view MUST carry a content description, meet a minimum touch target of 48 dp on Android or 44 pt on iOS, and scale its text with the system font scale.

**Rationale:** A view without a content description is silent to a screen reader, and a target below the platform minimum is missed by the finger it was drawn for. Text fixed at one size ignores the reader who enlarged the system font to read at all. Meeting the platform target size and scaling with its font scale keeps every screen usable with the platform's own accessibility settings. The device tests on the [Mobile Testing](testing.md) page prove each screen against this rule.

**Reviewed for:** Which interactive views lack a content description or the platform-minimum touch target, and which text ignores the system font scale? Look in the views, the text styles, and the accessibility device tests the Mobile Testing page names.

## Conventions

### Restore the Visitor's Place

**ID:** `litespec/frontend/mobile/screens#restore-the-visitors-place`

**Default:** Save the controller's navigation state in the platform's saved-state mechanism so a process recreation returns to the same screen.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The platform may destroy the app's process while it is in the background, and recreation without saved state drops the visitor on the first screen. Saving the controller's navigation state in the platform's saved-state mechanism returns the visitor to the same screen with the same overlay and history. Each stack page names the saved-state mechanism for its platform.

**Reviewed for:** Which process recreations drop the visitor outside the screen the saved navigation state names? Look in the saved-state handling.
