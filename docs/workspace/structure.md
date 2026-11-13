# Workspace Structure

**Stack:** any
**Concern:** workspace

## Intent

A LiteSpec consumer, a project built with LiteSpec, keeps one monorepo tree for its applications, shared packages, documentation, tests, and records. This page states that tree, where each file kind lives inside it, and what `.litespec/project.yaml` declares. It applies to anyone who creates a file, and to agents locating code without asking.

## Concepts

### Applications and Packages

`apps/` holds what runs: the API, each frontend, and any deployed host. `packages/` holds what is reused: shared configuration, generated API types, a thin client, and theme tokens. The split keeps deployable code out of libraries and reusable code out of applications.

### Records and Their Homes

Each record type has one home in the tree: the [model page](../core/model.md) names the home, and the canonical tree below realizes it. Entries live in their home file instead of their own files, so an agent derives the path from the record type without searching.

The model builder writes generated working files under `.litespec/model/` while it builds the model from records, code, routes, tests, and test runs; the folder is rebuilt rather than reviewed.

## Standards

### Use the Canonical Root Tree

**ID:** `litespec/workspace/structure#use-the-canonical-root-tree`

**Strength:** required

**Statement:** A consumer workspace MUST use the canonical root tree declared in this section.

**Example:** Consumer workspaces use this shape:

```text
{repo}/
  .config/
    dotnet-tools.json
  .litespec/
    project.yaml
    glossary.yaml
    intent/
      product.yaml
      goals/ problems/ constraints/ decisions/ research/ actors/ roles/ journeys/
      modules/
        {module}/
          module.yaml
          goals/ problems/ constraints/ decisions/ research/
          features/
            {feature}/
              {feature}.yaml
              {requirement}/{requirement}.yaml    one requirement, in its own folder
    behavior/
      rules/                            cross-module invariants only
      external-systems/                 create with the first product-level external system
      entry-points/
        {entry-point}/
          entry-point.yaml
          screens/
            {screen}/{screen}.yaml
      modules/
        {module}/
          permissions.yaml
          use-cases/ rules/ reactions/ jobs/ notifications/ settings/ integration-events/ external-systems/
          {aggregate}/
            aggregate.yaml
            {use-case}/{use-case}.yaml  results and scenarios as entries
            events/ rules/
    implementation/
      decisions/ api/ guides/ operations/ patterns/ writing/ runbooks/
      operations/
        limits.yaml                     create with the first limit record
        secrets.json                    create with the first secret
        log/                            create with the first operations entry
      entry-points/
        {entry-point}/                  UI catalog, captures and patterns
      modules/
        {module}/
          decisions/ runbooks/
    governance/
      departures/                       create with the first departure record
      modules/
        {module}/
          departures/
    proof/                              generated registers, gate files and run outputs; no authored records
    model/                              generated; ignored
  apps/
    api/
      <backend project manifest>
      src/
      tests/
    {frontend}/
    apphost/                            when one host runs more than one application
  packages/
  AGENTS.md
  <toolchain pin file>
  package.json                          when a frontend or TypeScript package exists
  pnpm-lock.yaml                        when a frontend or TypeScript package exists
  pnpm-workspace.yaml                   when a frontend or TypeScript package exists
```

A backend stack binding names the files that realize the entries in angle brackets.

The example places neither the backend application nor a frontend application at the workspace root. Under `behavior/modules/`, each module folder holds one folder per aggregate in plural kebab-case form. A module with one aggregate of the module's own name may stay flat.

A named record lives in its own folder, `<name>/<name>.yaml`, with its explanation `<name>.md` beside it. A record file lives at the home the [model page](../core/model.md) names for its type. A requirement is a named record in its own folder inside its feature's folder.

An entry lives in its home file: permissions in their module's permissions file, results and scenarios in their use case file, and rules and events in their aggregate file or type folder. Each entry point is one record in its own folder, with its screens beside it.

**Reviewed for:** The workspace tree matches the canonical tree in this section, with each record at its type's home under `.litespec/`.

### Declare Size and Application Stacks in Project Config

**ID:** `litespec/workspace/structure#declare-size-and-application-stacks-in-project-config`

**Strength:** required

**Statement:** `.litespec/project.yaml` MUST declare `size` as `small` or `large` and one stack for the backend and for each frontend.

**Rationale:** Size decides which record types and test projects apply: small or large selects the project's required-record set. Each stack decides which technology pages and checks apply to its application. Declaring both in `.litespec/project.yaml` lets tools and reviewers read the selection from one file. The [scope page](../core/scope.md) states the full selection.

**Reviewed for:** `.litespec/project.yaml` at the workspace root declares `size` and one stack for the backend and for each frontend.

### Keep the Generated Model Out of Source Control

**ID:** `litespec/workspace/structure#keep-the-generated-model-out-of-source-control`

**Strength:** required

**Statement:** The generated `.litespec/model/` folder MUST stay out of source control.

**Rationale:** The model builder writes generated working files there, such as frontend facts, while it builds the model from records, code, routes, tests, and test runs. Tracking them would commit derived files beside their sources, and every commit would carry stale working files until somebody rebuilds. The folder is rebuilt rather than reviewed.

