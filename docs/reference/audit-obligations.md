# Audit Obligations

The audit extension is the LiteSpec bundle a project selects to record who tried what against which target, and what each try produced. It answers obligations from four guidance bodies (NIST, ISO, PCI DSS, and OWASP) plus the General Data Protection Regulation (GDPR). This page gathers those obligations in one place: the frameworks, the GDPR articles, and the map from each obligation to the provision covering it. A team writing its audit adoption decision starts here, and cites this page instead of reading each framework from scratch.

The [Audit Trail](../core/audit.md) page owns every provision behind these obligations. This page adds no rules; it lists the outside sources with the map.

## Reference

### Audit Frameworks

Four guidance bodies cover auditing for a business application holding personal data or touching payments. Their requirements overlap, so one trail covering all four satisfies each one.

#### NIST SP 800-53, AU Family

The AU family splits auditing into six questions. The table below lists each control with its obligation.

| Control | Obligation |
|:---|:---|
| AU-2 | Select the audited event types and record the rationale for each. |
| AU-3 | State event type, time, location, source, outcome, and associated identities. |
| AU-6 | Review and analyze the records. |
| AU-9 | Protect the records from unauthorized change and deletion. |
| AU-11 | Retain records for a defined period. |
| AU-12 | Generate records across every component in scope. |

AU-3 carries the field list the other frameworks repeat. AU-2 is the control teams miss most often: a selection written nowhere leaves nothing to check.

#### ISO/IEC 27001:2022, Annex A 8.15

Control A.8.15 covers logs of user activities, exceptions, faults, and security events, with protection against tampering and unauthorized access. It also covers analysis, so a trail nobody reads counts as a gap instead of a control.

#### PCI DSS 4.0, Requirement 10

Requirement 10.2.2 lists the fields each event carries: user identification, event type, date and time, a success or failure indication, origination, and the affected resource identity. The table below quotes that list exactly as the check reads it.

Requirement 10.2.1 lists which events go on the record: individual access to cardholder data, actions by administrative accounts, access to the audit records, invalid logical access attempts, and changes to authentication credentials. Those five rows drive the adoption decision.

A trail holding successes only misses exactly two obligations: the success-or-failure field and the invalid-attempt event. Both rows stay visible in the map below.

#### OWASP Logging Cheat Sheet

The OWASP guidance adds the developer-facing half: authentication successes and failures, authorization failures, higher-risk actions, sensitive data access, and data import or export. Those rows mirror the PCI event list from the application side.

It also lists what stays out of the trail: passwords, session identifiers, access tokens, encryption keys, database connection strings, payment card data, and sensitive personal data. The map below points each exclusion at its provision.

### GDPR

The General Data Protection Regulation both calls for the trail and limits what it holds. Reading it as only a limit yields a trail too thin to show accountability, and reading it as only a mandate yields an erasure problem.

| Article | Effect on the trail |
|:---|:---|
| Article 5(2) | Accountability requires demonstrating lawful processing, which the trail shows. |
| Article 30 | Records of processing activities need evidence of who accessed personal data. |
| Article 32 | Security of processing covers the integrity and availability of the trail. |
| Article 17 | Erasure applies to the personal data a record holds. |
| Article 15 | Subject access can reach audit records naming that person. |

The record shape resolves that tension. A trail kept on a legal-obligation or legitimate-interest basis stays lawful, and a supervisory authority accepts it. But a trail that copies names, addresses, message bodies, or state snapshots into an append-only store conflicts with Article 17, and no retention policy repairs that conflict.

Two provisions draw that boundary: `litespec/core/audit#keep-secrets-and-payment-data-from-the-trail` keeps secrets and payment values off the trail, and `litespec/core/audit#delete-expired-records-through-a-separate-operator-account` runs retention deletion and tenant data erasure through the audited operator process. The [Audit Trail](../core/audit.md) page lists the full field set with the personal-data limits beside it.

