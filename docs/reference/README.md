# Overview

## Intent

The Reference pages list exact values a reader looks up: terms, provision identifiers (IDs citing one rule each), file shapes, schemas, URLs, and the records behind decisions and audit duties. Every project uses them during any task that names a term, cites a provision, or edits a project file. A developer or agent opens them for lookup in the middle of other work, not for reading in order, and they state no rules of their own: each rule lives on the standards page they point to, so these pages stay exact while the rules stay in one place.

## How the Pages Fit Together

Two pages resolve what other pages cite: [Glossary](glossary.md) defines each LiteSpec term, and [Provisions](provisions.md) resolves each provision ID to its heading, page, and mark. Four pages state exact file and contract shapes: [Project File](project-file.md), [Release Manifest](manifest.md), [Formats](formats.md), and [Published URLs](published-urls.md). [Decision Records](decision-records.md) and [Audit Obligations](audit-obligations.md) are the two lookup companions: where a decision record lives and what it holds, and which outside obligations the audit extension satisfies.

## Terms and Provisions

- [Glossary](glossary.md): the one term for each LiteSpec concept.
- [Provisions](provisions.md): every provision ID resolved to its heading, page, and mark.
- [Finding Codes](finding-codes.md): every finding code the tools emit, with its level, layer, trace, meaning, and fix.

## Files and Contracts

- [Project File](project-file.md): every field of `.litespec/project.yaml` and what reads it.
- [Release Manifest](manifest.md): every field of `litespec.manifest.yaml`, and each pinned version.
- [Record Folders](folders.md): the folder and file each record type lives in.
- [Formats](formats.md): every file format, who writes it, its envelope, and the schema and command that check it.
- [Published URLs](published-urls.md): every URL the site serves for agents and people.

## Decisions and Obligations

- [Decision Records](decision-records.md): where a decision record lives, what it holds, and how it ends.
- [Audit Obligations](audit-obligations.md): the external obligations the audit extension satisfies.
