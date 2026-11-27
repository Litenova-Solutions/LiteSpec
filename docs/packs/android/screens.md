# Mobile Screens and Navigation

**Stack:** android

**Concern:** frontend-ui

This page states how mobile screens render, how navigation stays in one controller, and how screens follow platform behavior. It is for developers who add screens to a mobile consumer. One controller and native views keep every screen behaving the way platform users expect.

## Concepts

### What Screens and Overlays Are

A screen is one full-window state of the app, such as the expense list or the receipt form. An overlay is a sheet or dialog above the current screen. The distinction sets what back closes first and what process restore returns to.

### How the Controller Owns Navigation

The controller owns the navigation state: the current screen, the open overlay, and the history behind them. Screens raise events instead of navigating anywhere themselves, and the controller answers each event by showing a screen, opening an overlay, or stepping back. App links, the back gesture, and process recreation all flow through the controller.

Native views draw each screen, and the platform supplies back, insets, and motion behavior. Edge-to-edge drawing uses the full display, with controls and text inside the system bar and cutout insets. Reduced motion replaces looping content with a still state, and each view releases its media when it leaves the window.

## Standards

### Render with the Platform's Own Views

**ID:** `litespec/packs/android/screens#render-with-the-platforms-own-views`

**Strength:** required

**Statement:** A mobile app MUST render every screen with the platform's native UI toolkit, with no product screen in a WebView or a cross-platform UI layer.

**Rationale:** The native toolkit gives each screen the platform behavior for focus, text input, and accessibility without extra work. A WebView or a cross-platform layer reimplements that behavior and lags behind it, so the screen feels foreign on its own platform. One toolkit also keeps every screen testable through the platform device tests.

**Reviewed for:** Does every product screen render with the platform's native UI toolkit, none in a WebView or cross-platform layer? Check the screen implementations.

### Own Navigation in One Controller

**ID:** `litespec/packs/android/screens#own-navigation-in-one-controller`

**Strength:** required

**Statement:** The current screen, the open overlay, and the history MUST live in one controller per app, with screens raising events instead of navigating.

**Rationale:** A screen that navigates itself keeps a second history nobody else reads, so back behavior and deep links disagree with what the screen shows. Holding the current screen, the open overlay, and the history in one controller gives navigation one answer for every question. Events keep screens decoupled from the controller, so the controller alone decides what each event opens.

**Reviewed for:** Does all navigation state live in the one controller, with screens raising events instead of navigating? Check the controller and the screens.

### Route App Links to Flow Entry Screens

**ID:** `litespec/packs/android/screens#route-app-links-to-flow-entry-screens`

**Strength:** required

**Statement:** A screen a flow can be entered from MUST open directly from its app link, with the controller resolving the link to that screen and its record.

**Rationale:** A flow that starts outside the app, in a message or a browser page, lands on the screen the link names rather than on the first screen with the work still to find. The controller resolves the link, so a cold start and a link that arrives while the app runs open the same screen with the same history behind it. Each stack page names how its platform delivers the link to the controller.

**Example:** A Claimwick approval message for expense EXP-1042 carries an app link that opens the approval screen for EXP-1042. The expenses list sits behind it for back, so the visitor returns to familiar ground.

**Reviewed for:** Does every app link open the flow entry screen and record it names, resolved through the one controller? Check the app link handling and the controller.

### Follow the System Back Gesture

**ID:** `litespec/packs/android/screens#follow-the-system-back-gesture`

**Strength:** required

**Statement:** The system back gesture MUST close the open overlay first, then step back one screen, and leave the app only from the first screen.

**Rationale:** Users learn one back behavior per platform: the gesture dismisses what is on top before it leaves where they are. An app that leaves from a deeper screen, or that traps the gesture on the first screen, breaks a habit the whole platform teaches. Closing the overlay first, then stepping back one screen, then leaving from the first screen matches that habit on every screen.

**Reviewed for:** Does the back gesture close the overlay first, step back one screen, and leave only from the first screen? Check the back handling.

### Draw Edge to Edge with Insets

**ID:** `litespec/packs/android/screens#draw-edge-to-edge-with-insets`

**Strength:** required

**Statement:** Screens MUST draw under the system bars and apply the system bar and cutout insets to their controls and text.

**Rationale:** Drawing under the system bars uses the whole display, but controls and text under a bar or a cutout turn unreadable and untappable. Applying the bar and cutout insets moves controls and text into the visible area while backgrounds keep the full-bleed look. Each stack page names the inset source for its platform.

**Reviewed for:** Does every screen draw edge to edge with its controls and text inside the system bar and cutout insets? Check the screen layouts.

### Honor Reduced Motion

**ID:** `litespec/packs/android/screens#honor-reduced-motion`

**Strength:** required

**Statement:** When the system turns animations off, a screen MUST replace looping motion and video with a still state.

**Rationale:** Some users turn animations off because motion causes discomfort, so a screen that keeps looping motion ignores an explicit request. A still state shows the same content without the movement: the final frame, the resting layout. Applying the system setting on every screen keeps the whole app honoring one choice.

**Reviewed for:** With animations off, does every screen replace looping motion and video with a still state? Check the screens with animations off.

### Release Media with Its Screen

**ID:** `litespec/packs/android/screens#release-media-with-its-screen`

**Strength:** required

**Statement:** A player, animator, or decoded image MUST be released or cancelled when its view leaves the window.

**Rationale:** Media holds memory and hardware the app stops showing once its view leaves the window. A player that keeps playing, an animator that keeps running, or a decoded image that stays resident drains the battery and starves the screen the user now sees. Releasing or cancelling with the view ties each life to the screen that uses it.

**Reviewed for:** Is every player, animator, and decoded image released or cancelled when its view leaves the window? Check the view teardown.

### Meet Touch and Text Accessibility

**ID:** `litespec/packs/android/screens#meet-touch-and-text-accessibility`

**Strength:** required

**Statement:** Every interactive view MUST carry a content description, meet a minimum touch target of 48 dp on Android or 44 pt on iOS, and scale its text with the system font scale.

**Rationale:** A view without a content description stays silent to a screen reader, and a target below the platform minimum turns too small to tap reliably. Text fixed at one size ignores the user who enlarged the system font to read. Meeting the platform target size and scaling with its font scale keeps every screen usable with the platform accessibility settings.

**Reviewed for:** Does every interactive view carry a content description and the platform-minimum touch target, with text scaling with the system font scale? Check the views, the text styles, and the accessibility device tests the Mobile Testing page names.

## Conventions

### Restore the User's Place

**ID:** `litespec/packs/android/screens#restore-the-users-place`

**Strength:** default

**Default:** Save the controller's navigation state in the platform's saved-state mechanism so a process recreation returns to the same screen.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The platform destroys background processes when memory runs short. Without saved state, recreation drops the user on the first screen. With the controller navigation state in the platform saved-state mechanism, the user returns to the same screen with the same overlay and history.

**Reviewed for:** Does every process recreation return to the screen the saved navigation state names? Check the saved-state handling.
