# Android UI Primitives

**Stack:** android
**Concern:** frontend-ui

An Android frontend serves the same jobs as the web product through phone ergonomics: one job per screen, bottom navigation, drill-down settings, and tokens for spacing, targets, type, and color. No screen invents its own margins, colors, or navigation.

This page states the Android visual contract: the single-job screen discipline, the navigation shape, the resource tokens, and the visual calibration that holds them.

## Concepts

### One Job per Screen

Each Android screen under `Screens/` serves one job with at most two or three actions. The limit is stricter than desktop: a phone screen shows less, so a screen that tries two jobs shows neither well. A job that needs more actions splits into two screens with one step each.

The Claimwick expense list screen shows waiting expenses with search and open actions only. Filing a new expense is a second screen with its own focused form.

### Bottom Navigation and the Settings Hub

The app navigates through bottom navigation with three to five primary destinations. The bar stays visible on top-level screens, and each destination opens its own stack. No drawer or sidebar carries primary navigation on the phone.

Settings live in a hub list that pushes one focused drill-down screen per configuration facet. Each drill-down edits one facet with one save path. Stacked multi-form cards never share a phone screen: two facets side by side shrink both below usable width.

### Spacing, Target, Type, and Color Tokens

All visual values live in resources. Spacing uses 4dp, 8dp, 16dp, and 24dp steps. Touch targets stand at least 48dp. Type styles and Material 3 color roles live in `styles.xml`. Screen code in `Screens/*.cs` sets no size, margin, or color of its own: the Roslyn analyzer and review check refuse inline values, so every value resolves to a named token.

The Claimwick app pads list rows 16dp, separates sections 24dp, and draws amounts in the agreed money style from resources.

### Visual Calibration

Five screens set the visual baseline: shell with bottom navigation, list, record detail, settings drill-down, and form. The team approves each on the standard emulator viewport at 1080 by 2400, in light and dark. Emulator screenshot capture runs inside `litespec ui review`, so every later screen compares against an approved look instead of a description. The table names each golden screen with what it locks.

| Golden screen | What it locks |
|:---|:---|
| Shell with bottom navigation | Destinations, bar visibility, and the workspace switcher |
| List | Row layout, search placement, and empty state |
| Record detail | Header, state badge, primary action, and peer views |
| Settings drill-down | Hub list, facet layout, and the save path |
| Form | Field rhythm, section breaks, and the footer bar |

## Standards

### Keep One Job per Screen

**ID:** `litespec/packs/android/ui-primitives#keep-one-job-per-screen`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A screen MUST serve one job with at most three actions.

**Rationale:** A phone screen shows less than a desktop page, so a second job crowds out the first. One job per screen keeps each screen focused enough to finish with a thumb. A job that needs more actions splits into two screens.

**Example:** The Claimwick approval screen shows one approval with approve, refuse, and history actions only.

**Reviewed for:** Does any screen serve two jobs or carry more than three actions? Check each screen class under `Screens/`.

### Keep Primary Navigation in the Bottom Bar

**ID:** `litespec/packs/android/ui-primitives#navigate-with-bottom-navigation`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Primary navigation MUST run through bottom navigation with three to five destinations, visible on top-level screens.

**Rationale:** The bottom bar sits under the thumb and stays visible, so the visitor always knows where they are and where they can go. A drawer hides destinations behind a gesture, and more than five items shrink below tappable width.

**Example:** The Claimwick app navigates between home, documents, entries, and settings through four bottom destinations.

**Reviewed for:** Does primary navigation run through a bottom bar of three to five destinations on top-level screens? Check the navigation controller and the host Activity.

### Show One Settings Facet per Screen

**ID:** `litespec/packs/android/ui-primitives#drill-down-settings-one-facet-per-screen`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Settings MUST open as a hub list that pushes one focused drill-down screen per configuration facet, with no screen holding two facets.

**Rationale:** A hub list names every facet in one place, and each drill-down edits one facet with one save path. Two facets on one phone screen stack long forms that bury the second facet below the fold.

**Example:** The Claimwick business settings hub pushes one drill-down for invoicing details and another for senders, each with its own save.

