# Android Stack

**Stack:** android
**Layer:** frontend-ui

## Intent

The Android stack is the technology binding for a product that ships a native Android application built with .NET for Android in C# against plain Android views. A team chooses it with either shape, `client-only` or `server`, declared in [Supported Scope](../../../core/scope.md). Exact .NET and Android API versions live only in `litespec.manifest.yaml`.

## Concepts

### What the Stack Holds

The stack is one native Android application: a .NET for Android project holding the screens, the navigation controller, and the device code, built against plain Android views. It publishes an AAB through the .NET toolchain, which builds, tests, and publishes the whole mobile solution. The shared mobile pages in the [Mobile overview](../README.md) state the rules every mobile stack follows; this page states what the Android stack is and which versions it pins.

### Version Pins

`litespec.manifest.yaml` pins the .NET version under `stack.dotnet` and the Android API level under `stack.android`, with every AndroidX and Google binding package version under `packages.nuget`. These keys name the pins; the manifest holds the values.

## Standards

None.

## Conventions

None.
