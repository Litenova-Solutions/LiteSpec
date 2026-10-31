---
{
  "type": "decision",
  "id": "decision/0007-adopt-setting-readby",
  "title": "Adopt Setting Readby",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Adopt readBy, drop sentBy and writes",
      "pros": "Authors already reach for a reader link the schema forbids, while the sender is provable in reverse and the writes list carries no information.",
      "cons": "One consumer cleans two invented fields, and the adopted name reads passively against the verb grammar."
    },
    {
      "name": "Leave all three unstandardized",
      "pros": "No schema, Links table, or provision change is needed.",
      "cons": "Real hierarchy information stays outside the schema and outside every check, exactly how the next overload starts."
    },
    {
      "name": "Adopt all three fields",
      "pros": "Every invented edge becomes checked data instead of silent prose.",
      "cons": "A sender link duplicates the reverse proof that already passes on every notification, and an always-empty writes list standardizes nothing."
    },
    {
      "name": "Make readBy the setting parent",
      "pros": "A setting nothing reads would then fail the parent rule directly.",
      "cons": "The parent stays the module by identifier, and readBy is a connection that never replaces serves or fulfills."
    }
  ],
  "choice": "Adopt readBy, drop sentBy and writes",
  "reopenedWhen": "A setting kind appears whose readers no use case can name.",
  "basedOn": [
    "research/behavior-containment-evidence",
    "research/field-naming-evidence"
  ]
}
---

# Adopt Setting Readby

## Context

The spec defines no reader link for a setting, so whether a setting is read at all is unknowable in the model.

## Decision

`setting.readBy`, naming the use cases that read the setting, is adopted into LiteSpec as a connection. The invented `notification.sentBy` and `rule.writes` fields are dropped instead of adopted.

## Consequences

`litespec/core/use-cases#name-the-settings-each-use-case-reads` states the obligation as reviewed guidance, since no tool proves completeness. The adopted name stays `readBy` even though it reads passively, because renaming it would break the consumer already using it.

## Evidence with Confidence

`research/behavior-containment-evidence` and `research/field-naming-evidence` are measured over the largest consumer on 2026-10-30, high confidence on the invented-field counts and on the reverse sender proof.