**Reviewed for:** Does any settings screen hold two configuration facets? Check each settings screen under `Screens/`.

### Keep Spacing and Type in Resource Tokens

**ID:** `litespec/packs/android/ui-primitives#keep-spacing-and-type-in-resource-tokens`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Spacing and type MUST resolve to named resource tokens: spacing to the 4dp, 8dp, 16dp, and 24dp steps in `Resources/values/dimens.xml`, and type to the styles in `Resources/values/styles.xml`.

**Rationale:** Named tokens keep every screen on one rhythm: two screens from different authors still align. Inline values drift one screen at a time until the app holds a dozen near-equal paddings.

**Example:** The Claimwick list screen pads rows 16dp from `dimens.xml` and draws titles in the agreed style from `styles.xml`.

**Reviewed for:** Does every spacing and type value resolve to a named resource token? Check the screen classes and layouts beside `dimens.xml` and `styles.xml`.

### Keep 48dp Minimum Touch Targets

**ID:** `litespec/packs/android/ui-primitives#keep-48dp-minimum-touch-targets`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Every interactive view MUST offer a touch target of at least 48dp.

**Rationale:** A smaller target misses taps, especially while walking. The 48dp minimum matches the platform rule the device tests prove.

**Example:** The Claimwick approve button keeps its full 48dp target even where its visible shape is smaller.

**Reviewed for:** Does every interactive view meet the 48dp minimum? Check the device tests beside the screens.

### Use Material 3 Color Roles

**ID:** `litespec/packs/android/ui-primitives#use-material-3-color-roles`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Screen colors MUST come from Material 3 color roles declared in resources, never from hardcoded color values in screen code.

**Rationale:** Color roles carry both light and dark values with the contrast each pair promises. A hardcoded color fixes one theme and breaks the other, so dark mode shows unreadable text on dark fills.

**Example:** The Claimwick warning banner uses the warning role, which resolves to its light value in day mode and its dark value at night.

**Reviewed for:** Does any screen class set a hardcoded color? Check the screen classes beside the declared color roles.

### Refuse Inline Sizing in Screens

**ID:** `litespec/packs/android/ui-primitives#refuse-inline-sizing-in-screens`

**Strength:** required

**Applies to:** artifact: component

**Statement:** C# screen classes MUST NOT set pixel sizes, margins, or colors inline.

**Rationale:** An inline value bypasses the tokens review approved, so the screen drifts from the baseline one value at a time. Refusing inline values forces every value through a named token a reviewer can check.

**Example:** A Claimwick screen that needs wider padding adds a token to `dimens.xml` instead of setting pixels in `Screens/`.

**Checked by:** The Roslyn analyzer reports inline sizing in screen classes, and review refuses any `Screens/*.cs` change that sets a size, margin, or color.

### Approve Five Golden Archetype Screens

**ID:** `litespec/packs/android/ui-primitives#approve-five-golden-archetype-screens`

**Strength:** required

**Applies to:** artifact: component

**Statement:** The team MUST approve five golden screens before building all routes: shell with bottom navigation, list, record detail, settings drill-down, and form, each on the 1080 by 2400 emulator viewport in light and dark.

**Rationale:** Five approved screens lock the look early: spacing, type, color, and navigation read in pixels, not prose. Later screens copy approved patterns instead of inventing new ones.

**Example:** The Claimwick team approves the golden expense list on the emulator in both themes before building the remaining screens.

**Reviewed for:** Do the five golden screens carry recorded approval on the stated viewport in both themes? Check the recorded baseline beside the emulator screenshots.

### Show One Primary Action per Screen

**ID:** `litespec/packs/android/ui-primitives#show-one-primary-action-per-screen`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A screen MUST offer at most one primary action, placed where the thumb reaches it.

**Rationale:** Two primaries force the visitor to weigh buttons instead of acting. One primary action names the screen's job: everything else stays secondary or moves to another screen.

**Example:** The Claimwick expense list offers New filing as its single primary action, with search and open as secondary.

**Reviewed for:** Does any screen offer two primary actions? Check each screen class under `Screens/`.

### Keep the Bottom Bar on Top-Level Screens

