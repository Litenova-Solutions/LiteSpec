# Mail Relay

The mail relay delivers the spending module's emails to managers. The approval reminder travels through it to the manager who decides the waiting expense. A reminder that never arrives leaves the expense waiting unseen.

## Use

The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` sends `notification/spending.approval-reminder` through the relay. Each waiting expense earns one reminder, keyed by its expense identity. For each reminder it delivers, the relay sees the manager's address, the expense and its waiting time, and it holds no Claimwick data after the send. A failed send retries with a growing delay. A failure past the last retry dead-letters the reminder after one day. The expense still waits for its decision, which the manager sees in the app.
