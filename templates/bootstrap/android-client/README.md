# __PROJECT__ Android

The __PROJECT__ Android app shows the recorded check-ins of the Health sample on the phone: a list and one detail screen reading the check-ins of the backend starter over HTTP.

## Run locally

1. Run the backend starter so its API listens on `http://localhost:5005`.
2. Open an Android emulator and build the mobile solution in Release:

```powershell
dotnet build apps/mobile/__NAMESPACE__.Mobile.slnx -c Release
```

3. Deploy `__NAMESPACE__.Android` to the emulator. The app reads the backend through `http://10.0.2.2:5005`, the emulator loopback to the host. On a physical device, point `CheckInApiClient.EmulatorBaseAddress` at the host LAN address instead.

4. Sign-in stays a documented step: the starter reads the sample rows and carries no sign-in of its own. Pair it with the backend development sign-in once its cast person is known.

## Checks

Run these with no hand edit before changing the starter:

```powershell
dotnet build apps/mobile/__NAMESPACE__.Mobile.slnx -c Release
dotnet litespec model check
```

The build passes with 0 warnings and 0 errors, and the model check reports 0 findings.

## Layout

- `apps/mobile/src/__NAMESPACE__.Domain/` holds the Health values.
- `apps/mobile/src/__NAMESPACE__.Application/` holds the typed check-in store behind an interface.
- `apps/mobile/src/__NAMESPACE__.Infrastructure/` holds the persisted check-in shape.
- `apps/mobile/src/Modules/__NAMESPACE__.Health/` holds the Health module home, where use-case handlers land as the team adds them.
- `apps/mobile/src/__NAMESPACE__.Android/` holds the host activity owning the one navigation controller, the Health list and detail screens under `Screens/Health/`, and the HTTP reader.
- `.litespec/` holds the Health module, the Android entry point with its two screens, the health visitor actor and cast, and the goal the interface requirements rest on.

Provenance: the sample domain is the Health module of `docs/packs/dotnet/golden-slice.md` in the LiteSpec release this starter was generated from.
