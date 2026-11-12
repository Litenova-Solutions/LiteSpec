# Overview

## Intent

LiteSpec is the standard for building one business application from written rules that tools check. Technical leads read these pages to decide whether LiteSpec fits, developers read them to learn how its parts work, and teams read them before handing work to agents. Without one written shape, each agent invents its own design and the reasons behind past designs leave with the people who made them. This page is the single start: it states the five-minute picture and the one order to read in.

## The Five-Minute Picture

LiteSpec has four parts. The standard states the published rules for how to build. The records are short YAML files in your project stating what to build and why. The model is what the tools compute from the records and the marks in code and tests: whether the two agree. The tools report the gaps and render the book the team reads.

## The Path

Read the five pages below in order. Each one ends with a link to the next.

- [Why LiteSpec Exists](why-litespec.md): the problem LiteSpec solves, for a team that is deciding whether LiteSpec fits.
- [How LiteSpec Works](how-litespec-works.md): the parts, the layers, the record homes, and how to read a specification.
- [Read the Standard](reading-the-standard.md): project selections, page arrangement, and provision IDs; how to find the rules that apply to a project.
- [Work with Agents](working-with-agents.md): how an agent finds the rules, what it decides alone, and what it brings back.
- [Adopt LiteSpec in Steps](adopting-litespec.md): adopting LiteSpec by record layer: the record model first, then code traced to records, then LiteSpec's layers, with the records path through the tutorial.

## Evaluate

Two pages sit beside the path for a team that is still comparing options. Read either after Why LiteSpec Exists, in any order.

- [How LiteSpec Compares](how-litespec-compares.md): LiteSpec beside 14 other approaches to spec-driven development, and when each fits.
- [Lineage](lineage.md): the practices LiteSpec borrows from, the words it takes from each, and what it does differently.

## Then Build

When the path above has answered why and how, build. [Start a Consumer Repository](../tutorial/getting-started.md) turns an empty folder into a checked consumer with one coded use case, then continues into [Model a Behavior Module](../tutorial/model-domain.md). A team that wants records without code yet takes [Start with Records Only](../guide/start-with-records-only.md) instead.
