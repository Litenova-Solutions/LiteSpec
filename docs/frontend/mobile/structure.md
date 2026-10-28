# Mobile Structure

## Intent

A mobile consumer holds its apps in one .NET solution whose core projects carry the domain model and whose platform projects carry the screens. This page states that solution tree, the projects each shape holds, the screen folders, and the import direction between them. A team that applies it while adding apps and screens keeps every platform sharing one core and every layer building only on the layers inside it.

## Concepts

### The Mobile Tree

Three core projects hold the shared code: Domain for rules and state, Application for handlers and interfaces, and Infrastructure for storage and platform-neutral adapters, with a `server` consumer holding a Client project with the generated API client instead. One app project per platform holds that platform's screens and adapters, named `{ProjectName}.{Platform}`. [Mobile Data and State](data.md) and [Mobile Testing](testing.md) state what each layer may do and how each is proved; this page states where each file lives.

## Standards

### Use the Mobile Solution Tree

**ID:** `litespec/frontend/mobile/structure#use-the-mobile-solution-tree`

**Statement:** A consumer selecting a mobile stack MUST hold its mobile apps in one solution at `apps/mobile/` with core projects under `src/` and one app project per platform named `{ProjectName}.{Platform}`.

**Rationale:** One solution gives every platform app one core to share, so a second platform reuses the domain model unchanged. Core projects under `src/` separate shared code from the platform apps that consume it, and one app project per platform named `{ProjectName}.{Platform}` makes each platform's screens findable by name. A reader moves from the solution to a screen by rule rather than by search. This replaces the API solution path in `litespec/workspace/structure#use-the-canonical-root-tree`, because a client-only consumer has no API directory.

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

Framework-generated `bin`, `obj`, and `publish` folders remain untracked.

**Reviewed for:** One solution at `apps/mobile/` holds the core projects under `src/` and one app project per platform named `{ProjectName}.{Platform}`.

### Hold the Domain in the App Only Without a Server

**ID:** `litespec/frontend/mobile/structure#hold-the-domain-in-the-app-only-without-a-server`

**Statement:** A mobile consumer MUST hold Domain, Application, and Infrastructure core projects in the `client-only` shape, and a `{ProjectName}.Client` core project with the generated API client instead of them in the `server` shape.

**Rationale:** A product with no server carries its own domain model, so the app holds the Domain, Application, and Infrastructure projects that define it. A product with a server keeps its rules on the server, so the app holds a Client project with the generated API client and reaches every rule through that client rather than through a copy of the rules. A copied rule drifts from the server's rule on the first change, and the app then enforces a rule the server does not hold.

**Reviewed for:** A `client-only` solution holds Domain, Application, and Infrastructure projects, and a `server` solution holds a Client project with the generated API client and no Domain copy.

### Keep Core Projects Platform-Free

**ID:** `litespec/frontend/mobile/structure#keep-core-projects-platform-free`

**Statement:** Domain, Application, Infrastructure, and Client core projects MUST target plain .NET and reference no platform SDK type.

**Rationale:** A core project that names a platform SDK type compiles only for that platform, so a second platform app copies the core instead of reusing it. Targeting plain .NET with no platform reference keeps the core compiling for every platform unchanged. Platform differences stay behind the interfaces Application owns, with the platform projects holding the adapters.

**Reviewed for:** Core projects target plain .NET and reference no platform SDK type.

### Organize Screens by Module

**ID:** `litespec/frontend/mobile/structure#organize-screens-by-module`

**Statement:** A screen MUST live under `Screens/` in the platform project, in a folder for its module when the product has more than one module.

**Rationale:** Screens grouped by module let a reader holding a module record find its screens by path, the way the frontend module mirror maps use cases to folders. A single-module product keeps its screens directly under `Screens/`, because one folder would add a level that distinguishes nothing. Use-case code reached from a screen lives in Application, so the screen folder holds the screen and the handler holds the behavior. This realizes litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case only as far as the screen path follows the module name.

**Reviewed for:** Each screen lives under `Screens/`, in its module folder where the product has more than one module.

### Keep Imports Directional

**ID:** `litespec/frontend/mobile/structure#keep-imports-directional`

**Statement:** A reference MUST point from the platform project to Infrastructure, Application, and Domain, from Infrastructure to Application and Domain, and from Application to Domain.

**Rationale:** The direction keeps each layer testable with only the layers inside it: Domain references nothing, Application references only Domain, and the platform project reaches Infrastructure through the interfaces Application owns. A reference in the other direction drags the outer layer's platform and storage dependencies into the inner layer's tests. This realizes litespec/frontend/structure#order-imports-from-routes-to-shared-packages for the mobile tree.

**Example:**

```text
Platform project -> Infrastructure, Application, Domain
Infrastructure -> Application, Domain
Application -> Domain
```

**Reviewed for:** The reference graph matches the declared direction from the platform project inward.

## Conventions

### Place Platform Adapters in the Platform Project

**ID:** `litespec/frontend/mobile/structure#place-platform-adapters-in-the-platform-project`

**Default:** Place adapters for players, pickers, sensors, and storage locations in the platform project behind an interface Application owns, one type per platform API surface.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** One type per platform API surface in the platform project, each implementing an interface declared in Application, puts every platform call behind a testable boundary the core tests can substitute. The platform project is the only layer that names the platform SDK, so each adapter resolves to its platform API by name. This realizes litespec/frontend/structure#locate-cross-route-values-in-the-frame.

**Reviewed for:** Each platform API surface has one adapter type in the platform project behind an interface Application owns.
