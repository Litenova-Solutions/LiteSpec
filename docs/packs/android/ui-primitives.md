# Android UI Primitives

**Stack:** android

**Concern:** frontend-ui

This page describes the Android visual contract: one focused screen per job, bottom navigation, drill-down settings, and tokens for spacing, targets, type, and color. It is for developers who build phone screens in the Android platform project. Shared tokens and five approved baselines keep every screen reading as one product.

## Concepts

### One Focused Screen per Job

Each Android screen under `Screens/` serves one job with two or three actions. The phone screen shows less than a desktop page, so a screen that tries two jobs shows neither well. A job that needs more actions splits into two screens with one step each.

The Claimwick expense list screen shows waiting expenses with search and open actions only. Filing a new expense is a second screen with its own focused form. The Claimwick approval screen shows one approval with approve, refuse, and history actions.

### Bottom Navigation and the Settings Hub

The app moves through bottom navigation with three to five primary destinations. The bar stays visible on top-level screens, and each destination opens its own stack. No drawer or sidebar carries primary navigation on the phone.

Settings live in a hub list that pushes one focused drill-down screen per configuration facet. Each drill-down edits one facet with one save path, and stacked multi-form cards stay off the phone screen. The path runs from hub to facet with at most one deeper level, and each level shows a back path.

The Claimwick app moves between home, documents, entries, and settings through four bottom destinations. Its business settings hub pushes one drill-down for invoicing details and another for senders, with numbering as the one deeper level under invoicing.

### Spacing, Target, Type, and Color Tokens

All visual values live in resources. Spacing follows 4dp, 8dp, 16dp, and 24dp steps, and touch targets stand at 48dp. Type styles and Material 3 color roles live in `styles.xml`, with the light value and the dark value behind each role. Screen code in `Screens/*.cs` sets no size, margin, or color of its own.

The Claimwick app pads list rows 16dp and separates sections 24dp, drawing amounts in the agreed money style from resources. Its approve control keeps the full 48dp target even where its visible shape runs smaller. Its warning banner reads the warning role, which resolves to its light value in day mode and its dark value at night.

### Five Approved Baselines and Review Captures

Five screens set the visual baseline: shell with bottom navigation, list, record detail, settings drill-down, and form. The team approves each one on the standard emulator viewport at 1080 by 2400, in light and dark. Emulator screenshot capture runs inside `litespec ui review`, so each later screen compares against an approved look instead of a description.

| Golden screen | What it locks |
|:---|:---|
| Shell with bottom navigation | Destinations, bar visibility, and the workspace switcher |
| List | Row layout, search placement, and empty state |
| Record detail | Header, state badge, primary action, and peer views |
| Settings drill-down | Hub list, facet layout, and the save path |
| Form | Field rhythm, section breaks, and the footer bar |

The Claimwick team approves the golden expense list on the emulator in both themes before building the remaining screens. A new Claimwick report screen captures beside the golden list screen at 1080 by 2400 in light and dark. Each mobile screen carries the name of the same job its desktop archetype serves, with fewer actions and a narrower layout, and each screen previews on the emulator in light and dark before review.

## Standards

### Refuse Inline Sizing in Screens

**ID:** `litespec/packs/android/ui-primitives#refuse-inline-sizing-in-screens`

**Strength:** required

**Applies to:** artifact: component

**Statement:** C# screen classes MUST NOT set pixel sizes, margins, or colors inline.

**Rationale:** An inline value bypasses the tokens review approved, so the screen drifts from the baseline one value at a time. Forcing each value through a named token gives each reviewer one place to check. The token files hold the approved rhythm while screens hold behavior.

**Example:** A Claimwick screen that needs wider padding adds a token to `dimens.xml` instead of setting pixels in `Screens/`. The next reader finds the value beside the other spacing steps.

**Checked by:** The Roslyn analyzer reports inline sizing in screen classes, and review refuses any `Screens/*.cs` change that sets a size, margin, or color.

## Conventions

None.
