# Overview

The Workspace pages define the shape every LiteSpec repository shares: one monorepo tree, names that state architectural role, the packages each layer from Domain to the API host may take, and one home for each configuration value. Every project applies them, whatever its size, shape, or technology stacks.

## How the Pages Fit Together

[Workspace Structure](structure.md) defines the tree, and the other four pages define what lives inside it: [Naming and Code Style](naming.md) sets names and suffixes, [Class Design](design.md) sets one responsibility per class, one capability per set of interfaces, and composition over helper base classes, [Dependencies](dependencies.md) sets which layer takes which package, and [Configuration](config.md) sets where each value lives and who owns it. The .NET stack refines these pages for its own projects and packages, starting from its [stack page](../packs/dotnet/stack.md).

## Pages

- [Workspace Structure](structure.md): the canonical monorepo tree for applications, packages, documentation, tests, and records.
- [Naming and Code Style](naming.md): the names, suffixes, and file conventions that state intent and role.
- [Class Design](design.md): the design rules behind the names: one responsibility per class, one capability per set of interfaces, and composition over helper base classes.
- [Dependencies](dependencies.md): which layer takes which package, who owns each package, and what a new package needs before it is added.
- [Configuration](config.md): where each value lives, who owns it, and how a wrong value fails.
