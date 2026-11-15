# Admin Screen Patterns

**Stack:** any
**Concern:** frontend-ui

Settings screens configure the company itself: the team and its roles, API keys and webhooks, data imports, the payout account, subscription invoices, and budget limits and advances. These screens carry powerful commands: granting access, rotating secrets, paying invoices. Each pattern below draws one settings screen as options with a named default to build, so those commands stay safe to operate and every settings area works the same way. When the team builds or revises a settings area, the product owner picks one option from the drawings, and the team records the choice in its pattern catalog.

## Concepts

### How to Read a Pattern

Each pattern below answers one recurring question about a settings screen, such as how the team page shows members and invitations. The pattern names the question it answers, the default option to build, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and sometimes a reference to the outside product it learns from. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI Patterns](../patterns.md) page states the catalog contract and the wireframe language these patterns share.

### The Running Example

Every drawing below uses the same example: the Claimwick settings for Harbor and Pine. The team has Dana as Owner; the API key is the Accounting export key; the import file is staff-2026-01.csv; the payout account sits at Example Bank; the subscription invoice is INV-2026-09; the budget code is TRIP10; and the advance is AC-2026-0117. The reader is the staff member using the screen. Two rules hold on every option: a new secret shows in full only once, and an invitation or role change shows what the role allows before it is sent. The Standards section below states both.

## Standards

### Show a New Secret Once

**ID:** `litespec/frontend/patterns/admin-screens#show-a-new-secret-once`

**Strength:** required

**Statement:** A screen MUST show a newly created API key or secret in full only once, at creation.

**Rationale:** Anyone who later opens the page can copy a secret that stays readable: on a shared screen or in a screenshot. Showing the secret once, at creation, forces the person creating it to store it in a vault or an environment file right away, so no later visit can leak it.

**Example:** Dana creates the Accounting export key; the secret shows once in the creation dialog, and afterward the keys table names the key's grants and last use.

**Reviewed for:** Does any newly created credential stay readable after its creation dialog closes? Open the creation flow for keys and secrets on a built settings screen.

### Show What a Role Allows Before Granting It

**ID:** `litespec/frontend/patterns/admin-screens#show-what-a-role-allows-before-granting-it`

**Strength:** required

**Statement:** An invitation or role change MUST show what the chosen role allows before it is sent.

**Rationale:** Role names such as Spender or Approver hide what they actually grant. A sender who sees only the name can hand out reimbursement rights by mistake. Showing the granted capabilities beside the name lets the sender confirm the access itself before the invitation goes out.

**Example:** When Dana invites Nina as Spender, the dialog lists that Spenders submit expenses and attach receipts but cannot reimburse, before Dana sends the invitation.

**Reviewed for:** Does any invitation or role change go out without listing what the role allows? Open the invite and role-change flows on a built team screen.

## Patterns

### Team Members and Their Roles

**ID:** `litespec/frontend/patterns/admin-screens#team-members-and-their-roles`

**Strength:** default

**Question:** How does the team page show the staff, their roles, and the invitations still outstanding? The team is the staff who work for the company, and a role states what each person may do. The staff member holding the Owner role invites people, grants roles, limits some staff to one cost centre, and suspends or removes people. Everyone else sees names and roles, and nothing more.

**Default:** `members-with-the-invitations-below`

**Compact:** The default reflows: the members table scrolls sideways while the invitations list stacks beneath it.

**Rationale:** The members table with invitations below answers three questions in one scan: who is in, who is invited, and what each person may do. That is why it is the default. The permission matrix is the runner-up; prefer it on the roles page, where capabilities are compared across roles. The cost-centre cards are the alternative for a staff group that claims for one cost centre with access that ends on a date.

Never ship the matrix as the team page: the team page manages people, and a capability grid hides who holds what. The invite command opens a dialog in place, with the role and its capabilities beside the address. A reader who cannot manage the team sees the same list without the row commands.

**Reviewed for:** Does the built member list, permission matrix, or staff card differ from the wireframe the catalog entry chose? Open the built team or roles screen beside the catalog entry recording the chosen option and its reason.

#### `members-with-the-invitations-below`: Members with the Invitations Below

