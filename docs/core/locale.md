# Localization

**Stack:** any

**Concern:** frontend-ui

Localization serves the product in more than one locale: every screen, message, route, and formatted value appears in the active locale. A Decision records the locale contract before the team builds catalogs and routes. Catalogs hold the copy, formatters follow the active locale, and business data stays locale-neutral. These rules apply when the shipped product serves more than one locale.

## Concepts

### When These Rules Apply

These rules apply when the product ships in more than one locale. A planned translation does not trigger them; only the shipped product counts. Before the team builds catalogs and routes, the product brief or a Decision records the supported locales, the default locale, the fallback behavior, and the URL strategy.

### How This Page Builds on the Baseline

This page replaces no baseline provision.

This page adds no record type: the Decision that records the locale contract is a core record every project writes. Each notification records its language source in `locale`.

## Standards

### Record Supported Locale Behavior

**ID:** `litespec/core/locale#record-supported-locale-behavior`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A localization decision MUST list supported locales, default locale, fallback chain, user selection, and browser detection.

**Rationale:** Catalogs, routes, and formatters read the same five choices, and the choices interact. Writing them once in the decision, before building any of the three, gives all three one contract to follow. Without it, each part guesses its own fallback, and the product handles each missing translation differently.

**Reviewed for:** Does the locale decision list the supported locales, the default locale, the fallback chain, user selection, and browser detection? Check the locale decision record.

### Avoid Catalog-Only Locale Claims

**ID:** `litespec/core/locale#avoid-catalog-only-locale-claims`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A product MUST NOT infer supported locales from catalog files alone.

**Rationale:** A catalog file shows only that someone wrote translations. It says nothing about routes, fallback, selection, or detection, so a product that counts locales by counting files claims a locale its routes cannot serve and its fallback never reaches.

**Reviewed for:** Does every shipped locale come from the locale decision rather than from catalog files alone? Check the shipped locales beside the locale decision.

### Define One Locale Route Shape

**ID:** `litespec/core/locale#define-one-locale-route-shape`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST define one documented locale route shape.

**Rationale:** Users, links, crawlers, and caches address a localized page, so one route shape gives each locale one predictable address. A path segment is the default shape: every localized page gets a distinct address that links, crawlers, and cache entries can each name. A cookie or header shape serves every locale from one address instead, so a shared cache and a search index each see only one locale. A product with no public pages, whose locale belongs to the signed-in account, documents one of those shapes instead of inventing a fourth.

**Example:** `/nl/orders/42` places the locale in the documented path segment, so a crawler and a shared cache each address one locale.

**Reviewed for:** Does every locale reach the application through the documented route shape? Check the route definitions.

### Handle Unavailable Locale Segments

**ID:** `litespec/core/locale#handle-unavailable-locale-segments`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST redirect unsupported or missing locale segments according to product policy.

**Rationale:** Readers mistype addresses and follow stale links, so a locale segment can name a locale the product never shipped, or go missing entirely. Without a documented policy, each route responds differently: one shows the default locale, another shows an error, and a crawler indexes both as content.

**Reviewed for:** Does every unsupported or missing locale segment redirect under the documented product policy? Check the redirect tests.

### Avoid Duplicate Localized URLs

**ID:** `litespec/core/locale#avoid-duplicate-localized-urls`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST NOT publish multiple indexable URLs for the same localized content.

**Rationale:** When two addresses serve the same locale, search engines split the page's rank between them, and canonical links point in two directions. One indexable address per locale keeps search, sharing, and caching on the same page.

**Reviewed for:** Does every localized content item serve from exactly one indexable URL? Check the served URLs of each content item.

### Store User-Facing Copy in Catalogs

**ID:** `litespec/core/locale#store-user-facing-copy-in-catalogs`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A localized application MUST store user-facing copy in locale catalogs.

**Rationale:** Copy in source code reaches translators only through developers, and each wording change becomes a code change with a review, a build, and a deployment. Holding every user-facing sentence in one catalog per locale lets translators edit wording while developers edit behavior.

