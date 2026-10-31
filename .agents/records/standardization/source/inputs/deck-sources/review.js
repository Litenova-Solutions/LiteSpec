// node review.js <findings.js> <layer> <title> <perSlide> > out.html
const fs = require('fs');
const [,, file, layer, label, perArg] = process.argv;
const per = parseInt(perArg || '6', 10);
const data = require(require('path').resolve(file)).map((f, i) => ({ n: i + 1, sev: f[0], cat: f[1], title: f[2], ev: f[3], fix: f[4] }));
const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const ticks = s => esc(s)
  .replace(/([A-Za-z0-9_\/.\-]+\.(?:md|cs|json)(?::[0-9]+(?:-[0-9]+)?(?:, ?:?[0-9]+(?:-[0-9]+)?)*)?)/g, '<code>$1</code>')
  .replace(/(^|[\s(])((?:topic|requirement|decision|journey|goal|problem|feature|constraint|research|product|module|use-case|permission|setting|reaction|job|actor|rule|aggregate|integration-event|event|result|scenario|notification|screen|role|entry-point|external-system|runbook|term|exception|known-gap|deviation)\/[A-Za-z0-9.\-#&;<>]+)/g, '$1<code>$2</code>');
const sevBadge = { high: 'red', medium: 'amber', low: 'blue' };
const count = (k, v) => data.filter(f => f[k] === v).length;
const cats = [...new Set(data.map(f => f.cat))];
let out = '';
out += `
<section class="slide section-title" data-layer="review" data-section="Review">
  <div class="num">REVIEW</div>
  <h1>Review of the ${label}</h1>
  <p class="lede">${data.length} findings still open after the 3.0.0 release, from re-reading the pages, the schema, the templates and the checker side by side. Everything the release fixed is listed in the closing slides, not here.</p>
</section>

<section class="slide" data-layer="review">
  <div class="eyebrow">Review</div>
  <h2>Findings at a Glance</h2>
  <p class="sub">Every finding cites file and line in the 3.0.0 release tree on 31 October 2026. Each was rechecked against the page, schema, template or code it names before it was included.</p>
  <div class="content">
  <div class="row r4">
    <div class="card stat" style="--accent:var(--foreground)"><div class="n">${data.length}</div><div class="l">findings in total</div></div>
    <div class="card stat red" style="--accent:var(--red)"><div class="n">${count('sev', 'high')}</div><div class="l">high: a rule that cannot hold as written, or a check that blocks the wrong thing</div></div>
    <div class="card stat amber" style="--accent:var(--amber)"><div class="n">${count('sev', 'medium')}</div><div class="l">medium: drift between page, schema, template and code</div></div>
    <div class="card stat blue" style="--accent:var(--blue)"><div class="n">${count('sev', 'low')}</div><div class="l">low: wording, examples and small mismatches</div></div>
  </div>
  <div class="row r2">
    <div class="card"><p class="t">By category</p><table class="tight" style="margin-top:6px"><tbody>${cats.map(c => `<tr><td>${c}</td><td class="c">${count('cat', c)}</td><td class="m">${data.filter(f => f.cat === c && f.sev === 'high').length} high</td></tr>`).join('')}</tbody></table></div>
    <div class="card"><p class="t">Fix first</p>${data.some(f => f.sev === 'high') ? `<ol class="list">${data.filter(f => f.sev === 'high').slice(0, 6).map(f => `<li><b>${esc(f.title)}</b></li>`).join('')}</ol>` : `<p class="d">No high finding remains. The lists below hold what is left: mediums, then lows.</p>`}</div>
  </div>
  </div>
</section>
`;
for (let i = 0; i < data.length; i += per) {
  const chunk = data.slice(i, i + per);
  const page = Math.floor(i / per) + 1, pages = Math.ceil(data.length / per);
  out += `
<section class="slide" data-layer="review">
  <div class="eyebrow">Review &middot; findings ${chunk[0].n} to ${chunk[chunk.length - 1].n}</div>
  <h2>Findings, ${page} of ${pages}</h2>
  <div class="content top" style="margin-top:8px">
  <div class="findings">
${chunk.map(f => `    <div class="f ${f.sev}"><div class="no">${String(f.n).padStart(2, '0')}</div><div><div class="hd">${esc(f.title)} <span class="badge ${sevBadge[f.sev]}">${f.sev}</span><span class="badge outline">${f.cat}</span></div><div class="ev">${ticks(f.ev)}</div><div class="fx">${ticks(f.fix)}</div></div></div>`).join('\n')}
  </div>
  </div>
</section>
`;
}
process.stdout.write(out);
