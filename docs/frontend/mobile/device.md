# Mobile Device Capabilities

## Intent

A mobile app borrows device capabilities: the camera roll, the microphone, the speaker, and the permissions guarding them. This page states how the app picks media, requests permissions, plays sound, and declares what it uses. A team that applies it while adding a device feature keeps the app asking for the least access and behaving while shared hardware is busy.

## Concepts

### Pickers, Permissions, and Focus

A picker lends one item without granting access to the library; a permission grants ongoing access the visitor can revoke. Sound shares one speaker with every other app, so playback holds audio focus and yields it. The platform pauses and reclaims background apps, so device work belongs in the foreground.

## Standards

### Prefer Permissionless System Pickers

**ID:** `litespec/frontend/mobile/device#prefer-permissionless-system-pickers`

**Statement:** An app MUST pick photos and files through the system picker that needs no storage permission when the platform offers one.

**Rationale:** A system picker that needs no storage permission shows the visitor exactly what the app receives: the chosen items and nothing else. Requesting broad storage access instead asks for the whole library to read one photo, which the visitor has good reason to refuse. Using the permissionless picker where the platform offers one keeps the app's access to what the visitor chose.

**Reviewed for:** Photo and file picking travels through the permissionless system picker where the platform offers one.

### Request a Permission at the Point of Use

**ID:** `litespec/frontend/mobile/device#request-a-permission-at-the-point-of-use`

**Statement:** A runtime permission MUST be requested when the visitor starts the action that needs it, never at launch.

**Rationale:** A permission requested at launch arrives before the visitor knows what it is for, so the safe answer is no. Requested when the visitor starts the action that needs it, the same permission answers a question the visitor just asked. Each request names the feature waiting on it, so a refusal disables that feature rather than the app.

**Reviewed for:** Runtime permission requests fire from the action needing them, with none at launch.

### Hold Audio Focus While Playing Sound

**ID:** `litespec/frontend/mobile/device#hold-audio-focus-while-playing-sound`

**Statement:** An app playing audible sound MUST request audio focus and pause when it loses focus.

**Rationale:** One speaker serves every app, so playback without focus talks over navigation prompts, calls, and other players. Requesting focus before playing lets the platform arbitrate, and pausing on focus loss yields to whatever took it. Resuming waits for focus to return rather than grabbing it back.

**Reviewed for:** Audible playback requests audio focus and pauses on focus loss.

### Pause Media in the Background

**ID:** `litespec/frontend/mobile/device#pause-media-in-the-background`

**Statement:** Sound and video MUST pause when the app leaves the foreground and resume only when it returns.

**Rationale:** An app the visitor can no longer see should go quiet: background sound from a hidden app reads as a bug, and background video decodes frames nobody watches. Pausing on leaving the foreground frees the speaker and the decoder for the app now in front. Resuming only on return keeps playback tied to the visible screen.

**Reviewed for:** Sound and video pause out of the foreground and resume only on return.

### Declare Only Used Permissions

**ID:** `litespec/frontend/mobile/device#declare-only-used-permissions`

**Statement:** The app manifest MUST declare only the permissions the app's current features use.

**Rationale:** Each declared permission is a promise the app's features must keep: a declaration without a feature asks for the trust nothing uses. Stale declarations accumulate as features are removed, and each one costs the app a question the visitor should never have been asked. Declaring only what current features use keeps the manifest an honest list of the app's needs.

**Reviewed for:** The app manifest declares only permissions current features use.

## Conventions

### Keep Device Work in the Foreground

**ID:** `litespec/frontend/mobile/device#keep-device-work-in-the-foreground`

**Default:** Run no background service, scheduled job, or push handler without a project decision naming it.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Background work runs where the visitor cannot see it, spending battery and data on a schedule the app chose. A project decision naming the service, job, or handler records why the work cannot wait for the foreground and what it costs. Without that decision, every device feature runs while the app is visible and stops with it.

**Reviewed for:** Background services, scheduled jobs, and push handlers each name a project decision, or none exist.