```wireframe
page
  breadcrumb "Settings" "Team"
  header
    heading "Team"
    text "Harbor and Pine · 5 members · 2 invitations outstanding."
    action primary "Invite member"
  region "Members"
    table 5 "Member" "Role" "Access" "State"
      item: avatar "D" · text "Dana" · text "Owner" · text "Everything" · status "Active" positive · action "Manage"
  region "Invitations · 2"
    list 2
      item: text "nina@example.org" · text muted "Invited as Spender · expires 13 Oct" · status "Sent" info · action "Resend"
```

**Rationale:** The member list carries role and state in line, with pending invitations below in the same view. Invited and active people are two states of one team list, so they sit on one page. The owner acts on a row, such as resending or revoking, without opening it.

**Cost:** The row cannot fit commands for suspending, removing, changing a role, and limiting access, so most move into a menu on each row.

**Reference:** Linear

#### `permission-matrix-per-role`: Permission Matrix per Role

```wireframe
page
  breadcrumb "Settings" "Team" "Roles"
  header
    heading "Roles"
    text "Harbor and Pine · 4 roles beside the owner."
    action primary "New role"
  region "What each role may do"
    table 6 "Capability" "Approver" "Finance" "Auditor" "Spender"
      item: text "Reimburse an expense" · text "No" · text "Yes" · text "No" · text "No"
    message "Spenders submit and attach receipts. Only Finance reimburses." info
```

**Rationale:** A capability matrix lays every role side by side, answering what each role may do without opening roles one by one. That makes it fit custom roles, whose grants would otherwise hide inside dialogs.

**Cost:** Once capabilities pass about 20, the matrix needs grouping by area, and it reads as a reference table rather than a working page.

**Reference:** GitHub

#### `cost-centre-scoped-staff-cards`: Cost-Centre-Scoped Staff Cards

```wireframe
page
  breadcrumb "Settings" "Team"
  header
    heading "Team"
    text "Harbor and Pine · interns claim for one cost centre."
    action primary "Invite staff"
  region "Travel cost centre"
    list 3
      item: avatar "N" · text "Nina" · text muted "Spender · access ends 31 Mar" · status "Active" positive · action "Remove"
    message "Seasonal access ends on the date on the card. Nothing is removed by hand." info
```

**Rationale:** Some staff claim for one cost centre and nothing else, such as interns. The card names the cost centre, the role, and the date the access ends.

**Cost:** Cards apart from the member list split the team across two places, so the owner checks both.

**Reference:** Vercel

### API Keys and Webhooks

**ID:** `litespec/frontend/patterns/admin-screens#api-keys-and-webhooks`

**Strength:** default

**Question:** Company systems read the product through API keys, and expense updates reach those systems through webhooks. How do the keys page and the webhooks page show what each key may do and whether each update arrived? A key secret shows once and never again, and each webhook delivery states whether it arrived. A missed expense update is money unaccounted for.

**Default:** `keys-table-with-the-grants-in-line`

**Compact:** The default reflows: the keys table keeps its grants in line while scrolling sideways under the tabs.

**Rationale:** The keys table with the grants in line states what each key may do, and last use tells finance whether it still runs. That is why it is the default. The secret dialog pairs with it at creation, where the new secret shows once and cannot be read again. The endpoint with its delivery log is the runner-up; prefer it on the webhooks tab, where deliveries are the work.

Never show the secret outside the creation dialog: a secret that can be read again is not a secret. Keep the key list and the endpoint list on separate tabs: a key grants access, and an endpoint receives updates.

**Reviewed for:** Does the built key list, secret dialog, or delivery list differ from the wireframe the catalog entry chose? Open the built keys or webhooks screen beside the catalog entry recording the chosen option and its reason.

#### `keys-table-with-the-grants-in-line`: Keys Table with the Grants in Line

```wireframe
page
  breadcrumb "Settings" "Developers"
  header
    heading "Developers"
    text "Keys and the endpoints that receive updates."
    action primary "Create key"
  tabs "API keys" "Webhooks"
    region "Keys"
      table 3 "Key" "Grants" "Last used" "State"
        item: text "Accounting export" · text "Read expenses, read payouts" · text "8 Oct, 09:31" · status "Live" positive · action "Rotate"
```

**Rationale:** The key list states grants, last use, and state in line, answering what a key may do and whether it is still used. Rotate sits on the row; revoking is a command on the key page.

