// Writes the Lantern example consumer project used by the Intent deck.
const fs = require('fs'), path = require('path');
const root = path.join(__dirname, 'lantern');
const write = (rel, text) => { const p = path.join(root, rel); fs.mkdirSync(path.dirname(p), { recursive: true }); fs.writeFileSync(p, text); };
const page = (fm, body) => '---\n' + JSON.stringify(fm, null, 2) + '\n---\n' + body.trim() + '\n';
const base = (type, id, title) => ({ type, id, title, owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' });

const project = JSON.parse(fs.readFileSync('D:/Personal-Projects/LiteSpec/templates/consumer/litespec.project.json', 'utf8').replace(/__PROJECT__/g, 'lantern').replace(/__OWNER__/g, 'founder'));
project.size = 'large';
project.owners = ['founder', 'product-team'];
// The example holds Intent records only, so it declares no frontend and no UI package.
project.stacks = ['dotnet'];
delete project.paths.uiPackage; delete project.paths.uiPatterns; project.paths.frontends = [];
write('litespec.project.json', JSON.stringify(project, null, 2) + '\n');

write('docs/intent/brief.md', page({ ...base('product', 'product/lantern', 'Lantern'),
  boundary: 'Travellers find hotel rooms, book them and manage their bookings.',
  neverDoes: ['Lantern never sells flights or other transport.', 'Lantern never stores a traveller\'s card details.'],
  audience: ['actor/traveller', 'actor/hotel-partner'] },
  '# Lantern\n\n## Summary\n\nLantern lets travellers book a hotel room at the price they were shown, and lets hotels fill rooms without a call centre.'));

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
  authority: 'EU consumer law on price indication (illustrative citation)', kind: 'organizational', applies: true,
  appliesBecause: 'Lantern sells to consumers in the EU.' },
  '# Total Price Display\n\n## Summary\n\nA price shown to a consumer includes every unavoidable tax and fee.\n\n## What It Requires\n\nThe total shown in search is the total charged.'));
write('docs/intent/constraints/package-travel-rules.md', page({ ...base('constraint', 'constraint/package-travel-rules', 'Package Travel Rules'),
  authority: 'EU package travel rules (illustrative citation)', kind: 'organizational', applies: false,
  appliesBecause: 'Lantern sells hotel stays only and never combines them with transport.' },
  '# Package Travel Rules\n\n## Summary\n\nA seller who combines two travel services, such as a flight and a hotel, carries the duties of a package organiser.\n\n## What It Requires\n\nNone.'));

write('docs/intent/research/partner-hold-limits.md', page({ ...base('research', 'research/partner-hold-limits', 'Partner Hold Limits'),
  question: 'How long will hotel partners hold a room without payment?',
  conclusion: 'Partner systems hold a room for at most 15 minutes without payment, so checkout must finish inside that window.',
  sources: [{ url: 'https://example.com/partner-api/holds', read: '2026-10-12' }] },
  '# Partner Hold Limits\n\n## Answer\n\nAll three pilot partners release an unpaid hold after 15 minutes; one releases it after 12. High confidence for the pilot partners, unknown for others.'));

write('docs/intent/decisions/0001-room-hold-length.md', page({ ...base('decision', 'decision/0001-room-hold-length', 'Room Hold Length'),
  options: [{ name: '10 minutes', for: 'Leaves a buffer before the shortest partner limit.', against: 'Slow payers may lose the room.' },
            { name: '15 minutes', for: 'Most time for the traveller.', against: 'Equals the partner limit, so a slow partner call loses the room after payment.' }],
  changeWhen: 'A partner holds rooms longer than 15 minutes, or more than 5 percent of checkouts end because the hold ran out.',
  decides: ['requirement/booking.room-held-during-checkout'], basedOn: ['research/partner-hold-limits'] },
  '# Room Hold Length\n\n## Context\n\nPartners release unpaid holds after 12 to 15 minutes.\n\n## Decision\n\nHold a room for 10 minutes during checkout.\n\n## Consequences\n\nTravellers who take longer than 10 minutes start again.'));

