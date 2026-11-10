# Localization

**Stack:** any

**Concern:** frontend-ui

## Intent

Localization makes every screen, message, route, and formatted value answer to the user's language. This page states what the team decides up front, the supported locales, the default locale, the fallback chain, and the route shape, and which behavior the code owns: catalogs for copy, locale-aware formatting, and business data that stays locale-neutral. A project selects it when it ships in more than one locale, so each language reads as a complete product instead of a partial translation.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `locale` when the product ships in more than one locale. A translation the team only plans does not select it; the shipped product decides. The product brief or a Decision records the supported locales, the default locale, the fallback behavior, and the URL strategy before catalogs and routes appear.

## Baseline Relationship

This extension replaces no baseline provision.

This extension switches on no record type. The Decision that records the locale contract is a core record every project writes. Each notification names where its language comes from in `locale`.

## Standards

### Record Supported Locale Behavior

**ID:** `litespec/ext/locale#record-supported-locale-behavior`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A localization decision MUST list supported locales, default locale, fallback chain, user selection, and browser detection.

**Rationale:** Locales, default, fallback, selection, and browser detection interact: each catalog, route, and formatter reads the same contract, so the team writes it once before building any of them. Without the contract, each part guesses a different fallback and the product answers each missing translation differently.

**Reviewed for:** Which locale decisions omit supported locales, the default locale, the fallback chain, user selection, or browser detection? Look at the locale decision record.

### Avoid Catalog-Only Locale Claims

**ID:** `litespec/ext/locale#avoid-catalog-only-locale-claims`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A product MUST NOT infer supported locales from catalog files alone.

**Rationale:** A catalog file proves only that someone wrote translations; it says nothing about routes, fallback, selection, or detection. A product that counts locales by counting files announces a language its routes cannot serve and its fallback never reaches.

**Reviewed for:** Which shipped locales come from outside the locale decision, or from catalog files that do more than translate? Look at the shipped locales beside the locale decision.

### Define One Locale Route Shape

**ID:** `litespec/ext/locale#define-one-locale-route-shape`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST define one documented locale route shape.

**Rationale:** Users, links, crawlers, and caches all address a localized page, and one route shape gives each language one predictable address. A path segment is the default shape: it gives every localized page a distinct address that links, crawlers, and cache entries can each name. A cookie or header shape serves every locale from one address instead, so a shared cache and a search index each see only one of them. A product with no public pages, whose locale belongs to the signed-in account, documents one of those shapes instead of inventing a fourth.

**Example:** `/nl/orders/42` places the locale in the documented path segment, so a crawler and a shared cache each address one language.

**Reviewed for:** Which locales reach the application through a route shape other than the documented one? Look at the route definitions.

### Handle Unavailable Locale Segments

**ID:** `litespec/ext/locale#handle-unavailable-locale-segments`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST redirect unsupported or missing locale segments according to product policy.

**Rationale:** Readers mistype addresses and follow stale links, so a locale segment can name a language the product never shipped or omit the segment entirely. Without a documented policy, each route answers differently: one shows the default language, another shows an error, and a crawler indexes both as content.

**Reviewed for:** Which unsupported or missing locale segments fail to redirect under the documented product policy? Look at the redirect tests.

### Avoid Duplicate Localized URLs

**ID:** `litespec/ext/locale#avoid-duplicate-localized-urls`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A localized application MUST NOT publish multiple indexable URLs for the same localized content.

**Rationale:** When two addresses serve the same language, search engines split the page's rank between them and canonical links point two ways. One indexable address per language keeps search, sharing, and caching pointed at the same page.

**Reviewed for:** Which localized content items serve from more than one indexable URL? Look at the served URLs of each content item.

### Store User-Facing Copy in Catalogs

**ID:** `litespec/ext/locale#store-user-facing-copy-in-catalogs`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A localized application MUST store user-facing copy in locale catalogs.

**Rationale:** Copy in source code reaches translators only through developers, and each wording change becomes a code change with a review, a build, and a deployment. Catalogs hold every user-facing sentence in one place per locale, so translators edit wording while developers edit behavior.