**Cost:** Grants fit in line only as short labels. The full grant list lives on the key page, one click down.

**Reference:** Stripe

#### `the-secret-shown-once`: The Secret, Shown Once

```wireframe
page
  breadcrumb "Settings" "Developers"
  header
    heading "Developers"
    text "Keys and the endpoints that receive updates."
  tabs "API keys" "Webhooks"
    region "Keys"
      list 2
        item: text "Accounting export" · text muted "Read expenses, read payouts" · status "Live" positive
  dialog "Copy the secret now"
    text "Grants: read approval state. Created by Dana on 8 Oct."
    field "Secret" text = "claimwick_live_9P4R2M7Q8H2K5T1BX6DW"
    action quiet "Copy"
    text muted "This secret shows once. It cannot be read again after this dialog closes."
    action primary "Secret stored, close"
```

**Rationale:** The creation dialog shows the new secret once, with copy first, because the secret cannot be read again. It states the grants, so finance stores the right secret against the right system.

**Cost:** The secret is lost when the owner closes the dialog too soon. The only repair is revoking it and creating a new one.

**Reference:** GitHub

#### `webhook-endpoints-with-their-deliveries`: Webhook Endpoints with Their Deliveries

```wireframe
page
  breadcrumb "Settings" "Developers" "Webhooks"
  header
    heading "Webhooks"
    text "The addresses expense updates are posted to."
    action primary "Register endpoint"
  region "https://bank.harbor-pine.example/hooks"
    list 3
      item: text "expense.reimbursed · EXP-1042" · text muted "8 Oct, 09:32 · answered 200 in 180 ms" · status "Delivered" positive
    action quiet "Replay the failed delivery"
  region "https://accounts.harbor-pine.example/hooks"
    message "Paused by Dana on 1 Oct. Deliveries queue until it is resumed." caution
    action "Resume"
```

**Rationale:** Each endpoint sits with its recent deliveries and their states, answering whether an update arrived. A failed delivery carries its next retry and a replay command.

**Cost:** Deliveries reach the thousands on a busy day, so the list needs the endpoint as a filter and aggressive paging.

**Reference:** Svix

### Importing Data from a File

**ID:** `litespec/frontend/patterns/admin-screens#importing-data-from-a-file`

**Strength:** default

**Question:** Staff, budget lines, and payout consents arrive in files. How does the import flow show what was brought in, what was refused, and why? Each import names what was brought in and what was refused, and each refusal names its reason. Finance fixes refused rows without importing the file twice.

**Default:** `imports-with-the-refused-rows-below`

**Compact:** The default reflows: the imports table scrolls sideways with the refused rows stacked beneath it.

**Rationale:** The imports table with the refused rows below keeps history and repairs on one page. That is why it is the default. The column mapping is the runner-up; prefer it at import time, where the file's columns meet the product's fields. The dry run is the alternative before a large file lands, where the count of refusals decides whether to continue.

Never ship the upload alone: an import without its refused rows hides the work it creates. Never show one list for refusals from several imports; each import groups its own refused rows.

**Reviewed for:** Does the built import flow, column match, or dry-run count differ from the wireframe the catalog entry chose? Open the built import flow beside the catalog entry recording the chosen option and its reason.

#### `imports-with-the-refused-rows-below`: Imports with the Refused Rows Below

```wireframe
page
  breadcrumb "Settings" "Data imports"
  header
    heading "Data imports"
    text "What was brought in from files, and what came of it."
    action primary "Register import"
  region "Imports"
    table 3 "File" "Brought in" "Accepted" "Refused" "State"
      item: text "staff-2026-01.csv" · text "Staff" · text "1,204" · text "18" · status "Done with refusals" caution
  region "Refused rows · staff-2026-01.csv"
    list 3
      item: text "Row 44 · no email" · text muted "Name present, email address missing." · action "Drop row"
```

**Rationale:** An import shows its refused rows with the reason per row, because a spreadsheet is never clean. Staff repair those rows without touching the rows that already went in.

**Cost:** Refused rows from several imports pile up, so each import groups its refused rows under it rather than in one shared list.

**Reference:** Shopify

#### `matching-columns-to-fields`: Matching Columns to Fields

