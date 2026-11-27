# Mobile Structure

**Stack:** android

**Concern:** frontend-ui

This page states the solution tree, the projects, and the screen folders for a mobile consumer. It is for developers who add apps and screens to the mobile solution. One shared core and one folder rule keep every platform on the same domain model.

## Concepts

### How the Solution Is Organized

One solution holds every mobile app, so every platform shares one core and a second platform reuses the domain model unchanged. Three core projects hold the shared code: Domain for rules and state, Application for handlers and interfaces, and Infrastructure for storage and platform-neutral adapters. One app project per platform holds that platform screens and adapters, named `{ProjectName}.{Platform}`.

A `server` consumer holds a Client project with the generated API client instead, reaching every rule through that client. A `client-only` consumer holds Domain, Application, and Infrastructure instead, carrying its own rules in the app. Imports point from screens toward the shared core, with no reverse edge.

```text
apps/mobile/
  {ProjectName}.Mobile.slnx
  src/
    {ProjectName}.Domain/
    {ProjectName}.Application/
    {ProjectName}.Infrastructure/
    {ProjectName}.Android/
  tests/
    {ProjectName}.Domain.Tests/
    {ProjectName}.Application.Tests/
    {ProjectName}.Android.Tests/
```

The framework-generated `bin`, `obj`, and `publish` folders stay untracked. [Mobile Data and State](data.md) states what each layer does, and [Mobile Testing](testing.md) states how each layer is proved.

## Standards

### Use the Mobile Solution Tree

**ID:** `litespec/packs/android/structure#use-the-mobile-solution-tree`

**Strength:** required

**Statement:** A consumer selecting a mobile stack MUST hold its mobile apps in one solution at `apps/mobile/` with core projects under `src/` and one app project per platform named `{ProjectName}.{Platform}`.

**Rationale:** One solution gives every platform app one core to share, so a second platform reuses the domain model unchanged. Core projects under `src/` separate shared code from the platform apps that consume it, and one app project per platform named `{ProjectName}.{Platform}` keeps each platform screens findable by name. The shape replaces the API solution path the [workspace structure](../../workspace/structure.md) page describes, since a client-only consumer holds no API directory.

**Example:**

```text
apps/mobile/
  {ProjectName}.Mobile.slnx
  src/
    {ProjectName}.Domain/
    {ProjectName}.Application/
    {ProjectName}.Infrastructure/
    {ProjectName}.Android/
  tests/
    {ProjectName}.Domain.Tests/
    {ProjectName}.Application.Tests/
    {ProjectName}.Android.Tests/
```

The framework-generated `bin`, `obj`, and `publish` folders stay untracked.

**Reviewed for:** Does every mobile app live in the one solution at `apps/mobile/`, with core projects under `src/` and one `{ProjectName}.{Platform}` app project per platform? Check the `apps/mobile/` tree.

### Match Core Projects to the Product Shape

**ID:** `litespec/packs/android/structure#match-core-projects-to-the-product-shape`

**Strength:** required

**Statement:** A mobile consumer MUST hold Domain, Application, and Infrastructure core projects in the `client-only` shape, and a `{ProjectName}.Client` core project with the generated API client instead of them in the `server` shape.

**Rationale:** Where the rules live decides what the app holds. A product with no server carries its own domain model in Domain, Application, and Infrastructure projects. A product with a server keeps its rules there, and the app holds a Client project with the generated API client instead, reaching every rule through that client.

**Reviewed for:** Does every `client-only` solution hold Domain, Application, and Infrastructure projects, and every `server` solution the Client project with no Domain copy? Check the solution tree.

### Keep Core Projects Platform-Free

**ID:** `litespec/packs/android/structure#keep-core-projects-platform-free`

**Strength:** required

**Statement:** Domain, Application, Infrastructure, and Client core projects MUST target plain .NET and reference no platform SDK type.

**Rationale:** A core project that names a platform SDK type compiles only for that platform, so a second platform app copies the core instead of reusing it. Targeting plain .NET with no platform reference keeps the core compiling for every platform unchanged. Platform differences sit behind the interfaces Application owns and the adapters in the platform projects.

**Reviewed for:** Does every core project target plain .NET with no platform SDK reference? Check the core project files and sources.

### Organize Screens by Module

**ID:** `litespec/packs/android/structure#organize-screens-by-module`

**Strength:** required

**Statement:** A screen MUST live under `Screens/` in the platform project, in a folder for its module when the product has more than one module.

**Rationale:** A reader holding a module record finds its screens by path when screens group by module. One folder for a single-module product adds a level that distinguishes nothing, so its screens sit directly under `Screens/`. Use-case code reached from a screen lives in Application, so the screen folder holds the screen and the handler holds the behavior.

**Reviewed for:** Does every screen sit under `Screens/`, in its module folder in a multi-module product? Check the platform project tree.

## Conventions

### Place Platform Adapters in the Platform Project

**ID:** `litespec/packs/android/structure#place-platform-adapters-in-the-platform-project`

**Strength:** default

**Default:** Place adapters for players, pickers, sensors, and storage locations in the platform project behind an interface Application owns, one type per platform API surface.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Each platform API surface gets one adapter type in the platform project, behind an interface declared in Application. That puts every platform call behind a testable boundary the core tests substitute. The platform project is the one layer naming the platform SDK, so each adapter resolves to its platform API by name.

**Reviewed for:** Does every platform API surface have one adapter type in the platform project behind an interface Application owns? Check the platform project adapters.