**Reviewed for:** Which user-facing copy sits in source code instead of the locale catalogs? Look at the source files for literal copy.

### Name Messages by Meaning

**ID:** `litespec/ext/locale#name-messages-by-meaning`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A locale catalog MUST use semantic keys that cite glossary terms rather than source-language sentences.

**Rationale:** A key that copies the English sentence breaks its meaning the day English rewords it: every other language still points at the old sentence. A semantic key names the intent instead, citing the glossary term that holds the word's labels per language, so wording changes in any language without disturbing the key.

**Example:** `orders.cancel.confirmation` identifies intent without copying the English sentence.

**Reviewed for:** Which catalog keys are source-language sentences, or lack a semantic name citing a glossary term? Look at the catalog keys.

### Complete or Fall Back Catalog Values

**ID:** `litespec/ext/locale#complete-or-fall-back-catalog-values`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each supported locale MUST contain required keys or use its declared fallback.

**Rationale:** A missing translation with no declared fallback becomes a runtime surprise: the interface shows a key, an empty string, or the wrong language, and nobody chose which. The declaration states the fallback locale and what a missing key does at build time.

A key missing from the default locale fails the build. No fallback exists for it, and the interface renders its identifier. A key missing from a non-default locale reports a warning and falls back. Shipping one untranslated string costs less than blocking a release on it.

**Example:** A catalog check runs in the frontend lint gate. The two outcomes are visible before review, not in a browser.

**Reviewed for:** Which locales lack a required key with no declared fallback to resolve it? Look at each locale catalog beside the fallback chain.

### Follow the Validation Message Grammar

**ID:** `litespec/ext/locale#follow-the-validation-message-grammar`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A validation catalog MUST state the field label, what is wrong, and what is accepted, without technical wording.

**Rationale:** A validation message that names the field, the fault, and the accepted input tells the user how to fix the entry in any language, while a message with only one of the three leaves the user guessing. The three-part shape holds across languages; each language's sentence pattern lives in the labels of the field's glossary term and the language's writing page.

**Example:** The glossary term for a field carries the sentence pattern for each supported language.

**Reviewed for:** Which validation messages omit the field label, the fault, or the accepted input, or use technical wording? Look at the validation messages in each locale.

### Resolve Every Shown Identifier

**ID:** `litespec/ext/locale#resolve-every-shown-identifier`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST resolve every closed-set code it renders to its glossary label and never render the raw code, excluding user-facing reference numbers.

**Rationale:** A code such as a refusal ground or a billing state is a contract value built for systems, while the glossary label beside it is copy built for users. A screen that renders the raw code shows one untranslated fragment in an otherwise translated page and forces the user to decode it. Resolving every shown code keeps the whole screen in the user's language, except reference numbers the user quotes back.

**Example:** A shipment row shows the glossary label for `shipment-delayed` in the active locale.

**Reviewed for:** Which shown codes render as raw codes instead of glossary labels, outside user-facing reference numbers? Look at the rendered screens.

### Format Values with Active Locale

**ID:** `litespec/ext/locale#format-values-with-active-locale`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A localized interface MUST format dates, times, numbers, currency, plurals, lists, and relative time by active locale.

**Rationale:** Dates, numbers, currency, plurals, lists, and relative time each follow the user's conventions, and a date the user misreads becomes a missed appointment or a wrong payment. The platform supplies all of them.

[The `Intl` namespace](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl) has `DateTimeFormat`, `NumberFormat`, `PluralRules`, `ListFormat`, and `RelativeTimeFormat`. Each one carries the locale data the browser already ships. A project-written formatter for any of these is a second locale database that no one updates. Its plural rules are wrong for the first language with more than two forms.

**Example:** Create one formatter instance per locale and reuse it. Construction is the expensive part.

**Reviewed for:** Which dates, numbers, currency, plurals, lists, or relative times bypass the active-locale formatters? Look at the formatting calls.

### Preserve Locale-Neutral Business Data

**ID:** `litespec/ext/locale#preserve-locale-neutral-business-data`

**Strength:** required

