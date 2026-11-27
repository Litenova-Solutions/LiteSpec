# Localization

**Stack:** any

**Concern:** frontend-ui

Some products serve more than one locale across screens, messages, routes, and formatted values. This page is for teams whose shipped product does that. A half-translated page confuses readers and splits search results, so one decision records the locale contract before catalogs and routes are built.

## Concepts

### The Locale Decision Comes First

The locale decision lists the supported locales, the default locale, the fallback chain, user selection, and browser detection. Catalogs, routes, and formatters read those five choices together. Catalog files alone prove nothing: they show that someone wrote translations, with no word on routes, fallback, selection, or detection.

Each notification records its language source in `locale`. The decision stays the one contract the three parts follow.

### Routes and Addresses

Each localized application documents one route shape. A path segment gives every locale its own address, such as `/nl/orders/42`, which links, crawlers, and caches address directly. A cookie or header shape serves every locale from one address, so a shared cache and a search index see a single locale. A product with no public pages documents one of those shapes for the locale of the signed-in account.

Readers mistype addresses and follow stale links, so the product policy states what happens to an unsupported or missing locale segment. Each localized item serves from exactly one indexable address, keeping search, sharing, and caching on the same page. The request carries the locale in the URL or a documented session preference, where each developer sees which locale a page serves.

### Copy and Catalogs

User-facing copy lives in locale catalogs under one application-owned root, so translators edit wording while developers edit behavior. Catalog keys use semantic names that cite glossary terms, such as `orders.cancel.confirmation`, keeping their meaning when any language is reworded. A catalog split by module waits until measured size calls for it.

Each supported locale holds its required keys or resolves them through its declared fallback. A key missing from the default locale fails the build, while a key missing from another locale warns and falls back. Validation messages state the field label, the fault, and the accepted input, with each language sentence pattern recorded in the field glossary term.

Screens render glossary labels for closed-set codes, leaving user-facing reference numbers untouched. Public metadata, form labels, validation messages, and user-safe errors follow the active locale with the surrounding page. Locale identifiers use BCP 47 from the address bar to the catalog lookup.

### Numbers, Dates, and Neutral Data

Dates, times, numbers, currency, plurals, lists, and relative time follow the active locale through the platform formatters. [The `Intl` namespace](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl) ships `DateTimeFormat`, `NumberFormat`, `PluralRules`, `ListFormat`, and `RelativeTimeFormat` with the locale data the browser carries. One formatter instance per locale is created and reused, since construction is the expensive part.

## Standards

### Preserve Locale-Neutral Business Data

**ID:** `litespec/core/locale#preserve-locale-neutral-business-data`

**Strength:** required

**Applies to:** artifact: value-object, endpoint

**Statement:** Domain values and API contracts MUST remain locale-neutral unless a use case explicitly exchanges localized content.

**Rationale:** An expense state stored in Dutch breaks for an English reader, and a contract that shifts per language forces each consumer to handle wording before reading data. The interface translates for display, while the stored value and the wire contract stay in one form.

**Example:** Claimwick stores the expense state as one code and renders the Dutch or English label from the catalog. A product description a merchant writes per language names its exchange explicitly.

**Reviewed for:** Is every domain value and API contract locale-neutral outside an explicitly localized exchange? Check the domain values and contracts.

### Keep Stable Error Codes Locale-Neutral

**ID:** `litespec/core/locale#keep-stable-error-codes-locale-neutral`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A stable API error code MUST NOT change by locale.

**Rationale:** Support tools and integrations match on the code. A code that varies per language forces each consumer to list every translation before handling one failure.

**Example:** Claimwick returns the same code for a missing receipt in every locale, with the human-readable message translated beside it.

**Reviewed for:** Is every stable API error code identical across locales? Check the error code values in each locale's responses.

## Conventions

None.