Two practical points follow for a project. Record the lawful basis and the retention period of each audited category in the adoption decision, and list the trail itself in the records of processing activities, since keeping the trail counts as processing activity.

### Shared Record Shape

The record shape follows a model three sources describe in matching terms: an initiator performs an action on a target and produces an outcome, at a time and from a place.

| Source | Contribution |
|:---|:---|
| DMTF CADF, DSP0262 | Initiator, action, target, outcome, and observer event model. |
| NIST AU-3 | The required content of one record. |
| OCSF | Vendor-neutral schema for shipping records to a security platform. |

Following that model at design time adds no extra work, and it lets a project map the trail onto a security platform later with no store remodeling.

### Obligation to Provision Map

| Obligation | Source | Provision |
|:---|:---|:---|
| Select audited events and record the rationale | AU-2 | `litespec/core/audit#record-the-audit-adoption-decision`, `litespec/core/audit#declare-which-commands-are-audited` |
| Cover the required event categories | AU-2, PCI 10.2.1, ISO 8.15 | `litespec/core/audit#record-the-audit-adoption-decision` |
| Generate records across every component | AU-12 | `litespec/core/audit#commit-a-success-record-with-its-business-change`, `litespec/core/audit#write-an-unsuccessful-record-outside-the-failed-transaction` |
| State the required record content | AU-3, PCI 10.2.2 | `litespec/core/audit#record-every-attempt-outcome`, `litespec/core/audit#scope-every-record-to-its-tenant` |
| Identify the affected resource | AU-3, PCI 10.2.2 | `litespec/core/audit#declare-which-commands-are-audited` |
| Indicate success or failure | AU-3, PCI 10.2.2 | `litespec/core/audit#record-every-attempt-outcome` |
| Record invalid access attempts | PCI 10.2.1.4, OWASP | `litespec/core/audit#record-refused-authorization` |
| Record the origination of the event | AU-3, PCI 10.2.2, ISO 8.15 | [Audit Trail](../core/audit.md) |
| Record credential and privilege change | PCI 10.2.1.5, ISO 8.15 | `litespec/core/audit#record-the-audit-adoption-decision` |
| Record access to the audit records | PCI 10.2.1.3 | `litespec/core/audit#declare-which-commands-are-audited` |
| Record sensitive data access and export | OWASP, GDPR Art. 30 | `litespec/core/audit#record-the-audit-adoption-decision` |
| Exclude secrets and payment data | OWASP, PCI | `litespec/core/audit#keep-secrets-and-payment-data-from-the-trail` |
| Exclude sensitive personal data | OWASP, GDPR Art. 17 | [Audit Trail](../core/audit.md) |
| Protect records from change and deletion | AU-9, ISO 8.15, GDPR Art. 32 | `litespec/core/audit#enforce-append-only-through-storage-privilege`, `litespec/core/audit#correct-mistakes-with-a-new-record` |
| Retain for a defined period | AU-11 | `litespec/core/audit#delete-expired-records-through-a-separate-operator-account` |
| Satisfy erasure without destroying evidence | GDPR Art. 17 | `litespec/core/audit#delete-expired-records-through-a-separate-operator-account` |
| Review and analyze the records | AU-6, ISO 8.15 | [Audit Trail](../core/audit.md) |

## Notes

The tables on this page draw on these sources. Each link below points at the edition the map was checked against.

- [NIST SP 800-53 Rev. 5](https://csrc.nist.gov/pubs/sp/800/53/r5/upd1/final)
- [ISO/IEC 27001:2022](https://www.iso.org/standard/27001)
- [PCI DSS](https://www.pcisecuritystandards.org/litespec/pci-dss/)
- [OWASP Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)
- [DMTF Cloud Auditing Data Federation](https://www.dmtf.org/litespec/cadf)
- [Open Cybersecurity Schema Framework](https://github.com/ocsf/ocsf-schema)
- [Erasure rights against audit retention](https://axiom.co/blog/the-right-to-be-forgotten-vs-audit-trail-mandates)
