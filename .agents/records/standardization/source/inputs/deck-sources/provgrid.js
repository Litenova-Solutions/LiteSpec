const fs = require('fs');
const text = fs.readFileSync(process.argv[2], 'utf8').split(/\r?\n/);
let inStd = false, cur = null; const out = [];
text.forEach((l, i) => {
  if (/^## /.test(l)) inStd = /^## (Standards|Conventions)/.test(l);
  const h = l.match(/^### (.+)/);
  if (h && inStd) { cur = { title: h[1], line: i + 1, how: '', codes: [] }; out.push(cur); }
  if (cur && /^\*\*Checked by:\*\*/.test(l)) { cur.how = 'checked'; cur.codes = (l.match(/`([a-z0-9-]+)`/g) || []).map(s => s.replace(/`/g, '')).filter(c => /^[a-z]+(-[a-z]+)+$/.test(c) && !c.startsWith('litespec')); }
  if (cur && /^\*\*Reviewed for:\*\*/.test(l)) cur.how = 'reviewed';
});
const groups = [
  ['Glossary and modules', 333, 366], ['Aggregates and data', 367, 452], ['Processes', 453, 486], ['Rules', 487, 570],
  ['Events', 571, 634], ['Use case shape', 635, 758], ['Consistency, effects, audit', 759, 832], ['Queries', 833, 874],
  ['Results and scenarios', 875, 998], ['Reactions', 999, 1072], ['Jobs', 1073, 1106], ['Notifications', 1107, 1162],
  ['Settings', 1163, 1232], ['Entry points and screens', 1233, 1312], ['External systems and retention', 1313, 1342], ['Records and pages', 1343, 1400]
];
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;');
const cards = groups.map(([name, a, b]) => {
  const ps = out.filter(p => p.line >= a && p.line <= b);
  const items = ps.map(p => '<li class="' + (p.how === 'checked' ? 'ck' : 'rv') + '">' + esc(p.title) + (p.codes.length ? ' <span class="cd">' + p.codes.join(', ') + '</span>' : '') + '</li>').join('');
  return { html: '<div class="pgrp"><p class="t">' + name + ' <span class="badge outline">' + ps.length + '</span></p><ul>' + items + '</ul></div>', n: ps.length };
});
const half = 8;
console.log('<!--A-->' + cards.slice(0, half).map(c => c.html).join('\n'));
console.log('<!--B-->' + cards.slice(half).map(c => c.html).join('\n'));
console.log('<!--STATS ' + out.length + ' checked=' + out.filter(p => p.how === 'checked').length + ' reviewed=' + out.filter(p => p.how === 'reviewed').length + '-->');