```wireframe
page
  breadcrumb "Settings" "Data imports" "Register"
  header
    heading "Register import"
    text "staff-2026-01.csv · 1,222 rows · match the columns."
    action primary "Import 1,222 rows"
  region "Column mapping"
    field "Voornaam" select = "First name"
    field "Achternaam" select = "Last name"
    field "E-mail" select = "Email address"
    field "Opmerkingen" select = "Ignore"
```

**Rationale:** The file's names never equal the system's, so a mapping step pairs each column with one field before the import. Finance corrects the guesses.

**Cost:** A wide file means 20 mapping rows, so the page shows the unmatched columns first and collapses the matched ones.

**Reference:** Mailchimp

#### `dry-run-before-the-import`: Dry Run Before the Import

```wireframe
page
  breadcrumb "Settings" "Data imports" "Register"
  header
    heading "Register import"
    text "staff-2026-01.csv · dry run · nothing written yet."
    action primary "Import 1,204 rows"
  row
    figure "Accepted" "1,204"
    figure "Refused" "18"
    figure "Repairable" "4"
  region "Why 18 rows are refused"
    list 3
      item: text "12 rows · address missing" · text muted "Rows 44, 61 and 88." · action "Drop rows"
```

**Rationale:** Before a bulk change lands, a dry run shows how many rows will fail without writing anything. The preview counts accepted, refused, and repairable rows.

**Cost:** A dry run reads a large file twice, so files over 5,000 rows get no preview.

**Reference:** Stripe

### Connecting the Payment Account

**ID:** `litespec/frontend/patterns/admin-screens#connecting-the-payment-account`

**Strength:** default

**Question:** Reimbursements leave through the company payout account: each payout sends an approved expense to its spender. How does the setup page show the connection, the methods on offer, and what each method costs? The company connects the account once, chooses which payout methods to offer, and reads what each method costs.

**Default:** `connected-account-with-the-methods-below`

**Compact:** The default reflows: the account card stacks above the methods table, which scrolls sideways.

**Rationale:** The connected account card with the methods list gives connection state and methods each a place, with costs in line. That is why it is the default. The capability checklist is the runner-up; prefer it while the connection is new and methods still need approval. The costs table is the alternative when finance compares methods on last month's payouts.

Never show the methods without the account card: a method means nothing while the account is disconnected. Before the company connects an account, the page names that state and offers one connect command, with no methods list.

**Reviewed for:** Does the built account card, capability checklist, or method cost differ from the wireframe the catalog entry chose? Open the built payment setup beside the catalog entry recording the chosen option and its reason.

#### `connected-account-with-the-methods-below`: Connected Account with the Methods Below

```wireframe
page
  breadcrumb "Settings" "Payments"
  header
    heading "Payments"
    text "The account payouts leave from, and what it costs."
    action "Refresh capabilities"
  card "Example Bank"
    status "Connected" positive
    fact "Account" "Harbor and Pine · acct_1H8K2M"
    fact "Paid from" "NL20 INGB 0001 2345 67 · Harbor and Pine"
    fact "Capabilities checked" "8 Oct 2026, 09:32 · all methods live"
    fact "Reversals" "Against each payout, within five days"
    action "Replace account"
    action destructive "Disconnect"
  region "Payment methods"
    table 4 "Method" "Cost" "State"
      item: text "Bank payouts" · text "EUR 0.29 per payout" · status "Live" positive · action "Disable"
```

**Rationale:** Methods depend on the connection, so the connected account sits above the enabled methods. The card states the account, the payout bank, and when the capabilities were last refreshed.

**Cost:** The account and the methods each want the top of the page. On a phone the account wins, so the methods start below the fold.

**Reference:** Stripe

#### `method-capabilities-as-a-checklist`: Method Capabilities as a Checklist

```wireframe
page
  breadcrumb "Settings" "Payments"
  header
    heading "Payments"
    text "What the connected account can do · checked 8 Oct 2026, 09:32."
    action "Refresh capabilities"
  region "Capabilities"
    list 5
      item: text "Send bank payouts" · text muted "Live · reversible within 5 days" · status "Live" positive
```

**Rationale:** A method can be connected but not yet live, so a checklist names what the connected account can actually do. The list states what works and what is still pending.

**Cost:** The provider changes capabilities without warning, so the checklist is a cached reading with a timestamp rather than the truth.

