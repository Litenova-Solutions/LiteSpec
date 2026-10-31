// Writes the Lantern example consumer project used by the Intent deck.
const fs = require('fs'), path = require('path');
const root = process.env.LANTERN_OUT || path.join(__dirname, 'lantern');
const write = (rel, text) => { const p = path.join(root, rel); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, text); };
const page = (fm, body) => '---\n' + JSON.stringify(fm, null, 2) + '\n---\n' + body.trim() + '\n';
const base = (type, id, title) => ({ type, id, title, owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' });

const project = JSON.parse(fs.readFileSync('D:/Personal-Projects/LiteSpec/templates/consumer/litespec.project.json', 'utf8').replace(/__PROJECT__/g, 'lantern').replace(/__OWNER__/g, 'founder'));
project.size = 'large';
project.owners = ['founder', 'product-team'];
// The example holds Intent records and one Behavior slice, so it declares no frontend and no UI package.
project.stacks = ['dotnet'];
delete project.paths.uiPackage; delete project.paths.uiPatterns; project.paths.frontends = [];
write('litespec.project.json', JSON.stringify(project, null, 2) + '\n');

write('docs/intent/brief.md', page({ ...base('product', 'product/lantern', 'Lantern'),
  boundary: 'Travellers find hotel rooms, book them and manage their bookings.',
  exclusions: ['Lantern never sells flights or other transport.', 'Lantern never stores a traveller\'s card details.'],
  addresses: ['actor/traveller', 'actor/hotel-partner'] },
  '# Lantern\n\n## Summary\n\nLantern lets travellers book a hotel room at the price they were shown, and lets hotels fill rooms without a call center.'));

const actor = (name, title, kind, summary) => write(`docs/intent/actors/${name}.md`, page({ ...base('actor', `actor/${name}`, title), kind }, `# ${title}\n\n## Summary\n\n${summary}`));
actor('traveller', 'Traveller', 'person', 'A person who searches for a room and books a stay for themselves or others.');
actor('hotel-partner', 'Hotel Partner', 'organization', 'A hotel that offers rooms on Lantern and receives the bookings.');
actor('support-agent', 'Support Agent', 'person', 'A Lantern employee who helps travellers whose booking went wrong.');

const goal = (name, title, statement, measure, target, dueBy, why) => write(`docs/intent/goals/${name}.md`, page({ ...base('goal', `goal/${name}`, title), statement, measure, target, dueBy }, `# ${title}\n\n## Why It Matters\n\n${why}`));
goal('more-completed-bookings', 'More Completed Bookings', 'More travellers who search for a room finish a booking.', 'Share of searches that end in a confirmed booking', 'At least 4 percent', 'Summer season 2027', 'Every abandoned checkout is a traveller who found what they wanted and still left. Lantern earns nothing from a search.');
goal('trusted-prices', 'Trusted Prices', 'Travellers pay exactly the price they were shown.', 'Share of bookings whose charged total differs from the total shown in search', '0 percent', 'Launch', 'A traveller who meets a surprise fee at checkout stops trusting every price Lantern shows.');

const problem = (name, title, statement, situation, evidence) => write(`docs/intent/problems/${name}.md`, page({ ...base('problem', `problem/${name}`, title), statement, affects: ['actor/traveller'] }, `# ${title}\n\n## Situation\n\n${situation}\n\n## Evidence\n\n${evidence}`));
problem('price-surprises', 'Price Surprises', 'Travellers find fees at checkout that the search result did not show.', 'A traveller picks a room at EUR 120 a night and meets city tax and a cleaning fee on the payment page.', 'In the pilot, 18 percent of support messages were about a fee the traveller had not seen.');
problem('rooms-sell-out-at-checkout', 'Rooms Sell Out at Checkout', 'A room sells out while the traveller is paying for it.', 'Two travellers pick the last room at the same hotel; the slower one pays and then learns there is no room.', 'Partners report about one double sale a week on busy dates.');
problem('pay-before-stay', 'Paying Before the Stay', 'Some travellers do not want to pay months before they arrive.', 'A traveller books a summer stay in January and would rather pay at the hotel.', 'Exit survey answers in the pilot.');

const feature = (name, title, extra, offering) => write(`docs/intent/features/${name}.md`, page({ ...base('feature', `feature/${name}`, title), ...extra }, `# ${title}\n\n## Offering\n\n${offering}`));
feature('all-in-pricing', 'All-In Pricing', { lifecycle: 'planned', solves: ['problem/price-surprises'], advances: ['goal/trusted-prices'], plannedFor: 'Launch' }, 'Every price in search is the total for the stay, with every tax and fee included. Out: currency conversion.');
feature('room-hold', 'Room Hold', { lifecycle: 'shipped', solves: ['problem/rooms-sell-out-at-checkout'], advances: ['goal/more-completed-bookings'] }, 'The room a traveller starts to book is held for them while they pay. Out: holds without starting checkout.');
feature('pay-at-hotel', 'Pay at the Hotel', { lifecycle: 'refused', solves: ['problem/pay-before-stay'], refusedBecause: 'Lantern would carry the no-show risk, which partners will not share.' }, 'Paying at the hotel instead of at booking. Refused; see refusedBecause.');

write('docs/intent/constraints/total-price-display.md', page({ ...base('constraint', 'constraint/total-price-display', 'Total Price Display'),
  authority: 'EU consumer law on price indication (illustrative citation)', kind: 'legal', applies: true,
  appliesBecause: 'Lantern sells to consumers in the EU.' },
  ['# Total Price Display', '## Summary', 'A price shown to a consumer includes every unavoidable tax and fee.', '## What It Requires',
   'This section renders from the requirements that cite this constraint. Edit the citing requirements, never here.',
   '<!-- generated:what-it-requires -->\n- `requirement/checkout.shown-price-is-charged`: Shown Price Is Charged.\n<!-- /generated:what-it-requires -->'].join('\n\n')));
write('docs/intent/constraints/package-travel-rules.md', page({ ...base('constraint', 'constraint/package-travel-rules', 'Package Travel Rules'),
  authority: 'EU package travel rules (illustrative citation)', kind: 'legal', applies: false,
  appliesBecause: 'Lantern sells hotel stays only and never combines them with transport.' },
  ['# Package Travel Rules', '## Summary', 'A seller who combines two travel services, such as a flight and a hotel, carries the duties of a package organizer.', '## What It Requires',
   'This section renders from the requirements that cite this constraint. Edit the citing requirements, never here.',
   '<!-- generated:what-it-requires -->\n<!-- /generated:what-it-requires -->'].join('\n\n')));

write('docs/intent/research/partner-hold-limits.md', page({ ...base('research', 'research/partner-hold-limits', 'Partner Hold Limits'),
  question: 'How long will hotel partners hold a room without payment?',
  conclusion: 'Partner systems hold a room for at most 15 minutes without payment, so checkout must finish inside that window.',
  sources: [{ url: 'https://example.com/partner-api/holds', readOn: '2026-10-12' }] },
  '# Partner Hold Limits\n\n## Answer\n\nAll three pilot partners release an unpaid hold after 15 minutes; one releases it after 12. High confidence for the pilot partners, unknown for others.'));

write('docs/intent/decisions/0001-room-hold-length.md', page({ ...base('decision', 'decision/0001-room-hold-length', 'Room Hold Length'),
  options: [{ name: '10 minutes', pros: 'Leaves a buffer before the shortest partner limit.', cons: 'Slow payers may lose the room.' },
            { name: '15 minutes', pros: 'Most time for the traveller.', cons: 'Equals the partner limit, so a slow partner call loses the room after payment.' }],
  reopenedWhen: 'A partner holds rooms longer than 15 minutes, or more than 5 percent of checkouts end because the hold ran out.',
  decides: ['requirement/checkout.room-held-during-checkout'], basedOn: ['research/partner-hold-limits'] },
  '# Room Hold Length\n\n## Context\n\nPartners release unpaid holds after 12 to 15 minutes.\n\n## Decision\n\nHold a room for 10 minutes during checkout.\n\n## Consequences\n\nTravellers who take longer than 10 minutes start again.'));

const req = (id, title, extra) => ({ type: 'requirement', id, title, ...extra });
write('docs/intent/booking/checkout.md', page({ type: 'topic', id: 'topic/checkout', title: 'Checkout', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01', reviewBy: '2027-06-01',
  records: [
    req('requirement/checkout.room-held-during-checkout', 'Room Held During Checkout', { kind: 'functional', statement: 'A room the traveller starts to book is held for them until the hold window ends.', rationale: 'The room must still exist when the payment succeeds.', cites: ['feature/room-hold', 'research/partner-hold-limits', 'decision/0001-room-hold-length'],
      except: [{ condition: 'The hotel partner refuses to hold the room.', fallback: 'requirement/checkout.no-charge-without-room' }] }),
    req('requirement/checkout.no-charge-without-room', 'No Charge Without a Room', { kind: 'functional', statement: 'A traveller is never charged for a room that was not confirmed.', rationale: 'Charging for nothing is the worst outcome a booking can have.', cites: ['feature/room-hold', 'problem/rooms-sell-out-at-checkout'], risk: ['money'] }),
    req('requirement/checkout.shown-price-is-charged', 'Shown Price Is Charged', { kind: 'functional', statement: 'The total shown in search is the total charged.', rationale: 'Consumer law and traveller trust both demand it.', cites: ['feature/all-in-pricing', 'constraint/total-price-display', 'goal/trusted-prices'], risk: ['money'] }),
    req('requirement/checkout.checkout-under-load', 'Checkout Under Load', { kind: 'quality', statement: 'During a flash sale, 95 percent of checkouts are answered within 3 seconds.', rationale: 'Flash sales are when most bookings happen.', cites: ['feature/room-hold'],
      stimulus: '2,000 travellers check out in the same minute during a flash sale', environment: 'Normal operation, one partner system answering slowly', response: 'Each booking is confirmed or refused', measure: 'Share of checkouts answered within 3 seconds: at least 95 percent', dueBy: 'Summer season 2027' }),
    req('requirement/checkout.service-fee', 'Service Fee', { kind: 'policy', statement: 'Lantern charges a fixed service fee per booking.', rationale: 'The fee pays for support and payment costs.', cites: ['feature/all-in-pricing'], values: [{ value: 'EUR 2.50', effectiveFrom: '2027-01-01' }], setBy: 'decider', risk: ['money'] }),
    req('requirement/checkout.free-cancellation-window', 'Free Cancellation Window', { kind: 'policy', statement: 'Each hotel sets the free cancellation window for its bookings.', rationale: 'Hotels carry the cost of a late cancellation.', cites: ['feature/room-hold'], values: [{ value: '24 hours by default, 0 to 72 hours', effectiveFrom: '2027-01-01' }], setBy: 'tenant' })
  ] },
  ['# Checkout', '## Summary', 'What Lantern promises from the moment a traveller picks a room until the booking is confirmed.', '## The Situation', 'Travellers abandon checkouts when a fee appears or the room vanishes.', '## What We Do', 'Hold the room, show the total, and never charge without a room.', '## Why It Is Better', 'The traveller sees one price and gets the room they paid for.', '## Example', 'Ana books a double room in Lisbon for 3 nights at a total of EUR 384.50.', '## Costs and Risks', 'Holds tie up partner inventory for up to 10 minutes.', '## How Sure We Are', 'Partner hold limits are measured for the pilot partners only.', '## Position', 'Hold length follows decision/0001-room-hold-length.'].join('\n\n')));

write('docs/intent/journeys/book-a-room.md', page({ ...base('journey', 'journey/book-a-room', 'Book a Room'),
  actor: 'actor/traveller', serves: ['goal/more-completed-bookings'],
  finishedWhen: 'The traveller holds a confirmation and the hotel has the booking.',
  cites: ['requirement/checkout.room-held-during-checkout', 'requirement/checkout.shown-price-is-charged', 'requirement/checkout.no-charge-without-room'],
  steps: [
    { id: 's1', statement: 'The traveller searches for rooms by city and dates.', actor: 'actor/traveller' },
    { id: 's2', statement: 'The traveller compares rooms by their total price for the stay.', actor: 'actor/traveller' },
    { id: 's3', statement: 'The traveller chooses a room and starts to book it.', actor: 'actor/traveller', failure: 'If the room has sold out, the traveller is told before paying and sees similar rooms.' },
    { id: 's4', statement: 'The traveller pays for the booking.', actor: 'actor/traveller', failure: 'If the payment fails, the room stays held until the 10 minutes end.' },
    { id: 's5', statement: 'The traveller receives the confirmation, and the hotel receives the booking.', actor: 'actor/traveller' }],
  questions: [{ question: 'How long should a room stay held during checkout?', decider: 'founder', decideBy: '2026-12-01', recommendation: '10 minutes', ballot: 'checkout-terms' },
              { question: 'What service fee does Lantern charge per booking?', decider: 'founder', decideBy: '2026-12-01', recommendation: 'EUR 2.50', ballot: 'checkout-terms' }] },
  '# Book a Room\n\n## Example\n\nAna searches Lisbon for 12 to 15 May 2027, picks a double room at EUR 384.50 in total, and pays by card.\n\n## Between the Steps\n\nA traveller who leaves at step 4 and returns after 10 minutes starts again at step 3.'));

write('docs/glossary.json', JSON.stringify({ schemaVersion: 2, mannered: [], terms: [
  { id: 'term/booking', term: 'booking', meaning: 'A confirmed reservation of one room for one stay.', layer: 'intent', aliases: ['reservation'], rejected: [{ word: 'order', reason: 'A booking is a reservation of a room, not a purchase of goods.' }], owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/hold', term: 'hold', meaning: 'A room kept for one traveller while they pay.', layer: 'intent', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/stay', term: 'stay', meaning: 'The nights between arrival and departure at one hotel.', layer: 'intent', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/room', term: 'room', meaning: 'A space a hotel rents for one stay.', layer: 'intent', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/confirmation', term: 'confirmation', meaning: 'A message proving that a booking exists.', layer: 'behavior', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' }] }, null, 2) + '\n');

// One Behavior slice: the bookings module with its holds, the checkout saga and the use cases around them.
write('docs/behavior/modules/bookings/README.md', page({ ...base('module', 'module/bookings', 'Bookings'),
  summary: 'Rooms, holds and bookings for every stay.', dependsOn: [] },
  '# Bookings\n\n## Example\n\nAna holds a double room in Lisbon, pays inside 10 minutes, and receives her confirmation.'));

write('docs/behavior/modules/bookings/room-hold/README.md', page({ ...base('aggregate', 'aggregate/bookings.room-hold', 'Room Hold'),
  summary: 'A room is held for at most one traveller at a time.',
  kind: 'entity',
  states: [
    { name: 'Held', meaning: 'A room waiting for payment.' },
    { name: 'Confirmed', meaning: 'A room paid for and booked.' },
    { name: 'Released', meaning: 'A hold given back to the hotel.' }],
  transitions: [
    { from: 'Held', to: 'Confirmed', by: 'use-case/bookings.complete-booking' },
    { from: 'Held', to: 'Released', by: 'use-case/bookings.expire-hold' }],
  data: [
    { name: 'hold-id', meaning: 'A unique identifier for the hold.', classification: 'internal', dataClass: 'hold-detail' },
    { name: 'room-id', meaning: 'The room the hold keeps.', classification: 'internal', dataClass: 'hold-detail' },
    { name: 'check-in', meaning: 'The date the stay starts.', classification: 'internal', dataClass: 'hold-detail' },
    { name: 'check-out', meaning: 'The date the stay ends.', classification: 'internal', dataClass: 'hold-detail' },
    { name: 'traveller-email', meaning: 'The address confirmations reach.', classification: 'personal', dataClass: 'guest-contact' },
    { name: 'total', meaning: 'The amount the traveller pays.', classification: 'internal', dataClass: 'hold-detail' }],
  dataClasses: [
    { name: 'guest-contact', classification: 'personal', fields: ['traveller-email'], onErasure: 'erase', basis: 'business', period: 'P2Y', enforcedBy: 'job/bookings.expire-lapsed-holds', purpose: 'confirmations and receipts reach the traveller' },
    { name: 'hold-detail', classification: 'internal', fields: ['hold-id', 'room-id', 'check-in', 'check-out', 'total'], onErasure: 'erase', basis: 'engineering', period: { mode: 'none', reason: 'holds stay until the guest checks out or the hold lapses' }, enforcedBy: 'job/bookings.expire-lapsed-holds', purpose: 'lapsed holds are diagnosed after release' }],
  records: [
    { type: 'rule', id: 'rule/room-hold.hold-lasts-ten-minutes', title: 'Hold Lasts Ten Minutes', kind: 'invariant', statement: 'A held room stays held until the hold window ends.', governs: ['aggregate/bookings.room-hold'], fulfills: ['requirement/checkout.room-held-during-checkout'] },
    { type: 'event', id: 'event/room-hold.held', title: 'Room Held', meaning: 'The traveller secured the room for the hold window.', carries: ['hold-id', 'room-id'] },
    { type: 'event', id: 'event/room-hold.released', title: 'Hold Released', meaning: 'The hold returned to the hotel.', carries: ['hold-id'] }] },
  ['# Room Hold', '## Transitions', 'List every allowed state change with the use case that moves it.',
   '<!-- generated:transitions -->\n| From | To | Triggered By |\n|:---|:---|:---|\n| Held | Confirmed | `use-case/bookings.complete-booking` |\n| Held | Released | `use-case/bookings.expire-hold` |\n<!-- /generated:transitions -->',
   '## Data', 'List every stored field with its meaning, data class, and classification.',
   '<!-- generated:data -->\n| Name | Meaning | Data Class | Classification |\n|:---|:---|:---|:---|\n| hold-id | A unique identifier for the hold. | hold-detail | internal |\n| room-id | The room the hold keeps. | hold-detail | internal |\n| check-in | The date the stay starts. | hold-detail | internal |\n| check-out | The date the stay ends. | hold-detail | internal |\n| traveller-email | The address confirmations reach. | guest-contact | personal |\n| total | The amount the traveller pays. | hold-detail | internal |\n<!-- /generated:data -->'].join('\n\n')));

write('docs/behavior/modules/bookings/booking/README.md', page({ ...base('aggregate', 'aggregate/bookings.booking', 'Booking'),
  summary: 'A booking confirms only a room that is still held.',
  kind: 'saga',
  states: [
    { name: 'Open', meaning: 'A checkout waiting for payment.' },
    { name: 'Confirmed', meaning: 'A stay paid for and booked.' },
    { name: 'Released', meaning: 'A checkout ended without payment.' }],
  steps: [
    { id: 'create-hold', runs: 'use-case/bookings.create-hold', compensation: { mode: 'none', reason: 'the partner releases an unpaid hold after 15 minutes' } },
    { id: 'complete-booking', runs: 'use-case/bookings.complete-booking', compensation: { mode: 'use-case', useCase: 'use-case/bookings.expire-hold' } }],
  exits: [
    { name: 'Confirmed', trigger: 'the traveller paid inside the hold window', then: 'The run leaves through this exit.' },
    { name: 'Released', trigger: 'the hold window lapsed before payment', then: 'The run leaves through this exit.' }],
  finishedWhen: 'the run reached the Confirmed exit or the Released exit',
  drivenBy: [{ person: 'actor/traveller', runbook: 'runbook/complete-checkout', dueWithin: 'PT10M' }],
  records: [
    { type: 'rule', id: 'rule/booking.charged-total-equals-shown-total', title: 'Charged Total Equals Shown Total', kind: 'invariant', statement: 'The charged total equals the total shown in search.', governs: ['aggregate/bookings.booking'], fulfills: ['requirement/checkout.shown-price-is-charged'] },
    { type: 'event', id: 'event/booking.completed', title: 'Booking Completed', meaning: 'The traveller paid for the stay.', carries: ['booking-id', 'total'] }] },
  '# Booking\n\n## Example\n\nAna starts checkout for a double room, pays the shown total of EUR 384.50 inside the hold window, and the run leaves through the Confirmed exit.'));

const res = (id, title, extra) => ({ type: 'result', id, title, ...extra });
const scn = (id, title, extra) => ({ type: 'scenario', id, title, ...extra });
write('docs/behavior/modules/bookings/room-hold/create-hold.md', page({ ...base('use-case', 'use-case/bookings.create-hold', 'Create Hold'),
  kind: 'command', serves: ['journey/book-a-room#s3', 'journey/book-a-room#s4'], fulfills: ['requirement/checkout.room-held-during-checkout', 'requirement/checkout.checkout-under-load'],
  access: { grant: 'anonymous', tenant: 'own' }, reachedThrough: ['entry-point/webshop'],
  input: [
    { name: 'room-id', meaning: 'The room the traveller books.', type: 'id', of: 'aggregate/bookings.room-hold', required: true, origin: 'caller', classification: 'internal' },
    { name: 'check-in', meaning: 'The date the stay starts.', type: 'date', required: true, origin: 'caller', classification: 'internal' },
    { name: 'check-out', meaning: 'The date the stay ends.', type: 'date', required: true, origin: 'caller', classification: 'internal' },
    { name: 'guests', meaning: 'The number of travellers staying.', type: 'integer', required: true, origin: 'caller', classification: 'internal' },
    { name: 'traveller-email', meaning: 'The address confirmations reach.', type: 'email', required: true, origin: 'caller', classification: 'personal' },
    { name: 'total', meaning: 'The amount the traveller pays.', type: 'money', required: true, origin: 'derived', classification: 'internal' }],
  output: [{ name: 'hold-id', meaning: 'The identifier of the created hold.' }],
  changes: ['aggregate/bookings.room-hold'], reads: ['setting/bookings.checkout-hold-window'], raises: ['event/room-hold.held'],
  idempotency: { mode: 'key', key: '{room-id}-{check-in}', scope: 'tenant' }, audit: { level: 'required', target: 'hold' }, risk: ['money'],
  background: 'A room is open for the requested dates.',
  records: [
    res('result/create-hold.held', 'Room Held', { outcome: 'success', meaning: 'A room held for the traveller until the hold window ends.' }),
    res('result/create-hold.sold-out', 'Room Gone', { outcome: 'refusal', code: 'bookings.room-gone', meaning: 'A message telling the traveller the room has sold out.' }),
    scn('scenario/create-hold.hold-room', 'Hold a Room', { exercises: ['rule/room-hold.hold-lasts-ten-minutes'], given: 'a room is open for the requested dates', when: 'the traveller starts to book the room', then: 'the room is held for the traveller', result: 'result/create-hold.held' }),
    scn('scenario/create-hold.room-gone', 'Room Gone', { given: 'a room sold out a moment ago', when: 'the traveller starts to book the room', then: 'the traveller sees similar rooms', result: 'result/create-hold.sold-out' })] },
  ['# Create Hold', '## Goal', 'The traveller holds a room while they pay.', '## What Happens', 'Lantern holds the room for the checkout hold window and refuses with a list of similar rooms when the room has sold out.'].join('\n\n')));

write('docs/behavior/modules/bookings/room-hold/complete-booking.md', page({ ...base('use-case', 'use-case/bookings.complete-booking', 'Complete Booking'),
  kind: 'command', serves: ['journey/book-a-room#s5'], fulfills: ['requirement/checkout.no-charge-without-room'],
  access: { grant: 'anonymous', tenant: 'own' }, reachedThrough: ['entry-point/webshop'],
  input: [
    { name: 'hold-id', meaning: 'The hold being paid for.', type: 'id', of: 'aggregate/bookings.room-hold', required: true, origin: 'caller', classification: 'internal' },
    { name: 'card-token', meaning: 'The token the payment provider charges.', type: 'text', required: true, origin: 'caller', classification: 'sensitive' }],
  output: [{ name: 'booking-id', meaning: 'The identifier of the confirmed booking.' }],
  changes: ['aggregate/bookings.room-hold'], raises: ['event/booking.completed'],
  effects: [{ emits: 'integration-event/booking-completed', timing: 'after-commit',
    onFailure: { mode: 'retry', retry: { maxAttempts: 5, initialDelay: 'PT1M', maxDelay: 'PT1H', backoff: 'exponential' }, then: 'dead-letter' } }],
  idempotency: { mode: 'key', key: '{hold-id}', scope: 'tenant' }, audit: { level: 'required', target: 'booking' }, risk: ['money'],
  records: [
    res('result/complete-booking.completed', 'Booking Completed', { outcome: 'success', meaning: 'A booking confirmed for the traveller.' }),
    res('result/complete-booking.hold-lapsed', 'Hold Lapsed', { outcome: 'refusal', code: 'bookings.hold-expired', meaning: 'A message telling the traveller the hold lapsed before payment.', recorded: true }),
    scn('scenario/complete-booking.pay-held-room', 'Pay a Held Room', { given: 'a room is held for the traveller', when: 'the traveller pays inside the hold window', then: 'the booking is confirmed', result: 'result/complete-booking.completed' }),
    scn('scenario/complete-booking.hold-lapsed', 'Hold Lapsed', { given: 'the hold window ended before payment', when: 'the traveller pays for the booking', then: 'the traveller is told the hold lapsed and is charged nothing', result: 'result/complete-booking.hold-lapsed' })] },
  ['# Complete Booking', '## Goal', 'The traveller pays for a held room and holds a confirmation.', '## What Happens', 'Lantern charges the shown total only while the hold stands, confirms the booking, and announces it to partners. A lapsed hold refuses without charging.'].join('\n\n')));

write('docs/behavior/modules/bookings/room-hold/expire-hold.md', page({ ...base('use-case', 'use-case/bookings.expire-hold', 'Expire Hold'),
  kind: 'command', serves: ['journey/book-a-room#s4'], fulfills: ['requirement/checkout.room-held-during-checkout'],
  access: { grant: 'system', tenant: 'own' },
  input: [{ name: 'hold-id', meaning: 'The hold that lapsed.', type: 'id', of: 'aggregate/bookings.room-hold', required: true, origin: 'caller', classification: 'internal' }],
  output: [{ name: 'hold-id', meaning: 'The identifier of the released hold.' }],
  changes: ['aggregate/bookings.room-hold'], raises: ['event/room-hold.released'],
  effects: [{ calls: 'external-system/partner-api#release-hold', timing: 'after-commit',
    onFailure: { mode: 'retry', retry: { maxAttempts: 5, initialDelay: 'PT1M', maxDelay: 'PT1H', backoff: 'exponential' }, then: 'dead-letter' } }],
  idempotency: { mode: 'key', key: '{hold-id}', scope: 'tenant' }, audit: { level: 'required', target: 'hold' },
  records: [
    res('result/expire-hold.released', 'Hold Released', { outcome: 'success', meaning: 'A lapsed hold returned to the hotel.' }),
    scn('scenario/expire-hold.release-lapsed', 'Release a Lapsed Hold', { given: 'a hold outlived its window', when: 'the expiry job runs', then: 'the hold returns to the hotel', result: 'result/expire-hold.released' })] },
  ['# Expire Hold', '## Goal', 'A lapsed hold returns to the hotel.', '## What Happens', 'Lantern releases the local hold and tells the partner system, which frees the room for other travellers. Only the expiry job runs this use case, so it names no entry point.'].join('\n\n')));

write('docs/behavior/modules/bookings/room-hold/search-rooms.md', page({ ...base('use-case', 'use-case/bookings.search-rooms', 'Search Rooms'),
  kind: 'query', serves: ['journey/book-a-room#s1', 'journey/book-a-room#s2'],
  access: { grant: 'anonymous', tenant: 'public' }, reachedThrough: ['entry-point/webshop'],
  input: [
    { name: 'city', meaning: 'The city the traveller visits.', type: 'text', required: true, origin: 'caller', classification: 'public' },
    { name: 'check-in', meaning: 'The date the stay starts.', type: 'date', required: true, origin: 'caller', classification: 'public' },
    { name: 'check-out', meaning: 'The date the stay ends.', type: 'date', required: true, origin: 'caller', classification: 'public' },
    { name: 'guests', meaning: 'The number of travellers staying.', type: 'integer', required: false, origin: 'caller', classification: 'public' }],
  output: { include: { rooms: { fields: [
      { name: 'room-id', meaning: 'The room on offer.' },
      { name: 'total', meaning: 'The amount the stay costs.' }],
    classification: 'public', reads: ['aggregate/bookings.room-hold'], freshness: { mode: 'bounded', within: 'PT5M' } } } },
  paging: { mode: 'pages', order: 'total asc', cursor: 'room-id', defaultLimit: 20, maxLimit: 100 },
  reads: ['aggregate/bookings.room-hold'], idempotency: { mode: 'none', reason: 'reads change nothing to repeat' },
  records: [
    res('result/search-rooms.found', 'Rooms Found', { outcome: 'success', meaning: 'A list of rooms with their totals.' }),
    scn('scenario/search-rooms.find-rooms', 'Find Rooms', { given: 'rooms are open in the city', when: 'the traveller searches by city and dates', then: 'matching rooms appear with their totals', result: 'result/search-rooms.found' })] },
  '# Search Rooms\n\n## Goal\n\nThe traveller sees rooms with the total each stay costs.'));

write('docs/behavior/modules/bookings/reactions/send-confirmation-on-booking-completed.md', page({ ...base('reaction', 'reaction/bookings.send-confirmation-on-booking-completed', 'Send Confirmation on Booking Completed'),
  triggeredBy: ['event/booking.completed'], effects: [{ sends: 'notification/bookings.booking-confirmation' }],
  serves: ['journey/book-a-room#s5'], reads: ['aggregate/bookings.booking'],
  onFailure: { mode: 'retry', retry: { maxAttempts: 5, initialDelay: 'PT1M', maxDelay: 'PT1H', backoff: 'exponential' }, then: 'dead-letter' },
  idempotency: { mode: 'key', key: '{booking-id}', scope: 'tenant' } },
  '# Send Confirmation on Booking Completed\n\n## Summary\n\nThe traveller receives a confirmation when the booking completes.'));

write('docs/behavior/modules/bookings/jobs/expire-lapsed-holds.md', page({ ...base('job', 'job/bookings.expire-lapsed-holds', 'Expire Lapsed Holds'),
  kind: 'scheduled', schedule: { every: 'PT1M' }, selection: 'holds past their window',
  effects: [{ runs: 'use-case/bookings.expire-hold', timing: 'async' }], fulfills: ['requirement/checkout.room-held-during-checkout'],
  onFailure: { mode: 'retry', retry: { maxAttempts: 5, initialDelay: 'PT1M', maxDelay: 'PT1H', backoff: 'exponential' }, then: 'dead-letter' },
  idempotency: { mode: 'state' } },
  '# Expire Lapsed Holds\n\n## Summary\n\nEach minute the job releases the holds whose window has passed.'));

write('docs/behavior/modules/bookings/notifications/booking-confirmation.md', page({ ...base('notification', 'notification/bookings.booking-confirmation', 'Booking Confirmation'),
  template: 'booking-confirmation-traveller', addresses: ['actor/traveller'], channel: ['email'],
  consent: 'Email goes only to the address the traveller gave.', frequency: 'once', dedupKey: '{booking-id}',
  params: ['booking-id', 'total'], locale: 'The booking language the traveller chose.',
  claims: [{ claim: 'The traveller pays only for a confirmed room.', requirement: 'requirement/checkout.no-charge-without-room' }],
  fulfills: ['requirement/checkout.no-charge-without-room'] },
  '# Booking Confirmation\n\n## Summary\n\nThe traveller receives the charged total and the booking details by email.'));

write('docs/behavior/integration-events/booking-completed.md', page({ ...base('integration-event', 'integration-event/booking-completed', 'Booking Completed'),
  direction: 'out', eventType: 'lantern.booking.completed', source: 'https://lantern.example/bookings', subject: 'The confirmed booking.',
  payload: [
    { name: 'booking-id', meaning: 'The booking the event announces.', type: 'id', of: 'aggregate/bookings.booking', required: true, classification: 'internal' },
    { name: 'total', meaning: 'The amount the traveller paid.', type: 'money', required: true, classification: 'internal' },
    { name: 'guest-email', meaning: 'The address confirmations reach.', type: 'email', required: true, classification: 'personal' }],
  addresses: 'permission/book-room', trust: 'payload', translates: 'event/booking.completed',
  dataschema: 'https://lantern.example/schemas/booking-completed.json' },
  '# Booking Completed\n\n## Summary\n\nPartners learn that a booking completed and for which total.'));

write('docs/behavior/access/entry-points.md', page({ type: 'entry-point-list', title: 'Entry Points', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01',
  records: [
    { type: 'entry-point', id: 'entry-point/webshop', title: 'Webshop', kind: 'browser', auth: ['session'] },
    { type: 'entry-point', id: 'entry-point/booking-api', title: 'Booking API', kind: 'api', auth: ['token'] }] },
  '# Entry Points'));

write('docs/behavior/screens/webshop.search.md', page({ ...base('screen', 'screen/webshop.search', 'Search'),
  entryPoint: 'entry-point/webshop', address: '/search',
  offers: ['use-case/bookings.search-rooms'], serves: ['journey/book-a-room#s1', 'journey/book-a-room#s2'] },
  '# Search\n\n## Summary\n\nThe traveller searches rooms by city and dates and compares their totals.'));
write('docs/behavior/screens/webshop.checkout.md', page({ ...base('screen', 'screen/webshop.checkout', 'Checkout'),
  entryPoint: 'entry-point/webshop', address: '/checkout',
  offers: ['use-case/bookings.create-hold', 'use-case/bookings.complete-booking'],
  serves: ['journey/book-a-room#s3', 'journey/book-a-room#s4', 'journey/book-a-room#s5'] },
  '# Checkout\n\n## Summary\n\nThe traveller holds a room and pays for the booking.'));

write('docs/behavior/modules/bookings/settings.md', page({ type: 'setting-set', title: 'Bookings Settings', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01',
  records: [
    { type: 'setting', id: 'setting/bookings.service-fee', title: 'Service Fee', valueType: 'money', default: { amount: 2.5, currency: 'EUR' }, setBy: 'decider', fulfills: ['requirement/checkout.service-fee'] },
    { type: 'setting', id: 'setting/bookings.free-cancellation-window', title: 'Free Cancellation Window', valueType: 'duration', default: 'PT24H', range: 'PT0H..PT72H', unit: 'hours', setBy: 'tenant', fulfills: ['requirement/checkout.free-cancellation-window'] },
    { type: 'setting', id: 'setting/bookings.checkout-hold-window', title: 'Checkout Hold Window', valueType: 'duration', default: 'PT10M', unit: 'minutes', setBy: 'decider', fulfills: ['requirement/checkout.room-held-during-checkout'] }] },
  '# Bookings Settings'));

write('docs/behavior/retention-schedule.md', page({ type: 'retention-schedule', title: 'Retention Schedule', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01',
  rows: [
    { dataClass: 'guest-contact', covers: ['aggregate/bookings.room-hold'], basis: 'business', purpose: 'confirmations and receipts reach the traveller', period: 'P2Y', enforcedBy: 'job/bookings.expire-lapsed-holds',
      cites: 'requirement/checkout.no-charge-without-room',
      processors: [{ system: 'external-system/partner-api', forwarding: { mode: 'call', operation: 'external-system/partner-api#erase-guest-data' }, acceptedBy: 'decision/0001-room-hold-length' }],
      conflicts: 'The tax file keeps the total, and the booking log records the erasure.' },
    { dataClass: 'hold-detail', covers: ['aggregate/bookings.room-hold'], basis: 'engineering', purpose: 'lapsed holds are diagnosed after release', period: { mode: 'none', reason: 'holds stay until the guest checks out or the hold lapses' }, enforcedBy: 'job/bookings.expire-lapsed-holds' }] },
  '# Retention Schedule\n\n<!-- generated:retention-schedule -->\n<!-- /generated:retention-schedule -->'));

write('docs/governance/exceptions/flash-sale-evidence-later.md', page({ ...base('exception', 'exception/flash-sale-evidence-later', 'Flash Sale Evidence Later'),
  kind: 'gap', covers: ['requirement-measure-missing', 'quality-alert-missing'], subject: ['requirement/checkout.checkout-under-load'],
  reason: 'the flash-sale window is booked for January', expiresOn: '2027-02-01' },
  '# Flash Sale Evidence Later\n\n## Exit\n\nRun the January flash-sale test and record the passing run as evidence.'));

write('docs/behavior/external-systems/partner-api.md', page({ ...base('external-system', 'external-system/partner-api', 'Partner API'),
  actor: 'actor/hotel-partner',
  operations: [
    { name: 'release-hold', direction: 'out', timeout: 'PT30S', onTimeout: 'The job retries the release with the same hold identifier.', response: 'The partner confirms the hold ended.', cache: 'No response is cached.' },
    { name: 'erase-guest-data', direction: 'out', timeout: 'PT30S', onTimeout: 'The erasure run retries with the same guest identifier.', response: 'The partner confirms the erasure.', cache: 'No response is cached.' }],
  erasure: { mode: 'call', operation: 'external-system/partner-api#erase-guest-data' }, acceptedBy: 'decision/0001-room-hold-length',
  failure: 'Travellers see cached room lists while the partner system stays down.' },
  '# Partner API\n\n## Summary\n\nThe hotel partner system that holds rooms and erases guest data for Lantern.'));

write('docs/behavior/runbooks/complete-checkout.md', page({ ...base('runbook', 'runbook/complete-checkout', 'Complete Checkout'),
  trigger: 'The traveller starts checkout.' },
  ['# Complete Checkout', '## Impact', 'Checkout stops while this runbook runs: travellers wait with an open hold.', '## Prerequisites',
   '- The hold identifier from the traveller.\n- Access to the booking log.\n- The support channel open.', '## Procedure',
   '1. Confirm the hold exists and the window is open.\n2. Charge the card token for the shown total.\n3. Send the confirmation and record the booking.',
   '```text\ncharge --hold <id> --token <token>\n```', '## Verification', 'The booking appears in the log with the charged total.', '## Rollback or Recovery',
   'Void the charge and release the hold when the payment fails after the hold lapses.', '## Escalation', 'Hand the hold identifier to the on-call engineer and stop when the window ends.'].join('\n\n')));

write('docs/behavior/access/roles/traveller.md', page({ ...base('role', 'role/traveller', 'Traveller'),
  scope: 'self', allows: ['permission/book-room'] },
  '# Traveller\n\n## Summary\n\nA person booking a stay for themselves or others; Lantern grants it at signup.'));

write('docs/behavior/access/permissions.md', page({ type: 'permission-list', title: 'Permissions', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01',
  records: [{ type: 'permission', id: 'permission/book-room', title: 'Book Room', meaning: 'The right to hold and pay for a room.', scope: 'self' }] },
  '# Permissions'));

console.log('written', root);
