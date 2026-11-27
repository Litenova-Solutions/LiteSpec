# __PROJECT__ App

The __PROJECT__ App is the web app where the visitor looks over recorded check-ins. Every Health screen in these records is reached through it, so it is the only entry point of the starter. The list owns the record action, and each screen links back instead of navigating on its own.

## Audience

Visitors who recorded a check-in reach Health through this app. The record names them as `actor/health-visitor`. They open the home, list the check-ins, read one check-in behind it, and step back.

## Authentication

The app signs the cast person in through the development route, which sets the session cookie the Health endpoints require. Production carries no sign-in of its own beyond that session.
