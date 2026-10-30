---
{
  "type": "reference",
  "id": "release/<YYYY-MM-DD>",
  "title": "<Release display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>"
}
---
# <Release Display Title>

## Intent

This record binds one immutable artifact to the behavior it ships and the evidence that proves it. Specifications own the behavior and scenarios, so the record links outward instead of restating them.

| Name | Value | Meaning |
|:---|:---|:---|
| `source` | `<source commit>` | The reviewed commit this artifact builds from. |
| `artifact` | `<artifact reference>` | The immutable reference of the deployed output. |

Included behavior:

- journey/<name> (<the outcome it proves, or remove this line>)
- use-case/<module>.<name> (<the behavior it ships, or remove this line>)
- <extension id, or remove this line when no extension applies>

## Reference

| Evidence | Result |
|:---|:---|
| Build | <the build result with its reference> |
| Test | <the test result with its reference> |
| Contract | <the contract result with its reference> |
| Security | <the security result with its reference> |
| Journey | <the journey result with its reference> |
| Schema | <the schema result with its reference> |
| Backup | <the backup result with its reference> |
| Deployment | <the deployment result with its reference> |
| Readiness | <the readiness result with its reference> |
| Diagnostics | <the diagnostics result with its reference> |
| Smoke Test | <the smoke-test result with its reference> |
| Rollback | <the rollback result with its reference> |

Operating readiness:

- Limits: <the operating limits, or none>
- Alerts: <the alerts watching this release, or none>
- Runbooks: <runbook/<name>, or none>
- Conditions: <the known operating conditions, or none>
- Skipped checks: <the skipped checks with their reasons, or none>

## Notes

Optional. Remove this section when the record needs no note.

Record anything a reader needs beside the evidence, such as where the deployed artifact runs.