**ID:** `litespec/packs/android/ui-primitives#keep-the-bottom-bar-on-top-level-screens`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Top-level destinations MUST keep the bottom bar visible, with drill-down and form screens showing a back path instead.

**Rationale:** The bar orients the visitor among destinations, so it stays where destinations change. A drill-down or form has one job with one way back, so a back path replaces the bar and keeps the focus on the task.

**Example:** The Claimwick list screens keep the bar visible, while the filing form shows a back path to the list.

**Reviewed for:** Does the bar stay visible on every top-level destination with a back path on deeper screens? Check the navigation controller.

### Match Type to the System Setting

**ID:** `litespec/packs/android/ui-primitives#scale-type-with-the-system-setting`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Text MUST scale with the system font scale without clipping or overlap.

**Rationale:** Readers who enlarge system text do so to read. Fixed-size text ignores their choice, and clipped text hides the words they enlarged to see. Fluid layouts with scaled type keep every screen readable at every setting.

**Example:** The Claimwick expense total wraps instead of clipping when the reader enlarges system text.

**Reviewed for:** Does any text clip or overlap at enlarged font scales? Check the device tests beside the screens.

### Record Emulator Screenshots in UI Review

**ID:** `litespec/packs/android/ui-primitives#capture-emulator-screenshots-in-ui-review`

**Strength:** required

**Applies to:** artifact: component

**Statement:** `litespec ui review` MUST capture emulator screenshots of new or changed screens beside their golden archetype screen.

**Rationale:** A side-by-side capture shows drift the text review misses: misaligned columns, uneven rhythm, or a new pattern beside the approved one. Capturing on the same viewport in both themes keeps the comparison honest.

**Example:** A new Claimwick report screen captures beside the golden list screen at 1080 by 2400 in light and dark.

**Reviewed for:** Does every new or changed screen carry a fresh capture beside its golden archetype? Check the review output.

## Conventions

### Mirror Desktop Jobs on Mobile

**ID:** `litespec/packs/android/ui-primitives#mirror-desktop-jobs-on-mobile`

**Strength:** default

**Applies to:** artifact: component

**Default:** Name each mobile screen for the same job its desktop archetype serves, with fewer actions and a narrower layout.

**Replacement:** A product whose mobile jobs differ from desktop names the differing jobs in its product record.

**Rationale:** One job vocabulary across platforms keeps records, tests, and support speaking one language. The phone layout narrows the presentation but keeps the job name, so a record still maps to one screen on each platform.

**Example:** The Claimwick mobile expense list serves the same list job as desktop with search and open actions only.

**Reviewed for:** Does every mobile screen name a desktop job, with differences recorded? Check the screens beside the product record.

### Keep Drill-Down Depth Shallow

**ID:** `litespec/packs/android/ui-primitives#keep-drill-down-depth-shallow`

**Strength:** default

**Applies to:** artifact: component

**Default:** Keep settings drill-downs to hub, facet, and at most one deeper level, with a visible back path at each level.

**Replacement:** A product with deeper configuration names its extra levels and their back paths in its product record.

**Rationale:** Each level buries the hub one step further, and the visitor loses the path back. Three levels stay navigable: the hub, the facet, and one detail. Deeper trees need their own recorded map.

**Example:** The Claimwick invoicing drill-down opens numbering as its one deeper level, each with a visible back path.

**Reviewed for:** Does any settings path run deeper than hub, facet, and one detail without a recorded map? Check the settings screens.

### Show Every Screen in Both Themes

**ID:** `litespec/packs/android/ui-primitives#preview-every-screen-in-both-themes`

**Strength:** default

**Applies to:** artifact: component

**Default:** Preview every screen on the emulator in light and dark before sending it to review.

**Replacement:** A product with a single-theme build names the theme it previews in its product record.

**Rationale:** A screen approved in one theme can fail in the other: light text on a light fill, or a dark fill that swallows the divider. Previewing both themes before review catches the failure where the fix is cheap.

**Example:** The Claimwick filing form previews in both themes on the emulator before review, catching a low-contrast hint in dark mode.

**Reviewed for:** Does every screen carry light and dark previews from the emulator? Check the review evidence.
