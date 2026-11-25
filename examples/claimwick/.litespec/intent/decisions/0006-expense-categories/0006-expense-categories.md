# Expense Categories

Each expense carries one category from a fixed list, so finance sorts payouts by kind of cost. The list holds travel, meals, lodging and other, and this decision sets fixed categories over free text and per-team lists.

## Context

Finance sorts payouts by kind of cost. Each expense carries the kind it pays for, and the list of kinds stays fixed so every report reads the same values.

Free text would let each report spell each kind its own way, and per-team lists would leave finance comparing teams on different values. One fixed list keeps every report on the same values.

## Options

Three options were weighed on the same points: free text, fixed categories, and categories per team.

- Free text: a submitter names any kind, but every report spells each kind its own way.
- Fixed categories: every report reads the same values, but a new kind needs a record change.
- Categories per team: each team names its own kinds, but finance compares teams on different values.

Free text fails on spelling, and per-team lists fail on comparison. Fixed categories keep every report on the same values, and accept the record change each new kind needs as their drawback.

## Decision

Each expense carries one category from a fixed list.

The table `reference-data/spending.expense-category` holds the list: travel, meals, lodging and other. Finance chose fixed categories over free text and categories per team.

## Consequences

Expenses carry one of travel, meals, lodging or other: a new kind arrives as a new choice with its rows, and the generated closed set carries it, so reports keep reading one list.

Each new kind needs its meaning and its row before submissions may carry it.

## Revisit Conditions

The choice reopens when finance needs a kind the list does not hold. A kind filed as other twice would show the list needs a new choice.
