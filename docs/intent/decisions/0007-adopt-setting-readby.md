# Adopt Setting Readby

## Context

The spec defines no reader link for a setting, so whether a setting is read at all is unknowable in the model.

## Decision

`setting.readBy`, naming the use cases that read the setting, is adopted into LiteSpec as a connection. The invented `notification.sentBy` and `rule.writes` fields are dropped instead of adopted.

## Evidence with Confidence

`research/behavior-containment-evidence` and `research/field-naming-evidence` are measured over the largest consumer on 2026-10-30, high confidence on the invented-field counts and on the reverse sender proof.