**Reference:** Adyen

#### `method-costs-side-by-side`: Method Costs Side by Side

```wireframe
page
  breadcrumb "Settings" "Payments"
  header
    heading "Payments"
    text "What each method costs on last month's payouts."
    action primary "Change methods"
  region "September · 3,412 payouts · EUR 168,204.00 paid"
    table 3 "Method" "Share" "Fee" "Cost"
      item: text "Bank payouts" · text "71%" · text "EUR 0.29 per payout" · text "EUR 702.47"
```

**Rationale:** Finance compares costs when choosing methods, so each method carries its fee in line. The estimate grounds the percentages in last month's payouts.

**Cost:** An estimate from last month misleads when the next month sells differently, so the figures need their basis in the open.

**Reference:** Mollie

### Subscription Invoices

**ID:** `litespec/frontend/patterns/admin-screens#subscription-invoices`

**Strength:** default

**Question:** Claimwick invoices the company each month. How does the billing area show what each line charges, the mandate it pays under, and the history of months? The invoice holds the plan, the fee per expense, and the text-message cost. The company reads what each line charges, downloads the invoice for the books, and pays by direct debit under its mandate.

**Default:** `invoice-sheet-with-its-lines`

**Compact:** The default reflows: the invoice lines stack with quantities and rates above the total.

**Rationale:** The invoice sheet with its lines reads as the document the books need, with the mandate and the download beside it. That is why it is the default. The plan page with the mandate is the runner-up; prefer it for changing the plan rather than reading a month. The invoice list is the alternative for the history of months, where state and total are the facts.

Never show usage lines without their rates: a metered figure without its rate cannot be checked. Never bury a failed payment in the list; the invoice states the failure and the next retry.

**Reviewed for:** Does the built invoice sheet, invoice list, or plan and mandate differ from the wireframe the catalog entry chose? Open the built billing screens beside the catalog entry recording the chosen option and its reason.

#### `invoice-sheet-with-its-lines`: Invoice Sheet with Its Lines

```wireframe
page
  breadcrumb "Settings" "Billing" "Invoices" "INV-2026-09"
  header
    heading "Invoice INV-2026-09"
    status "Paid" positive
    text "Harbor and Pine · September 2026 · paid 3 Oct by direct debit."
    action "Download PDF"
  card "INV-2026-09 · issued 1 Oct 2026 · due 15 Oct"
    fact "Business plan · September" "EUR 49.00"
    fact "3,412 expenses · EUR 0.35 each" "EUR 1,194.20"
    fact "812 text messages · EUR 0.08 each" "EUR 64.96"
    figure "Total · paid 3 Oct" "EUR 1,308.16"
    text muted "Paid by direct debit under mandate MND-8841."
```

**Rationale:** The company files the invoice and pays it, so the invoice reads as the document itself. The lines carry quantities and rates, and the totals add up from top to bottom.

**Cost:** A document-shaped page is read, not worked. Changing the plan or the mandate means leaving it.

**Reference:** Stripe Billing

#### `invoices-newest-first`: Invoices Newest First

```wireframe
page
  breadcrumb "Settings" "Billing" "Invoices"
  header
    heading "Invoices"
    text "What Claimwick charged Harbor and Pine."
    action primary "Change plan"
  region "Invoices"
    list 3
      item: text "INV-2026-09 · September" · text muted "Business plan · 3,412 expenses · 812 messages" · text "EUR 1,308.16" · status "Paid" positive · action "Open"
```

**Rationale:** The company asks which month is paid and which is open, so the list carries state and total in line. The overdue row carries the retry date.

**Cost:** The list shows totals without lines. Checking one figure means opening the invoice.

**Reference:** Chargebee

#### `plan-with-the-mandate-beside-it`: Plan with the Mandate Beside It

```wireframe
page
  breadcrumb "Settings" "Billing"
  header
    heading "Billing"
    text "The plan, the mandate, and the next invoice."
    action primary "Change plan"
  split 1:1
    region "Business plan · EUR 49.00 a month"
      fact "Expenses included" "1,000 a month · then EUR 0.35 each"
      fact "Text messages" "EUR 0.08 each"
      fact "Team" "Unlimited members"
      action "Change plan"
    region "How it is paid"
      fact "Method" "Direct debit"
      fact "Mandate" "MND-8841 · signed 10 Jan"
      fact "Next invoice" "1 Nov · about EUR 210.00"
      action "Replace mandate"
```

