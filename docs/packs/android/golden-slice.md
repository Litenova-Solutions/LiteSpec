# Android Golden Slice

**Stack:** android

**Concern:** frontend-ui

This page describes one small working Android slice that shows one job on a phone viewport with native views. It is for teams starting a new Android frontend who need a concrete shape to copy. One list screen and one detail screen prove the navigation, the resources, and the data path before the team builds the rest.

## Concepts

### What the Slice Contains

The slice shows the health entries of the backend golden slice through two screens: a list under `Screens/Health/` and a detail for one entry. The host Activity owns the one navigation controller, and each screen raises events to it instead of navigating on its own. The list carries the screen mark of the list job, and the detail carries the mark of the record job, while the host Activity carries no screen mark.

The shape gives each newcomer the same starting point. A team copies the two-screen shape for each new job instead of inventing a second screen layout. The [stack page](stack.md) states the solution layout and the navigation controller, and the [screens page](screens.md) states how each screen is built.

### How Screens Draw and Read Data

Screens draw with plain Android views and take colors and text styles from `Resources/values/styles.xml`, with dimensions from `Resources/values/dimens.xml` and copy from `Resources/values/strings.xml`. Screen code sets no style value directly, so a theme change touches resources instead of screens. The token files hold one palette, one type scale, and one spacing scale for every screen.

```text
Claimwick.Android/Screens/Health/HealthListScreen.cs
Claimwick.Android/Screens/Health/HealthDetailScreen.cs
Resources/values/dimens.xml
Resources/values/styles.xml
Resources/values/strings.xml
```

On the `server` shape the Client project holds the generated API client with the base address, the credentials, and the mapping from a failure to a result. On the `client-only` shape the core projects hold the handlers the screens call. Persisted state sits under app-private storage and each write lands atomically, so a write stopped midway leaves the prior complete file.

### How the Slice Is Proved

Core tests run as plain .NET tests with no device: the health rule and its handler tests sit beside the Domain and Application projects they prove. Device checks run on an emulator at the standard phone viewport in light and dark, with fresh screenshots beside the locked golden phone baselines. The slice keeps one probe that opens the list and follows one row to its detail, so a fresh checkout shows the navigation, the resources, and the data path together.

The same jobs run narrower than their desktop archetypes with fewer actions and a tighter layout, outside the shared web UI contract. The [stack page](stack.md) states the platform project and the resource tokens the slice uses. The [information architecture page](../../core/ui-ia.md) states the one-job discipline and the archetypes the phone screens follow.

## Standards

None.

## Conventions

None.
