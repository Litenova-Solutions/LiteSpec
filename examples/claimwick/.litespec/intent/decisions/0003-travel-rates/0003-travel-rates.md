# Travel Rates

Claimwick reimburses work travel with a daily allowance and a mileage rate. Each country pays its own rates, so the submission reads the row in force for the travel date and country. This decision sets per-country rates, and it records the two other options that were weighed.

## Context

Employees travel to clients and file the cost. Each country pays its own daily allowance and mileage rate, and the rates change on their own schedule.

One worldwide rate would fit no country well, and actuals only would ask for a receipt on every meal. Per-country rates keep each country's figures apart.

## Options

Three options were weighed on the same points: a single worldwide rate, per-country rates, and actuals only.

- Single worldwide rate: one row covers every trip, but the rate fits no country well.
- Per-country rates: each country pays its own rate, but each country needs its own rows.
- Actuals only: nobody sets a rate, but every meal needs its own receipt.

The worldwide rate fails on fit, and actuals fail on filing cost. Per-country rates keep each country's figures apart, and accept the rows each country needs as their drawback.

## Decision

Work travel is reimbursed with a per-diem allowance and a mileage rate per country.

The travel tables in `knowledge-area/travel` hold the rates: `reference-data/travel.per-diem-rates` and `reference-data/travel.mileage-rates`. Finance chose per-country rates over a single worldwide rate and actuals only.

## Consequences

Travel rates live in per-country tables that vary by country: new rates arrive as new rows with the old rows end-dated, so a past trip still shows the rate it used.

Each new country needs its own rows in both tables before its trips can be filed.

## Revisit Conditions

The choice reopens when finance sees rates that no longer fit a country. Either sign would show that the figures need new rows.
