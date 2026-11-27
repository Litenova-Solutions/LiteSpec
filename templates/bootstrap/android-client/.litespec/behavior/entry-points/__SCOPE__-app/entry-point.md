# __PROJECT__ App

The __PROJECT__ app is the Android app where the visitor looks over recorded check-ins. Every Health screen in these records is reached through it, so it is the only entry point of the starter. The host activity owns the one navigation controller, and each screen raises events to it instead of navigating on its own.

## Audience

Visitors who had a check-in recorded reach Health through this app. The record names them as `actor/health-visitor`. They open the list, read one check-in behind it, and step back.

## Authentication

The app carries no sign-in of its own. The starter reads the sample rows of the backend starter, and pairing with a signed-in backend stays a documented step in the starter readme.
