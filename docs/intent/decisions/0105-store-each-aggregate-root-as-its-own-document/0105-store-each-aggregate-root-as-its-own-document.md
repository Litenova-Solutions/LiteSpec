# Store Each Aggregate Root as Its Own Document

A project stores each aggregate root as its own Marten document and writes no parallel document type for an aggregate. Infrastructure owns the JSON contract that makes it work, so Domain carries no serialization attribute.

## Context

The pages implied that aggregates are stored directly but never said so: only the repository example, with its load of the aggregate type, showed it. A consumer backend therefore followed the common .NET habit and wrote a parallel document type per aggregate, with the repository mapping between the aggregate and its document. Query handlers then queried those Infrastructure documents from Application, which the layer analyzer reported in every file. Nothing said how an aggregate with private setters, closed-set states, and value objects becomes a Marten document while Domain carries no serialization attribute.

## Options

LiteSpec compared 2 options for aggregate storage. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Store each aggregate root as its own Marten document, with the JSON contract owned in Infrastructure | One shape from the handler to the disk, no mapping code, and queries read the aggregate type through the read session. | The store serializer carries the converters, discriminator registrations, and non-public access the aggregate shape needs. |
| Let each project choose between storing the aggregate and writing parallel document types | Projects keep their existing document habit. | Two models of one thing, mapping code between them, and reads split across both. |

## Decision

Marten stores each aggregate root as its own document, with no parallel document type for an aggregate. The Infrastructure store setup carries the alias, the typed-identifier mapping, the serializer with its converters and discriminator registrations, and optimistic concurrency.

## Consequences

The new provision is `litespec/backend/dotnet/persistence#store-each-aggregate-root-as-its-own-document`. Query handlers query the aggregate type through `IQuerySession`. LS2018 names the fix when the Infrastructure type it reports is a Marten document of an aggregate: query the aggregate type through `IQuerySession`. A query-shaped read document still serves queries under `litespec/backend/persistence#add-read-documents-for-query-shaped-data` and is no parallel aggregate document.

## Revisit Conditions

This decision reopens when a stored shape proves unreachable through the aggregate type with the contract Infrastructure can own.

That case means the aggregate shape outgrew what the store contract can carry. The team then decides again how that shape is stored.
