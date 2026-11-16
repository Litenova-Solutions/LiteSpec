# Overview

Claimwick is a small expense product: employees submit work expenses with receipts, managers approve or refuse them, and finance reimburses the approved ones. These pages follow Claimwick from its first records down to the checks that prove it.

Its records live in `examples/claimwick/.litespec/`, with its code, screen, and tests beside them in the same repository. Every record ID, file path, count, and command output on these pages is copied from that repository or from a real run of `litespec` against it, so you can open the same file and read the same text. The pages add no rules: each rule lives on the standards page the text cites.

## How to Read the Example

The pages follow one thread from what the product promises down to how the tools check it:

```trail
product        Claimwick: submit, approve, reimburse
  goal         Decided in days
  problem      Expenses wait weeks
  feature      Approval, with six functional requirements
  research     Approval time benchmarks
  decision     Receipt threshold of 25 EUR
  constraint   Tax receipt retention
  flow         Submit and reimburse, in steps s1 to s5
Behavior        Modules Spending and Reporting: aggregates, rules, use cases, entry point, screen, reaction, job, read model, module events
Implementation  Expense code, handlers, endpoints, screen and tests, each marked with its record
Proof           Acceptance tests, the waivers that cover the gaps, the model check
```

## Reading Order

- [The Claimwick Product](the-product.md): the product brief, module, goal, problem, features, research, decision, constraint and flow records.
- [Modules and Records](modules-and-records.md): the Spending aggregates, rules, use cases, entry point and screen, the reaction, job, notification and setting around them, and the Reporting read model and query.
- [A Use Case in Code](a-use-case-in-code.md): Submit expense, from its record into the .NET code and the acceptance tests that prove it.
- [The Claimwick Screen](the-screen.md): the expenses screen, the generated client it reads, the shared UI package it composes, and the pattern catalog, with the model check over the screen.
- [The Model and Its Checks](model-and-checks.md): the full model check, the findings that waivers cover, a waiver record, and a query of one record.

The cast of people, dates and amounts that the examples share is in `examples/claimwick/.litespec/example-cast.md`.