**Reviewed for:** Does every user-facing sentence live in the locale catalogs instead of source code? Check the source files for literal copy.

### Name Messages by Meaning

**ID:** `litespec/core/locale#name-messages-by-meaning`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A locale catalog MUST use semantic keys that cite glossary terms rather than source-language sentences.

**Rationale:** A key that copies the English sentence loses its meaning when English is reworded: every other language still refers to the old sentence. A semantic key names the intent instead, citing the glossary term that holds the word's labels per language, so wording changes in any language without disturbing the key.

**Example:** `orders.cancel.confirmation` identifies intent without copying the English sentence.

**Reviewed for:** Does every catalog key use a semantic name citing a glossary term instead of a source-language sentence? Check the catalog keys.

### Complete or Fall Back Catalog Values

**ID:** `litespec/core/locale#complete-or-fall-back-catalog-values`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each supported locale MUST contain required keys or use its declared fallback.

**Rationale:** A missing translation with no declared fallback reaches the interface with no chosen outcome: the interface shows a key, an empty string, or the wrong language. The declaration states the fallback locale and what a missing key does at build time.

A key missing from the default locale fails the build. No fallback exists for it, and the interface renders its identifier. A key missing from a non-default locale reports a warning and falls back. Shipping one untranslated string costs less than blocking a release on it.

**Example:** A catalog check runs in the frontend lint gate, so a missing key fails or warns before review, not in a browser.

**Reviewed for:** Does every supported locale contain its required keys or resolve them through its declared fallback? Check each locale catalog beside the fallback chain.

### Follow the Validation Message Grammar

**ID:** `litespec/core/locale#follow-the-validation-message-grammar`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A validation catalog MUST state the field label, what is wrong, and what is accepted, without technical wording.

**Rationale:** A validation message that names the field, the fault, and the accepted input tells the user how to fix the entry in any language, while a message with only one of the three does not. The three-part shape holds across languages; each language's sentence pattern is recorded in the labels of the field's glossary term and the language's writing page.

**Example:** The glossary term for a Claimwick expense field records the sentence pattern for each supported language.

**Reviewed for:** Does every validation message state the field label, the fault, and the accepted input without technical wording? Check the validation messages in each locale.

### Resolve Every Shown Identifier

**ID:** `litespec/core/locale#resolve-every-shown-identifier`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST resolve every closed-set code it renders to its glossary label and never render the raw code, excluding user-facing reference numbers.

**Rationale:** A code such as a refusal ground or a billing state is a contract value built for systems, while the glossary label beside it is copy built for users. A screen that renders the raw code shows one untranslated fragment in an otherwise translated page and forces the user to decode it. Resolving every shown code keeps the whole screen in the user's language, except reference numbers the user quotes back.

**Example:** A Claimwick shipment row shows the glossary label for `shipment-delayed` in the active locale.

**Reviewed for:** Does every screen render glossary labels instead of raw codes, outside user-facing reference numbers? Check the rendered screens.

### Format Values with Active Locale

**ID:** `litespec/core/locale#format-values-with-active-locale`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A localized interface MUST format dates, times, numbers, currency, plurals, lists, and relative time by active locale.

**Rationale:** Dates, times, numbers, currency, plurals, lists, and relative time each follow the user's conventions, and a date the user misreads becomes a missed appointment or a wrong payment. The platform supplies all of them.

[The `Intl` namespace](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl) has `DateTimeFormat`, `NumberFormat`, `PluralRules`, `ListFormat`, and `RelativeTimeFormat`. Each one uses the locale data the browser already ships. A project-written formatter for any of these is a second locale database that no one updates. Its plural rules are wrong for the first language with more than two forms.

**Example:** Create one formatter instance per locale and reuse it. Construction is the expensive part.

**Reviewed for:** Does every date, number, currency, plural, list, and relative time render through the active-locale formatters? Check the formatting calls.

### Preserve Locale-Neutral Business Data

**ID:** `litespec/core/locale#preserve-locale-neutral-business-data`

