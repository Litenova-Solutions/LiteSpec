# Overview

The dotnet pack binds LiteSpec to a .NET backend. Selecting `dotnet` applies every page in this folder with its checks, analyzers, and readers. Read the pages below in order when starting backend work, then load only the pages each task touches.

## Reading Order

- [Stack](stack.md): what selecting `dotnet` applies, and how the stack reader links code with records.
- [Architecture](architecture.md): projects, modules, layers, and the boundaries between them.
- [Domain](domain.md): aggregates, lifecycles, value objects, closed sets, and refusals in C#.
- [Application](application.md): command and query handlers, reactions, jobs, and the mediator pipeline.
- [API](api.md): endpoints, operation metadata, transport models, and the global failure mapping.
- [Persistence](persistence.md): repositories, the read session, the single commit, and the Infrastructure layout.
- [Testing](testing.md): acceptance tests through the outbox, and executable business scenarios.
- [Config](config.md): SDK, build settings, package versions, and local tools.
- [Naming](naming.md): file names, cancellation tokens, and event names in C#.
- [Packages](packages.md): the LiteBus modules each layer references, and the analyzers.
- [Workspace](workspace.md): solutions, the AppHost, and development shortcuts.
- [Golden Slice](golden-slice.md): one complete end-to-end .NET vertical slice in v7 style.
