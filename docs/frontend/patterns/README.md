# Overview

## Intent

The pattern pages decide each recurring interface question once, as drawn options with a named default to build, so every screen answers the same question the same way. A team building a screen starts from the page for its question, an owner picks an option from the drawings, and the team records the choice in its pattern catalog. The [UI Patterns](../patterns.md) page states the catalog contract, the pattern layout, and the wireframe language these pages share.

## How the Pages Fit Together

Each page holds one group of patterns: one page per kind of screen or element, from the frame around every page to the fields inside one form. Every pattern names its question, its default option, its narrow-screen behavior, and why the default won, then draws each option as a wireframe with its own rationale and cost. A project builds the default unless its catalog records another option.

## Pattern Pages

- [Page Frame Patterns](page-frames.md): the layout for each kind of page, from home and list to report, settings, and outcome.
- [Navigation Patterns](navigation.md): the product frame around every screen, from the sidebar to the command palette and phone navigation.
- [Record Detail Patterns](record-detail.md): the screen for one record, from its header and tabs to its timeline, notes, and attachments.
- [List and Table Patterns](lists-and-tables.md): columns, sorting, filters, selection, paging, and row commands for many records of one kind.
- [Form and Field Patterns](forms-and-fields.md): how each field kind takes its value, and how a form reports a refused submit or unsaved work.
- [Create, Edit and Remove Patterns](create-edit-remove.md): the layout for each record task, including drafts, corrections, duplicates, and invitations.
- [Feedback and State Patterns](feedback-and-states.md): what each screen state shows, from empty and loading to the forbidden, offline, and failed states.
- [Dialog and Overlay Patterns](dialogs-and-overlays.md): when each overlay fits, and how it opens, holds focus, and closes.
- [Money, Time and Density Patterns](money-time-density.md): how amounts, moments, periods, counts, and identifiers read at every width.
- [Admin Screen Patterns](admin-screens.md): the settings screens for the team, API keys, imports, the payout account, and invoices.
