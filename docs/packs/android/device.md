# Mobile Device Capabilities

**Stack:** android

**Concern:** frontend-ui

This page states how a mobile app picks media, requests permissions, plays sound, and declares what it uses. It is for developers who add device features to a mobile consumer. Least access and quiet background behavior keep the app trusted on hardware the user shares with every other app.

## Concepts

### What the App Borrows and Yields

A picker lends one item without opening the library behind it, while a permission opens ongoing access the user later revokes. The app picks through a permissionless picker where the platform offers one, and it requests each permission from the action that needs it. Each request names the feature waiting on it, so refusing it disables that feature instead of the app.

Sound shares one speaker with every other app, so playback holds audio focus and yields it. Playback pauses on focus loss and when the app leaves the foreground, and it resumes only on return. The pause frees the speaker and the decoder for the app now in front.

The platform pauses and reclaims background apps, so device work belongs in the foreground. A background service, a scheduled job, or a push handler runs only with a project decision naming it. Without that decision, each device feature runs while the app stays visible and stops with it.

## Standards

### Pick Media Through Permissionless System Pickers

**ID:** `litespec/packs/android/device#pick-media-through-permissionless-system-pickers`

**Strength:** required

**Statement:** An app MUST pick photos and files through the system picker that needs no storage permission when the platform offers one.

**Rationale:** A permissionless picker shows the user exactly what the app receives: the chosen items and nothing else. Broad storage access asks for the whole library to read one photo, which gives the user good reason to refuse. The permissionless picker keeps app access to what the user chose.

**Reviewed for:** Does every photo and file pick go through the permissionless system picker where the platform offers one? Check the picking call sites.

### Request a Permission at the Point of Use

**ID:** `litespec/packs/android/device#request-a-permission-at-the-point-of-use`

**Strength:** required

**Statement:** A runtime permission MUST be requested when the user starts the action that needs it, never at launch.

**Rationale:** A permission at launch arrives before the user knows what it serves, so the safe answer is refusal. The same permission at the point of use answers a question the user just asked. Each request names the waiting feature, so refusal disables that feature instead of the app.

**Reviewed for:** Is every runtime permission requested from the action that needs it, none at launch? Check the permission requests.

### Hold Audio Focus While Playing Sound

**ID:** `litespec/packs/android/device#hold-audio-focus-while-playing-sound`

**Strength:** required

**Statement:** An app playing audible sound MUST request audio focus and pause when it loses focus.

**Rationale:** One speaker serves every app, so playback without focus talks over navigation prompts, calls, and other players. Requesting focus before playing lets the platform arbitrate between them. Pausing on focus loss yields to whatever took it, and resuming waits for focus to return.

**Reviewed for:** Does every audible playback request audio focus first and pause on focus loss? Check the playback code.

### Pause Media in the Background

**ID:** `litespec/packs/android/device#pause-media-in-the-background`

**Strength:** required

**Statement:** Sound and video MUST pause when the app leaves the foreground and resume only when it returns.

**Rationale:** An app the user has left goes quiet, since background sound from a hidden app reads as a defect. Background video decodes frames nobody watches and drains the battery for nothing. Pausing on leave frees the speaker and the decoder, and resuming only on return ties playback to the visible screen.

**Reviewed for:** Does all sound and video pause when the app leaves the foreground and resume only on return? Check the lifecycle handling.

### Declare Only Used Permissions

**ID:** `litespec/packs/android/device#declare-only-used-permissions`

**Strength:** required

**Statement:** The app manifest MUST declare only the permissions the app's current features use.

**Rationale:** Each declared permission is a promise the features keep, so a declaration without a feature asks for trust no feature uses. Stale declarations pile up as features leave, and each one asks the user a question no feature needs. Declaring only what current features use keeps the manifest an honest list of app needs.

**Reviewed for:** Does every permission in the app manifest serve a current feature? Check the app manifest beside the features.

## Conventions

### Keep Device Work in the Foreground

**ID:** `litespec/packs/android/device#keep-device-work-in-the-foreground`

**Strength:** default

**Default:** Run no background service, scheduled job, or push handler without a project decision naming it.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Background work runs where the user has no view of it, spending battery and data on a schedule the app chose. A project decision naming the service, job, or handler records why the work waits for no foreground. Without that decision, every device feature runs with the visible app and stops with it.

**Reviewed for:** Does every background service, scheduled job, and push handler have a project decision naming it? Check the background work beside the decision records.