**Strength:** required

**Applies to:** artifact: value-object, endpoint

**Statement:** Domain values and API contracts MUST remain locale-neutral unless a use case explicitly exchanges localized content.

**Rationale:** An expense state or a price stored in Dutch breaks when an English speaker opens it, and an API contract that shifts per language forces every consumer to handle wording before reading data. Business values and wire contracts stay in one neutral form, and only the interface translates them for display. A use case that genuinely exchanges localized content, such as a product description the merchant writes per language, names that exchange explicitly.

**Reviewed for:** Is every domain value and API contract locale-neutral outside an explicitly localized exchange? Check the domain values and contracts.

### Localize Public Presentation Content

**ID:** `litespec/core/locale#localize-public-presentation-content`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** Public metadata, form labels, validation messages, and user-safe errors MUST follow the active locale.

**Rationale:** A Dutch page with an English title, English form labels, or an English error tells the user the translation stopped halfway and tells a search engine the page serves two languages. Metadata, labels, validation messages, and user-safe errors follow the active locale together with the surrounding page.

**Reviewed for:** Do public metadata, form labels, validation messages, and user-safe errors all follow the active locale? Check each rendered page.

### Keep Stable Error Codes Locale-Neutral

**ID:** `litespec/core/locale#keep-stable-error-codes-locale-neutral`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A stable API error code MUST NOT change by locale.

**Rationale:** Integrations and support tools match on the error code. A code that changes per language forces every consumer to list every translation before handling one failure. The code stays fixed while the human-readable message beside it translates.

**Reviewed for:** Is every stable API error code identical across locales? Check the error code values in each locale's responses.

## Conventions

### Use One Catalog Root

**ID:** `litespec/core/locale#use-one-catalog-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep catalogs under one application-owned locale root.

**Replacement:** A project names another root for locale catalogs in its documentation.

**Rationale:** One application-owned root tells every developer and tool where catalogs live, so a new locale sits beside the old ones instead of in a folder nobody checks. Scattered catalogs split the set, and one of them misses the fallback check.

**Reviewed for:** Does every catalog sit under the application-owned root, or under another root the documentation names? Check the catalog folders.

### Split Catalogs by Module When Needed

**ID:** `litespec/core/locale#split-catalogs-by-module-when-needed`

**Strength:** default

**Applies to:** artifact: any

**Default:** Split catalogs by module only when catalog size requires it.

**Replacement:** A project splits catalogs by feature from the first locale.

**Rationale:** Small catalogs read better whole: one file per locale shows every key at a glance and survives a rename without cross-file edits. Splitting waits until measured size, such as slow loads or conflicting edits, proves one file no longer serves.

**Reviewed for:** Is every catalog whole unless measured size calls for a split, or the project chose a feature split from the start? Check the catalog files beside the size measurements.

### Use BCP 47 Identifiers

**ID:** `litespec/core/locale#use-bcp-47-identifiers`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use BCP 47 locale identifiers.

**Replacement:** A project names another identifier form for locales in its documentation.

**Rationale:** BCP 47 is the identifier form routes, catalogs, browsers, and libraries recognize: one tag such as `nl` passes unchanged from the address bar to the catalog lookup. A home-grown form needs conversion at every boundary and confuses every new developer.

**Reviewed for:** Does every locale identifier use BCP 47, or another identifier form the documentation names? Check the locale identifiers in the catalogs and routes.

### Keep Locale Selection Explicit

**ID:** `litespec/core/locale#keep-locale-selection-explicit`

**Strength:** default

**Applies to:** artifact: route

**Default:** Keep locale selection in the URL or documented session preference.

**Replacement:** A project keeps the active locale in ambient request state.

**Rationale:** A locale stored in the URL or a documented session preference travels with the request, so any developer can see which locale a page serves and link to it. An implicit global locale hides in process state, where one request can read another's locale and no link reproduces the page.

**Reviewed for:** Does every request carry the locale in the URL or a documented session preference instead of ambient request state? Check the request handling for the locale.
