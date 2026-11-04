# Overview

## Intent

The pages in this folder state the extensions only a .NET backend offers: executable acceptance scenarios and Entity Framework Core persistence for selected records, and layer projects for backends that package each layer as its own project. A local extension stays inactive until one use case, journey, or module meets the criterion its page states; the project extension applies to the whole backend once selected. A use case is one command or query, a journey is the steps to one goal, and a module is one business area; authors turn to these pages when shared examples need business-language review, when aggregates need relational storage the Marten document-storage baseline does not offer, or when the backend keeps the five-project packaging.

## How the Pages Fit Together

The shared [Extensions overview](../../../ext/README.md) catalog holds the extensions any stack can select; the three pages below live beside the .NET stack because they bind to its runners, packages, and providers. Each states its activation criterion, the baseline provisions it replaces, and the records it switches on. The BDD page replaces nothing and switches on scenario records; the EF Core page replaces nothing and names the aggregates that use Entity Framework Core for storage; the Layer Projects page replaces the module-layout provisions its page names for the whole backend.

## .NET Stack Extensions

- [Executable BDD Acceptance Tests](bdd.md): Gherkin features or LightBDD classes for critical examples that need business-language review.
- [EF Core Persistence](efcore.md): relational storage through Entity Framework Core for selected aggregates.
- [Layer Projects](layer-projects.md): the five-project packaging of the layers, the project reference graph, and the per-project layouts.
