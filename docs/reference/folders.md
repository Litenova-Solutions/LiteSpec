# Record Folders

Use this generated page to find the folder and file each record type lives in, relative to `.litespec/`. A named record sits in a folder that carries its name, beside its explanation, so `<name>/<name>.yaml` and `<name>/<name>.md` are the pair. A record in any other folder is a `record-placement-invalid` finding, a folder whose name differs from its file stem is a `record-placement-invalid` finding. Folders carry ownership and layer only, meaning product or module. Categories live in fields and generated indexes, because a record often fits several categories and a folder per category forces one and churns history on every recategorization. Decision numbers stay identity and proposal order only.

## Reference

| Type | Folder | File |
|:---|:---|:---|
| `product` | `` | `product.yaml` |
| `goal` | `intent/goals/{name}` | `{name}.yaml` |
| `direction` | `intent/directions/{name}` | `{name}.yaml` |
| `problem` | `intent/problems/{name}` | `{name}.yaml` |
| `feature` | `intent/features/{name}` | `{name}.yaml` |
| `requirement` | `intent/features/{feature}/requirements/{name}` | `{name}.yaml` |
| `requirement` | `intent/requirements/{name}` | `{name}.yaml` |
| `constraint` | `intent/constraints/{name}` | `{name}.yaml` |
| `decision` | `intent/decisions/{name}` | `{name}.yaml` |
| `decision` | `implementation/decisions/{name}` | `{name}.yaml` |
| `decision` | `implementation/modules/{module}/decisions/{name}` | `{name}.yaml` |
| `research` | `intent/research/{name}` | `{name}.yaml` |
| `flow` | `intent/flows/{name}` | `{name}.yaml` |
| `actor` | `intent/actors/{name}` | `{name}.yaml` |
| `role` | `intent/roles/{name}` | `{name}.yaml` |
| `module` | `behavior/modules/{name}` | `module.yaml` |
| `term` | `` | `glossary.yaml` |
| `aggregate` | `behavior/modules/{module}/aggregates/{aggregate}` | `aggregate.yaml` |
| `use-case` | `behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}` | `{name}.yaml` |
| `use-case` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` |
| `result` | `behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}` | `{name}.yaml` |
| `result` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` |
| `scenario` | `behavior/modules/{module}/aggregates/{aggregate}/use-cases/{name}` | `{name}.yaml` |
| `scenario` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` |
| `scenario` | `behavior/modules/{module}/reactions/{name}` | `{name}.yaml` |
| `scenario` | `behavior/modules/{module}/jobs/{name}` | `{name}.yaml` |
| `domain-event` | `behavior/modules/{module}/aggregates/{aggregate}` | `aggregate.yaml` |
| `domain-event` | `behavior/modules/{module}/aggregates/{aggregate}/domain-events` | `*` |
| `rule` | `behavior/rules` | `*` |
| `rule` | `behavior/modules/{module}/rules` | `*` |
| `rule` | `behavior/modules/{module}/aggregates/{aggregate}/rules` | `*` |
| `reaction` | `behavior/modules/{module}/reactions/{name}` | `{name}.yaml` |
| `job` | `behavior/modules/{module}/jobs/{name}` | `{name}.yaml` |
| `notification` | `behavior/modules/{module}/notifications/{name}` | `{name}.yaml` |
| `setting` | `behavior/modules/{module}/settings` | `*` |
| `module-event` | `behavior/modules/{module}/module-events/{name}` | `{name}.yaml` |
| `integration-event` | `behavior/modules/{module}/integration-events/{name}` | `{name}.yaml` |
| `read-model` | `behavior/modules/{module}/read-models/{name}` | `{name}.yaml` |
| `permission` | `behavior/modules/{module}` | `permissions.yaml` |
| `refusal-set` | `behavior/modules/{module}` | `refusals.yaml` |
| `entry-point` | `behavior/entry-points/{entry-point}` | `entry-point.yaml` |
| `screen` | `behavior/entry-points/{entry-point}/screens/{home}.{name}` | `{home}.{name}.yaml` |
| `external-system` | `behavior/external-systems/{name}` | `{name}.yaml` |
| `external-system` | `behavior/modules/{module}/external-systems/{name}` | `{name}.yaml` |
| `runbook` | `implementation/runbooks/{name}` | `{name}.yaml` |
| `runbook` | `implementation/modules/{module}/runbooks/{name}` | `{name}.yaml` |
| `waiver` | `governance/waivers/{name}` | `{name}.yaml` |
| `waiver` | `governance/modules/{module}/waivers/{name}` | `{name}.yaml` |

## Notes

The `litespec-maintain provisions` command writes this page from the record folder table in the model, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.
