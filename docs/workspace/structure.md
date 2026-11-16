# Workspace Structure

**Stack:** any
**Concern:** workspace

A LiteSpec consumer, a project built with LiteSpec, keeps one monorepo tree for its applications, shared packages, documentation, tests, and records. This page defines that tree, where each file kind lives inside it, and what `.litespec/project.yaml` declares. It applies to anyone who creates a file, and to agents locating code without asking.

## Concepts

### Applications and Packages

Two folders split the tree by purpose. The `apps/` folder holds what runs: the API, each frontend, and any deployed host. The `packages/` folder holds what is reused: shared configuration, generated API types, a thin client, and theme tokens. Deployable code stays out of `packages/`, and reusable code stays out of applications. A second frontend reuses the generated API types from `packages/` instead of importing the first frontend's code.

### Records and Their Homes

Each record type has one home in the tree. The [model page](../core/model.md) names the home for each type, and the canonical tree in `Use the Canonical Root Tree` shows where it is.

Entries live in their home file instead of their own files, so an agent derives the path from the record type without searching. A scenario lives in its use-case file, while a requirement lives in its own folder inside its feature's folder.

While it builds the model from records, code, routes, tests, and test runs, the model builder writes generated working files under `.litespec/model/`. The folder is rebuilt rather than reviewed.

## Standards

### Use the Canonical Root Tree

**ID:** `litespec/workspace/structure#use-the-canonical-root-tree`

**Strength:** required

**Statement:** A consumer workspace MUST use the canonical root tree declared in this section.

**Example:** A consumer workspace uses this shape:

```text
{repo}/
  .config/
    dotnet-tools.json
  .litespec/
    project.yaml
    glossary.yaml
    product.yaml
    intent/
      goals/ visions/ problems/ constraints/ decisions/ research/ actors/ roles/ flows/
      features/
        {feature}/
          {feature}.yaml
          requirements/
            {requirement}/
              {requirement}.yaml    one requirement, in its own folder
      requirements/                 product requirements
        {requirement}/
          {requirement}.yaml
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
          stores/ use-cases/ rules/ reactions/ jobs/ read-models/ notifications/ settings/ module-events/ integration-events/ external-systems/
          aggregates/
            {aggregate}/
              aggregate.yaml
              use-cases/
                {use-case}/
                  {use-case}.yaml   results and scenarios as entries
              domain-events/ rules/
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
      waivers/                       create with the first waiver record
      modules/
        {module}/
          waivers/
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

A backend stack binding names the files for the entries in angle brackets.

Neither the backend application nor a frontend application sits at the workspace root. Under `behavior/modules/`, each module folder holds its typed folders, and `aggregates/` holds one folder per aggregate in plural kebab-case form.

A named record lives in its own folder as `<name>/<name>.yaml`, with its explanation `<name>.md` beside it. Each record file lives at the home the [model page](../core/model.md) names for its type. A requirement is a named record in its own folder inside the folder of its feature.

An entry lives in its home file: permissions in the permissions file of their module, results and scenarios in their use case file, and rules and domain events in their aggregate file or type folder. Each entry point is one record in its own folder, with its screens beside it.

**Reviewed for:** Does the workspace tree match the canonical tree in this section, with each record at its type's home under `.litespec/`? Walk the tree from the workspace root.

### Declare Size and Application Stacks in Project Config

**ID:** `litespec/workspace/structure#declare-size-and-application-stacks-in-project-config`

**Strength:** required

**Statement:** `.litespec/project.yaml` MUST declare `size` as `small` or `large` and one stack for the backend and for each frontend.

**Rationale:** Size decides which record types and test projects apply: `small` or `large` selects the required-record set of the project. Each stack decides which technology pages and checks apply to its application. Declaring both in `.litespec/project.yaml` lets tools and reviewers read the selection from one file. The [scope page](../core/scope.md) states the full selection.

**Reviewed for:** Does `.litespec/project.yaml` at the workspace root declare `size` and one stack for the backend and for each frontend? Read the file.

### Keep the Generated Model Out of Source Control

**ID:** `litespec/workspace/structure#keep-the-generated-model-out-of-source-control`

**Strength:** required

**Statement:** The generated `.litespec/model/` folder MUST stay out of source control.

**Rationale:** The model builder writes generated working files there, such as frontend facts, while it builds the model from records, code, routes, tests, and test runs. Tracking them commits derived files beside their sources, so every commit carries stale working files until somebody rebuilds. The folder is rebuilt rather than reviewed.

**Reviewed for:** Is any path under `.litespec/model/` tracked in git? `git ls-files .litespec/model` prints nothing when the folder stays untracked.

### Keep Production and Test Code Separate

**ID:** `litespec/workspace/structure#keep-production-and-test-code-separate`

**Strength:** required

