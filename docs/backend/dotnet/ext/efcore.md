# EF Core Persistence

**Stack:** dotnet
**Layer:** infrastructure

## Intent

This extension gives selected aggregates relational storage through Entity Framework Core instead of the Marten document-storage baseline. A .NET backend selects it for the aggregates that need relational navigation, change tracking, or provider behavior the baseline does not offer. The [persistence page](../persistence.md) states the baseline every other aggregate keeps.

## Activation

Activation scope: `local`.

Applicable specification kinds: `module`, `use-case`.

The consumer enables `efcore` for the modules or use cases whose aggregates need relational storage the Marten baseline does not offer.

## Baseline Relationship

This extension replaces no baseline provision. It names the aggregates that use Entity Framework Core for storage, while the baseline persistence rules keep describing the document-storage behavior the rest of the backend uses.

This extension switches on no optional record type.

## Standards

None.

## Conventions

None.

## Dependencies

Packages: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `EFCore.NamingConventions`.

Related extensions: the [Outbox Worker](../../../ext/outbox.md), for commands needing durable delivery.

Guides: None.
