# Loose Approval

## Context

LiteSpec serves one-person teams, but the approval pages demand reviewers and gates solo projects cannot staff, while the single-record path skips every guard and re-approval keeps the old name.

## Decision

Approval stays loose: an agent's reading counts as the tier 1 second reading; a project whose owners list holds one name needs no second reviewer; `review.securityReviewer` is optional and the security queue is a list to read, not a gate; the pages say so. Two defects are fixed: single-record approval promotes the record's status, and re-approval writes the new approver and date.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D25, high confidence.
