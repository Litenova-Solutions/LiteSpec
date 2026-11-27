# State Class Design Rules

LiteSpec states its class design rules on one workspace page: one responsibility per class, one capability per set of interfaces, one class per pipeline concern, and composition over helper base classes. Classes and structs avoid primary constructors; positional records keep them.

## Context

LiteSpec covered naming, layers, folders, and file-to-type matching, but no provision stated the design principles under them. Without one, an author optimizes for fewer files and merges unrelated concerns wherever an interface allows it: one class implementing the authorization and audit contracts for a message, a repository that also renders, a loader that also holds constants. Each merge reads in one review and changes for one reason too many.

At the same time the pages recommended primary constructors on classes and structs and used them in examples. That shape hides state: a primary constructor parameter is captured, mutable, and visible in the whole class body, with no read-only marking and no naming boundary between state and parameter. Constructor logic has no home, documentation moves to the type, long declarations hide the type's name and role, and the resemblance to positional records misleads readers who know records.

## Options

LiteSpec compared 2 options for class design guidance. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Add the class design page and advise against primary constructors on classes and structs | One responsibility, one capability, one pipeline concern per class, and composition over helper bases become reviewable rules, and every example shows explicit fields and constructors. | More, smaller classes with longer constructor declarations, and every .NET example is rewritten once. |
| Leave class design unstated and keep recommending primary constructors | No new page and no example rewrites. | Unrelated concerns keep merging into one class, and the examples keep teaching the less readable shape. |

## Decision

The workspace gains the Class Design page with its four rules and the pipeline declaration naming convention. Classes and structs avoid primary constructors; positional records keep their positional form. Every .NET example uses explicit fields and constructors.

## Consequences

The new provisions are `litespec/workspace/design#give-each-class-one-responsibility`, `litespec/workspace/design#implement-one-capability-per-class`, `litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class`, `litespec/workspace/design#take-shared-behavior-through-composition`, and `litespec/workspace/design#name-each-definition-after-its-message-and-concern`, with `litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs` carrying the advice against primary constructors. An analyzer reports a class that implements two LiteBus message-definition contracts, and an info-level analyzer reports a class or struct primary constructor. The scaffold ships `csharp_style_prefer_primary_constructors = false` in the `.editorconfig` it writes. The review list asks whether any class implements two unrelated interfaces.

## Revisit Conditions

This decision reopens when reviews cannot apply the one-responsibility test plainly, or when a language change removes the stated costs of primary constructors.

Either case means the stated rules no longer match how classes read. The team then decides again what class design LiteSpec asks for.
