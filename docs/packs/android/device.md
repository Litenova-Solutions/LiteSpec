# Mobile Device Capabilities

**Stack:** android
**Concern:** frontend-ui

A mobile app borrows device capabilities: the camera roll, the microphone, the speaker, and the permissions guarding them. These rules cover how the app picks media, requests permissions, plays sound, and declares what it uses. A team that applies them while adding a device feature keeps the app asking for the least access and behaving while shared hardware is busy.

## Concepts

### What the App Borrows and How It Yields

A picker lends one item without granting access to the library, while a permission grants ongoing access the user can revoke. The least-access rule follows: pick through a permissionless picker where the platform offers one, and request each permission when its action starts.

Sound shares one speaker with every other app, so playback holds audio focus and yields it, pausing when focus is lost and when the app leaves the foreground.

The platform pauses and reclaims background apps, so device work belongs in the foreground: no background service, scheduled job, or push handler runs without a project decision naming it.

## Standards

### Pick Media Through Permissionless System Pickers

**ID:** `litespec/packs/android/device#pick-media-through-permissionless-system-pickers`

**Strength:** required

**Statement:** An app MUST pick photos and files through the system picker that needs no storage permission when the platform offers one.

**Rationale:** A permissionless system picker shows the user exactly what the app receives: the chosen items and nothing else. Requesting broad storage access instead asks for the whole library to read one photo, which the user has good reason to refuse. Using the permissionless picker where the platform offers one keeps the app's access to what the user chose.

**Reviewed for:** Does every photo and file pick go through the permissionless system picker where the platform offers one? Check the picking call sites.

### Request a Permission at the Point of Use

**ID:** `litespec/packs/android/device#request-a-permission-at-the-point-of-use`

**Strength:** required

**Statement:** A runtime permission MUST be requested when the user starts the action that needs it, never at launch.

**Rationale:** Asked at launch, a permission arrives before the user knows what it is for, so the safe answer is no. Asked when the user starts the action that needs it, the same permission answers a question the user just asked. Each request names the feature waiting on it, so refusing it disables that feature rather than the app.

**Reviewed for:** Is every runtime permission requested from the action that needs it, none at launch? Check the permission requests.

### Hold Audio Focus While Playing Sound

**ID:** `litespec/packs/android/device#hold-audio-focus-while-playing-sound`

**Strength:** required

**Statement:** An app playing audible sound MUST request audio focus and pause when it loses focus.

**Rationale:** One speaker serves every app, so playback without focus talks over navigation prompts, calls, and other players. Requesting focus before playing lets the platform arbitrate between them. Pausing on focus loss yields to whatever took it, and resuming waits for focus to return rather than grabbing it back.

**Reviewed for:** Does every audible playback request audio focus first and pause on focus loss? Check the playback code.

### Pause Media in the Background

**ID:** `litespec/packs/android/device#pause-media-in-the-background`

**Strength:** required

**Statement:** Sound and video MUST pause when the app leaves the foreground and resume only when it returns.

**Rationale:** An app the user cannot see goes quiet: background sound from a hidden app reads as a bug, and background video decodes frames nobody watches. Pausing when the app leaves the foreground frees the speaker and the decoder for the app now in front, and resuming only on return keeps playback tied to the visible screen.

**Reviewed for:** Does all sound and video pause when the app leaves the foreground and resume only on return? Check the lifecycle handling.

### Declare Only Used Permissions

**ID:** `litespec/packs/android/device#declare-only-used-permissions`

**Strength:** required

**Statement:** The app manifest MUST declare only the permissions the app's current features use.

**Rationale:** Each declared permission is a promise the app's features must keep: a declaration without a feature asks for trust no feature uses. Stale declarations accumulate as features leave, and each one asks the user a question no feature needs. Declaring only what current features use keeps the manifest an honest list of the app's needs.

**Reviewed for:** Does every permission in the app manifest serve a current feature? Check the app manifest beside the features.

## Conventions

### Keep Device Work in the Foreground

**ID:** `litespec/packs/android/device#keep-device-work-in-the-foreground`

**Strength:** default

**Default:** Run no background service, scheduled job, or push handler without a project decision naming it.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Background work runs where the user cannot see it, spending battery and data on a schedule the app chose. A project decision naming the service, job, or handler records why the work cannot wait for the foreground and what it costs. Without that decision, every device feature runs while the app is visible and stops with it.

**Reviewed for:** Does every background service, scheduled job, and push handler have a project decision naming it? Check the background work beside the decision records.
