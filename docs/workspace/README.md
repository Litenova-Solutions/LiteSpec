# Overview

Every repository built with LiteSpec shares one shape: a canonical monorepo tree, names that expose architectural role, the packages each layer from Domain to the API host may take, and where each configuration value lives. The Workspace pages define that shape. Every project applies them, whatever its size, shape, or technology stacks.

## How the Pages Fit Together

[Workspace Structure](structure.md) defines the tree, and the other three pages define what lives where inside it. [Naming and Code Style](naming.md) fixes names and suffixes, [Dependencies](dependencies.md) fixes which layer may take which package, and [Configuration](config.md) fixes where each value lives and who owns it. The .NET stack refines these pages for its own projects and packages, starting from its [stack page](../backend/dotnet/stack.md).

## Pages

- [Workspace Structure](structure.md): the canonical monorepo tree for applications, packages, documentation, tests, and records.
- [Naming and Code Style](naming.md): the names, suffixes, and file conventions that expose intent and role.
- [Dependencies](dependencies.md): which layer takes which package, who owns each package, and what a new package needs before it lands.
- [Configuration](config.md): where each value lives, who owns it, and how a wrong value fails.
