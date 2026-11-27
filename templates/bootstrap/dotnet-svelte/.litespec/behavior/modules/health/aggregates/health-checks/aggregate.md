# Health Check

A health check is one recorded check-in moment that the Health module holds. The web list shows each check-in with the moment it was recorded, and the record screen shows the note beside it.

## Lifecycle

A check-in starts Recorded when the visitor records it with `use-case/health.record-check-in`. Recorded has no transition out in the records, so no use case moves it again; the sample keeps the one state.

## Data and Retention

The check-in holds three fields: the recorded moment, the note, and the state. All three are business facts of the sample, so every field is internal data and no field carries personal data of its own.

| Field | Sensitivity | Kept for |
|:---|:---|:---|
| `recordedAt` | internal | the stored row |
| `note` | internal | the stored row |
| `state` | internal | the stored row |

## Example

Ava opens the check-ins list in the morning. She records a check-in with the note `Morning check-in`. The check-in is stored as Recorded, and the list shows it newest first.
