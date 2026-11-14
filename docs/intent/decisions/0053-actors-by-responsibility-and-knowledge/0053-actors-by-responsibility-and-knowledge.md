# Describe Actors by Responsibility and Knowledge

An actor is a person, organization or system that uses the product. This decision describes each actor by what it answers for and what the product assumes it knows, and it removes the section that restated what the actor may reach.

## Context

The Access section of an actor restated the access its role grants, and it added rules about sessions and sign-in. The role's own files already state the permissions and their limits, so the same grant appeared in two places and could drift.

An actor often stated what users know as a fact about every user, for example that a user has no training in accounting. The actor had no section for an assumption of the product, so the assumption landed in the opening paragraph.

Product rules, such as an approval rule, also appeared in actor files, since the rule against behavior in explanation files applied only to features and requirements at that time. The example for an actor often carried numbers that said nothing about the actor.

## Options

LiteSpec compared 3 options for the sections of an actor explanation. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Responsibilities, Knowledge and Example, with Access removed | Each section says what the actor answers for or what the product assumes. | Every actor file is rewritten. |
| Access narrowed to why the actor holds its role | The reason for the grant sits in the actor's own file. | The role's Limits section states the same reason. |
| Sections kept, prompts fixed | No section moves. | The Access section keeps restating the role, and no section holds assumptions. |

## Decision

An actor explanation carries Responsibilities, Knowledge and Example (optional), in that order, after its opening paragraph. Access is removed.

Responsibilities states the tasks and decisions the actor answers for. It states no product rule, since what the product accepts, refuses, calculates or records belongs in a requirement.

Knowledge states what the product assumes the actor knows and what it does not assume. It is written as an assumption of the product, such as "Claimwick does not assume the employee knows the expense policy", and never as a fact about every user.

The `holds` field names the role and its scope. The role's own files state its permissions and why its scope fits.

## Consequences

`litespec/core/intent#state-what-the-product-assumes-each-actor-knows` is required and reviewed for. It asks whether an actor explanation states a user's knowledge as a fact about every user. The shared provision that keeps product rules out of Intent explanation files applies to actors as it applies to every other Intent record.

`litespec new actor` writes the new sections. The section check fails an actor explanation that keeps an Access section or lacks Responsibilities or Knowledge.

## Revisit Conditions

This decision holds until readers ask what an actor may reach and cannot find it without opening the role. It also holds until most actor files carry a Knowledge section that repeats the opening paragraph. Either report reopens the choice.
