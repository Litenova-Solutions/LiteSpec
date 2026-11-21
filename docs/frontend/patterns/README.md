# Overview

The pattern pages hold tested layouts a team chooses from, not required rules. Each pattern decides one recurring interface question once, as drawn options with a named default to build, so every screen answers the same question the same way. A team building a screen starts from the page for its question, an owner picks an option from the drawings, and the team records the choice in its pattern catalog. The [UI Patterns](../patterns.md) page states the catalog contract, the pattern layout, and the wireframe language these pages share.

## What Each Page Holds

Each page below covers one group of patterns: one page per kind of screen or element, from the frame around every page to the fields inside one form. Every pattern names its question, its default option, how the default behaves on a narrow screen, and why the default won. Each option then shows its wireframe with its own rationale and cost. A project builds the default unless its catalog records another option. The Conventions on each page state the defaults every option keeps; a project that needs another behavior records its own choice.

## Pattern Categories

### Page Frames

Pages that fix the frame around every screen.

- [Page Frame Patterns](page-frames.md): the layout for each kind of page, from home and list to report, settings, and outcome.
- [Navigation Patterns](navigation.md): the product frame around every screen, from the sidebar to the command palette and phone navigation.

### Lists and Tables

Pages that present records, one record or many.

- [Record Detail Patterns](record-detail.md): the screen for one record, from its header and tabs to its timeline, notes, and attachments.
- [List and Table Patterns](lists-and-tables.md): columns, sorting, filters, selection, paging, and row commands for many records of one kind.

### Forms and Input

Pages that take values and lay out record tasks.

- [Form and Field Patterns](forms-and-fields.md): how each field kind takes its value, and how a form reports a refused submit or unsaved work.
- [Create, Edit and Remove Patterns](create-edit-remove.md): the layout for each record task, including drafts, corrections, duplicates, and invitations.

### Feedback and States

Pages that report screen states and floating layers.

- [Feedback and State Patterns](feedback-and-states.md): what each screen state shows, from empty and loading to the forbidden, offline, and failed states.
- [Dialog and Overlay Patterns](dialogs-and-overlays.md): when each overlay fits, and how it opens, holds focus, and closes.

### Money and Time

Patterns for amounts, moments, periods, counts, and identifiers that recur in every product, on [Money, Time and Density Patterns](money-time-density.md): how amounts, moments, periods, counts, and identifiers read at every width.

### Documents and Charts

Product-domain examples from a finance product: spending charts, payout timing, invoices, and VAT layouts, on [Money, Time and Density Patterns](money-time-density.md). A team building another product reads them as worked examples, not as patterns it follows.

### Administration

Product-domain examples from a finance product: the settings screens for the team, API keys, imports, the payout account, and invoices, on [Admin Screen Patterns](admin-screens.md). A team building another product reads them as worked examples, not as patterns it follows.
