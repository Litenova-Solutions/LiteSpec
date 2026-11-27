# Receipt Attached

The product raises Receipt Attached when an employee adds proof of payment to a submitted expense. The event carries the receipt and the expense it supports. Nothing answers the event: the expense keeps its Submitted state, so a manager can still decide it.

## Example

Ava attaches the receipt for her train fare with `use-case/spending.attach-receipt`. The receipt moves to Attached, and the event carries the receipt and the expense the receipt proves.