const req = (id, title, extra) => ({ type: 'requirement', id, title, ...extra });
write('docs/intent/booking/checkout.md', page({ type: 'requirement-page', title: 'Checkout', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01', reviewBy: '2027-06-01',
  records: [
    req('requirement/booking.room-held-during-checkout', 'Room Held During Checkout', { kind: 'functional', statement: 'A room the traveller starts to book is held for them for 10 minutes.', rationale: 'The room must still exist when the payment succeeds.', features: ['feature/room-hold'],
      except: [{ when: 'The hotel partner refuses to hold the room.', fallback: 'requirement/booking.no-charge-without-room' }] }),
    req('requirement/booking.no-charge-without-room', 'No Charge Without a Room', { kind: 'functional', statement: 'A traveller is never charged for a room that was not confirmed.', rationale: 'Charging for nothing is the worst outcome a booking can have.', features: ['feature/room-hold'], risk: ['money'] }),
    req('requirement/booking.shown-price-is-charged', 'Shown Price Is Charged', { kind: 'functional', statement: 'The total shown in search is the total charged.', rationale: 'Consumer law and traveller trust both demand it.', features: ['feature/all-in-pricing'], basedOn: ['constraint/total-price-display'], risk: ['money'] }),
    req('requirement/booking.checkout-under-load', 'Checkout Under Load', { kind: 'quality', statement: 'During a flash sale, 95 percent of checkouts are answered within 3 seconds.', rationale: 'Flash sales are when most bookings happen.', features: ['feature/room-hold'],
      stimulus: '2,000 travellers check out in the same minute during a flash sale', environment: 'Normal operation, one partner system answering slowly', response: 'Each booking is confirmed or refused', measure: 'Share of checkouts answered within 3 seconds: at least 95 percent', dueBy: 'Summer season 2027' }),
    req('requirement/booking.service-fee', 'Service Fee', { kind: 'business', statement: 'Lantern charges a fixed service fee per booking.', rationale: 'The fee pays for support and payment costs.', features: ['feature/all-in-pricing'], values: [{ value: 'EUR 2.50', effectiveFrom: '2027-01-01' }], setBy: 'founder', risk: ['money'] }),
    req('requirement/booking.free-cancellation-window', 'Free Cancellation Window', { kind: 'business', statement: 'Each hotel sets how long before arrival a booking can be cancelled free, between 0 and 72 hours.', rationale: 'Hotels carry the cost of a late cancellation.', features: ['feature/room-hold'], values: [{ value: '24 hours by default, 0 to 72 hours', effectiveFrom: '2027-01-01' }], setBy: 'tenant' })
  ] },
  ['# Checkout', '## Summary', 'What Lantern promises from the moment a traveller picks a room until the booking is confirmed.', '## The Situation', 'Travellers abandon checkouts when a fee appears or the room vanishes.', '## What We Do', 'Hold the room, show the total, and never charge without a room.', '## Why It Is Better', 'The traveller sees one price and gets the room they paid for.', '## Example', 'Ana books a double room in Lisbon for 3 nights at a total of EUR 384.50.', '## Costs and Risks', 'Holds tie up partner inventory for up to 10 minutes.', '## How Sure We Are', 'Partner hold limits are measured for the pilot partners only.', '## Position', 'Hold length follows decision/0001-room-hold-length.', '## Refusals', 'No pay-at-hotel option.'].join('\n\n')));

write('docs/intent/journeys/book-a-room.md', page({ ...base('journey', 'journey/book-a-room', 'Book a Room'),
  actor: 'actor/traveller', serves: ['goal/more-completed-bookings'], aim: 'A traveller finds a room and books it.',
  finishedWhen: 'The traveller holds a confirmation and the hotel has the booking.',
  fulfills: ['requirement/booking.room-held-during-checkout', 'requirement/booking.shown-price-is-charged', 'requirement/booking.no-charge-without-room'],
  steps: [
    { id: 's1', says: 'The traveller searches for rooms by city and dates.', actor: 'actor/traveller' },
    { id: 's2', says: 'The traveller compares rooms by their total price for the stay.', actor: 'actor/traveller' },
    { id: 's3', says: 'The traveller chooses a room and starts to book it.', actor: 'actor/traveller', ifItGoesWrong: 'If the room has sold out, the traveller is told before paying and sees similar rooms.' },
    { id: 's4', says: 'The traveller pays for the booking.', actor: 'actor/traveller', ifItGoesWrong: 'If the payment fails, the room stays held until the 10 minutes end.' },
    { id: 's5', says: 'The traveller receives the confirmation, and the hotel receives the booking.', actor: 'actor/traveller' }],
  questions: [{ question: 'How long should a room stay held during checkout?', decider: 'founder', decideBy: '2026-12-01', recommended: '10 minutes', ballot: 'checkout-terms', tier: 3 },
              { question: 'What service fee does Lantern charge per booking?', decider: 'founder', decideBy: '2026-12-01', recommended: 'EUR 2.50', ballot: 'checkout-terms', tier: 1 }] },
  '# Book a Room\n\n## Example\n\nAna searches Lisbon for 12 to 15 May 2027, picks a double room at EUR 384.50 in total, and pays by card.\n\n## Between the Steps\n\nA traveller who leaves at step 4 and returns after 10 minutes starts again at step 3.'));

write('docs/glossary.json', JSON.stringify({ schemaVersion: 2, mannered: [], terms: [
  { id: 'term/booking', term: 'booking', meaning: 'A confirmed reservation of one room for one stay.', layer: 'intent', aliases: ['reservation'], rejected: [{ word: 'order', reason: 'A booking is a reservation of a room, not a purchase of goods.' }], owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/hold', term: 'hold', meaning: 'A room kept for one traveller while they pay.', layer: 'intent', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' },
  { id: 'term/stay', term: 'stay', meaning: 'The nights between arrival and departure at one hotel.', layer: 'intent', owner: 'founder', status: 'draft', decider: 'founder', decideBy: '2026-12-01' }] }, null, 2) + '\n');
console.log('written', root);
