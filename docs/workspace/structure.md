# Workspace Structure

**Stack:** any
**Concern:** workspace

A LiteSpec consumer keeps one monorepo tree for its applications, shared packages, documentation, tests, and records. This page defines that tree, where each file kind lives inside it, and what `.litespec/project.yaml` declares. It is for anyone who creates a file, and for agents locating code without asking.

## Concepts

### Applications and Packages

Two folders split the tree by purpose. The `apps/` folder holds what runs: the API, each frontend, and each deployed host. The `packages/` folder holds what gets reused: shared configuration, generated API types, a thin client, and theme tokens.

Deployable code stays out of `packages/`, and reusable code stays out of applications. A second frontend reuses the generated API types from `packages/` instead of importing the first frontend code.

### Records and Their Homes

Each record type keeps one home in the tree, and the model page names the home for each type. Entries live in their home file instead of their own files, so you derive the path from the record type without searching.

A scenario lives in its use-case file, while a requirement lives in its own folder inside its feature folder. While it builds the model from records, code, routes, tests, and test runs, the model builder writes generated working files under `.litespec/model/`. That folder gets rebuilt rather than reviewed.

A named record lives in its own folder as `<name>/<name>.yaml`, with its explanation `<name>.md` beside it. Each record file sits at the home the model page names for its type. A requirement is a named record in its own folder inside the folder of its feature.

An entry lives in its home file: permissions in the permissions file of their module, refusals in the refusals file of their aggregate or, when no aggregate raises them, of their module, and results and scenarios in their use case file. A rule, a setting, and a domain event each live in their own folder under their type folder. Each entry point is one record in its own folder, with its screens beside it.

### The Canonical Tree

Each consumer workspace uses the canonical tree below:

```text
{repo}/
  .config/
    dotnet-tools.json
  .litespec/
    project.yaml
    glossary.yaml
    product.yaml
    intent/
      goals/ directions/ problems/ constraints/ decisions/ research/ actors/ roles/ flows/
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
      rules/{rule}/{rule}.yaml          one cross-module invariant, in its own folder
      external-systems/                 create with the first product-level external system
      entry-points/
        {entry-point}/
          entry-point.yaml
          screens/
            {screen}/{screen}.yaml
      modules/
        {module}/
          permissions.yaml
          refusals.yaml                       refusals no aggregate raises
          stores/ use-cases/ reactions/ jobs/ read-models/ notifications/ external-systems/
          rules/{rule}/{rule}.yaml            one module rule, in its own folder
          settings/{setting}/{setting}.yaml   one setting, in its own folder
          aggregates/
            {aggregate}/
              aggregate.yaml
              refusals.yaml                   refusals the aggregate rules raise
              use-cases/
                {use-case}/
                  {use-case}.yaml   results and scenarios as entries
              module-events/ integration-events/
              domain-events/{event}/{event}.yaml   one event, in its own folder
              rules/{rule}/{rule}.yaml        one aggregate rule, in its own folder
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
  deploy/                               how the applications run together: the AppHost and the deployment files
  packages/
  AGENTS.md
  <toolchain pin file>
  package.json                          when a frontend or TypeScript package exists
  pnpm-lock.yaml                        when a frontend or TypeScript package exists
  pnpm-workspace.yaml                   when a frontend or TypeScript package exists
```

A backend stack binding names the files for the entries in angle brackets. No backend application or frontend application sits at the workspace root. Under `behavior/modules/`, each module folder holds its typed folders, and `aggregates/` holds one folder per aggregate in plural kebab-case form.

### Project Selection and Code Separation

`.litespec/project.yaml` declares `size` as `small` or `large` and one stack for the backend and for each frontend. Size decides which record types and test projects apply, and each stack decides which technology pages and checks apply to its application.

The generated `.litespec/model/` folder stays out of source control. The model builder writes generated working files there, such as frontend facts, while it builds the model from records, code, routes, tests, and test runs. Tracking them commits derived files beside their sources, so each commit carries stale working files until somebody rebuilds. `git ls-files .litespec/model` prints nothing when the folder stays untracked.

Production code lives under `apps/api/src/` and test code under `apps/api/tests/`. The two graphs stay apart by path. An independently runnable frontend, API, or deployed host lives under `apps/{name}/`. Each root folder keeps one purpose: `apps/` runs, and `packages/` gets reused.

### Shared Code, Records, and Frontend Names

A shared TypeScript package serves at least two consumers or carries generated output shared by design. A shared package with one consumer still costs versioning, publishing, and cross-package edits. The baseline permits shared configuration, generated API types, a thin client, and CSS theme tokens.

Project records live under the root `.litespec/` directory. Placed inside the application tree, they disappear when the application gets rewritten. A README under `.litespec/` links to the record instead of restating it: a README orients while a record decides, and a second authored source drifts from the record it summarizes.

Frontends take audience names: `web`, `admin`, `portal`, or `docs`. The names `frontend`, `client`, and `app` name the technology instead of the audience it serves. Generated OpenAPI types sit in `packages/api-types/` and a thin client in `packages/api-client/` when more than one frontend consumes the API; a single frontend keeps both locally until a second consumer appears.

## Standards

None.

## Conventions

None.
