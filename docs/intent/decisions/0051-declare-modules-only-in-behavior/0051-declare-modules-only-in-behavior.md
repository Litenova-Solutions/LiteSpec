# Declare Modules Only in Behavior

LiteSpec's layers run from the product's intent down to its code, and each layer is written from the one above it. This decision keeps the module, a code boundary with its dependencies, in Behavior, where it is written from the aggregates it groups.

## Context

LiteSpec defined Intent modules from Behavior, so the Intent grouping of records followed code boundaries and not product reasons. A module record also carried `dependsOn`, a code dependency, and its summary described what the code does.

Once features sit under problems, the Intent module folders hold nothing. A reader who opens Intent to learn why the product is built sees code boundaries where the product's reasons should be.

Keeping both layers equal would have made every module name a product claim, and it would have kept the code boundary in Intent.

## Options

LiteSpec compared 3 options for where a module is declared. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Modules only in Behavior | Intent is organized by reason, and the module sits beside its aggregates. | The Intent tree loses its module grouping. |
| Intent modules equal to Behavior modules | No record moves. | Intent follows code boundaries, and an Intent module folder is empty. |
| Intent product areas apart from Behavior modules | Intent gets its own grouping by area. | Two sets of names must stay in step. |

## Decision

A module is declared only in Behavior, at `behavior/modules/<module>/module.yaml`, with its explanation beside it. Intent holds goals, problems, constraints, decisions and research at product level, and its features sit under problems.

The Behavior page holds the Modules as Product Areas concept, since a module is the place where one name runs through Behavior and code. The book groups Intent by problem and Behavior by module.

## Consequences

`record-placement-invalid` reports any file under `intent/modules/`, and the closed folder set of Intent loses its module level. The Intent questions name problem, then feature, then requirement. The glossary names Behavior as the layer of Module.

The change is a major-release change, since the module record moves and the module provision takes a Behavior identifier. Each consumer moves its module records under the changelog's upgrade step.

## Revisit Conditions

This decision holds until consumers report that a product area spans several problems and cannot be found in Intent. It also holds until a module's product reasons are lost once the module moves to Behavior. Either report reopens the choice.
