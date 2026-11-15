# Claimwick

Claimwick is the LiteSpec example consumer: a small, realistic SaaS product the docs show as a real, checked project.

Employees submit work expenses with receipts. Managers approve or reject them, and finance reimburses the approved ones.

## Layout

Records live under `.litespec/` by layer: the product record at `.litespec/product.yaml`, above the record layers; Intent records under `.litespec/intent/`, with standalone features under `.litespec/intent/features/`, flows under `.litespec/intent/flows/`, and roles under `.litespec/intent/roles/`; Behavior records under `.litespec/behavior/`, with read models under `.litespec/behavior/modules/<module>/read-models/`, domain events in the aggregate files, and screens under `.litespec/behavior/entry-points/claimwick-app/screens/`; the UI pattern catalog under `.litespec/implementation/patterns/`, runbooks under `.litespec/implementation/modules/spending/runbooks/`, governance records under `.litespec/governance/`, and the glossary in `.litespec/glossary.yaml`. Code lives under `apps/`: the .NET backend sample under `apps/api/`, the Svelte frontend under `apps/web/`, and the generated API client under `packages/api/`.

The code shows the shape the stack pages require: sealed state records, one `DomainException` subclass per refusal, internal sealed handlers, `Async` method names, and acceptance scenarios carrying proves marks. It is not a compiled solution: it holds no project or solution files, and its samples exist to give the model builder its marks.

## Checks

Check the example with:

```bash
litespec model check --root examples/claimwick
```

The check reports no open findings, only covered ones. The departures park the export with no handler and no endpoint, the browser-reached use cases and flow steps with no screen yet, the reimbursed-expenses list no flow step uses, the scenarios with no passing proof, and the weekend export review bypass.
