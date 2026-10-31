// node provgrid.js <docs/core dir> > provgrid.html
// Reads the five Behavior pages (behavior.md holds only the overview) and
// groups every provision under ## Standards by its area. Output contract is
// unchanged: <!--A--> first half, <!--B--> second half, <!--STATS ...-->.
const fs = require('fs');
const dir = process.argv[2].replace(/\/$/, '');
const files = [
  ['domain-model.md', [
    ['Glossary and modules', 3], ['Aggregates and lifecycles', 5], ['Sagas', 3],
    ['Rules', 10], ['Events', 3]
  ]],
  ['use-cases.md', [
    ['Use case shape', 6], ['Access and inputs', 4], ['Consistency and effects', 8],
    ['Queries', 4], ['Results and refusals', 7], ['Scenarios', 6], ['Settings', 5]
  ]],
  ['automation.md', [
    ['Reactions and jobs', 5], ['Names, failure, runbooks', 7]
  ]],
  ['edges.md', [
    ['Integration events', 4], ['Notifications', 5], ['Entry points and screens', 7],
    ['Served content and external systems', 2]
  ]],
  ['privacy.md', [
    ['Data classes and retention', 6]
  ]]
];
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const cards = [];
let total = 0, checked = 0, reviewed = 0;
for (const [file, groups] of files) {
  const text = fs.readFileSync(dir + '/' + file, 'utf8').split(/\r?\n/);
  let inStd = false, cur = null; const out = [];
  text.forEach(l => {
    if (/^## /.test(l)) inStd = /^## Standards/.test(l);
    const h = l.match(/^### (.+)/);
    if (h && inStd) { cur = { title: h[1], how: '', codes: [] }; out.push(cur); }
    if (cur && /^\*\*Checked by:\*\*/.test(l)) { cur.how = 'checked'; cur.codes = (l.match(/`([a-z0-9-]+)`/g) || []).map(s => s.replace(/`/g, '')).filter(c => /^[a-z]+(-[a-z]+)+$/.test(c) && !c.startsWith('litespec')); }
    if (cur && /^\*\*Reviewed for:\*\*/.test(l)) cur.how = 'reviewed';
  });
  const want = groups.reduce((n, g) => n + g[1], 0);
  if (out.length !== want) console.error('warning: ' + file + ' holds ' + out.length + ' provisions, groups expect ' + want);
  let at = 0;
  for (const [name, n] of groups) {
    const ps = out.slice(at, at + n); at += n;
    const items = ps.map(p => '<li class="' + (p.how === 'checked' ? 'ck' : 'rv') + '">' + esc(p.title) + (p.codes.length ? ' <span class="cd">' + p.codes.join(', ') + '</span>' : '') + '</li>').join('');
    cards.push('<div class="pgrp"><p class="t">' + name + ' <span class="badge outline">' + ps.length + '</span></p><ul>' + items + '</ul></div>');
  }
  total += out.length;
  checked += out.filter(p => p.how === 'checked').length;
  reviewed += out.filter(p => p.how === 'reviewed').length;
}
console.log('<!--A-->' + cards.slice(0, 7).join('\n'));
console.log('<!--B-->' + cards.slice(7, 13).join('\n'));
console.log('<!--C-->' + cards.slice(13).join('\n'));
console.log('<!--STATS ' + total + ' checked=' + checked + ' reviewed=' + reviewed + '-->');
