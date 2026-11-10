# <Project> Agent Context

## LiteSpec
This project follows LiteSpec __LITESPEC_VERSION__: size __SIZE__, shape __SHAPE__, stacks __STACKS__, extensions none.
Before a task, read https://litespec.dev/__LITESPEC_VERSION__/select/__SELECTION_KEY__/llms.txt and fetch only the pages the task touches.
Selected extensions: none
UI work: the pattern pages listed in that index.

Read `.litespec/project.yaml` before changing the project. (litespec/core/agent#select-task-context)

Run `__LITESPEC_COMMAND__ rules <path>` before editing a file. (litespec/core/agent#select-task-context)

Answer each returned provision's "Reviewed for" question before reporting done. (litespec/core/agent#select-task-context)

Query the model builder for the records a task touches, with their links and the tool-read code and test state, instead of reading page lists. (litespec/core/agent#select-task-context, litespec/core/scope#record-selected-extensions)

`__LITESPEC_COMMAND__ model query <record-id>` returns one record with its links. Query the record before changing behavior it describes, and query again when a link leads further. (litespec/core/agent#select-task-context)

Write each layer from the layer above it: Intent states the promises, Behavior states how the system keeps them, Implementation is code only, and Proof shows the checks. Never compute a record from code.

Build only what the specification states, and take anything missing to the owner.

Declare every edge between use cases, reactions, jobs, notifications, integration events, and external systems. An undeclared edge is the defect.

A project Exception record overrides a Standard only when it names the provision ID, with reason and expiry date. (litespec/core/agent#apply-guidance-precedence)

An explicit project convention in `.litespec/governance/conventions/` can replace a baseline Convention when it cites the Convention's ID. (litespec/core/agent#apply-guidance-precedence)

Read `.litespec/intent/product.yaml` and the affected module's feature files before changing behavior that touches money, payouts, or terms. (litespec/core/intent#keep-intent-in-its-homes)

Record a product decision for any changed product choice. Research outside `.litespec/` is source material, never authority. (litespec/core/intent#treat-generated-research-as-source-material)

## Project Commands

Run the backend gates for every changed API boundary. (litespec/core/release#run-boundary-selected-checks)

```bash
dotnet build <api-solution> --configuration Release
dotnet test <api-solution> --configuration Release --no-build
```

Run the root pnpm gates for every changed frontend in `.litespec/project.yaml`. (litespec/core/release#run-boundary-selected-checks)

Run metadata and model checks for each changed record, source, or test. Query the model again after the change, and report what stays red. (litespec/core/system#update-records-with-behavior, litespec/core/authoring#declare-structured-specification-metadata)

Report every skipped check with its reason, and name the provisions `rules` returned and how each was judged. (litespec/core/release#report-verification-exactly)

Compose each route from one floorplan in the frontend `DESIGN.md`, or name the decision behind a freestyle route. (litespec/frontend/ui#compose-each-route-from-one-floorplan, litespec/frontend/ui#record-a-freestyle-route)

Cite every path of every use case a route calls in a test title, or record an exclusion with its reach. (litespec/frontend/ui#prove-every-result-a-route-calls)

Run `__LITESPEC_COMMAND__ model check` after changing UI configuration, the design contract, source locks, CSS, or primitives. Run the route suite after changing a route. (litespec/frontend/ui#prove-ui-behavior-and-appearance, litespec/frontend/ui#check-every-route)

Record an exception before selecting another visual system or component base. (litespec/frontend/ui#select-one-visual-authority)
