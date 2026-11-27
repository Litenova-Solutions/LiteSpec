# Health

Health is the module that holds the recorded check-ins of the __PROJECT__ sample. Ava opens the web list to record a check-in and to look over the check-ins recorded for her, so the module keeps the record, the single read, and the list behind the same rows.

## Covers

- Health records check-ins with an optional note through the web list.
- Health reads one check-in and lists recorded check-ins newest first.

## Boundaries

- Health sends no reminder and keeps no second aggregate; the sample ends at record and read.

Its record lists no dependencies, so Health holds only the one aggregate and the three use cases behind the web screens.
