# Mobile Testing

## Intent

The mobile app proves its behavior at two boundaries: core tests without a device, and device tests that drive each screen on an emulator or device. This page states how it follows the testing principles in [Frontend Testing](../testing.md) and where each kind of test lives. A team that applies it while writing tests keeps fast tests fast and still proves every screen where users meet it.

## Concepts

### Two Boundaries, Two Runners

The core holds no platform type, so its tests run on the plain .NET runner with no emulator. Screens render through the platform toolkit, so their tests drive them on an emulator or a device. The release build adds one human walk on a physical device per platform before publishing.

## Standards

### Test the Core as Plain .NET

**Statement:** Domain, Application, Infrastructure, and Client tests MUST run as plain .NET test projects with no emulator.

**Rationale:** The core holds no platform type, so its tests need no emulator, device, or platform host. They run as plain .NET test projects in xunit.v3, the test framework the manifest pins. A core test that needs a device proves the layering already broke, and the fix belongs in the layering rather than in the test setup. This realizes litespec/frontend/testing#match-test-level-to-risk.

### Prove Screens on a Device or Emulator

**Statement:** Each screen a use case reaches MUST have a device test that drives it on an emulator or device.

**Rationale:** Screens render through the platform toolkit, so only a device test meets them where users do: layout, insets, back behavior, and accessibility. A screen no device test drives is proved by nothing, whatever the core tests cover. Driving each screen a use case reaches on an emulator or device keeps every reachable screen proved. This realizes litespec/frontend/testing#match-test-level-to-risk.

## Conventions

### Run the Release Build Before a Release

**Default:** Install and walk the release build on one physical device per platform before publishing, and record the device and result.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Emulators approximate the device: performance, sensors, and vendor behavior differ on hardware. Walking the release build on one physical device per platform meets the app as users will, and the recorded device and result make the walk reviewable. A release without that record ships on emulator evidence alone.

## Verification

| ID | Mark | Evidence |
|:---|:---|:---|
| litespec/frontend/mobile/testing#test-the-core-as-plain-net | guidance | Core test projects run on the plain .NET runner with no emulator. |
| litespec/frontend/mobile/testing#prove-screens-on-a-device-or-emulator | guidance | Each screen a use case reaches has a device test driving it on an emulator or device. |
| litespec/frontend/mobile/testing#run-the-release-build-before-a-release | guidance | The release record names one physical device per platform and the walk result. |
