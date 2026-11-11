# Overview

## Intent

The example pages show LiteSpec applied to one product, Claimwick, a small application in which employees submit work expenses with receipts, managers approve or refuse them, and finance reimburses the approved ones. Its records live in `examples/claimwick/.litespec/`, and its code, screen and tests sit beside them in the same repository. Every record ID, file path, count and command output on these pages is copied from that repository or from a real run of `litespec` against it, so a reader can open the same file and see the same text. The pages state no rules of their own: each rule lives on the standards page that the text cites.

## How to Read the Example

The pages follow one thread from the product's intent down to its checks:

```trail
product        Claimwick: submit, approve, reimburse
  module       Spending, holding two features and their requirements
  goal         Decided in days
  problem      Expenses wait weeks
    feature    Approval, with five functional requirements
  research     Approval time benchmarks
  decision     Receipt threshold of 25 EUR
  constraint   Tax receipt retention
  journey      Submit and reimburse, in steps s1 to s8
Behavior        Aggregates, rules, use cases, entry point, screen, reaction, job
Implementation  Expense code, handlers, endpoints, screen and tests, each marked with its record
Proof           Acceptance tests, the departures that cover the gaps, the model check
```

## Reading Order

- [The Claimwick Product](the-product.md): the product brief, module, goal, problem, features, research, decision, constraint and journey records.
- [Modules and Records](modules-and-records.md): the Spending aggregates, rules, use cases, entry point and screen, and the reaction, job, notification and settings around them.
- [A Use Case in Code](a-use-case-in-code.md): Submit expense, from its record into the .NET code and the acceptance tests that prove it.
- [The Claimwick Screen](the-screen.md): the expenses screen, the generated client it reads, the shared UI package it composes, and the pattern catalog, with the model check over the screen.
- [The Model and Its Checks](model-and-checks.md): the full model check, the findings that departures cover, a departure record, and a query of one record.

The cast of people, dates and amounts that the examples share is in `examples/claimwick/.litespec/example-cast.md`.
