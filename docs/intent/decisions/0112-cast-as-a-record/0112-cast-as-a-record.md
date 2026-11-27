# Cast as a Record

The cast is a typed record, not a Markdown document. This decision gives every person, party, occasion and figure an identifier and a machine-readable value, so links are checked, back-references are computed and development seed data draws from one source, under a rule that keeps cast values out of production.

## Context

The example cast was a Markdown document at `.litespec/example-cast.md`, with the `example-cast` document page style. Its people, parties, occasions, figures and values were typed as prose, so tools could not read them as data. Links to actors and terms went unchecked, and "where it appears" back-references were not computed.

A reader could not ask which record or code mark used a person without searching the whole tree. Development seed data had no one machine-readable source to draw from, so each local setup repeated the names and amounts by hand.

## Options

LiteSpec compared 2 options for the example cast. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Make the cast a typed record at `.litespec/cast.yaml` with four optional entry lists | Names, actors, parties, occasions and typed values become machine-readable. Link checks resolve `actor`, `partyOf`, `involves` and `term`, back-references are computed, and one cast feeds development seed data under a rule that keeps cast values out of production. | Every example, template, init copy and page-style check changes in one change. |
| Keep the example cast as a Markdown document | Nothing changes in the examples, templates or validator. | Values stay unreadable as data, links go unchecked, back-references stay uncomputed, and seed data keeps no one source. |

## Decision

The cast becomes a typed record at `.litespec/cast.yaml`, a fixed-name file at the top of `.litespec/` beside `product.yaml` and `glossary.yaml`. An optional explanation `.litespec/cast.md` sits beside it and tells the story of the cast for people, adding no fact. The record has type `cast`, id `cast/<product-name>`, one cast per project, and four optional entry lists: `parties`, `people`, `occasions` and `figures`. Each entry carries `type` equal to its kind, as other entry-carrying records do.

A figure holds exactly one of `money`, `quantity`, `percentage`, `date`, `count` or `text`. Entry ids follow `party/<name>`, `person/<name>`, `occasion/<name>` and `figure/<name>`. Each link field reads in the Links table generated from the schema, like every other link: `actor` (plays / is played by), `partyOf` (belongs to / has member), `involves` (involves / takes part in), and `term` on a figure or a fact (instantiates / is instantiated by).

The `example-cast` document page style, its page sections check, the `init` copy of `example-cast.md` and the `example-cast` template are removed. `litespec init` copies a schema-valid `cast.yaml` template with a placeholder in every field, and the Claimwick example converts its cast to `cast.yaml` plus `cast.md`, keeping Ava, Ben, Cara, the consultancy, the occasion and the amounts.

## Consequences

The cast is optional, and a project with no cast has no finding. The schema reports `record-invalid` for a malformed cast, and the existing unknown-link checks cover `actor`, `partyOf`, `involves` and `term`. Two checks are added: `cast-identity-unreserved` (level G, blocking) for a person email outside a reserved test domain, and `cast-entry-unused` (level A, advisory) for an entry no record field, explanation file or code mark cites by id or, for a person or party, by exact name. Examples draw their details from the cast record.

`litespec/quality/development#seed-only-development-and-test-environments` allows cast names and values in development seed data, demo data, fixtures and tests and keeps them out of production code paths and production data: seed and reset commands refuse to run in production. Seed and fixture code marks the entries it uses with `<seeds>party/...</seeds>` in C# or `// litespec: seeds party/...` in TypeScript, read like the other code marks, and `mark-unknown` reports an unknown id.

The book renders the cast as one page: the parties, the people with their actors, the occasions and the typed figures. "Where it appears" is computed from the records, explanation files and code marks that cite each entry; nobody types it.

## Revisit Conditions

This decision reopens when a cast figure needs a value the six kinds do not hold, or when a project needs two casts.

Either case means one cast record no longer holds every example value the project uses. The team then decides again what the cast holds and how many a project keeps.