**Rationale:** Changing one of the plan, the mandate, or the next invoice affects the others, so the three sit together. The company reads what it pays and how it pays before changing the plan or the mandate.

**Cost:** Plan, mandate, and next invoice each deserve the top. The page orders three equals, and one of them loses.

**Reference:** Stripe Billing

### Budget Limits and Advances

**ID:** `litespec/frontend/patterns/admin-screens#budget-limits-and-advances`

**Strength:** default

**Question:** A budget code funds spending on one trip, with its own ceiling, claim limit, and dates. An advance holds money for later expenses and spends down as claims arrive. How does the finance area show what each code spends and what each advance still holds? Finance watches what each code spends, retires what leaks, and issues new codes for trips and projects.

**Default:** `code-table-with-use-and-state`

**Compact:** The default reflows: the code table keeps value, use, and state in line while scrolling sideways.

**Rationale:** The code table with use and state shows value, use, and dates in one scan. A leaked code opens from its row and is retired in its editor. That is why it is the default. The condition editor is the runner-up; prefer it for creating or revising a code with several conditions. The advances list is the alternative when the balance of the money held is the question.

Never ship the claims-only list: claims explain the past, and finance manages the codes. A code with a handful of fields is issued from the list; a code with conditions opens the editor.

**Reviewed for:** Does the built code table, condition editor, or advance balance differ from the wireframe the catalog entry chose? Open the built budget screens beside the catalog entry recording the chosen option and its reason.

#### `code-table-with-use-and-state`: Code Table with Use and State

```wireframe
page
  breadcrumb "Finance" "Budgets"
  header
    heading "Budgets"
    text "Budget codes, per-diem rates and advances across the company."
    action primary "Issue code"
  tabs "Budget codes" "Per-diem" "Advances"
    region "Codes"
      table 4 "Code" "Value" "Used" "Valid" "State"
        item: text "TRIP10" · text "EUR 500 cap" · text "412 of 500" · text "1 Feb to 14 Mar" · status "Active" positive · action "Open"
```

**Rationale:** Finance asks what each code spends and whether it leaked, so the table shows value, use, and limits in line. The state badge names what the code does now.

**Cost:** A row fits the conditions behind a code only as a second line; the code page holds the full rule.

**Reference:** Shopify

#### `code-editor-with-its-conditions`: Code Editor with Its Conditions

```wireframe
page
  breadcrumb "Finance" "Budgets" "TRIP10"
  header
    heading "TRIP10"
    text "Budget code · 412 of 500 claimed."
    action destructive "Retire"
    action primary "Save changes"
  split 1:1
    region "Value"
      field "Type" select = "Fixed ceiling"
      field "Ceiling" number = "500"
      field "Maximum uses" number = "500"
      field "Valid until" date = "14 Mar 2026"
    aside
      region "Summary"
        fact "Ceiling" "EUR 500 per trip"
        fact "Claimed" "412 of 500"
        fact "Spent so far" "EUR 1,854.00"
        fact "State" "Active"
```

**Rationale:** A code whose ceiling, use limit, and end date all change together needs an editor that pairs each condition with the value it bounds. The summary beside the fields states what the code spends before the change is saved.

**Cost:** The blocks take a long page. The back office cannot check one code and move to the next without scrolling past every condition.

**Reference:** Shopify

#### `advances-with-their-balances`: Advances with Their Balances

```wireframe
page
  breadcrumb "Finance" "Budgets"
  header
    heading "Budgets"
    text "Budget codes, per-diem rates and advances across the company."
    action primary "Issue advance"
  tabs "Budget codes" "Per-diem" "Advances"
    region "Advances"
      list 3
        item: text "Advance AC-2026-0117" · text muted "Issued to Ava · 6 Oct" · text "EUR 25.00 of EUR 25.00 left" · status "Unused" info · action "Open"
```

**Rationale:** An advance is money held for a later expense, so its balance spends down as claims arrive. The balance, not the code, is the figure the company watches.

**Cost:** Advances and budget codes share little beyond the word code. One list for both reads as two lists stapled together.

**Reference:** Shopify