**Reviewed for:** No path under `.litespec/model/` is tracked in git; `git ls-files .model` prints nothing.

### Keep Production and Test Code Separate

**ID:** `litespec/workspace/structure#keep-production-and-test-code-separate`

**Strength:** required

**Statement:** Production code MUST live under `apps/api/src/` and test code under `apps/api/tests/`.

**Rationale:** Production code under `apps/api/src/` and test code under `apps/api/tests/` keep the two graphs apart by path. The split makes a test dependency reaching production code visible in the path itself, and keeps test helpers from shipping with the product.

**Example:** The backend tree is:

```text
apps/api/
  <backend project manifest>
  src/
  tests/
```

**Reviewed for:** Production files sit under `apps/api/src/` and test files under `apps/api/tests/`, with no test helper shipped beside production code.

### Keep Runnable Applications Under Apps

**ID:** `litespec/workspace/structure#keep-runnable-applications-under-apps`

**Strength:** required

**Statement:** An independently runnable frontend, API, or deployed host MUST live under `apps/{name}/`.

**Rationale:** Each root makes one promise: `apps/` runs, `packages/` is reused. A reusable package under `apps/` or deployable code under `packages/` inverts that promise, so neither location tells the reader what it holds. One home per kind keeps the promise readable.

**Reviewed for:** Each directory under `apps/` runs on its own, and each directory under `packages/` is imported by an application.

### Limit Shared TypeScript Packages

**ID:** `litespec/workspace/structure#limit-shared-typescript-packages`

**Strength:** required

**Statement:** A shared TypeScript package MUST have at least two consumers or carry generated output shared by design.

**Rationale:** A shared package with one consumer pays versioning, publishing, and cross-package edits for an audience of one. Requiring two consumers or generated output shared by design keeps sharing deliberate. The baseline permits shared configuration, generated API types, a thin client, and CSS theme tokens.

**Reviewed for:** Each package under `packages/` is imported by at least two consumers, or carries generated output shared by design; read the imports in `apps/`.

### Keep Consumer Documentation at the Root

**ID:** `litespec/workspace/structure#keep-consumer-documentation-at-the-root`

**Strength:** required

**Statement:** Project records MUST live under the root `.litespec/` directory.

**Rationale:** Project records describe the product, not the standard and not the code. Placing them inside the application tree makes them disappear when it is rewritten. The root `.litespec/` directory keeps records in a place that survives replacing the standard or rewriting the application.

**Reviewed for:** Every record path sits under the root `.litespec/` directory.

### Keep Orientation Documents Separate from Canonical Records

**ID:** `litespec/workspace/structure#keep-orientation-documents-separate-from-canonical-records`

**Strength:** required

**Statement:** A README under `.litespec/` MUST link to the record rather than restate its content.

**Rationale:** A README orients; a record decides. A README that restates a decision becomes a second authored source that drifts from the record it summarizes, until the two disagree and neither is trusted. Linking to the record keeps one source with many pointers.

**Reviewed for:** Each `README.md` under `.litespec/` links to its record and restates no record content.

## Conventions

### Name Frontends by Audience

**ID:** `litespec/workspace/structure#name-frontends-by-audience`

**Strength:** default

**Default:** Name a frontend for its audience, such as `web`, `admin`, `portal`, or `docs`.

**Replacement:** A project whose frontends serve one audience records its own frontend names in `.litespec/project.yaml` under `frontends`.

**Rationale:** `apps/web` tells the reader who it serves; `apps/frontend` tells them only what it is built with. Naming each frontend for its audience keeps the folder meaningful when the technology changes. `frontend`, `client`, or `app` names the technology rather than the audience it serves.

**Reviewed for:** Each directory under `apps/` carries an audience name; where replaced, `frontends` in `.litespec/project.yaml` names the project's own frontend names.

### Keep Generated API Contracts in Packages

**ID:** `litespec/workspace/structure#keep-generated-api-contracts-in-packages`

**Strength:** default

**Default:** Place generated OpenAPI types in `packages/api-types/` and a thin client in `packages/api-client/` when more than one frontend consumes the API.

**Replacement:** A project with a single frontend records that frontend as keeping both outputs.

**Rationale:** Generated OpenAPI types and their thin client belong to every frontend that reads the API. With more than one consumer, `packages/api-types/` and `packages/api-client/` keep one copy each. A single frontend may own both locally until a second consumer appears.

**Reviewed for:** Generated OpenAPI types sit in `packages/api-types/` and the thin client in `packages/api-client/` once two frontends consume the API; with one frontend, that frontend keeps both outputs.

## Reference Example

This informative example demonstrates `litespec/workspace/structure#use-the-canonical-root-tree` and `litespec/workspace/structure#limit-shared-typescript-packages`.

An API with public and admin frontends uses `apps/api/`, `apps/web/`, and `apps/admin/`. Both frontends may import generated transport types from `packages/api-types/`. Neither imports the other frontend's feature code.
