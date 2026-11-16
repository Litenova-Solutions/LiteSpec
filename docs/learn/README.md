# Overview

LiteSpec is the standard for building one business application from written rules that tools check. These pages explain what it is and how its parts work: technical leads read them to decide whether LiteSpec fits their team, developers read them to learn the parts, and teams read them before handing work to agents.

Without one written shape, each agent invents its own design, and the reasons behind past designs leave with the people who made them.

## The Five-Minute Picture

LiteSpec has four parts. The standard states the published rules for how to build. The records are short YAML files in your project that state what to build and why. The model is what the tools compute from the records and the marks in code and tests: whether the two agree. The tools report the gaps and render the book the team reads.

## The Path

Read these five pages in order. Each one ends with a link to the next.

- [Why LiteSpec Exists](why-litespec.md): the problem LiteSpec solves, for a team deciding whether it fits.
- [How LiteSpec Works](how-litespec-works.md): the parts, the layers, where records live, and how to read a specification.
- [Read the Standard](reading-the-standard.md): project selections, the page arrangement, and provision IDs; how to find the rules that apply to a project.
- [Work with Agents](working-with-agents.md): how an agent finds the rules, what it decides alone, and what it brings back.
- [Adopt LiteSpec in Steps](adopting-litespec.md): taking up LiteSpec one record layer at a time: checked records first, then code traced to records and passing tests that prove scenarios, in either order.

## Evaluate

Two pages sit beside the path for a team that is still comparing options. Read either after Why LiteSpec Exists, in any order.

- [How LiteSpec Compares](how-litespec-compares.md): LiteSpec beside 14 other approaches to spec-driven development, and when each fits.
- [Lineage](lineage.md): the practices LiteSpec borrows from, the words it takes from each, and what it does differently.

## Then Build

Once the path has answered why and how, build. The [Guides overview](../guide/README.md) states what to build first.
