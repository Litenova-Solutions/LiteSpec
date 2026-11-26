# Per Diem Rates

Claimwick pays a daily allowance for each day of work travel. The table holds that allowance per country and day kind with the date it takes effect, so the submission reads one value for the travel date, country, and day kind instead of stating its own.

## Values

The table holds five rows playing the `full-day` and `partial-day` roles, each resting on `decision/0003-travel-rates`:

- Germany: 28 EUR per day for a full day from 2025-01-01 to 2025-12-31, then 30 EUR per day for a full day from 2026-01-01, and 15 EUR per day for a partial day from 2026-01-01.
- Netherlands: 25 EUR per day for a full day from 2026-01-01, and 12 EUR per day for a partial day from 2026-01-01.

The figures are example rates. The first German full-day row is end-dated at 2025-12-31 because the change to 30 EUR on 2026-01-01 arrives as a new row; released rows stay unchanged.

## Example

Ava spends a full workday at a client in Germany in 2026: the submission reads the 30 EUR `full-day` row in force on that date. When she returns the same afternoon, the submission reads the 15 EUR `partial-day` row.
