# Mileage Rates

Claimwick reimburses work travel per kilometer by vehicle kind. The table holds that rate per country and vehicle kind with the date it takes effect, so the submission reads one value for the travel date, country, and vehicle instead of stating its own.

## Values

The table holds six rows playing the `car`, `motorcycle`, and `bicycle` roles, each resting on `decision/0003-travel-rates`:

- Germany: 0.30 EUR per km for a car, 0.20 EUR per km for a motorcycle, and 0.10 EUR per km for a bicycle, each from 2026-01-01.
- Netherlands: 0.25 EUR per km for a car, 0.15 EUR per km for a motorcycle, and 0.10 EUR per km for a bicycle, each from 2026-01-01.

The figures are example rates. A new row takes effect on its date, and the old row carries its end date.

## Example

Ava drives 40 km by car to a client in Germany: the submission reads the 0.30 EUR `car` row in force on that date.
