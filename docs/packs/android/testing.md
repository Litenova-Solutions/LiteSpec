# Mobile Testing

**Stack:** android

**Concern:** tests

This page states how a mobile consumer proves its behavior at two boundaries: fast core tests and on-device screen tests. It is for developers who write core tests and device tests for a mobile app. Fast tests stay fast and every reachable screen still meets a reviewer on hardware.

## Concepts

### Which Tests Run Where

The core holds no platform type, so its tests run on the plain .NET runner with no emulator. Screens render through the platform toolkit, so their tests drive them on an emulator or a device, where layout, insets, back behavior, and accessibility meet the platform. The split keeps layering honest: a core test that needs a device points to platform code in the core.

The release build adds one manual run on a physical device per platform before publishing. Emulators approximate performance, sensors, and vendor behavior, so the hand run meets the app where users meet it. The recorded device and result make the check reviewable.

## Standards

### Test the Core as Plain .NET

**ID:** `litespec/packs/android/testing#test-the-core-as-plain-net`

**Strength:** required

**Statement:** Domain, Application, Infrastructure, and Client tests MUST run as plain .NET test projects with no emulator.

**Rationale:** Core tests need no emulator, device, or platform host, because the core holds no platform type. Plain .NET test projects in xunit.v3, the framework the manifest pins, keep them fast. A core test that needs a device proves the layering broke, and the fix belongs in the layering rather than in the test setup.

**Reviewed for:** Does every core test project run on the plain .NET runner with no emulator? Check the core test projects.

### Prove Screens on a Device or Emulator

**ID:** `litespec/packs/android/testing#prove-screens-on-a-device-or-emulator`

**Strength:** required

**Statement:** Each screen a use case reaches MUST have a device test that drives it on an emulator or device.

**Rationale:** Screens render through the platform toolkit, so only a device test meets them where users do: layout, insets, back behavior, and accessibility. A screen no device test drives is proved by nothing, whatever the core tests cover. Driving each reachable screen on an emulator or device keeps every reachable screen proved.

**Reviewed for:** Does every screen a use case reaches have a device test driving it on an emulator or a device? Check the device tests beside the screens.

### Prove Touch and Text Accessibility on a Device

**ID:** `litespec/packs/android/testing#prove-touch-and-text-accessibility-on-a-device`

**Strength:** required

**Statement:** A device test for each screen MUST check that every interactive view carries a content description and the platform-minimum touch target, and that text scales with the system font scale.

**Rationale:** A screen that looks right at the default font size still hides commands from a screen reader, offers a target too small to tap, or clips enlarged text. Checking descriptions, targets, and scaling on a device meets the screen with the platform accessibility settings its visitors use. This proves `litespec/packs/android/screens#meet-touch-and-text-accessibility` on each screen.

**Reviewed for:** Does every screen have a device test checking content descriptions, platform-minimum touch targets, and text scaling with the system font scale? Check the device tests beside the screens.

## Conventions

### Try the Release Build on Hardware Before Publishing

**ID:** `litespec/packs/android/testing#try-the-release-build-on-hardware-before-publishing`

**Strength:** default

**Default:** Install the release build and try it by hand on one physical device per platform before publishing, and record the device and result.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Emulators approximate performance, sensors, and vendor behavior, and hardware differs on all three. Trying the release build by hand on one physical device per platform meets the app as users will. The recorded device and result make the check reviewable, and a release without that record ships on emulator evidence alone.

**Reviewed for:** Does every release record one physical device per platform with the result of the hand check? Check the release records.
