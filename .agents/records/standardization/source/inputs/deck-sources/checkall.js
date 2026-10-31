async page => {
  await page.setViewportSize({ width: 1320, height: 760 });
  const files = ['1-intent-layer.html', '2-behavior-layer.html', '3-promise-to-proof.html'];
  const out = [];
  for (const f of files) {
    await page.goto('about:blank');
    await page.goto('http://localhost:8791/' + f);
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(800);
    const r = await page.evaluate(() => {
      const res = []; const all = Array.from(document.querySelectorAll('.slide'));
      all.forEach((s, n) => {
        all.forEach(x => x.classList.remove('active')); s.classList.add('active');
        const tr = s.style.transform; s.style.transform = 'none';
        const b = s.getBoundingClientRect();
        s.querySelectorAll('.content > *, .card, table, pre, svg, h2, .sub, .f, .ran, .story, dl').forEach(el => { const e = el.getBoundingClientRect(); if (e.bottom > b.bottom - 29 || e.right > b.right - 19) res.push((n + 1) + ':' + Math.round(Math.max(e.bottom - b.bottom + 29, e.right - b.right + 19))); });
        s.querySelectorAll('pre').forEach(p => { const o = p.scrollWidth - p.clientWidth; if (o > 1) res.push((n + 1) + ':pre+' + o); });
        s.style.transform = tr;
      });
      return all.length + ' slides, overflow on: ' + ([...new Set(res)].join(' ') || 'none');
    });
    out.push(f + ': ' + r);
  }
  return out.join('\n');
}