**Applies to:** artifact: value-object, endpoint

**Statement:** Domain values and API contracts MUST remain locale-neutral unless a use case explicitly exchanges localized content.

**Rationale:** An expense state or a price stored in Dutch breaks the moment an English speaker opens it, and an API contract that shifts per language forces every consumer to negotiate wording before reading data. Business values and wire contracts stay in one neutral form, and only the interface translates them for display. A use case that genuinely exchanges localized content, such as a product description the merchant writes per language, names that exchange explicitly.

**Reviewed for:** Which domain values or API contracts carry locale-specific text outside an explicitly localized exchange? Look at the domain values and contracts.

### Localize Public Presentation Content

**ID:** `litespec/ext/locale#localize-public-presentation-content`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** Public metadata, form labels, validation messages, and user-safe errors MUST follow the active locale.

**Rationale:** A Dutch page with an English title, English form labels, or an English error tells the user the translation stopped halfway and tells a search engine the page serves two languages. Metadata, labels, validation messages, and user-safe errors follow the active locale with the page around them.

**Reviewed for:** Which public metadata, form labels, validation messages, or user-safe errors render outside the active locale? Look at each rendered page.

### Keep Stable Error Codes Locale-Neutral

**ID:** `litespec/ext/locale#keep-stable-error-codes-locale-neutral`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A stable API error code MUST NOT change by locale.

**Rationale:** Integrations and support tools match on the error code, and a code that changes per language forces every consumer to list every translation before handling one failure. The code stays fixed while the human-readable message beside it translates.

**Reviewed for:** Which stable API error codes differ across locales? Look at the error code values in each locale's responses.

## Conventions

### Use One Catalog Root

**ID:** `litespec/ext/locale#use-one-catalog-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep catalogs under one application-owned locale root.

**Replacement:** A project names another root for locale catalogs in its documentation.

**Rationale:** One application-owned root tells every developer and tool where catalogs live, so a new locale lands beside the old ones instead of in a folder nobody checks. Scattered catalogs split the set, and one of them misses the fallback check.

**Reviewed for:** Which catalogs sit outside the application-owned root, with no other root named in the documentation? Look at the catalog folders.

### Split Catalogs by Module When Needed

**ID:** `litespec/ext/locale#split-catalogs-by-module-when-needed`

**Strength:** default

**Applies to:** artifact: any

**Default:** Split catalogs by module only when catalog size requires it.

**Replacement:** A project splits catalogs by feature from the first locale.

**Rationale:** Small catalogs read better whole: one file per locale shows every key at a glance and survives a rename without cross-file edits. Splitting waits until measured size, such as slow loads or conflicting edits, proves one file no longer serves.

**Reviewed for:** Which catalogs split before measured size calls for it, with no feature split the project chose from the start? Look at the catalog files beside the size measurements.

### Use BCP 47 Identifiers

**ID:** `litespec/ext/locale#use-bcp-47-identifiers`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use BCP 47 locale identifiers.

**Replacement:** A project names another identifier form for locales in its documentation.

**Rationale:** BCP 47 is the identifier form routes, catalogs, browsers, and libraries recognize: one tag such as `nl` travels unchanged from the address bar to the catalog lookup. A home-grown form needs translation at every boundary and puzzles every new developer.

**Reviewed for:** Which locale identifiers break BCP 47, with no other identifier form named in the documentation? Look at the locale identifiers in the catalogs and routes.

### Keep Locale Selection Explicit

**ID:** `litespec/ext/locale#keep-locale-selection-explicit`

**Strength:** default

**Applies to:** artifact: route

**Default:** Keep locale selection in the URL or documented session preference.

**Replacement:** A project keeps the active locale in ambient request state.

**Rationale:** A locale stored in the URL or a documented session preference travels with the request, so any developer can see which language a page serves and link to it. An implicit global locale hides in process state, where one request can read another's language and no link reproduces the page.

**Reviewed for:** Which requests carry the locale in ambient request state instead of the URL or a documented session preference? Look at the request handling for the locale.

## Dependencies

Packages: None.

Related extensions: None.

Guides: None.
