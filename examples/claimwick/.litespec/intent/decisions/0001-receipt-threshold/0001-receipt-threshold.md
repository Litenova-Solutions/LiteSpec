# Receipt Threshold

Claimwick asks for a receipt on expenses above a set amount. That amount is the receipt threshold, and it decides how much proof each expense needs before finance files it. This decision sets the threshold at 25 EUR, and it records the two other amounts that were weighed.

## Context

Small expenses need no proof, and large expenses do. Finance must keep the tax file defensible, and a receipt is the proof it uses.

Proof on every expense would cost more to file than tiny expenses pay out. A high threshold, on the other hand, lets some fraud pass without proof.

## Options

Three amounts were weighed on the same points: 0 EUR, 25 EUR and 75 EUR.

- 0 EUR: every expense carries proof, but tiny expenses cost more to file than they pay out.
- 25 EUR: small expenses stay light to submit, but some fraud slips under the line.
- 75 EUR: almost nothing needs a receipt, but finance cannot defend the tax file.

The 0 EUR option fails on cost, because filing tiny expenses costs more than they pay out. The 75 EUR option fails on the tax file, because finance cannot defend it. The 25 EUR option keeps small expenses light, and its drawback is the fraud it lets through.

## Decision

Expenses above 25 EUR need a receipt.

This amount is the threshold in `requirement/approval.receipt-over-threshold`. Finance chose 25 EUR over 0 EUR and 75 EUR.

## Consequences

Small expenses stay light to submit. Expenses at or below 25 EUR need no receipt, so the submitter does not have to find one for them.

Some fraud slips under the line. The choice accepts that cost to keep small submissions light.

## Evidence

Finance reviewed the filing cost against the fraud risk in February 2026, and it stands behind 25 EUR.

## Revisit Conditions

The choice reopens when finance sees abuse or friction. Either sign would show that 25 EUR no longer fits the filing cost and the fraud risk.
