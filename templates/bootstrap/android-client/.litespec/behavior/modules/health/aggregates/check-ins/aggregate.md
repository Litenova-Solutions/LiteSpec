# Check-in

A check-in is one recorded health entry the app holds on the phone. The backend sample records it, the app reads it over HTTP, and the file store keeps it in app-private storage, writing each update atomically.

## Lifecycle

A check-in becomes Recorded when the backend sample records it, which is the only state the app holds. The app records no check-in of its own, so no transition leaves Recorded in these records.

## Data and Retention

The check-in holds no fields of its own in these records. The file store keeps the identifier the backend sample assigned with the recorded state beside it, and replaces the prior copy only after the new one turns whole.
