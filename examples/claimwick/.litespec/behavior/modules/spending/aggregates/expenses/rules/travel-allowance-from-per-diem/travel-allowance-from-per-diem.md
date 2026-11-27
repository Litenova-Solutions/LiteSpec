# Travel Allowance from Per-Diem

This rule reads a trip's daily allowance from the per-diem table. It keeps each country's figures apart, so a trip is paid at the rate its country sets for the travel date.

## Rationale

`rule/expense.travel-allowance-from-per-diem` governs `use-case/spending.submit-expense` on `aggregate/spending.expense`, so the submission reads the row in force instead of a fixed figure. The decision `decision/0003-travel-rates` chose per-country rates, and the table `reference-data/travel.per-diem-rates` holds the dated rows. The submission reads by role and date: the role states which daily figure the trip needs, and the travel date selects the row in force.

## Example

Ava travels to a client in Germany and stays one night. The submission reads the German full-day row in force on the travel date from `reference-data/travel.per-diem-rates`, and the expense keeps that figure.

## Limits

This rule reads the allowance. The mileage for the same trip comes from `reference-data/travel.mileage-rates`, and the receipt check stays with `rule/expense.receipt-required-above-threshold`.
