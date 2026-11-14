# Put Child Records in Typed Folders

A parent record holds child records, such as the requirements of a feature or the use cases of an aggregate. This decision puts every child stored as its own file in a folder named for its type, so a reader of a parent folder sees its own files and each kind of child apart from the others.

## Context

Some parents held child folders directly beside their own files, and others used a folder named for the child type. A requirement folder sat directly in its feature's folder, and an aggregate folder sat directly in its module's folder, where any other subfolder of a module counted as an aggregate.

A use case sat directly in its aggregate's folder, while a second home for use cases existed at module level. The record layer also kept flat homes for a module that kept its records flat, which was a second layout for the same records.

A reader who opened an untyped folder could not tell a parent's own files from its children, or one kind of child from another, without opening each file.

## Options

LiteSpec compared 3 options for where child records sit. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One typed folder per child type inside every parent | Each kind of child has its own folder, and one table governs every parent. | A parent with one child type gains a folder, and existing children move. |
| Typed folders only where a parent holds several child types | A parent with one child type keeps its layout. | Two layouts stay for the same kind of record. |
| Keep the current mix | No path changes. | A reader cannot tell a parent's files from its children. |

## Decision

A child record stored as its own file sits in its own folder. That folder is named for the child type in the plural and sits inside the parent's folder. A child stored as an entry, such as a result, scenario, permission, journey step or source, stays in its parent's file.

The homes are the same for every module. Requirements sit in a requirements folder under their feature. Aggregates sit in an aggregates folder under their module. Use cases sit in a use-cases folder under their aggregate, and a use case that belongs to no aggregate sits in a use-cases folder under its module. Events and rules sit in folders under the aggregate. A product-level quality requirement sits in the product's requirements folder.

## Consequences

The flat module layouts are removed, so each module has one layout. The set of folders that a module may hold is closed, and a subfolder outside that set reports `record-placement-invalid`. Creation and renaming write only these homes.

Identifiers do not change, because none names a type folder. Code paths keep their layer names, since the typed folders organize records and do not change how code is named. The folder reference page is generated from the folder table, so the table and the page cannot drift.

## Revisit Conditions

This decision holds until a parent with one child type gets a folder that helps no reader. It also holds until the closed set of folders blocks a child type that a product needs. Either report reopens the choice.