**Statement:** Production code MUST live under `apps/api/src/` and test code under `apps/api/tests/`.

**Rationale:** The two graphs stay apart by path. The split makes a test dependency reaching production code visible in the path itself, and keeps test helpers from shipping with the product.

**Example:** The backend tree looks like this:

```text
apps/api/
  <backend project manifest>
  src/
  tests/
```

**Reviewed for:** Do production files sit under `apps/api/src/` and test files under `apps/api/tests/`, with no test helper shipped beside production code? Walk both folders.

### Keep Runnable Applications Under Apps

**ID:** `litespec/workspace/structure#keep-runnable-applications-under-apps`

**Strength:** required

**Statement:** An independently runnable frontend, API, or deployed host MUST live under `apps/{name}/`.

**Rationale:** Each root folder has one purpose: `apps/` runs, and `packages/` is reused. A reusable package under `apps/`, or deployable code under `packages/`, breaks that split, so neither location tells the reader what it holds. One home per kind keeps each location meaningful.

**Reviewed for:** Does each directory under `apps/` run on its own, with each directory under `packages/` imported by an application? Read the workspace root and the imports in `apps/`.

### Limit Shared TypeScript Packages

**ID:** `litespec/workspace/structure#limit-shared-typescript-packages`

**Strength:** required

**Statement:** A shared TypeScript package MUST have at least two consumers or carry generated output shared by design.

**Rationale:** A shared package with one consumer still costs versioning, publishing, and cross-package edits. Requiring two consumers, or generated output shared by design, keeps sharing deliberate. The baseline permits shared configuration, generated API types, a thin client, and CSS theme tokens.

**Reviewed for:** Is each package under `packages/` imported by at least two consumers, or does it carry generated output shared by design? Read the imports in `apps/`.

### Keep Project Records Under the Root LiteSpec Directory

**ID:** `litespec/workspace/structure#keep-project-records-under-the-root-litespec-directory`

**Strength:** required

**Statement:** Project records MUST live under the root `.litespec/` directory.

**Rationale:** Project records describe the product, not the standard and not the code. Placed inside the application tree, they disappear when the application is rewritten. The root `.litespec/` directory keeps records where they survive replacing the standard or rewriting the application.

**Reviewed for:** Does every record path sit under the root `.litespec/` directory? Walk the tree from the workspace root.

### Keep Orientation Documents Separate from Canonical Records

**ID:** `litespec/workspace/structure#keep-orientation-documents-separate-from-canonical-records`

**Strength:** required

**Statement:** A README under `.litespec/` MUST link to the record rather than restate its content.

**Rationale:** A README orients while a record decides. A README that restates a decision becomes a second authored source that drifts from the record it summarizes, until the two disagree and neither is trusted. Linking to the record keeps one source with many pointers.

**Reviewed for:** Does each `README.md` under `.litespec/` link to its record and restate no record content? Read each README beside its record.

## Conventions

### Name Frontends by Audience

**ID:** `litespec/workspace/structure#name-frontends-by-audience`

**Strength:** default

**Default:** Name a frontend for its audience, such as `web`, `admin`, `portal`, or `docs`.

**Replacement:** A project whose frontends serve one audience records its own frontend names in `.litespec/project.yaml` under `frontends`.

**Rationale:** The name `apps/web` says who the frontend serves, while `apps/frontend` says only what it is built with. Naming each frontend for its audience keeps the folder meaningful when the technology changes. The names `frontend`, `client`, and `app` name the technology rather than the audience it serves.

**Reviewed for:** Does each directory under `apps/` carry an audience name, or does `frontends` in `.litespec/project.yaml` name the project's own frontend names? Read the `apps/` folders and the project file.

### Keep Generated API Contracts in Packages

**ID:** `litespec/workspace/structure#keep-generated-api-contracts-in-packages`

**Strength:** default

**Default:** Place generated OpenAPI types in `packages/api-types/` and a thin client in `packages/api-client/` when more than one frontend consumes the API.

**Replacement:** A project with a single frontend records that frontend as keeping both outputs.

**Rationale:** Generated OpenAPI types and their thin client belong to every frontend that reads the API. With more than one consumer, `packages/api-types/` and `packages/api-client/` hold one copy each. A single frontend may keep both locally until a second consumer appears.

**Reviewed for:** Do generated OpenAPI types sit in `packages/api-types/` with the thin client in `packages/api-client/` once two frontends consume the API, or does the single frontend keep both outputs? Count the API consumers and read their imports.

## Reference Example

This informative example demonstrates `litespec/workspace/structure#use-the-canonical-root-tree` and `litespec/workspace/structure#limit-shared-typescript-packages`.

An API with public and admin frontends lives in `apps/api/`, `apps/web/`, and `apps/admin/`. Both frontends may import generated transport types from `packages/api-types/`, and neither imports the feature code of the other frontend.
