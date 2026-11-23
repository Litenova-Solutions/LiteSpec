# Overview

The backend pages state how a LiteSpec server application is built: its layers, business model, coordination of use cases, storage, HTTP boundary, testing, and record identifiers.

A server project applies these pages through its stack, while a product without a server applies only the architecture, domain, and application pages.

A use case is one command or query with its access, results, and scenarios; a reaction answers an event without a request, and a job runs work on a schedule or by hand.

## How the Pages Fit Together

The seven principle pages directly in this folder hold for every backend. They state what each layer owns and which way references point, without naming a framework or a package. The [.NET Stack](dotnet/stack.md) states how the .NET backend realizes those principles: its Kernel, Platform, knowledge, and module projects with their hosts, its C# shapes, and its Marten persistence.

When you add a use case, read the principle page for the layer it touches first, then the matching .NET page for the binding.

## Backend Principles

- [Backend Architecture](architecture.md): the four layers, the knowledge layer beside modules, the direction references point, and the module tree that organizes every layer.
- [Knowledge](knowledge.md): knowledge areas, models and sets, how operational code reads rules by role and date, and what is not knowledge.
- [Domain Layer](domain.md): aggregates, lifecycles, closed value sets, and domain repositories, with the boundary that keeps technical concerns out of business rules.
- [Application Layer](application.md): how use cases coordinate, and what pipeline stages, ports, reactions, jobs, and declared effects do.
- [Persistence](persistence.md): domain repositories, the read session, the one commit per command, and the obligations on stored data.
- [HTTP API](api.md): how endpoints, routes, errors, and collection paging serve HTTP requests, and what the generated OpenAPI contract holds.
- [Backend Testing](testing.md): which test suites each project size keeps, and what each suite runs with.

## Backend Stack

- [.NET Stack](dotnet/stack.md): the .NET binding of every principle page, and the test-result formats the model builder reads.
- [.NET Extensions Overview](dotnet/ext/README.md): the extension only a .NET backend offers: executable BDD scenarios.
